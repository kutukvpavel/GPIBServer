using System;

namespace GPIBServer
{
    public class ExceptionEventArgs : EventArgs
    {
        public ExceptionEventArgs(Exception ex, object data = null)
        {
            Exception = ex;
            Data = data;
        }

        public Exception Exception { get; }
        public object Data { get; }
    }

    public class GpibResponseEventArgs : GpibCommandEventArgs
    {
        public GpibResponseEventArgs(GpibCommand cmd, GpibInstrument instrument, string response) : base(cmd, instrument)
        {
            Response = response;
        }

        public string Response { get; set; }
    }

    public class GpibCommandEventArgs : EventArgs
    {
        public GpibCommandEventArgs(GpibCommand cmd, GpibInstrument instrument)
        {
            Command = cmd;
            Instrument = instrument;
        }

        public GpibCommand Command { get; }
        public GpibInstrument Instrument { get; }
    }
}
