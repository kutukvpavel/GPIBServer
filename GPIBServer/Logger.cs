using LLibrary;
using System;
using System.IO;

namespace GPIBServer
{
    public static class Logger //TODO: rewrite using a blocking queue
    {
        static Logger()
        {
            try
            {
                string path = Path.Combine(Environment.CurrentDirectory, @"\log");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                path = Path.Combine(path, $@"\terminal_{DateTime.Now:yyyy-MM-dd}.log");
                _TerminalStream = new StreamWriter(path, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _Instance.Error(ex);
                _TerminalStream = null;
            }
        }

        private readonly static L _Instance = new L();
        private readonly static TextWriter _TerminalStream;
        

        public static void Fatal(Exception ex)
        {
            Console.WriteLine(ex);
            _Instance.Fatal(ex);
        }

        public static void Write(string msg)
        {
            Console.WriteLine(msg);
            _Instance.Info(msg);
        }

        public static void Write(object sender, ExceptionEventArgs e)
        {
            Console.WriteLine(e.Exception);
            _Instance.Error(e.Exception);
            _Instance.Info($"Data from object {sender.GetType()} for previous exception: {e.Data ?? "null"}");
        }

        public static void Terminal(string msg)
        {
            if (_TerminalStream == null) return;
            try
            {
                Console.WriteLine(msg);
                _TerminalStream.WriteLine(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _Instance.Error(ex);
            }
        }
    }
}
