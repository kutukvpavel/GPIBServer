using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using RJCP.IO.Ports;
using System.Timers;
using System.Text;

namespace GPIBServer
{
    public class GpibController : CommandSetBase, IDisposable
    {
        public event EventHandler<GpibResponseEventArgs> ResponseReceived;
        public event EventHandler<GpibCommandEventArgs> CommandTimeout;

        public static int ControllerPollInterval { get; set; }

        public GpibController()
        {
            SynchronizingObject = new object();
            _ResponseBuilder = new StringBuilder(16);
            _ResponseTimer = new Timer() { AutoReset = false, Enabled = false };
            _ResponseTimer.Elapsed += ResponseTimer_Elapsed;
        }

        #region Properties

        public string Name { get; set; }
        public string EndOfReceive { get; set; }
        public string AddressSelectCommandName { get; set; }
        public string AddressQueryCommandName { get; set; }
        public GpibInstrument[] InstrumentSet { get; set; }
        public SerialPortConfiguration PortConfiguration { get; set; }

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
                    PortConfiguration.Parity, PortConfiguration.StopBits);
                SerialPort.ErrorReceived += SerialPort_ErrorReceived;
                SerialPort.DataReceived += SerialPort_DataReceived;
                SerialPort.Open();
                return true;
            }
            catch (Exception ex)
            {
                RaiseError(ex);
                return false;
            }
        }

        public bool Send(GpibCommand cmd)
        {
            try
            {
                if (IsBusy) return false;
                if (!(SerialPort?.IsOpen ?? false)) throw new InvalidOperationException("Port is closed.");
                lock (SynchronizingObject)
                {
                    _ResponseTimer.Interval = cmd.TimeoutMilliseconds;
                    LastCommand = cmd;
                    SerialPort.Write(cmd.CommandString);
                    if (cmd.AwaitResponse) _ResponseTimer.Start();
                }
                return true;
            }
            catch (Exception ex)
            {
                RaiseError(ex, cmd.CommandString);
                return false;
            }
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

        #endregion

        #region Private

        private readonly Timer _ResponseTimer;
        private readonly StringBuilder _ResponseBuilder;

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
                RaiseError(ex, resp);
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
                    _ResponseBuilder.Append(newData);
                    if (newData.EndsWith(EndOfReceive))
                    {
                        _ResponseTimer.Stop();
                        string resp = _ResponseBuilder.ToString()
                            .Remove(_ResponseBuilder.Length - EndOfReceive.Length, EndOfReceive.Length);
                        _ResponseBuilder.Clear();
                        resp = ParseResponse(resp);
                        ResponseReceived?.BeginInvoke(this, 
                            new GpibResponseEventArgs(LastCommand, LastInstrument, resp), null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                RaiseError(ex);
            }
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            if (e.EventType == SerialError.NoError) return;
            ClearResponseBuffer();
            RaiseError(new Exception("Serial port error."), Enum.GetName(typeof(SerialError), e.EventType));
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
