﻿using System;
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
            FailedToExecuteScript,
            FailedToSaveConfiguration,
            Canceled
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
                try
                {
                    if (ret == ExitCodes.OK) Configuration.SaveConfiguration(Configuration.Instance);
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex);
                    ret = ExitCodes.FailedToSaveConfiguration;
                }
                return (int)ret;
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
                return (int)ExitCodes.FatalInternalError;
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
            string p = Path.Combine(Environment.CurrentDirectory, "example_{0}.json");
            Serializer.Serialize(ctrl, string.Format(p, "controller"));
            Serializer.Serialize(ics, string.Format(p, "instrument"));
            return ExitCodes.OK;
        }

        private static ExitCodes MainHelper(string[] args)
        {
            //Load settings
            try
            {
                Configuration.LoadConfiguration();
                if (args.Length > 0 && args[0].Length > 0) Configuration.Instance.ScriptName = args[0];
                GpibThread.DevicePathDelimeter = Configuration.Instance.ScriptDevicePathDelimeter;
                GpibThread.ControllerPollInterval = Configuration.Instance.ControllerPollInterval;
                GpibThread.DelayCommandPrefix = Configuration.Instance.DelayCommandPrefix;
                GpibController.ControllerPollInterval = Configuration.Instance.ControllerPollInterval;
                Output.Separation = Configuration.Instance.OutputSeparation;
                Output.Path = Configuration.Instance.GetFullyQualifiedOutputPath();
                Output.LineFormat = Configuration.Instance.OutputLineFormat;
                Output.SeparationLabelFormat = Configuration.Instance.OutputSeparationLabelFormat;
                Output.Retries = Configuration.Instance.OutputRetries;
                Output.RetryDelayMilliseconds = Configuration.Instance.OutputRetryDelayMilliseconds;
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
                Output.ErrorOccurred += ErrorMessageSink;
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
                }
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
                return ExitCodes.FailedToInitializeObjects;
            }
            //Execute script
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

        private static void ErrorMessageSink(object sender, ExceptionEventArgs e)
        {
            Logger.Write(sender, e);
        }
    }
}
