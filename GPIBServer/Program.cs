using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace GPIBServer
{
    public static class Program
    {
        public enum ExitCodes
        {
            OK,
            FatalInternalError,
            FailedToLoadConfiguration,
            FailedToDeserializeObjects,
            FailedToInitializeObjects,
            FailedToExecuteScript
        }

        public static Dictionary<string, GpibController> Controllers { get; private set; }
        public static Dictionary<string, GpibInstrumentCommandSet> Instruments { get; private set; }
        public static GpibScript Script { get; private set; }

        private static int Main(string[] args)
        {
            try
            {
                string name = Assembly.GetExecutingAssembly().GetName().ToString();
                Console.WriteLine(name);
                Logger.Write(name);
                return (int)MainHelper(args);
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
                return (int)ExitCodes.FatalInternalError;
            }
        }

        private static ExitCodes MainHelper(string[] args)
        {
            //Load settings
            try
            {
                Configuration.LoadConfiguration();
                if (args.Length > 0 && args[0].Length > 0) Configuration.Instance.ScriptName = args[0];
                GpibScript.DevicePathDelimeter = Configuration.Instance.ScriptDevicePathDelimeter;
                GpibScript.ControllerPollInterval = Configuration.Instance.ControllerPollInterval;
                GpibScript.DelayCommandPrefix = Configuration.Instance.DelayCommandPrefix;
                GpibController.ControllerPollInterval = Configuration.Instance.ControllerPollInterval;
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
                return ExitCodes.FailedToLoadConfiguration;
            }
            //Deserialize objects
            try
            {
                Instruments = Configuration.Instance.GetInstrumentFiles()
                    .Select(x => Serializer.Deserialize<GpibInstrumentCommandSet>(null, x))
                    .Where(x => x != null).ToDictionary(x => x.Name);
                Controllers = Configuration.Instance.GetControllerFiles()
                    .Select(x => Serializer.Deserialize<GpibController>(null, x))
                    .Where(x => x != null).ToDictionary(x => x.Name);
                Script = Configuration.Instance.GetScriptFiles().Select(x => Serializer.Deserialize<GpibScript>(null, x))
                    .First(x => x.Name == Configuration.Instance.ScriptName);
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
                return ExitCodes.FailedToDeserializeObjects;
            }
            //Initialize objects
            try
            {
                foreach (var item in Instruments)
                {
                    item.Value.InitializeCommandSet();
                }
                foreach (var item in Controllers)
                {
                    item.Value.InitializeCommandSet();
                }
                Output.Initialize(Configuration.Instance.GetFullyQualifiedOutputPath(), Configuration.Instance.OutputLineFormat);
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
                return ExitCodes.FailedToInitializeObjects;
            }
            //Execute script
            try
            {
                if (!Script.Execute(Controllers, Instruments)) return ExitCodes.FailedToExecuteScript;
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
                return ExitCodes.FailedToExecuteScript;
            }
            return ExitCodes.OK;
        }
    }
}
