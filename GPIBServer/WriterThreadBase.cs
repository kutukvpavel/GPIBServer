using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GPIBServer
{
    public static partial class Output
    {
        private abstract class WriterThreadBase<T> : IDisposable
        {
            public WriterThreadBase(string path, CancellationToken token)
            {
                Path = path;
                string dir = System.IO.Path.GetDirectoryName(Path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                _Writer = File.AppendText(Path);
                _Collection = new BlockingCollection<T>();
                _Source = new CancellationTokenSource();
                token.Register(() => _Source.Cancel());
                _Thread = new Thread(Process);
                _Thread.Start();
            }

            public string Path { get; }
            public bool WriteToConsole { get; set; } = false;

            public void Queue(T data)
            {
                _Collection.Add(data);
            }

            public void Dispose()
            {
                if (!_Source.IsCancellationRequested) _Source.Cancel();
                if (_Thread.IsAlive) _Thread.Join();
                _Writer.Close();
                _Writer.Dispose();
                _Source.Dispose();
            }

            private readonly TextWriter _Writer;
            private readonly BlockingCollection<T> _Collection;
            private readonly Thread _Thread;
            private readonly CancellationTokenSource _Source;

            private void Process()
            {
                while (!_Source.Token.IsCancellationRequested)
                {
                    try
                    {
                        WriteLine(_Collection.Take(_Source.Token));
                    }
                    catch (OperationCanceledException)
                    { }
                    catch (Exception ex)
                    {
                        Task.Run(() => ErrorOccurred?.Invoke(null, new ExceptionEventArgs(ex, Path)));
                    }
                }
            }

            protected abstract string ConvertData(T data);

            private void WriteLine(T data)
            {
                int retry = Retries;
                IOException lastIoExc = null;
                string line = ConvertData(data);
                if (WriteToConsole) Console.WriteLine(line);
                while (retry-- > 0)
                {
                    try
                    {
                        _Writer.WriteLine(line);
                        break;
                    }
                    catch (IOException ex)
                    {
                        lastIoExc = ex;
                        Thread.Sleep(RetryDelayMilliseconds);
                    }
                }
                if (retry < 0) 
                    Task.Run(() => ErrorOccurred?.Invoke(null, new ExceptionEventArgs(lastIoExc, Path)));
            }
        }
    }
}
