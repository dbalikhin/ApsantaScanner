// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace ApsantaScanner.Vsix.Shared.ErrorList
{
    /// <summary>
    /// Used as event arguments for the navigate and selection changed events from <see cref="ErrorListEventProcessor"/>.
    /// </summary>
    internal class ErrorListSelectionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorListSelectionChangedEventArgs"/> class.
        /// </summary>
        /// <param name="oldItem">The previous item.</param>
        /// <param name="newItem">The new item.</param>
        /// <remarks>Both parameters may be null.</remarks>
        public ErrorListSelectionChangedEventArgs(ErrorListItem oldItem, ErrorListItem newItem)
        {
            this.OldItem = oldItem;
            this.NewItem = newItem;
        }

        /// <summary>
        /// Gets the previous item.
        /// </summary>
        /// <remarks>
        /// May be null.
        /// </remarks>
        public ErrorListItem OldItem { get; }

        /// <summary>
        /// Gets the new item.
        /// </summary>
        /// <remarks>
        /// May be null.
        /// </remarks>
        public ErrorListItem NewItem { get; }
    }
}
