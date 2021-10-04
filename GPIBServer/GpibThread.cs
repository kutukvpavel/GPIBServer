using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading;

namespace GPIBServer
{
    public class GpibThread : ErrorReporterBase
    {
        public GpibThread()
        { }

        #region Properties

        public string[] Commands { get; set; } = new string[0];
        public string Name { get; set; } = "Example";
        public int TimeoutRetry { get; set; } = 3;
        public int LoopIndex { get; set; } = 0;
        public int LoopCount { get; set; } = 1;
        public int DefaultCommandInterval { get; set; } = 1000;
        public int StartDelay { get; set; } = 1000;

        [JsonIgnore]
        public CancellationToken? Cancel { get; set; }

        #endregion

        #region Public Methods

        public bool Execute(Dictionary<string, GpibController> controllers, Dictionary<string, GpibInstrumentCommandSet> instruments)
        {
            int loop = LoopCount;
            bool initialized = false;
            while ((loop < 0 || loop-- > 0) && !(Cancel?.IsCancellationRequested ?? false))
            {
                int i;
                for (i = initialized ? LoopIndex : 0; (i < Commands.Length) && !(Cancel?.IsCancellationRequested ?? false); i++)
                {
                    string item = Commands[i];
                    try
                    {
                        if (item.StartsWith(GpibScript.DelayCommandPrefix))
                        {
                            Thread.Sleep(int.Parse(item.Remove(0, GpibScript.DelayCommandPrefix.Length)));
                        }
                        else
                        {
                            if (!ExecuteCommand(item, controllers, instruments)) break;
                        }
                    }
                    catch (Exception ex) when (ex is NullReferenceException || ex is KeyNotFoundException)
                    {
                        RaiseError(this, ex, item);
                        return false;
                    }
                    Thread.Sleep(DefaultCommandInterval);
                }
                if (i != Commands.Length) return false;
                initialized = true;
            }
            return true;
        }

        #endregion

        #region Private

        private bool ExecuteCommand(string item, 
            Dictionary<string, GpibController> controllers, Dictionary<string, GpibInstrumentCommandSet> instruments)
        {
            var res = ParseCommand(item, controllers, instruments, 
                out GpibController ctrl, out GpibInstrument instr, out GpibCommand cmd);
            if (res == null) return true;
            int retry = TimeoutRetry;
            while ((retry-- > 0) && !(Cancel?.IsCancellationRequested ?? false))
            {
                if (res.Value)
                {
                    if (!ctrl.SelectInstrument(instr, Cancel, out bool delay)) continue;
                    if (delay) Thread.Sleep(DefaultCommandInterval);
                }
                if (!ctrl.Send(cmd)) continue;
                ctrl.Wait(Cancel);
                return true;
            }
            return false;
        }

        private bool? ParseCommand(string s,
            Dictionary<string, GpibController> controllers, 
            Dictionary<string, GpibInstrumentCommandSet> instruments, 
            out GpibController ctrl, out GpibInstrument instr, out GpibCommand cmd)
        {
            string[] split = s.Split(GpibScript.DevicePathDelimeter);
            if (split.Length < 2)
            {
                ctrl = null;
                instr = null;
                cmd = null;
                return null;
            };
            ctrl = controllers[split[0]];
            if (split.Length > 2)
            {
                instr = ctrl.GetInstrument(split[1]);
                cmd = instruments[instr.CommandSetName][split[2]];
                return true;
            }
            else
            {
                instr = null;
                cmd = ctrl[split[1]];
                return false;
            }
        }

        #endregion
    }
}
