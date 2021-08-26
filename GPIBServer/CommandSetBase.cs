using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GPIBServer
{
    public abstract class CommandSetBase : ErrorReporterBase
    {
        private Dictionary<string, GpibCommand> _Commands;

        public GpibCommand[] CommandSet { get; set; }

        public void InitializeCommandSet()
        {
            _Commands = CommandSet.ToDictionary(x => x.Name);
        }

        public GpibCommand this[string name]
        {
            get
            {
                try
                {
                    if (_Commands == null) throw new InvalidOperationException("Command set is not initialized!");
                    return _Commands[name];
                }
                catch (KeyNotFoundException)
                {
                    return null;
                }
            }
        }
    }
}
