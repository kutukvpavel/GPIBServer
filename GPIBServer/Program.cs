using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

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
            FailedToConnectToControllers,
            FailedToExecuteScript,
            FailedToSaveConfiguration,
            Canceled,
            InvalidScript
        }

        public static Dictionary<string, GpibController> Controllers { get; private set; }
        public static Dictionary<string, GpibInstrumentCommandSet> Instruments { get; private set; }
        public static GpibScript Script { get; private set; }
        public static CancellationTokenSource Cancel { get; private set; } = new CancellationTokenSource();

        private static int Main(string[] args)
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
            try
            {
                string name = Assembly.GetExecutingAssembly().GetName().ToString();
                Logger.Write(name);
                ExitCodes ret = (args.Length > 0 && args[0] == "-g") ? GenerateExampleJson() : MainHelper(args);
                Console.WriteLine("Saving configuration...");
                try
                {
                    if (ret == ExitCodes.OK) Configuration.SaveConfiguration(Configuration.Instance);
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex);
                    ret = ExitCodes.FailedToSaveConfiguration;
                }
                Console.WriteLine("Exiting.");
                return (int)ret;
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
                return (int)ExitCodes.FatalInternalError;
            }
            finally
            {
                try
                {
                    if (!Cancel.IsCancellationRequested) Cancel.Cancel();
                    Output.Dispose();
                }
                catch (Exception)
                { }
            }
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (!Cancel.IsCancellationRequested) Cancel.Cancel();
            e.Cancel = true;
        }

        private static ExitCodes GenerateExampleJson()
        {
            var ctrl = new GpibController() 
            { 
                CommandSet = new GpibCommand[] { new GpibCommand() },
                InstrumentSet = new GpibInstrument[] { new GpibInstrument() }
            };
            var ics = new GpibInstrumentCommandSet() { CommandSet = ctrl.CommandSet };
            var sc = new GpibScript() { Threads = new GpibThread[] { new GpibThread() } };
            Serializer.Serialize(ctrl, Path.Combine(
                Environment.CurrentDirectory, Configuration.Instance.ControllersFolder, "controller.json"));
            Serializer.Serialize(ics, Path.Combine(
                Environment.CurrentDirectory, Configuration.Instance.ControllersFolder, "instrument.json"));
            Serializer.Serialize(sc, Path.Combine(
                Environment.CurrentDirectory, Configuration.Instance.ControllersFolder, "script.json"));
            return ExitCodes.OK;
        }

        private static ExitCodes MainHelper(string[] args)
        {
            //Load settings
            Console.WriteLine("Loading configuration...");
            try
            {
                Configuration.LoadConfiguration();
                if (args.Length > 0 && args[0].Length > 0) Configuration.Instance.ScriptName = args[0];
                GpibScript.DevicePathDelimeter = Configuration.Instance.ScriptDevicePathDelimeter;
                GpibScript.ControllerPollInterval = Configuration.Instance.ControllerPollInterval;
                GpibScript.DelayCommandPrefix = Configuration.Instance.DelayCommandPrefix;
                GpibScript.VariablePrefix = Configuration.Instance.VariablePrefix;
                GpibController.ControllerPollInterval = Configuration.Instance.ControllerPollInterval;
                Output.Separation = Configuration.Instance.OutputSeparation;
                Output.LineFormat = Configuration.Instance.OutputLineFormat;
                Output.SeparationLabelFormat = Configuration.Instance.OutputSeparationLabelFormat;
                Output.Retries = Configuration.Instance.OutputRetries;
                Output.RetryDelayMilliseconds = Configuration.Instance.OutputRetryDelayMilliseconds;
                Output.FlushEachLine = Configuration.Instance.FlushEachOutputLineImmediately;
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
            Console.WriteLine("Initializing objects...");
            try
            {
                if (!Script.ValidateNames())
                {
                    Logger.Write("Invalid script (probably duplicate thread names).");
                    return ExitCodes.InvalidScript;
                }
                Output.ErrorOccurred += ErrorMessageSink;
                Output.Initialize(Configuration.Instance.GetFullyQualifiedOutputPath(),
                    Configuration.Instance.GetFullyQualifiedLogPath(),
                    Configuration.Instance.PipeName,
                    Cancel.Token);
                Script.ErrorOccured += ErrorMessageSink;
                Serializer.ErrorOccured += ErrorMessageSink;
                foreach (var item in Instruments)
                {
                    item.Value.InitializeCommandSet();
                    item.Value.ErrorOccured += ErrorMessageSink;
                }
                foreach (var item in Controllers)
                {
                    item.Value.Initialize();
                    item.Value.InitializeCommandSet();
                    item.Value.ErrorOccured += ErrorMessageSink;
                    item.Value.ResponseReceived += Output.QueueData;
                    item.Value.LogTerminal += Output.QueueTerminal;
                    item.Value.CommandTimeout += Controller_CommandTimeout;
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
                return ExitCodes.FailedToInitializeObjects;
            }
            //Connect to required controllers
            Console.WriteLine("Connecting to controllers...");
            try
            {
                foreach (var item in Script.GetRequiredControllerNames())
                {
                    Controllers[item].Connect();
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
                return ExitCodes.FailedToConnectToControllers;
            }
            //Execute script
            Console.WriteLine("Executing script...");
            try
            {
                if (!Script.Execute(Controllers, Instruments, Cancel.Token))
                    return Cancel.IsCancellationRequested ? ExitCodes.Canceled : ExitCodes.FailedToExecuteScript;
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
                return ExitCodes.FailedToExecuteScript;
            }
            return ExitCodes.OK;
        }

        private static void Controller_CommandTimeout(object sender, GpibCommandEventArgs e)
        {
            Logger.Write(
                $"Command timeout: {(sender as GpibController).Name} -> {e.Instrument.Name} -> '{e.Command.CommandString}'."
                );
        }

        private static void ErrorMessageSink(object sender, ExceptionEventArgs e)
        {
            Logger.Write(sender, e);
        }
    }
}
