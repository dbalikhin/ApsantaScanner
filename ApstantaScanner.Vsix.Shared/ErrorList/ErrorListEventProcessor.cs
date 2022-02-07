// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using ApstantaScanner.Vsix.Shared.ErrorList;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using VisualStudio2022;

namespace ApsantaScanner.Vsix.Shared.ErrorList
{
    /// <summary>
    /// Maintains currently selected and navigated to <see cref="ErrorListItem"/> from the Visual Studio error list.
    /// </summary>
    [Export(typeof(IErrorListEventSelectionService))]
    internal class ErrorListEventProcessor : TableControlEventProcessorBase, IErrorListEventSelectionService
    {
        private ErrorListItem currentlySelectedItem;
        private ErrorListItem currentlyNavigatedItem;
        private IEnumerable<ErrorListItem> selectedItems;

        /// <inheritdoc/>
        public ErrorListItem SelectedItem
        {
            get => this.currentlySelectedItem;

            set
            {
                if (this.currentlySelectedItem != value)
                {
                    ErrorListItem previouslySelectedItem = this.currentlySelectedItem;
                    this.currentlySelectedItem = value;

                    SelectedItemChanged?.Invoke(this, new ErrorListSelectionChangedEventArgs(previouslySelectedItem, this.currentlySelectedItem));
                }
            }
        }

        /// <inheritdoc/>
        public IEnumerable<ErrorListItem> SelectedItems => this.selectedItems;

        /// <inheritdoc/>
        public event EventHandler<ErrorListSelectionChangedEventArgs> SelectedItemChanged;

        /// <inheritdoc/>
        public ErrorListItem NavigatedItem
        {
            get => this.currentlyNavigatedItem;
            set
            {
                if (this.currentlyNavigatedItem != value)
                {
                    ErrorListItem previouslyNavigatedItem = this.currentlyNavigatedItem;
                    this.currentlyNavigatedItem = value;

                    NavigatedItemChanged?.Invoke(this, new ErrorListSelectionChangedEventArgs(previouslyNavigatedItem, this.currentlyNavigatedItem));
                }
            }
        }

        /// <inheritdoc/>
        public event EventHandler<ErrorListSelectionChangedEventArgs> NavigatedItemChanged;

        private IWpfTableControl errorListTableControl;

        /// <summary>
        /// Called by <see cref="ErrorListEventProcessorProvider"/> to set the table this service will
        /// handle.
        /// </summary>
        /// <param name="wpfTableControl">The WPF table control representing the error list.</param>
        public void SetTableControl(IWpfTableControl wpfTableControl)
        {
            this.errorListTableControl = wpfTableControl;
        }

        public override void PostprocessSelectionChanged(TableSelectionChangedEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            base.PostprocessSelectionChanged(e);
            //var source = e.SelectionChangedEventArgs.Source;
            if (this.errorListTableControl == null)
            {
                return;
            }

            // Make sure there is only one selection, that's all we support.
            IEnumerator<ITableEntryHandle> enumerator = (this.errorListTableControl.SelectedEntries ?? Enumerable.Empty<ITableEntryHandle>()).GetEnumerator();
            ITableEntryHandle selectedTableEntry = null;
            ICollection<ErrorListItem> selectedErrorListItems = null;
            int itemCount = 0;

            while (enumerator.MoveNext())
            {
                itemCount++;
                ITableEntryHandle current = enumerator.Current;
                selectedTableEntry ??= current;
                if (TryGetErrorResult(current, out ErrorListItem errorResult))
                {
                    selectedErrorListItems ??= new List<ErrorListItem>();
                    selectedErrorListItems.Add(errorResult);
                }
            }

            selectedTableEntry = (itemCount > 1) ? null : selectedTableEntry;
            ErrorListItem selectedSarifErrorItem = null;
            if (selectedTableEntry != null)
            {
                this.TryGetErrorResult(selectedTableEntry, out selectedSarifErrorItem);
            }

            ErrorListItem previouslySelectedItem = this.currentlySelectedItem;
            this.currentlySelectedItem = selectedSarifErrorItem;
            this.selectedItems = selectedErrorListItems;

            SelectedItemChanged?.Invoke(this, new ErrorListSelectionChangedEventArgs(previouslySelectedItem, this.currentlySelectedItem));
        }

        public override void PreprocessNavigate(ITableEntryHandle entry, TableEntryNavigateEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            base.PreprocessNavigate(entry, e);            

            // We need to show the explorer window before navigation so
            // it has time to subscribe to navigation events.
            if (this.TryGetErrorResult(entry, out ErrorListItem aboutToNavigateItem)) ///&&                aboutToNavigateItem?.HasDetails == true)
            {
                //MyToolWindow.ShowAsync().Wait();
                //SarifExplorerWindow.Find()?.Show();
            }

        }

        public override void PostprocessNavigate(ITableEntryHandle entry, TableEntryNavigateEventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            base.PostprocessNavigate(entry, e);

            this.TryGetErrorResult(entry, out ErrorListItem newlyNavigatedErrorItem);

            ErrorListItem previouslyNavigatedItem = this.currentlyNavigatedItem;
            this.currentlyNavigatedItem = newlyNavigatedErrorItem;
            /*
            if (this.currentlyNavigatedItem != null)
            {
                // There are two conditions to consider here..
                // The first is that Visual Studio opened the document through the course of normal navigation
                // because the SARIF result had a file name that existed on the local file system.
                // The second case is that no document was opened because the file name doesn't exist on the local file system.
                // In the first case, where the file existed, Visual Studio has already opened the document
                // for us (and the editor is active). The only thing left to do is move the caret to the right location (but do NOT move focus).
                // In the second case, where the file does not exist, we want to attempt to navigate
                // the "first location" AND move the focus to the resulting caret location.
                // The navigation request will prompt the user to remap the path. If the user remaps the path,
                // the file will then be opened in the editor focus will be moved to the proper caret location.
                bool moveFocusToCaretLocation = this.currentlyNavigatedItem.FileName != null && !File.Exists(this.currentlyNavigatedItem.FileName);
                this.currentlyNavigatedItem.Locations?.FirstOrDefault()?.NavigateTo(usePreviewPane: false, moveFocusToCaretLocation: moveFocusToCaretLocation);
            }*/

            NavigatedItemChanged?.Invoke(this, new ErrorListSelectionChangedEventArgs(previouslyNavigatedItem, this.currentlyNavigatedItem));
        }

        private bool TryGetErrorResult(ITableEntryHandle entryHandle, out ErrorListItem errorListItem)
        {
            errorListItem = null;
            entryHandle.TryGetValue<string>(StandardTableColumnDefinitions.ErrorCode, out var code);

            // ignore findings from other analyzers (excluding first party analyzers for now)
            if (!code.StartsWith("APS", StringComparison.InvariantCulture) && !code.StartsWith("SCS", StringComparison.InvariantCulture) && !code.StartsWith("CS", StringComparison.InvariantCulture) && !code.StartsWith("CA", StringComparison.InvariantCulture))
            { 
                return false;
            }

            entryHandle.TryGetValue<string>(StandardTableColumnDefinitions.Text, out var text);
            entryHandle.TryGetValue<int>(StandardTableColumnDefinitions.Line, out var line);
            entryHandle.TryGetValue<int>(StandardTableColumnDefinitions.Column, out var column);
            entryHandle.TryGetValue<string>(StandardTableColumnDefinitions.ProjectName, out var projectName);
            errorListItem = new ErrorListItem()
            {
                DiagnosticItem = new DiagnosticItem()
                {
                    ErrorCode = code,
                    ErrorText = text,
                    Line = line,
                    Column = column,
                    ProjectName = projectName,
                }
            };

            //https://github.com/namse/Roslyn-CSX/blob/master/src/VisualStudio/Core/Def/Implementation/TableDataSource/VisualStudioDiagnosticListTable.BuildTableDataSource.cs
            return errorListItem != null;
        }
    }
}
