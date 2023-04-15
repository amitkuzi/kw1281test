using System;
using System.Diagnostics;
using System.Threading;

namespace BitFab.KW1281Test.Interface
{
    public interface IInterface : IDisposable
    {
        /// <summary>
        /// Read a byte from the interface.
        /// </summary>
        /// <returns>The byte.</returns>
        byte ReadByte();

        /// <summary>
        /// Write a byte to the interface but do not read/discard its echo.
        /// </summary>
        void WriteByteRaw(byte b);

        void SetBreak(bool on);

        void ClearReceiveBuffer();

        void SetBaudRate(int baudRate);

        void SetDtr(bool on);

        void SetRts(bool on);
    }
    public static class InterfaceExtention
    {
        public static byte? WaitReadByte(this IInterface @interface, byte byteToWaitFor, int maxCount = 100 , int maxWaiteSec = 60) 
        {
            
            TimeSpan maxWaite = TimeSpan.FromSeconds(maxWaiteSec);
            var start = DateTime.UtcNow;
            for (int i = 1; i <= maxCount; i++)
            {
                Thread.Sleep(300);
                byte syncByte;

                Log.WriteLine("Reading sync byte");
                try
                {
                    do
                    {
                        Thread.Sleep(5);
                        syncByte = @interface.ReadByte();
                        if (syncByte == byteToWaitFor) return syncByte;
                        if (DateTime.UtcNow - maxWaite > start) return null;
                        Log.WriteLine($"waiting for 0x{byteToWaitFor:X2} got 0x{syncByte:X2}");
                    }
                    while (syncByte != byteToWaitFor);
                        
                }
                catch (TimeoutException ex)
                {
                    Log.WriteLine($"TimeoutException {ex.Message}");
                }
            }
            return null;
        }  
        
    }
}



