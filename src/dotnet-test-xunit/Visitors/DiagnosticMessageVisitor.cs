using System;
using Xunit.Abstractions;

namespace Xunit.Runner.DotNet
{
    public class DiagnosticMessageVisitor : TestMessageVisitor
    {
        readonly string _assemblyDisplayName;
        readonly object _consoleLock;
        readonly bool _noColor;
        readonly bool _showDiagnostics;

        public DiagnosticMessageVisitor(object consoleLock, string assemblyDisplayName, bool showDiagnostics, bool noColor)
        {
            _noColor = noColor;
            _consoleLock = consoleLock;
            _assemblyDisplayName = assemblyDisplayName;
            _showDiagnostics = showDiagnostics;
        }

        protected override bool Visit(IDiagnosticMessage diagnosticMessage)
        {
            if (_showDiagnostics)
                lock (_consoleLock)
                {
                    if (!_noColor)
                        Console.ForegroundColor = ConsoleColor.Yellow;

                    Console.WriteLine("   {0}: {1}", _assemblyDisplayName, diagnosticMessage.Message);

                    if (!_noColor)
                        Console.ForegroundColor = ConsoleColor.Gray;
                }

            return base.Visit(diagnosticMessage);
        }
    }
}
