using System;
using System.Collections.Generic;
using System.Text;

namespace GPIBServer
{
    public class GpibInstrumentCommandSet : CommandSetBase
    {
        public GpibInstrumentCommandSet()
        { }

        #region Properties

        public string Name { get; set; }

        #endregion
    }
}
