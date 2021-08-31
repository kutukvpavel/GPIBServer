using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GPIBServer
{
    public enum OutputSeparation
    {
        None,
        InstrumentType,
        InstrumentModel,
        InstrumentName
    }

    public static class Output
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

        public static void Write(object sender, GpibResponseEventArgs e)
        {
            string p = Separation switch
            {
                OutputSeparation.None => string.Empty,
                OutputSeparation.InstrumentType => string.Format(SeparationLabelFormat, e.Instrument.Type),
                OutputSeparation.InstrumentModel => string.Format(SeparationLabelFormat, e.Instrument.CommandSetName),
                OutputSeparation.InstrumentName => string.Format(SeparationLabelFormat, e.Instrument.Name),
                _ => throw new ArgumentOutOfRangeException("Output separation value is out of range."),
            };
            p = string.Format(Path, p);
            if (!_Writers.ContainsKey(p)) _Writers.Add(p, File.AppendText(p));
            var w = _Writers[p];
            int retry = Retries;
            IOException lastIoExc = null;
            while (retry-- > 0)
            {
                try
                {
                    w.WriteLine(LineFormat, (sender as GpibController).Name, e.Instrument.Name, e.Response);
                }
                catch (IOException ex)
                {
                    lastIoExc = ex;
                }
                System.Threading.Thread.Sleep(RetryDelayMilliseconds);
            }
            if (retry < 0) ErrorOccurred?.BeginInvoke(null, new ExceptionEventArgs(lastIoExc, p), null, null);
        }

        #endregion

        #region Private

        private static readonly Dictionary<string, TextWriter> _Writers = new Dictionary<string, TextWriter>();

        #endregion
    }
}
