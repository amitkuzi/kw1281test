using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace BitFab.KW1281Test.Interface
{
    public class GenericInterface : IInterface
    {
        private readonly TimeSpan DefaultTimeOut = TimeSpan.FromSeconds(4);
        private readonly Queue<byte> _queue = new Queue<byte>();
        public GenericInterface(string portName, int baudRate)
        {
            _port = new SerialPort(portName)
            {
                BaudRate = baudRate,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                RtsEnable = false,
                DtrEnable = true,
                ReadTimeout = (int)DefaultTimeOut.TotalMilliseconds,
                WriteTimeout = (int)DefaultTimeOut.TotalMilliseconds
            };

            _port.Open();

            _port.DataReceived += _port_DataReceived;
            _port.ErrorReceived += _port_ErrorReceived;
            _port.PinChanged += _port_PinChanged;
        }

        private void _port_PinChanged(object sender, SerialPinChangedEventArgs e)
        {
            Log.WriteLine($"_port_PinChanged {e.EventType}");
        }

        private void _port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Log.WriteLine($"_port_ErrorReceived {e.EventType}");
        }

        private void _port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Log.WriteLine($"_port_DataReceived {e.EventType}");

            var byteToRead = _port.BytesToRead;
            var readBuff = new byte[byteToRead];
            var read = _port.Read(readBuff, 0, byteToRead);
            if (read != byteToRead) Log.WriteLine("read wrong amount ");
            readBuff.ToList().ForEach(b => _queue.Enqueue(b));
            Log.WriteLine($"_port_DataReceived  _queue = {String.Join(" ", _queue):X2}");
        }

        public void Dispose()
        {
            SetDtr(false);
            _port.Close();
        }

        public byte ReadByte()
        {
            byte b;
            Log.WriteLine($"_ReadByte  _queue = {String.Join(" ", _queue)}");
            if (_queue.Any())
            {
                b = _queue.Dequeue();
            }
            else
            {
                b = (byte)_port.ReadByte();
            }
            Log.WriteLine($"_ReadByte =  {b:X2}");
            return b;
        }

        public void WriteByteRaw(byte b)
        {
            Thread.Sleep(5);
            _buf[0] = b;
            _port.Write(_buf, 0, 1);
            Log.WriteLine($"WriteByteRaw =  {b:X2}");
        }

        public void SetBreak(bool on)
        {
            _port.BreakState = on;
            Log.WriteLine($"_port.BreakState =  {on}");
        }

        public void ClearReceiveBuffer()
        {
            _port.DiscardInBuffer();
        }

        public void SetBaudRate(int baudRate)
        {
            _port.BaudRate = baudRate;
        }

        public void SetDtr(bool on)
        {
            _port.DtrEnable = on;
        }

        public void SetRts(bool on)
        {
            _port.RtsEnable = on;
        }

        private readonly SerialPort _port;

        private readonly byte[] _buf = new byte[1];
    }
}
