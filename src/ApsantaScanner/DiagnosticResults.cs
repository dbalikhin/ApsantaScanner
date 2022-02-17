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
            _diagnosticQueue.Enqueue(diagnostic.Id + ": " + diagnostic.Descriptor.Title);
        }

        public static void AddEntry(string entry)
        {
            _diagnosticQueue.Enqueue(entry);
        }

        public static void Clear()
        {
            _diagnosticQueue = new ConcurrentQueue<string>();
        }

        public static int Count => _diagnosticQueue.Count;

        public static async Task WriteToFileAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                using (StreamWriter w = File.AppendText("c:\\tmp\\myfile.txt"))
                {
                    while (_diagnosticQueue.TryDequeue(out string textLine))
                    {
                        await w.WriteLineAsync(textLine);
                    }
                    w.Flush();
                    Thread.Sleep(100);
                }
            }
        }

        public static void WriteToFile()
        {
            var filename = "C:\\temp\\MyTest.txt";
            // Create a file to write to.
            using (StreamWriter sw = File.CreateText(filename))
            {
                while (_diagnosticQueue.TryDequeue(out string textLine))
                {
                    sw.WriteLine(textLine);
                }
                sw.Flush();
            }

            return;


            var rootDirectory = StaticMother.TryGetSolutionDirectoryInfo();
            if (rootDirectory == null)
            {
                // something is wrong, clear the queue and exit
                _diagnosticQueue = new ConcurrentQueue<string>();
                return;
            }

            string path = Path.Combine(rootDirectory.FullName, ".apsanta", "current");
            Directory.CreateDirectory(path);
            filename = Path.Combine(path, ".results");
           
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
