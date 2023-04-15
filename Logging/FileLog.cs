using System;
using System.IO;

namespace BitFab.KW1281Test.Logging
{
    public class FileLog : ILog
    {
        private readonly StreamWriter _writer;

        public FileLog(string filename)
        {
            _writer = new StreamWriter(filename, append: true);
        }

        public void WriteLine(string message, LogDest dest)
        {
            if (dest != LogDest.File)
            {
                Console.WriteLine($"{DateTime.Now.ToString("HH mm ss.fff")} " + message);
            }
            if (dest != LogDest.Console)
            {
                _writer.WriteLine($"{DateTime.Now.ToString("HH mm ss.fff")} " + message);
            }
        }

        public void WriteLine(LogDest dest)
        {
            if (dest != LogDest.File)
            {
                Console.WriteLine();
            }
            if (dest != LogDest.Console)
            {
                _writer.WriteLine();
            }
        }

        public void Write(string message, LogDest dest)
        {
            if (dest != LogDest.File)
            {
                Console.Write($"{DateTime.Now.ToString("HH mm ss.fff")} " +message);
            }
            if (dest != LogDest.Console)
            {
                _writer.Write($"{DateTime.Now.ToString("HH mm ss.fff")} " + message);
            }
        }

        public void Close()
        {
            _writer.Close();
        }

        public void Dispose()
        {
            Close();
        }
    }
}
