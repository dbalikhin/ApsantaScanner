﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ApstantaScanner.Vsix.Shared.ErrorList;
using System.ComponentModel;

namespace ApsantaScanner.Vsix.Shared.ErrorList
{
    internal class ErrorListItem : NotifyPropertyChangedObject, IDisposable
    {

        private string _selectedTab;

        private bool isDisposed;

        internal DiagnosticItem DiagnosticItem { get; set; }

        internal ErrorListItem()
        {

        }


        /// <summary>
        /// Fired when this error list item is disposed.
        /// </summary>
        /// <remarks>
        /// An example of the usage of this is making sure that the SARIF explorer window
        /// doesn't hold on to a disposed object when the error list is cleared.
        /// </remarks>
        public event EventHandler Disposed;


        [Browsable(false)]
        public string SelectedTab
        {
            get => this._selectedTab;

            set
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                this._selectedTab = value;

                // If a new tab is selected, reset the Properties window.
                //SarifExplorerWindow.Find()?.ResetSelection();
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;

            if (disposing)
            {
                //IEnumerable<ResultTextMarker> resultTextMarkers = this.CollectResultTextMarkers(includeChildTags: true, includeResultTag: true);
                //foreach (ResultTextMarker resultTextMarker in resultTextMarkers)
                //{
                //    resultTextMarker.Dispose();
                //}
            }

            Disposed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }


    }
}
