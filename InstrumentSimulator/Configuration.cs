using System;
using System.Collections.Generic;
using System.Text;

namespace InstrumentSimulator
{
    public class Configuration
    {
        public string AddressSelectPrefix { get; set; } = "++addr ";
        public string ControllerPrefix { get; set; } = "++";
        public string ScpiIdCommand { get; set; } = "*ID?";
        public string NewLine { get; set; } = "\r\n";

        public Instrument[] Instruments { get; set; } = new Instrument[]
                {
                    new Instrument()
                    {
                        Address = 1,
                        ReplyTable = new Dictionary<string, string>() { { "*ID?", "ExampleInstrument" } }
                    }
                };

        public Instrument Controller { get; set; } = new Instrument
        {
            Address = 0,
            ReplyTable = new Dictionary<string, string>() { { "++ver", "ExampleController" } }
        };
    }
}
