using LLibrary;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace GPIBServer
{
    public static class Logger //TODO: Separate terminal output for different controllers
    {
        public static void InitializeTerminal(CancellationToken token)
        {
            try
            {
                string path = Path.Combine(Environment.CurrentDirectory, @"\log");
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                path = Path.Combine(path, $@"\terminal_{DateTime.Now:yyyy-MM-dd}.log");
                _Source = new CancellationTokenSource();
                token.Register(() => _Source.Cancel());
                _TerminalStream = new StreamWriter(path, true);
                _TerminalQueue = new BlockingCollection<string>();
                _TerminalThread = new Thread(TerminalProcess);
                _TerminalThread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                _Instance.Error(ex);
                if (_Source != null) _Source.Cancel();
                _TerminalStream = null;
            }
        }

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
            _TerminalQueue.Add(msg);
        }

        #region Private

        private readonly static L _Instance = new L();
        private static TextWriter _TerminalStream;
        private static BlockingCollection<string> _TerminalQueue;
        private static Thread _TerminalThread;
        private static CancellationTokenSource _Source;

        private static void TerminalProcess()
        {
            while (!_Source.IsCancellationRequested)
            {
                try
                {
                    string msg = _TerminalQueue.Take(_Source.Token);
                    Console.WriteLine(msg);
                    _TerminalStream.WriteLine(msg);
                }
                catch (OperationCanceledException)
                { }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    _Instance.Error(ex);
                }
            }
        }

        #endregion
    }
}
