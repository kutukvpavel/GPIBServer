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

    public static partial class Output
    {
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

        public static void Initialize(string dataPath, string terminalPath, CancellationToken cancel)
        {
            _Cancel = cancel;
            TerminalLogPath = terminalPath;
            DataPath = dataPath;
        }

        public static void QueueData(object sender, GpibResponseEventArgs e)
        {
            if (!e.Command.OutputResponse) return;
            CheckInitialization();
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
            _DataWriters[p].Queue(new Tuple<object, GpibResponseEventArgs>(sender, e));
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

        private static CancellationToken _Cancel;

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
                return string.Format(LineFormat, data.Item2.TimeReceived,
                    (data.Item1 as GpibController).Name, data.Item2.Instrument.Name, data.Item2.Command.CommandString,
                    data.Item2.Response);
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
