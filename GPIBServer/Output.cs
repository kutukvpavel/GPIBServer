using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using NamedPipeWrapper;

namespace GPIBServer
{
    public enum OutputSeparation
    {
        None,
        InstrumentType,
        InstrumentModel,
        InstrumentName
    }

    public static partial class Output
    {
        public class PipePacket
        {
            public string TimeReceived;
            public string ControllerName;
            public string InstrumentName;
            public string Command;
            public string Response;
        }

        public static event EventHandler<ExceptionEventArgs> ErrorOccurred;

        #region Properties

        public static string TerminalLogPath { get; private set; }
        public static string DataPath { get; private set; }
        public static string LineFormat { get; set; }
        public static OutputSeparation Separation { get; set; }
        public static string SeparationLabelFormat { get; set; }
        public static int Retries { get; set; }
        public static int RetryDelayMilliseconds { get; set; }

        #endregion

        #region Public Methods

        public static void Dispose()
        {
            foreach (var item in _DataWriters)
            {
                try
                {
                    item.Value.Dispose();
                }
                catch (Exception ex)
                {
                    ErrorOccurred.Invoke(null, new ExceptionEventArgs(ex));
                }
            }
            foreach (var item in _TerminalWriters)
            {
                try
                {
                    item.Value.Dispose();
                }
                catch (Exception ex)
                {
                    ErrorOccurred.Invoke(null, new ExceptionEventArgs(ex));
                }
            }
        }

        public static void Initialize(string dataPath, string terminalPath, string pipeName, CancellationToken cancel)
        {
            _Cancel = cancel;
            TerminalLogPath = terminalPath;
            DataPath = dataPath;
            _PipeQueue = new BlockingCollection<Tuple<object, GpibResponseEventArgs>>();
            _Pipe = new NamedPipeServer<string>(pipeName);
            _Pipe.Start();
            _PipeThread = new Thread(() =>
            {
                try
                {
                    while (!cancel.IsCancellationRequested)
                    {
                        var d = _PipeQueue.Take(cancel);
                        _Pipe.PushMessage(Serializer.Serialize(new PipePacket() {
                            ControllerName = (d.Item1 as GpibController).Name,
                            TimeReceived = d.Item2.TimeReceived.ToString(Configuration.Instance.PipeDatetimeFormat),
                            InstrumentName = d.Item2.Instrument.Name,
                            Command = d.Item2.Command.CommandString,
                            Response = d.Item2.Response
                        }));
                    }
                }
                catch (OperationCanceledException)
                { }
            });
            _PipeThread.Start();
        }

        public static void QueueData(object sender, GpibResponseEventArgs e)
        {
            if (!e.Command.OutputResponse) return;
            CheckInitialization();
            var tuple = new Tuple<object, GpibResponseEventArgs>(sender, e);
            _PipeQueue.Add(tuple);
            string p = Separation switch
            {
                OutputSeparation.None => string.Empty,
                OutputSeparation.InstrumentType => string.Format(SeparationLabelFormat, e.Instrument.Type),
                OutputSeparation.InstrumentModel => string.Format(SeparationLabelFormat, e.Instrument.CommandSetName),
                OutputSeparation.InstrumentName => string.Format(SeparationLabelFormat, e.Instrument.Name),
                _ => throw new ArgumentOutOfRangeException("Output separation value is out of range.")
            };
            p = string.Format(DataPath, p);
            if (!_DataWriters.ContainsKey(p))
            {
                lock (_DataWriters)
                {
                    var t = new DataWriterThread(p, _Cancel);
                    _DataWriters.TryAdd(p, t);
                }
            }
            _DataWriters[p].Queue(tuple);
        }

        public static void QueueTerminal(object sender, string data)
        {
            CheckInitialization();
            string p = string.Format(TerminalLogPath, (sender as GpibController).Name);
            try
            {
                if (!_TerminalWriters.ContainsKey(p))
                {
                    lock (_TerminalWriters)
                    {
                        var t = new TerminalWriterThread(p, _Cancel);
                        _TerminalWriters.TryAdd(p, t);
                    }
                }
            }
            catch (IOException)
            {

            }
            _TerminalWriters[p].Queue(data);
        }

        #endregion

        #region Private

        private static readonly ConcurrentDictionary<string, DataWriterThread> _DataWriters
            = new ConcurrentDictionary<string, DataWriterThread>();
        private static readonly ConcurrentDictionary<string, TerminalWriterThread> _TerminalWriters
            = new ConcurrentDictionary<string, TerminalWriterThread>();
        private static NamedPipeServer<string> _Pipe;
        private static Thread _PipeThread;
        private static BlockingCollection<Tuple<object, GpibResponseEventArgs>> _PipeQueue;

        private static CancellationToken _Cancel;

        private static string DataConverter(object s, GpibResponseEventArgs e)
        {
            return string.Format(LineFormat, e.TimeReceived,
                    (s as GpibController).Name, e.Instrument.Name, e.Command.CommandString, e.Response);
        }

        private static void CheckInitialization()
        {
            if (_Cancel == null) throw new InvalidOperationException("Output module not initialized.");
        }

        private class DataWriterThread : WriterThreadBase<Tuple<object, GpibResponseEventArgs>>
        {
            public DataWriterThread(string path, CancellationToken token) : base(path, token)
            { }

            protected override string ConvertData(Tuple<object, GpibResponseEventArgs> data)
            {
                return DataConverter(data.Item1, data.Item2);
            }
        }

        private class TerminalWriterThread : WriterThreadBase<string>
        {
            public TerminalWriterThread(string path, CancellationToken token) : base(path, token)
            {
                WriteToConsole = true;
            }

            protected override string ConvertData(string data)
            {
                return data;
            }
        }

        #endregion
    }
}
