using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace GPIBServer
{
    public class GpibScript : ErrorReporterBase
    {
        #region Static

        public static string DevicePathDelimeter { get; set; }
        public static int ControllerPollInterval { get; set; }
        public static string DelayCommandPrefix { get; set; }

        #endregion

        public GpibScript()
        { }

        #region Properties

        public string[] Commands { get; set; }
        public string Name { get; set; }
        public int TimeoutRetry { get; set; }
        public int LoopIndex { get; set; }
        public int LoopCount { get; set; }
        public int DefaultCommandInterval { get; set; }

        [JsonIgnore]
        public GpibInstrument LastInstrument { get; set; }

        #endregion

        #region Public Methods

        public bool Execute(Dictionary<string, GpibController> controllers, Dictionary<string, GpibInstrumentCommandSet> instruments)
        {
            int loop = LoopCount;
            bool initialized = false;
            while (loop < 0 || loop-- > 0)
            {
                for (int i = initialized ? LoopIndex : 0; i < Commands.Length; i++)
                {
                    string item = Commands[i];
                    try
                    {
                        if (item.StartsWith(DelayCommandPrefix))
                        {
                            System.Threading.Thread.Sleep(int.Parse(item.Remove(0, DelayCommandPrefix.Length)));
                        }
                        else
                        {
                            var cmd = GetCommand(item, controllers, instruments, out GpibController ctrl);
                            int retry = TimeoutRetry;
                            while (retry-- > 0)
                            {
                                if (!ctrl.Send(cmd)) continue;
                                while (ctrl.IsBusy) System.Threading.Thread.Sleep(10);

                            }
                        }
                    }
                    catch (Exception ex) when (ex is NullReferenceException || ex is KeyNotFoundException)
                    {
                        RaiseError(ex, item);
                    }
                }
                initialized = true;
            }
            return true;
        }

        #endregion

        #region Private

        private GpibCommand GetCommand(string s,
            Dictionary<string, GpibController> controllers, 
            Dictionary<string, GpibInstrumentCommandSet> instruments, 
            out GpibController c)
        {
            c = null;
            string[] split = s.Split(DevicePathDelimeter);
            if (split.Length < 2) return null;
            c = controllers[split[0]];
            GpibCommand res;
            if (split.Length > 2)
            {
                res = instruments[split[1]][split[2]];
            }
            else
            {
                res = c[split[1]];
            }
            return res;
        }

        #endregion
    }
}
