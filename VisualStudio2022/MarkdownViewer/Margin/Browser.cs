using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Markdig.Renderers;
using Markdig.Syntax;
using Microsoft.VisualStudio.PlatformUI;
using mshtml;
using VisualStudio2022.MarkdownViewer.Options;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using WebBrowser = System.Windows.Controls.WebBrowser;

namespace VisualStudio2022.MarkdownViewer.Margin
{
    public class Browser : IDisposable
    {
        private readonly string _file;

        private MDocument _currentmDoc;
        private HTMLDocument _htmlDocument;

        [ThreadStatic]
        private static StringWriter _htmlWriterStatic;

        public Browser(string file, MDocument mdocument)
        {
            _file = file;
            _currentmDoc = mdocument;
         

            _browser.LoadCompleted += BrowserLoadCompleted;
            _browser.Navigating += BrowserNavigating;

            _browser.SetResourceReference(Control.BackgroundProperty, VsBrushes.ToolWindowBackgroundKey);
            
        }

        public readonly WebBrowser _browser = new() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch, Margin = new Thickness(0), Visibility = Visibility.Hidden };

        private void BrowserNavigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (e.Uri == null)
            {
                return;
            }

            e.Cancel = true;

            // If it's a file-based anchor we converted, open the related file if possible
            if (e.Uri.Scheme == "about")
            {
                string file = Uri.UnescapeDataString(e.Uri.LocalPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (file == "blank")
                {
                    string fragment = e.Uri.Fragment?.TrimStart('#');
                    NavigateToFragment(fragment);
                    return;
                }

                if (!File.Exists(file))
                {
                    string ext = null;

                    // If the file has no extension, see if one exists with a markdown extension.  If so,
                    // treat it as the file to open.
                    //if (string.IsNullOrEmpty(Path.GetExtension(file)))
                    //{
                    //    ext = LanguageFactory. ContentTypeDefinition.MarkdownExtensions.FirstOrDefault(fx => File.Exists(file + fx));
                    //}

                    if (ext != null)
                    {
                        VS.Documents.OpenInPreviewTabAsync(file + ext).FireAndForget();
                    }
                }
                else
                {
                    VS.Documents.OpenInPreviewTabAsync(file).FireAndForget();
                }
            }
            else if (e.Uri.IsAbsoluteUri && e.Uri.Scheme.StartsWith("http"))
            {
                Process.Start(e.Uri.ToString());
            }
        }

        private void BrowserLoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            _browser.Visibility = Visibility.Visible;
            _htmlDocument = (HTMLDocument)_browser.Document;

            //_cachedHeight = _htmlDocument.body.offsetHeight;
            //_htmlDocument.documentElement.setAttribute("scrollTop", _positionPercentage * _cachedHeight / 100);

            AdjustAnchors();
        }

        private void NavigateToFragment(string fragmentId)
        {
            IHTMLElement element = _htmlDocument.getElementById(fragmentId);
            element?.scrollIntoView(true);
        }

        /// <summary>
        /// Adjust the file-based anchors so that they are navigable on the local file system
        /// </summary>
        /// <remarks>Anchors using the "file:" protocol appear to be blocked by security settings and won't work.
        /// If we convert them to use the "about:" protocol so that we recognize them, we can open the file in
        /// the <c>Navigating</c> event handler.</remarks>
        private void AdjustAnchors()
        {
            try
            {
                foreach (IHTMLElement link in _htmlDocument.links)
                {
                    if (link is HTMLAnchorElement anchor && anchor.protocol == "file:")
                    {
                        string pathName = null, hash = anchor.hash;

                        // Anchors with a hash cause a crash if you try to set the protocol without clearing the
                        // hash and path name first.
                        if (hash != null)
                        {
                            pathName = anchor.pathname;
                            anchor.hash = null;
                            anchor.pathname = string.Empty;
                        }

                        anchor.protocol = "about:";

                        if (hash != null)
                        {
                            // For an in-page section link, use "blank" as the path name.  These don't work
                            // anyway but this is the proper way to handle them.
                            if (pathName == null || pathName.EndsWith("/"))
                            {
                                pathName = "blank";
                            }

                            anchor.pathname = pathName;
                            anchor.hash = hash;
                        }
                    }
                }
            }
            catch
            {
                // Ignore exceptions
            }
        }

        public Task RefreshAsync()
        {
            _htmlDocument = null;
            return UpdateBrowserAsync(_currentmDoc);
        }

        public Task UpdateBrowserAsync(MDocument mdoc)
        {
            return ThreadHelper.JoinableTaskFactory.StartOnIdle(() =>
            {
                _currentmDoc = mdoc;
                // Generate the HTML document
                string html = null;
                StringWriter htmlWriter = null;
                try
                {
                    htmlWriter = (_htmlWriterStatic ??= new StringWriter());
                    htmlWriter.GetStringBuilder().Clear();

                    HtmlRenderer htmlRenderer = new(htmlWriter);
                    MDocument.Pipeline.Setup(htmlRenderer);
                    htmlRenderer.UseNonAsciiNoEscape = true;
                    htmlRenderer.Render(mdoc.Markdown);

                    htmlWriter.Flush();
                    html = htmlWriter.ToString();
                    html = Regex.Replace(html, "\"language-(c|C)#\"", "\"language-csharp\"", RegexOptions.Compiled);
                }
                catch (Exception ex)
                {
                    // We could output this to the exception pane of VS?
                    // Though, it's easier to output it directly to the browser
                    html = "<p>An unexpected exception occurred:</p><pre>" +
                           ex.ToString().Replace("<", "&lt;").Replace("&", "&amp;") + "</pre>";
                }
                finally
                {
                    // Free any resources allocated by HtmlWriter
                    htmlWriter?.GetStringBuilder().Clear();
                }

                IHTMLElement content = null;

                if (_htmlDocument != null)
                {
                    content = _htmlDocument.getElementById("___markdown-content___");
                }

                // Content may be null if the Refresh context menu option is used.  If so, reload the template.
                if (content != null)
                {
                    content.innerHTML = html;

                    // Makes sure that any code blocks get syntax highlighted by Prism
                    IHTMLWindow2 win = _htmlDocument.parentWindow;
                    try { win.execScript("Prism.highlightAll();", "javascript"); } catch { }

                    // Adjust the anchors after and edit
                    AdjustAnchors();
                }
                else
                {
                    string htmlTemplate = GetHtmlTemplate();
                    html = string.Format(CultureInfo.InvariantCulture, "{0}", html);
                    html = htmlTemplate.Replace("[content]", html);
                    _browser.NavigateToString(html);
                }

            }, VsTaskRunContext.UIThreadIdlePriority).Task;
        }

        public static string GetFolder()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            return Path.GetDirectoryName(assembly);
        }

        private static string GetHtmlTemplateFileNameFromResource()
        {
            string assembly = Assembly.GetExecutingAssembly().Location;
            string assemblyDir = Path.GetDirectoryName(assembly);

            return Path.Combine(assemblyDir, "MarkdownViewer\\Margin\\md-template.html");
        }

        private string GetHtmlTemplate()
        {
            string baseHref = Path.GetDirectoryName(_file).Replace("\\", "/");
            string folder = GetFolder();
            string cssHighlight = File.ReadAllText(Path.Combine(folder, "MarkdownViewer\\Margin\\highlight.css"));
            string scriptPrismPath = Path.Combine(folder, "MarkdownViewer\\Margin\\prism.js");
            string cssPrism = File.ReadAllText(Path.Combine(folder, "MarkdownViewer\\Margin\\prism.css"));

            if (AdvancedOptions.Instance.EnableDarkTheme)
            {
                SolidColorBrush brush = (SolidColorBrush)Application.Current.Resources[VsBrushes.ToolWindowBackgroundKey];
                ContrastComparisonResult contrast = ColorUtilities.CompareContrastWithBlackAndWhite(brush.Color);

                if (contrast == ContrastComparisonResult.ContrastHigherWithWhite)
                {
                    cssHighlight = File.ReadAllText(Path.Combine(folder, "MarkdownViewer\\Margin\\highlight-dark.css"));
                    cssPrism = File.ReadAllText(Path.Combine(folder, "MarkdownViewer\\Margin\\prism-dark.css"));
                }
            }

            string defaultHeadBeg = $@"
<head>
    <meta http-equiv=""X-UA-Compatible"" content=""IE=Edge"" />
    <meta charset=""utf-8"" />
    <base href=""file:///{baseHref}/"" />
    <style>
        html, body {{margin: 0; padding-bottom:10px}}
        {cssHighlight}
        {cssPrism}
    </style>";

            string defaultContent = $@"
    <div id=""___markdown-content___"" class=""markdown-body"">
        [content]
    </div>
    <script async src=""{scriptPrismPath}""></script>";

            string templateFileName = GetHtmlTemplateFileNameFromResource();
            string template = File.ReadAllText(templateFileName);
            return template
                .Replace("<head>", defaultHeadBeg)
                .Replace("[content]", defaultContent)
                .Replace("[title]", "Markdown Preview");
        }

        public void Dispose()
        {
            if (_browser != null)
            {
                _browser.LoadCompleted -= BrowserLoadCompleted;
                _browser.Navigating -= BrowserNavigating;
                _browser.Dispose();
            }

            _htmlDocument = null;
        }
    }
}