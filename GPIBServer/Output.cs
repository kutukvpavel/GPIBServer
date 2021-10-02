using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace GPIBServer
{
    public enum OutputSeparation
    {
        None,
        InstrumentType,
        InstrumentModel,
        InstrumentName
    }

    public static class Output //TODO: Use blocking queue to avoid reentrancy issues
    {
        public static event EventHandler<ExceptionEventArgs> ErrorOccurred;

        #region Properties

        public static string Path { get; set; }
        public static string LineFormat { get; set; }
        public static OutputSeparation Separation { get; set; }
        public static string SeparationLabelFormat { get; set; }
        public static int Retries { get; set; }
        public static int RetryDelayMilliseconds { get; set; }

        #endregion

        #region Public Methods

        public static void Initialize(CancellationToken cancel)
        {
            _Cancel = cancel;
        }

        public static void QueueForWrite(object sender, GpibResponseEventArgs e)
        {
            if (!e.Command.OutputResponse) return;
            if (_Cancel == null) throw new InvalidOperationException("Output module not initialized.");
            string p = Separation switch
            {
                OutputSeparation.None => string.Empty,
                OutputSeparation.InstrumentType => string.Format(SeparationLabelFormat, e.Instrument.Type),
                OutputSeparation.InstrumentModel => string.Format(SeparationLabelFormat, e.Instrument.CommandSetName),
                OutputSeparation.InstrumentName => string.Format(SeparationLabelFormat, e.Instrument.Name),
                _ => throw new ArgumentOutOfRangeException("Output separation value is out of range."),
            };
            p = string.Format(Path, p);
            if (!_Writers.ContainsKey(p))
            {
                _Writers.Add(p, new WriterThread(p, _Cancel));
            }
            _Writers[p].Queue(sender, e);
        }

        #endregion

        #region Private

        private static readonly Dictionary<string, WriterThread> _Writers = new Dictionary<string, WriterThread>();

        private static CancellationToken _Cancel;

        private class WriterThread : IDisposable
        {
            public WriterThread(string path, CancellationToken token)
            {
                Path = path;
                string dir = System.IO.Path.GetDirectoryName(Path);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                _Writer = File.AppendText(Path);
                _Collection = new BlockingCollection<Tuple<object, GpibResponseEventArgs>>();
                _Source = new CancellationTokenSource();
                token.Register(() => _Source.Cancel());
                _Thread = new Thread(Process);
                _Thread.Start();
            }

            public string Path { get; }

            public void Queue(object sender, GpibResponseEventArgs e)
            {
                _Collection.Add(new Tuple<object, GpibResponseEventArgs>(sender, e));
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
            private readonly BlockingCollection<Tuple<object, GpibResponseEventArgs>> _Collection;
            private readonly Thread _Thread;
            private readonly CancellationTokenSource _Source;

            private void Process()
            {
                while (!_Source.Token.IsCancellationRequested)
                {
                    try
                    {
                        var t = _Collection.Take(_Source.Token);
                        Write(t.Item1, t.Item2);
                    }
                    catch (OperationCanceledException)
                    { }
                    catch (Exception ex)
                    {
                        ErrorOccurred?.BeginInvoke(null, new ExceptionEventArgs(ex, Path), null, null);
                    }
                }
            }

            private void Write(object sender, GpibResponseEventArgs e)
            {
                int retry = Retries;
                IOException lastIoExc = null;
                string line = string.Format(LineFormat,
                            (sender as GpibController).Name, e.Instrument.Name, e.Command.CommandString, e.Response);
                while (retry-- > 0)
                {
                    try
                    {
                        _Writer.WriteLine(line);
                    }
                    catch (IOException ex)
                    {
                        lastIoExc = ex;
                    }
                    Thread.Sleep(RetryDelayMilliseconds);
                }
                if (retry < 0) ErrorOccurred?.BeginInvoke(null, new ExceptionEventArgs(lastIoExc, Path), null, null);
            }
        }

        #endregion
    }
}
