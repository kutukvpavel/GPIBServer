using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace InstrumentSimulator
{
    public class Instrument
    {
        public Instrument()
        { }

        #region Properties

        public int Address { get; set; }

        public Dictionary<string, string> ReplyTable { get; set; }


        #endregion
    }
}
