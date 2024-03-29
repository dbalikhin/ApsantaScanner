﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.Text;

namespace Microsoft.Sarif.Viewer.Tags
{
    internal interface ISarifLocationTagger
    {
        /// <summary>
        /// Notifies the tagger that all existing tags should be considered dirty.
        /// </summary>
        /// <remarks>
        /// As an example, this happens when SARIF results are cleared from the error list service <see cref="ErrorListService"/>.
        /// </remarks>
        void RefreshTags();

        /// <summary>
        /// Gets the <see cref="ITextBuffer"/> for which this tagger provides tags.
        /// </summary>
        ITextBuffer TextBuffer { get; }

        /// <summary>
        /// Fired when a tagger is disposed.
        /// </summary>
        event EventHandler Disposed;
    }
}
