using System;
using System.Collections.Generic;
using System.Text;

namespace GPIBServer
{
    public class GpibCommand
    {
        public GpibCommand()
        { }

        public string Name { get; set; }
        public string CommandString { get; set; }
        public bool AwaitResponse { get; set; }
        public int TimeoutMilliseconds { get; set; }
        public int ResponsePrefixLength { get; set; }
        public string ExpectedResponse { get; set; }
    }
}
