# ApsantaScanner
Visual Studio extension to empower Securiy Code Scan C# analyzer.

## What works
### Authentication: Github Apps with Device Flow.
Allows to get identity of the github user.
Intention: Sync rules with Github App

### Markdown taint analysis visualization
Simplified data flow using markdown syntax highlighting
Intention: Display information about any security issue in a tool windows with a proper syntax highlighingt + easy to confirm if it is a true positive finding.

### Secret management
Secrets are not stored in a plain-text, hehe, MS Teams.

### Relatively fresh code base from MS Roslyn analyzers
Security Code Scan uses much older version.
See: https://github.com/dbalikhin/ApsantaScanner/tree/main/src/Utilities

## What works partially
### A proper taint analysis reconstruction.
E.g. show me the whole chain between the Source and the Sink. It works only for a single Source => a single Sink. Need more changes to the underlying Roslyn example.
Extraction code: https://github.com/dbalikhin/ApsantaScanner/blob/main/src/ApsantaScanner/Security/Taint/SourceTriggeredTaintedDataAnalyzerBase.cs#L244-L373

Note: It can be a limitation of the algorithm used by taint analysis. Currently it holds only a Source and a Sink.

### Dark mode 
Yay, with some colour bugs from VS Toolkit.

## Pain points
It is not easy to extract all information from Roslyn analyzer into the Error List. Visual Studio doesn't provide access to some crucial fields. The easiest way is to use Error Message and Error Description. Alternatively, abandon the Error List, create a custom control and invoke complilation with analyzers on demand-only (not as a part of build).

Analyzers are not invoked in some cases, aka nuget analyzers vs everything else. Compilation with analyzers will compile the code again. The original idea was to analyze the code on every build with no overhead (e.g. double compilation)

## Future Direction
Probably, a dedicated toolbar window for security findings + scanning on demand is the right approach. In this case, we can use Compilation with Analyzers and have full access to Roslyn SimpleDiagnostic class.

