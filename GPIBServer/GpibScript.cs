using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GPIBServer
{
    public class GpibScript : ErrorReporterBase
    {
        public event EventHandler ScriptCompleted;

        public GpibScript()
        { }

        #region Properties

        public string[] Commands { get; set; }
        public string Name { get; set; }
        [JsonIgnore]
        public int CurrentIndex { get; private set; }

        #endregion

        #region Public Methods

        public void ExecuteNextCommand()
        {

        }

        #endregion
    }
}
