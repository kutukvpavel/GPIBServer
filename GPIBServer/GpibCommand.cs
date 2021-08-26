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
    }
}
