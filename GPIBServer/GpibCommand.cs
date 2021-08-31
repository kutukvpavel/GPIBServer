using System;
using System.Collections.Generic;
using System.Text;

namespace GPIBServer
{
    public class GpibCommand
    {
        public GpibCommand()
        { }
        public GpibCommand(GpibCommand existing)
        {
            Name = existing.Name;
            CommandString = existing.CommandString;
            AwaitResponse = existing.AwaitResponse;
            TimeoutMilliseconds = existing.TimeoutMilliseconds;
            ResponsePrefixLength = existing.ResponsePrefixLength;
            ExpectedResponse = existing.ExpectedResponse;
            OutputResponse = existing.OutputResponse;
        }

        public string Name { get; set; }
        public string CommandString { get; set; }
        public bool AwaitResponse { get; set; }
        public int TimeoutMilliseconds { get; set; }
        public int ResponsePrefixLength { get; set; }
        public string ExpectedResponse { get; set; }
        public bool OutputResponse { get; set; }

        public GpibCommand PutInParameters(params object[] p)
        {
            return PutInParameters(p, p);
        }

        public GpibCommand PutInParamters(object[] pCmd, object[] pResp)
        {
            var b = new GpibCommand(this);
            b.CommandString = string.Format(b.CommandString, pCmd);
            b.ExpectedResponse = string.Format(b.ExpectedResponse, pResp);
            return b;
        }
    }
}
