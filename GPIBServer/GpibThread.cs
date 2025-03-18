using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Serialization;
using System.Threading;
using Org.MathEval;

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
            Dictionary<string, double> variables = new Dictionary<string, double>();
            while ((loop < 0 || loop-- > 0) && !(Cancel?.IsCancellationRequested ?? false))
            {
                int i;
                for (i = initialized ? LoopIndex : 0; (i < Commands.Length) && !(Cancel?.IsCancellationRequested ?? false); i++)
                {
                    string item = Commands[i];
                    bool sleep = true;
                    try
                    {
                        if (item.StartsWith(GpibScript.DelayCommandPrefix))
                        {
                            Thread.Sleep(int.Parse(item.Remove(0, GpibScript.DelayCommandPrefix.Length)));
                            sleep = false;
                        }
                        else if (item.StartsWith(GpibScript.VariablePrefix))
                        {
                            string[] strValue = item.Remove(0, GpibScript.VariablePrefix.Length).Split('=');
                            if (strValue.Length != 2) throw new KeyNotFoundException($"Invalid variable syntax");
                            var expression = new Expression(strValue[1]);
                            foreach (var variable in variables)
                            {
                                expression.Bind(variable.Key, variable.Value);
                            }
                            double result = expression.Eval<double>();
                            if (!variables.TryAdd(strValue[0], result))
                            {
                                variables[strValue[0]] = result;
                            }
                            sleep = false;
                        }
                        else
                        {
                            string substitutedCommand = item;
                            foreach (var v in variables)
                            {
                                substitutedCommand = substitutedCommand.Replace($"${{{v.Key}}}", v.Value.ToString("G4", CultureInfo.InvariantCulture));
                            }
                            string[] splitCommand = substitutedCommand.TrimEnd(')').Split('(');
                            if (splitCommand.Length == 1)
                            {
                                if (!ExecuteCommand(substitutedCommand, controllers, instruments)) break;
                            }
                            else
                            {
                                string[] splitArguments = splitCommand[1].Split(',');
                                if (!ExecuteCommand(splitCommand[0], controllers, instruments, splitArguments)) break;
                            }
                        }
                    }
                    catch (Exception ex) when (ex is NullReferenceException || ex is KeyNotFoundException || ex is FormatException)
                    {
                        RaiseError(this, ex, item);
                        return false;
                    }
                    if (sleep) Thread.Sleep(DefaultCommandInterval);
                }
                if (i != Commands.Length) return false;
                initialized = true;
            }
            return true;
        }

        #endregion

        #region Private

        private bool ExecuteCommand(string item, 
            Dictionary<string, GpibController> controllers, Dictionary<string, GpibInstrumentCommandSet> instruments, string[] arguments = null)
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
                if (arguments != null) cmd = cmd.PutInParameters(arguments);
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
