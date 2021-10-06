using RJCP.IO.Ports;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;
using ElapsedEventArgs = System.Timers.ElapsedEventArgs;

namespace GPIBServer
{
    public class GpibController : CommandSetBase, IDisposable
    {
        public event EventHandler<GpibResponseEventArgs> ResponseReceived;
        public event EventHandler<GpibCommandEventArgs> CommandTimeout;
        public event EventHandler<string> LogTerminal;

        public static int ControllerPollInterval { get; set; }

        public GpibController()
        {
            SynchronizingObject = new object();
            _ResponseBuilder = new StringBuilder(16);
            _ResponseTimer = new Timer() { AutoReset = false, Enabled = false };
            _ResponseTimer.Elapsed += ResponseTimer_Elapsed;
        }

        #region Properties

        public string Name { get; set; } = "Example";
        public string EndOfReceive { get; set; } = "\r\n";
        public bool PermissiveEndOfReceive { get; set; } = true;
        public string EndOfSend { get; set; } = "\r\n";
        public string AddressSelectCommandName { get; set; } = "select";
        public string AddressQueryCommandName { get; set; } = "query";
        public SerialPortConfiguration PortConfiguration { get; set; } = new SerialPortConfiguration();
        public GpibInstrument[] InstrumentSet { get; set; }

        [JsonIgnore]
        public GpibResponseEventArgs LastResponse { get; private set; }
        [JsonIgnore]
        public SerialPortStream SerialPort { get; private set; }
        [JsonIgnore]
        public GpibCommand LastCommand { get; private set; }
        [JsonIgnore]
        public GpibInstrument LastInstrument { get; private set; }
        [JsonIgnore]
        public object SynchronizingObject { get; }
        [JsonIgnore]
        public bool IsBusy { get => _ResponseTimer.Enabled; }

        #endregion

        #region Public Methods

        public void Initialize()
        {
            _Instruments = InstrumentSet.ToDictionary(x => x.Name);
            if (PermissiveEndOfReceive) _Trim = EndOfReceive.ToCharArray();
        }

        public GpibInstrument GetInstrument(string name)
        {
            return _Instruments[name];
        }

        public bool Connect()
        {
            try
            {
                if (SerialPort != null)
                {
                    if (SerialPort.IsOpen)
                    {
                        DiscardBuffers();
                        SerialPort.Close();
                    }
                    SerialPort.DataReceived -= SerialPort_DataReceived;
                    SerialPort.ErrorReceived -= SerialPort_ErrorReceived;
                }
                SerialPort = new SerialPortStream(PortConfiguration.Name,
                    PortConfiguration.BaudRate, PortConfiguration.DataBits,
                    PortConfiguration.Parity, PortConfiguration.StopBits)
                {
                    WriteTimeout = PortConfiguration.SendTimeout
                };
                SerialPort.ErrorReceived += SerialPort_ErrorReceived;
                SerialPort.DataReceived += SerialPort_DataReceived;
                SerialPort.Open();
                return true;
            }
            catch (Exception ex)
            {
                RaiseError(this, ex);
                return false;
            }
        }

        public bool Send(GpibCommand cmd)
        {
            try
            {
                if (SendReturnHelper()) return false;
                string c = cmd.CommandString + EndOfSend;
                lock (SynchronizingObject)
                {
                    _ResponseTimer.Interval = cmd.TimeoutMilliseconds;
                    LastCommand = cmd;
                    LastResponse = null;
                    try
                    {
                        SerialPort.Write(c);
                        if (cmd.CommandString.Length * 2 < SerialPort.WriteBufferSize) SerialPort.Flush();
                    }
                    catch (TimeoutException)
                    {
                        return false;
                    }
                    if (cmd.AwaitResponse) _ResponseTimer.Start();
                }
                Task.Run(() => LogTerminal?.Invoke(this, cmd.CommandString));
                return true;
            }
            catch (Exception ex)
            {
                RaiseError(this, ex, cmd.CommandString);
                return false;
            }
        }

        public bool SelectInstrument(GpibInstrument instrument, CancellationToken? token, out bool performed)
        {
            performed = false;
            try
            {
                if (SendReturnHelper()) return false;
                if ((LastInstrument?.Address ?? -1) == instrument.Address) return true;
                performed = true;
                string addr = instrument.Address.ToString();
                var selCmd = this[AddressSelectCommandName].PutInParameters(addr);
                if (!Send(selCmd)) return false;
                Wait(token);
                if ((AddressQueryCommandName?.Length ?? 0) > 0)
                {
                    selCmd = this[AddressQueryCommandName].PutInParameters(addr);
                    if (!Send(selCmd)) return false;
                    Wait(token);
                }
                if (LastCommand.ExpectedResponse == null || LastResponse.Response == LastCommand.ExpectedResponse)
                {
                    LastInstrument = instrument;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                RaiseError(this, ex, instrument.Name);
                return false;
            }
        }
        public bool SelectInstrument(GpibInstrument instrument, CancellationToken? token)
        {
            return SelectInstrument(instrument, token, out _);
        }

        public void Dispose()
        {
            if (SerialPort?.IsOpen ?? false) SerialPort.Close();
            if (!(SerialPort?.IsDisposed ?? true))
            {
                SerialPort.Dispose();
                SerialPort = null;
            }
        }

        public void Wait(CancellationToken? token)
        {
            while (IsBusy && !(token?.IsCancellationRequested ?? false)) Thread.Sleep(ControllerPollInterval);
        }

        #endregion

        #region Private

        private readonly Timer _ResponseTimer;
        private readonly StringBuilder _ResponseBuilder;
        private Dictionary<string, GpibInstrument> _Instruments;
        private char[] _Trim;

        private bool SendReturnHelper()
        {
            if (IsBusy) return true;
            if (!(SerialPort?.IsOpen ?? false)) throw new InvalidOperationException("Port is closed.");
            return false;
        }

        private void DiscardBuffers()
        {
            SerialPort.DiscardOutBuffer();
            SerialPort.DiscardInBuffer();
        }

        private string ParseResponse(string resp)
        {
            try
            {
                return resp.Remove(0, LastCommand.ResponsePrefixLength);
            }
            catch (Exception ex)
            {
                RaiseError(this, ex, resp);
                return null;
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (e.EventType == SerialData.NoData) return;
                if (SerialPort.BytesToRead == 0) return;
                lock (SynchronizingObject)
                {
                    string newData = SerialPort.ReadExisting();
                    Task.Run(() => LogTerminal?.Invoke(this, newData));
                    _ResponseBuilder.Append(newData);
                    if (PermissiveEndOfReceive ? 
                        newData.EndsWith(EndOfReceive[EndOfReceive.Length - 1]) :
                        newData.EndsWith(EndOfReceive))
                    {
                        _ResponseTimer.Stop();
                        string resp = _ResponseBuilder.ToString();
                        resp = PermissiveEndOfReceive ?
                            resp.TrimEnd(_Trim) :
                            resp.Remove(_ResponseBuilder.Length - EndOfReceive.Length, EndOfReceive.Length);
                        _ResponseBuilder.Clear();
                        if (LastCommand == null) return;
                        resp = ParseResponse(resp);
                        LastResponse = new GpibResponseEventArgs(LastCommand, LastInstrument, resp);
                        LastCommand = null;                                        //TODO: RECHECK!!
                        Task.Run(() => ResponseReceived?.Invoke(this, LastResponse));
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError(this, ex);
            }
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            if (e.EventType == SerialError.NoError) return;
            ClearResponseBuffer();
            RaiseError(this, new Exception("Serial port error."), Enum.GetName(typeof(SerialError), e.EventType));
        }

        private void ClearResponseBuffer()
        {
            lock (SynchronizingObject)
            {
                DiscardBuffers();
                _ResponseBuilder.Clear();
            }
        }

        private void ResponseTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            CommandTimeout?.Invoke(this, new GpibCommandEventArgs(LastCommand, LastInstrument));
            ClearResponseBuffer();
        }

        #endregion
    }
}
