using LLibrary;
using System;

namespace GPIBServer
{
    public static class Logger
    {
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
            if (e.Exception != null)
            {
                Console.WriteLine(e.Exception);
                _Instance.Error(e.Exception);
            }
            string info = $"Data from object '{sender?.GetType().Name ?? "static"}' for previous exception: {e.Data ?? "null"}";
            Console.WriteLine(info);
            _Instance.Info(info);
        }

        #region Private

        private readonly static L _Instance = new L();

        #endregion
    }
}
