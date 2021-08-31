using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Linq;

namespace GPIBServer
{
    public class GpibInstrument
    {
        #region Properties

        public string CommandSetName { get; set; }
        public int Address { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }

        #endregion
    }
}
