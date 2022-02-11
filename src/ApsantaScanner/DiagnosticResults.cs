using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ApsantaScanner
{
    internal class DiagnosticResults
    {
        private static ConcurrentQueue<string> _diagnosticQueue = new ConcurrentQueue<string>();

        private CancellationTokenSource _token = new CancellationTokenSource();

        public static void AddDiagnostic(Diagnostic diagnostic)
        {
            _diagnosticQueue.Enqueue(diagnostic.Id);
        }

        public static async Task WriteToFileAsync()
        {
            string path = @"c:\temp\MyTest.txt";

            // Create a file to write to.
            using (StreamWriter sw = File.CreateText(path))
            {
                while (_diagnosticQueue.TryDequeue(out string textLine))
                {
                    await sw.WriteLineAsync(textLine);
                }
                sw.Flush();
            }
            
        }

        public static void WriteToFile()
        {
            string path = @"c:\temp\MyTest.txt";

            // Create a file to write to.
            using (StreamWriter sw = File.CreateText(path))
            {
                while (_diagnosticQueue.TryDequeue(out string textLine))
                {
                    sw.WriteLine(textLine);
                }
                sw.Flush();
            }

        }
    }
}
