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
        #region Properties

        public static string Path { get; private set; }
        public static string LineFormat { get; set; }

        #endregion

        #region Public Methods

        public static void Initialize(string path, string lineFormat)
        {
            Path = path;
            LineFormat = lineFormat;
            _Writer = new StreamWriter(Path, true);
        }

        public static void Write(object sender, GpibResponseEventArgs e)
        {
            if (_Writer == null) throw new InvalidOperationException("Output module is not initialized!");
            _Writer.WriteLine(string.Format(LineFormat, 
                (sender as GpibController).Name, e.Instrument.Name, e.Command.Name, e.Response));
        }

        #endregion

        #region Private

        private static TextWriter _Writer;

        #endregion
    }
}
