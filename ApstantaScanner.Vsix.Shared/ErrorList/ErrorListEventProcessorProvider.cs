// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Shell.TableControl;
using Microsoft.VisualStudio.Shell.TableManager;
using Microsoft.VisualStudio.Utilities;

namespace ApsantaScanner.Vsix.Shared.ErrorList
{
    [Export(typeof(ITableControlEventProcessorProvider))]
    [ManagerType(StandardTables.ErrorsTable)]
    [DataSourceType(StandardTableDataSources.ErrorTableDataSource)]
    [DataSource("*")]
    [Name("SARIF Location Text Marker Tag")]
    [Order(Before = "Default")]
    internal class ErrorListEventProcessorProvider : ITableControlEventProcessorProvider
    {
#pragma warning disable CS0649 // Filled in by MEF
#pragma warning disable IDE0044 // Assigned by MEF
        [Import]
        private IErrorListEventSelectionService sarifErrorListEventSelectionService;
#pragma warning restore IDE0044
#pragma warning restore CS0649

        public ITableControlEventProcessor GetAssociatedEventProcessor(IWpfTableControl tableControl)
        {
            (this.sarifErrorListEventSelectionService as SarifErrorListEventProcessor)?.SetTableControl(tableControl);

            return this.sarifErrorListEventSelectionService as ITableControlEventProcessor;
        }
    }
}