using RJCP.IO.Ports;

namespace GPIBServer
{
    public class SerialPortConfiguration
    {
        public string Name { get; set; } = "COM";
        public int BaudRate { get; set; } = 115200;
        public int DataBits { get; set; } = 8;
        public Parity Parity { get; set; } = Parity.None;
        public StopBits StopBits { get; set; } = StopBits.One;
    }
}
