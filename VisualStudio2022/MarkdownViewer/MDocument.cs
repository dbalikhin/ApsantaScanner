using System.Threading.Tasks;
using Markdig;
using Markdig.Syntax;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Threading;
using VisualStudio2022.MarkdownViewer.Options;

namespace VisualStudio2022.MarkdownViewer
{
    public class MDocument : IDisposable
    {
        private readonly string _text;
        private bool _isDisposed;

        public static MarkdownPipeline Pipeline { get; } = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UsePragmaLines()
            .UsePreciseSourceLocation()
            .UseYamlFrontMatter()

            .UseEmojiAndSmiley()
            .Build();

        public MDocument(string text)
        {
            _text = text;

            ParseAsync().FireAndForget();
            AdvancedOptions.Saved += AdvancedOptionsSaved;
        }

        public MarkdownDocument Markdown { get; private set; }

        public string FileName { get; }

        public bool IsParsing { get; private set; }

        private void BufferChanged(object sender, TextContentChangedEventArgs e)
        {
            ParseAsync().FireAndForget();
        }

        private async Task ParseAsync()
        {
            IsParsing = true;
            bool success = false;

            try
            {
                await TaskScheduler.Default; // move to a background thread
                Markdown = Markdig.Markdown.Parse(_text, Pipeline);
                success = true;
            }
            catch (Exception ex)
            {
                await ex.LogAsync();
            }
            finally
            {
                IsParsing = false;

                if (success)
                {
                    Parsed?.Invoke(this);
                }
            }
        }

        private void AdvancedOptionsSaved(AdvancedOptions obj)
        {
            ParseAsync().FireAndForget();
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                AdvancedOptions.Saved -= AdvancedOptionsSaved;
            }

            _isDisposed = true;
        }

        public event Action<MDocument> Parsed;
    }
}
