using System;
using System.Reflection;
using LLibrary;

namespace GPIBServer
{
    public class Program
    {
        public enum ExitCodes
        {
            OK,
            FatalInternalError,
            FailedToLoadConfiguration,
            FailedToDeserializeObjects
        }

        static int Main(string[] args)
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
            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
                return ExitCodes.FailedToLoadConfiguration;
            }
            //Deserialize objects
            try
            {

            }
            catch (Exception ex)
            {
                Logger.Fatal(ex);
                return ExitCodes.FailedToDeserializeObjects;
            }

            return ExitCodes.OK;
        }
    }
}
