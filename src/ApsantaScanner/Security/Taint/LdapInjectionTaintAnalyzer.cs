using Analyzer.Utilities.FlowAnalysis.Analysis.TaintedDataAnalysis;
using ApsantaScanner.Security.Locale;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ApsantaScanner.Security.Taint
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    public class LdapFilterTaintAnalyzer : SourceTriggeredTaintedDataAnalyzerBase
    {
        internal static readonly DiagnosticDescriptor Rule = LocaleUtil.GetDescriptor("SCS0031"); // also need to iclude LdapPathTaintAnalyzer - SCS0026

        protected override SinkKind SinkKind { get { return (SinkKind)(int)TaintType.SCS0031; } }

        protected override DiagnosticDescriptor TaintedDataEnteringSinkDescriptor { get { return Rule; } }
    }
}
