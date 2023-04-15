﻿using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace BitFab.KW1281Test.Interface
{
    class FtdiInterface : IInterface 
    {
        private readonly TimeSpan DefaultTimeOut = TimeSpan.FromSeconds(8);

        private readonly FT _ft;
        private IntPtr _handle = IntPtr.Zero;
        private readonly byte[] _buf = new byte[1];

        public FtdiInterface(string serialNumber, int baudRate)
        {
            _ft = new FT();

            var status = _ft.Open(
                serialNumber, FT.OpenExFlags.BySerialNumber, out _handle);
            FT.AssertOk(status);

            status = _ft.SetBaudRate(_handle, (uint)baudRate);
            FT.AssertOk(status);

            status = _ft.SetDataCharacteristics(
                _handle,
                FT.Bits.Eight,
                FT.StopBits.One,
                FT.Parity.None);
            FT.AssertOk(status);

            status = _ft.SetFlowControl(
                _handle,
                FT.FlowControl.None, 0, 0);
            FT.AssertOk(status);

            SetRts(false);
            SetDtr(true);

            status = _ft.SetTimeouts(
                _handle,
                (uint)DefaultTimeOut.TotalMilliseconds,
                (uint)DefaultTimeOut.TotalMilliseconds);
            FT.AssertOk(status);

            // Should allow faster response times for small packets
            status = _ft.SetLatencyTimer(_handle, 2);
            FT.AssertOk(status);
        }

        public void Dispose()
        {
            if (_handle != IntPtr.Zero)
            {
                SetDtr(false);

                var status = _ft.Close(_handle);
                _handle = IntPtr.Zero;
                FT.AssertOk(status);
            }

            _ft.Dispose();
        }

        public byte ReadByte()
        {
            var status = _ft.Read(_handle, _buf, 1, out uint countOfBytesRead);
            FT.AssertOk(status);
            if (countOfBytesRead != 1)
            {
                throw new TimeoutException("Read timed out");
            }

            var b = _buf[0];
            return b;
        }

        /// <summary>
        /// Write a byte to the interface but do not read/discard its echo.
        /// </summary>
        public void WriteByteRaw(byte b)
        {
            _buf[0] = b;
            var status = _ft.Write(_handle, _buf, 1, out uint countOfBytesWritten);
            FT.AssertOk(status);
            if (countOfBytesWritten != 1)
            {
                throw new InvalidOperationException(
                    $"Expected to write 1 byte but wrote {countOfBytesWritten} bytes");
            }
        }

        public void SetBreak(bool on)
        {
            FT.Status status;
            if (on)
            {
                status = _ft.SetBreakOn(_handle);
            }
            else
            {
                status = _ft.SetBreakOff(_handle);
            }
            FT.AssertOk(status);
        }

        public void ClearReceiveBuffer()
        {
            var status = _ft.Purge(_handle, FT.PurgeMask.RX);
            FT.AssertOk(status);
        }

        public void SetBaudRate(int baudRate)
        {
            var status = _ft.SetBaudRate(_handle, (uint)baudRate);
            FT.AssertOk(status);
        }

        public void SetDtr(bool on)
        {
            FT.Status status;
            if (on)
            {
                status = _ft.SetDtr(_handle);
            }
            else
            {
                status = _ft.ClrDtr(_handle);
            }
            FT.AssertOk(status);
        }

        public void SetRts(bool on)
        {
            FT.Status status;
            if (on)
            {
                status = _ft.SetRts(_handle);
            }
            else
            {
                status = _ft.ClrRts(_handle);
            }
            FT.AssertOk(status);
        }
    }

    class FT : IDisposable
    {
        private IntPtr _d2xx = IntPtr.Zero;

        // Delegates used to call into the FTID D2xx DLL
#pragma warning disable CS0649
        private readonly FTDll.SetVidPid _setVidPid;
        private readonly FTDll.OpenBySerialNumber _openBySerialNumber;
        private readonly FTDll.Close _close;
        private readonly FTDll.SetBaudRate _setBaudRate;
        private readonly FTDll.SetDataCharacteristics _setDataCharacteristics;
        private readonly FTDll.SetFlowControl _setFlowControl;
        private readonly FTDll.SetDtr _setDtr;
        private readonly FTDll.ClrDtr _clrDtr;
        private readonly FTDll.SetRts _setRts;
        private readonly FTDll.ClrRts _clrRts;
        private readonly FTDll.SetTimeouts _setTimeouts;
        private readonly FTDll.SetLatencyTimer _setLatencyTimer;
        private readonly FTDll.Purge _purge;
        private readonly FTDll.SetBreakOn _setBreakOn;
        private readonly FTDll.SetBreakOff _setBreakOff;
        private readonly FTDll.Read _read;
        private readonly FTDll.Write _write;
#pragma warning restore CS0649

        public FT()
        {
            string libName;
            bool isMacOs = false;
            bool isLinux = false;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                libName = "libftd2xx.dylib";
                isMacOs = true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                libName = "ftd2xx.so";
                isLinux = true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                libName = Environment.Is64BitProcess ? "ftd2xx64.dll" : "ftd2xx.dll";
            }
            else
            {
                throw new InvalidOperationException($"Unknown OS: {RuntimeInformation.OSDescription}");
            }

            _d2xx = NativeLibrary.Load(
                libName, typeof(FT).Assembly, DllImportSearchPath.SafeDirectories);

            InitDelegate(nameof(_openBySerialNumber), out _openBySerialNumber);
            InitDelegate(nameof(_close), out _close);
            InitDelegate(nameof(_setBaudRate), out _setBaudRate);
            InitDelegate(nameof(_setDataCharacteristics), out _setDataCharacteristics);
            InitDelegate(nameof(_setFlowControl), out _setFlowControl);
            InitDelegate(nameof(_setDtr), out _setDtr);
            InitDelegate(nameof(_clrDtr), out _clrDtr);
            InitDelegate(nameof(_setRts), out _setRts);
            InitDelegate(nameof(_clrRts), out _clrRts);
            InitDelegate(nameof(_setTimeouts), out _setTimeouts);
            InitDelegate(nameof(_setLatencyTimer), out _setLatencyTimer);
            InitDelegate(nameof(_purge), out _purge);
            InitDelegate(nameof(_setBreakOn), out _setBreakOn);
            InitDelegate(nameof(_setBreakOff), out _setBreakOff);
            InitDelegate(nameof(_read), out _read);
            InitDelegate(nameof(_write), out _write);
            if (isMacOs || isLinux)
            {
                InitDelegate(nameof(_setVidPid), out _setVidPid);
            }
            else
            {
                _setVidPid = (uint vid, uint pid) => Status.Ok;
            }

            if (isMacOs || isLinux)
            {
                var vidStr = Environment.GetEnvironmentVariable("FTDI_VID");
                var pidStr = Environment.GetEnvironmentVariable("FTDI_PID");
                if (!string.IsNullOrEmpty(vidStr) && !string.IsNullOrEmpty(pidStr))
                {
                    var vid = Utils.ParseUint(vidStr);
                    var pid = Utils.ParseUint(pidStr);
                    Log.WriteLine($"Setting FTDI VID=0x{vid:X4}, PID=0x{pid:X4}");
                    var status = SetVidPid(vid, pid);
                    AssertOk(status);
                }
            }
        }

        private void InitDelegate<T>(string fieldName, out T delegateVal) where T : Delegate
        {
            var symbolNameAttribute = typeof(T).GetCustomAttribute<SymbolNameAttribute>();
            if (symbolNameAttribute == null)
            {
                throw new InvalidOperationException(
                    $"Type {typeof(T)} is missing required SymbolName attribute.");
            }
            var nativeMethodName = symbolNameAttribute.Name;
            var export = NativeLibrary.GetExport(_d2xx, nativeMethodName);
            delegateVal = Marshal.GetDelegateForFunctionPointer<T>(export);
        }

        public void Dispose()
        {
            if (_d2xx != IntPtr.Zero)
            {
                NativeLibrary.Free(_d2xx);
                _d2xx = IntPtr.Zero;
            }
        }

        public static void AssertOk(FT.Status status)
        {
            if (status != FT.Status.Ok)
            {
                throw new InvalidOperationException(
                    $"D2xx library returned {status} instead of Ok");
            }
        }

        public Status SetVidPid(
            uint vid,
            uint pid)
        {
            return _setVidPid(vid, pid);
        }

        public Status Open(
            string serialNumber,
            OpenExFlags flags,
            out IntPtr handle)
        {
            return _openBySerialNumber(serialNumber, flags, out handle);
        }

        public Status Close(
            IntPtr handle)
        {
            return _close(handle);
        }

        public Status SetBaudRate(
            IntPtr handle,
            uint baudRate)
        {
            return _setBaudRate(handle, baudRate);
        }

        public Status SetDataCharacteristics(
            IntPtr handle,
            Bits wordLength,
            StopBits stopBits,
            Parity parity)
        {
            return _setDataCharacteristics(handle, wordLength, stopBits, parity);
        }

        public Status SetFlowControl(
            IntPtr handle,
            FlowControl flowControl,
            byte xonChar,
            byte xoffChar)
        {
            return _setFlowControl(handle, flowControl, xonChar, xoffChar);
        }

        public Status SetDtr(
            IntPtr handle)
        {
            return _setDtr(handle);
        }

        public Status ClrDtr(
            IntPtr handle)
        {
            return _clrDtr(handle);
        }

        public Status SetRts(
            IntPtr handle)
        {
            return _setRts(handle);
        }

        public Status ClrRts(
            IntPtr handle)
        {
            return _clrRts(handle);
        }

        public Status SetTimeouts(
            IntPtr handle,
            uint readTimeoutMS,
            uint writeTimeoutMS)
        {
            return _setTimeouts(handle, readTimeoutMS, writeTimeoutMS);
        }

        public Status SetLatencyTimer(
            IntPtr handle,
            byte timerMS)
        {
            return _setLatencyTimer(handle, timerMS);
        }

        public Status Purge(
            IntPtr handle,
            PurgeMask mask)
        {
            return _purge(handle, mask);
        }

        public Status SetBreakOn(
            IntPtr handle)
        {
            return _setBreakOn(handle);
        }

        public Status SetBreakOff(
            IntPtr handle)
        {
            return _setBreakOff(handle);
        }

        public Status Read(
            IntPtr handle,
            byte[] buffer,
            uint countOfBytesToRead,
            out uint countOfBytesRead)
        {
            return _read(handle, buffer, countOfBytesToRead, out countOfBytesRead);
        }

        public Status Write(
            IntPtr handle,
            byte[] buffer,
            uint countOfBytesToWrite,
            out uint countOfBytesWritten)
        {
            return _write(handle, buffer, countOfBytesToWrite, out countOfBytesWritten);
        }

        public enum Status : uint
        {
            Ok = 0,
            InvalidHandle,
            DeviceNotFound,
            DeviceNotOpened,
            IOError,
            insufficient_resources,
            InvalidParameter,
            InvalidBaudRate,
            DeviceNotOpenedForErase,
            DeviceNotOpenedForWrite,
            FailedToWriteDevice,
            EepromReadFailed,
            EepromWriteFailed,
            EepromEraseFailed,
            EepromNotPresent,
            EepromNotProgrammed,
            InvalidArgs,
            NotSupported,
            OtherError,
            DeviceListNotReady,
        };

        [Flags]
        public enum OpenExFlags : uint
        {
            BySerialNumber = 1,
            ByDescription = 2,
            ByLocation = 4
        };

        public enum Bits : byte
        {
            Eight = 8,
            Seven = 7
        };

        public enum StopBits : byte
        {
            One = 0,
            Two = 2
        };

        public enum Parity : byte
        {
            None = 0,
            Odd = 1,
            Even = 2,
            Mark = 3,
            Space = 4
        };

        public enum FlowControl : ushort
        {
            None = 0x0000,
            RtsCts = 0x0100,
            DtrDsr = 0x0200,
            XonXoff = 0x0400
        };

        [Flags]
        public enum PurgeMask : uint
        {
            RX = 1,
            TX = 2
        };
    }

    [AttributeUsage(AttributeTargets.Delegate)]
    public class SymbolNameAttribute : Attribute
    {
        public SymbolNameAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; private set; }
    }

    static class FTDll
    {
        [SymbolName("FT_SetVIDPID")]
        public delegate FT.Status SetVidPid(
            uint vid, uint pid);

        [SymbolName("FT_OpenEx")]
        public delegate FT.Status OpenBySerialNumber(
            [MarshalAs(UnmanagedType.LPStr)] string serialNumber,
            FT.OpenExFlags flags,
            out IntPtr handle);

        [SymbolName("FT_Close")]
        public delegate FT.Status Close(
            IntPtr handle);

        [SymbolName("FT_SetBaudRate")]
        public delegate FT.Status SetBaudRate(
            IntPtr handle,
            uint baudRate);

        [SymbolName("FT_SetDataCharacteristics")]
        public delegate FT.Status SetDataCharacteristics(
            IntPtr handle,
            FT.Bits wordLength,
            FT.StopBits stopBits,
            FT.Parity parity);

        [SymbolName("FT_SetFlowControl")]
        public delegate FT.Status SetFlowControl(
            IntPtr handle,
            FT.FlowControl flowControl,
            byte xonChar,
            byte xoffChar);

        [SymbolName("FT_SetDtr")]
        public delegate FT.Status SetDtr(
            IntPtr handle);

        [SymbolName("FT_ClrDtr")]
        public delegate FT.Status ClrDtr(
            IntPtr handle);

        [SymbolName("FT_SetRts")]
        public delegate FT.Status SetRts(
            IntPtr handle);

        [SymbolName("FT_ClrRts")]
        public delegate FT.Status ClrRts(
            IntPtr handle);

        [SymbolName("FT_SetTimeouts")]
        public delegate FT.Status SetTimeouts(
            IntPtr handle,
            uint readTimeoutMS,
            uint writeTimeoutMS);

        [SymbolName("FT_SetLatencyTimer")]
        public delegate FT.Status SetLatencyTimer(
            IntPtr handle,
            byte timerMS);

        [SymbolName("FT_Purge")]
        public delegate FT.Status Purge(
            IntPtr handle,
            FT.PurgeMask mask);

        [SymbolName("FT_SetBreakOn")]
        public delegate FT.Status SetBreakOn(
            IntPtr handle);

        [SymbolName("FT_SetBreakOff")]
        public delegate FT.Status SetBreakOff(
            IntPtr handle);

        [SymbolName("FT_Read")]
        public delegate FT.Status Read(
            IntPtr handle,
            byte[] buffer,
            uint countOfBytesToRead,
            out uint countOfBytesRead);

        [SymbolName("FT_Write")]
        public delegate FT.Status Write(
            IntPtr handle,
            byte[] buffer,
            uint countOfBytesToWrite,
            out uint countOfBytesWritten);
    }
}
