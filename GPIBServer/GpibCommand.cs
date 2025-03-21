﻿using System.Globalization;

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

        #region Properties

        public string Name { get; set; } = "ExampleCmd";
        public string CommandString { get; set; } = "++ver";
        public bool AwaitResponse { get; set; } = false;
        public int TimeoutMilliseconds { get; set; } = 3000;
        public int ResponsePrefixLength { get; set; } = 0;
        public string ExpectedResponse { get; set; }
        public bool OutputResponse { get; set; } = false;

        #endregion

        public GpibCommand PutInParameters(params object[] p)
        {
            return PutInParameters(p, p);
        }

        public GpibCommand PutInParameters(object[] pCmd, object[] pResp)
        {
            var b = new GpibCommand(this);
            b.CommandString = string.Format(b.CommandString, pCmd);
            if (b.ExpectedResponse != null) b.ExpectedResponse = string.Format(CultureInfo.InvariantCulture, b.ExpectedResponse, pResp);
            return b;
        }
    }
}
