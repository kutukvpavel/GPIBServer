using System;
using System.Collections.Generic;
using System.Text;

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

    public class GpibResponseEventArgs : EventArgs
    {
        public GpibResponseEventArgs(string response)
        {
            Response = response;
        }

        public string Response { get; set; }
    }

    public class GpibCommandEventArgs : EventArgs
    {
        public GpibCommandEventArgs(GpibCommand cmd)
        {
            Command = cmd;
        }

        public GpibCommand Command { get; }
    }
}
