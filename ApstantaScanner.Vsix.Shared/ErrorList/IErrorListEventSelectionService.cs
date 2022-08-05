// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace ApsantaScanner.Vsix.Shared.ErrorList
{
    /// <summary>
    /// Service interface that transforms the selections and navigations from Visual Studio's error list into selections
    /// of <see cref="ErrorListItem"/> items that the rest of the extension can leverage.
    /// </summary>
    /// <remarks>
    /// A "navigated item" is one that a user "double clicked on" in the Error list, and a "selected item" is one that the user "single clicked" on in the error list.
    /// Note that VS does NOT navigate to the file on single click of an item in the error list hence the need for these two properties.
    /// </remarks>
    internal interface IErrorListEventSelectionService
    {
        /// <summary>
        /// Gets or sets the currently selected <see cref="ErrorListItem"/>.
        /// </summary>
        /// <remarks>
        /// May be null.
        /// </remarks>
        ErrorListItem SelectedItem { get; set; }

        /// <summary>
        /// Gets the list of selected <see cref="ErrorListItem"/> in error list.
        /// </summary>
        /// <remarks>
        /// May be null.
        /// </remarks>
        IEnumerable<ErrorListItem> SelectedItems { get; }

        /// <summary>
        /// Fired when the selection in the Visual Studio error list has changed.
        /// </summary>
        event EventHandler<ErrorListSelectionChangedEventArgs> SelectedItemChanged;

        /// <summary>
        /// Gets or sets the currently navigated to <see cref="ErrorListItem"/>.
        /// </summary>
        /// <remarks>
        /// May be null.
        /// </remarks>
        ErrorListItem NavigatedItem { get; set; }

        /// <summary>
        /// Fired when the Visual Studio error list navigates to an item.
        /// </summary>
        event EventHandler<ErrorListSelectionChangedEventArgs> NavigatedItemChanged;
    }
}
