using System;
using System.Collections.Generic;
using System.IO;

namespace GPIBServer
{
    public class Configuration
    {
        public Configuration()
        { }
        public Configuration(bool init)
        {
            if (!init) return;
            ScriptsFolder = @".\scripts";
            ScriptsFilter = "*.json";
            OutputFilePath = @".\output\{{0}}{0:yyyy-MM-dd_HH-mm-ss}.csv";
            InstrumentsFolder = @".\instruments";
            InstrumentsFilter = "*.json";
            ControllersFolder = @".\controllers";
            ControllersFilter = "*.json";
            ScriptName = "ExampleScript";
            ScriptDevicePathDelimeter = ".";
            ControllerPollInterval = 10;
            DelayCommandPrefix = "delay=";
            VariablePrefix = "var:";
            OutputLineFormat = "{0:yyyy-MM-dd HH:mm:ss};{1};{2};{3};{4}";
            OutputSeparation = OutputSeparation.InstrumentName;
            OutputSeparationLabelFormat = "{0}_";
            OutputRetries = 3;
            OutputRetryDelayMilliseconds = 300;
            PipeName = "GPIBServer_Broadcast_Pipe";
            FlushEachOutputLineImmediately = true;
        }

        public string ScriptsFilter { get; set; }
        public string ScriptsFolder { get; set; }
        public string OutputFilePath { get; set; }
        public string InstrumentsFolder { get; set; }
        public string InstrumentsFilter { get; set; }
        public string ControllersFolder { get; set; }
        public string ControllersFilter { get; set; }
        public string ScriptName { get; set; }
        public string ScriptDevicePathDelimeter { get; set; }
        public int ControllerPollInterval { get; set; } //mS
        public string DelayCommandPrefix { get; set; }
        public string VariablePrefix { get; set; }
        public string OutputLineFormat { get; set; }
        public OutputSeparation OutputSeparation { get; set; }
        public string OutputSeparationLabelFormat { get; set; }
        public int OutputRetries { get; set; }
        public int OutputRetryDelayMilliseconds { get; set; }
        public string PipeName { get; set; }
        public bool FlushEachOutputLineImmediately { get; set; }

        public string GetFullyQualifiedLogPath()
        {
            string path = Path.Combine(Environment.CurrentDirectory, "logs");
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return Path.Combine(path, $@"terminal_{{0}}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
        }
        public string GetFullyQualifiedOutputPath()
        {
            return GetFullyQualifiedPath(string.Format(OutputFilePath, DateTime.Now));
        }
        public IEnumerable<string> GetScriptFiles()
        {
            return Directory.EnumerateFiles(GetFullyQualifiedPath(ScriptsFolder), ScriptsFilter);
        }
        public IEnumerable<string> GetInstrumentFiles()
        {
            return Directory.EnumerateFiles(GetFullyQualifiedPath(InstrumentsFolder), InstrumentsFilter);
        }
        public IEnumerable<string> GetControllerFiles()
        {
            return Directory.EnumerateFiles(GetFullyQualifiedPath(ControllersFolder), ControllersFilter);
        }

        #region Static

        public static Configuration Instance { get; private set; } = new Configuration(true);

        public static string ConfigurationFileName { get; set; } = "configuration.json";

        public static void SaveConfiguration(Configuration cfg)
        {
            string path = Path.Combine(Environment.CurrentDirectory, ConfigurationFileName);
            Serializer.Serialize(cfg, path);
        }

        public static void LoadConfiguration()
        {
            string path = Path.Combine(Environment.CurrentDirectory, ConfigurationFileName);
            Instance = Serializer.Deserialize(Instance, path);
        }

        private static string GetFullyQualifiedPath(string probablyRelativePath)
        {
            if ((probablyRelativePath?.Length ?? 0) == 0) return null;
            if (Path.IsPathFullyQualified(probablyRelativePath)) return probablyRelativePath;
            string workingDirWithSlash = Environment.CurrentDirectory;
            if (!workingDirWithSlash.EndsWith(Path.DirectorySeparatorChar))
                workingDirWithSlash += Path.DirectorySeparatorChar;
            return Path.GetFullPath(workingDirWithSlash + probablyRelativePath);
        }

        #endregion
    }
}
