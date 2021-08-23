using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.Text;

namespace GPIBServer
{
    public class SerialPortConfiguration
    {
        public string Name { get; set; }
        public int BaudRate { get; set; } = 115200;
        public int DataBits { get; set; } = 8;
        public Parity Parity { get; set; } = Parity.None;
        public StopBits StopBits { get; set; } = StopBits.One;
    }
}
