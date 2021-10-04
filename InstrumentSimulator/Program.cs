using RJCP.IO.Ports;
using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Collections.Generic;

namespace InstrumentSimulator
{
    class Program
    {
        static Configuration Config;
        static SerialPortStream Port;
        static readonly CancellationTokenSource Cancel = new CancellationTokenSource();

        static JsonSerializerOptions SerializerOptions = new JsonSerializerOptions() 
        { 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        static void Main(string[] args)
        {
            //Init
            Console.CancelKeyPress += Console_CancelKeyPress;
            if (LoadInstruments()) return;
            int baud;
            try
            {
                baud = int.Parse(args[1]);
            }
            catch (IndexOutOfRangeException)
            {
                baud = 115200;
            }
            Port = new SerialPortStream(args[0], baud) 
            { 
                Handshake = Handshake.None,
                ReadTimeout = 1000,
                NewLine = Config.NewLine
            };
            Port.Open();

            //Main loop
            StringBuilder buffer = new StringBuilder();
            Instrument selected = null;
            while (!Cancel.IsCancellationRequested)
            {
                try
                {
                    string l = Port.ReadLine();
                    Console.WriteLine(l);
                    string reply = null;
                    if (l.StartsWith(Config.AddressSelectPrefix))
                    {
                        int addr = int.Parse(l.Remove(0, Config.AddressSelectPrefix.Length));
                        selected = null;
                        foreach (var item in Config.Instruments)
                        {
                            if (item.Address == addr)
                            {
                                selected = item;
                                string msg = null;
                                try
                                {
                                    msg = $"Selected = {selected.ReplyTable[Config.ScpiIdCommand]}";
                                }
                                catch (KeyNotFoundException)
                                {
                                    msg = $"Selected {addr}";
                                }
                                Console.WriteLine(msg);
                            }
                        }
                    }
                    else if (l.StartsWith(Config.ControllerPrefix))
                    {
                        if (Config.Controller.ReplyTable.ContainsKey(l)) 
                            reply = Config.Controller.ReplyTable[l];
                    }
                    else
                    {
                        if (selected != null && selected.ReplyTable.ContainsKey(l)) reply = selected.ReplyTable[l];
                    }
                    if (reply != null)
                    {
                        Console.WriteLine(reply);
                        Port.WriteLine(reply);
                    }
                }
                catch (TimeoutException)
                {

                }
                Thread.Sleep(100);
            }

            //Cleanup
            Port.Close();
            Port.Dispose();
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            if (!Cancel.IsCancellationRequested) Cancel.Cancel();
            e.Cancel = true;
        }

        private static bool LoadInstruments()
        {
            string f = Path.Combine(Environment.CurrentDirectory, "instruments.json");
            try
            {
                Config = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(f), SerializerOptions);
                return false;
            }
            catch (FileNotFoundException)
            {
                Config = new Configuration();
                string json = JsonSerializer.Serialize(Config, SerializerOptions);
                File.WriteAllText(f, json);
                Console.WriteLine("Example instruments file was created. Exiting...");
                return true;
            }
        }
    }
}
