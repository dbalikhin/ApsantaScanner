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

        public static void AddEntry(string entry)
        {
            _diagnosticQueue.Enqueue(entry);
        }

        public static int Count => _diagnosticQueue.Count;

        public static async Task WriteToFileAsync()
        {
            var rootDirectory = StaticMother.TryGetSolutionDirectoryInfo();
            if (rootDirectory == null)
            {
                // something is wrong, clear the queue and exit
                _diagnosticQueue = new ConcurrentQueue<string>();
                return;
            }

            string path = Path.Combine(rootDirectory.FullName, ".apsanta", "current");
            Directory.CreateDirectory(path);
            string filename = Path.Combine(path, ".results");

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
            var rootDirectory = StaticMother.TryGetSolutionDirectoryInfo();
            if (rootDirectory == null)
            {
                // something is wrong, clear the queue and exit
                _diagnosticQueue = new ConcurrentQueue<string>();
                return;
            }

            string path = Path.Combine(rootDirectory.FullName, ".apsanta", "current");
            Directory.CreateDirectory(path);
            string filename = Path.Combine(path, ".results");

            // Create a file to write to.
            using (StreamWriter sw = File.CreateText(filename))
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
