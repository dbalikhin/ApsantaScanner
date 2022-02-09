using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ApsantaScanner
{
    public class MultiThreadFileWriter
    {
        private static MultiThreadFileWriter instance;
        private static readonly object padlock = new object();


        public static MultiThreadFileWriter Instance
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new MultiThreadFileWriter();
                    }
                    return instance;
                }
            }
        }

        private static ConcurrentQueue<string> _textToWrite = new ConcurrentQueue<string>();
        private CancellationTokenSource _source = new CancellationTokenSource();
        private CancellationToken _token;

        public MultiThreadFileWriter()
        {
            _token = _source.Token;
            // This is the task that will run
            // in the background and do the actual file writing
            //Task.Run(WriteToFile, _token);

        }

        /// The public method where a thread can ask for a line
        /// to be written.
        public void WriteLine(string line)
        {
            _textToWrite.Enqueue(line);
        }

        /// The actual file writer, running
        /// in the background.
        public async Task WriteToFile()
        {
            //while (true)
            //{
            if (_token.IsCancellationRequested)
            {
                return;
            }

            string path = @"c:\temp\MyTest.txt";
            // This text is added only once to the file.
            if (!File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    while (_textToWrite.TryDequeue(out string textLine))
                    {
                        await sw.WriteLineAsync(textLine);
                    }
                    sw.Flush();
                }
            }
        }

        public void WriteToFileSync()
        {

            string path = @"c:\temp\MyTest.txt";

            // Create a file to write to.
            using (StreamWriter sw = File.CreateText(path))
            {
                while (_textToWrite.TryDequeue(out string textLine))
                {
                    sw.WriteLine(textLine);
                }
                sw.Flush();
            }
        }
            




            /*
            // This text is always added, making the file longer over time
            // if it is not deleted.
            using (StreamWriter sw = File.AppendText(path))
            {
                while (_textToWrite.TryDequeue(out string textLine))
                {
                    await sw.WriteLineAsync(textLine);
                }
                sw.Flush();
            }*/

            // }
        
    }
}
