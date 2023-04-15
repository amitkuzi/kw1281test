﻿using BitFab.KW1281Test.Interface;
using System;
using System.Diagnostics;
using System.Threading;

namespace BitFab.KW1281Test
{
    public interface IKwpCommon
    {
        IInterface Interface { get; }

        int WakeUp(byte controllerAddress, bool evenParity = false);

        byte ReadByte();

        /// <summary>
        /// Write a byte to the interface and receive its echo.
        /// </summary>
        /// <param name="b">The byte to write.</param>
        void WriteByte(byte b);

        void ReadComplement(byte b);
    }

    public class KwpCommon : IKwpCommon
    {
        public IInterface Interface { get; }

        public int WakeUp(byte controllerAddress, bool evenParity)
        {
            // Disable garbage collection int this time-critical method
            bool noGC = GC.TryStartNoGCRegion(1024 * 1024);
            byte syncByte = 0;
            const int maxTries = 3;
            try
            {
                if (!noGC)
                {
                    Log.WriteLine("Warning: Unable to disable GC so timing may be compromised.");
                }

                for (int i = 1; i <= maxTries; i++)
                {
                    Thread.Sleep(300);

                    BitBang5Baud(controllerAddress, evenParity);

                    // Throw away anything that might be in the receive buffer
                    Interface.ClearReceiveBuffer();

                    Log.WriteLine("Reading sync byte");
                    try
                    {
                        while (syncByte != 0x55)
                        {
                            syncByte = Interface.ReadByte();
                            Log.WriteLine($"waiting for WakeUp 0x55 {syncByte:X2}");
                        }
                        break;
                    }
                    catch (TimeoutException)
                    {
                        if (i < maxTries)
                        {
                            Log.WriteLine("Retrying wakeup message...");
                        }
                        else
                        {
                            throw new InvalidOperationException("Controller did not wake up.");
                        }
                    }
                }
            }
            finally
            {
                if (noGC)
                {
                    GC.EndNoGCRegion();
                }
            }
          



          
            Log.WriteLine($"syncByte {syncByte}");
            if (syncByte != 0x55)
            {
                Log.WriteLine(
                    $"Unexpected sync byte: Expected $55, Actual ${syncByte:X2}");
            }

            var keywordLsb = Interface.ReadByte();
            Log.WriteLine($"Keyword Lsb ${keywordLsb:X2}");

            var keywordMsb = ReadByte();
            Log.WriteLine($"Keyword Msb ${keywordMsb:X2}");

            Thread.Sleep(25);

            var complement = (byte)~keywordMsb;

            Interface.WriteByteRaw(complement);
            var ackComplet = Interface.ReadByte();
            while(ackComplet != complement)
            {
                Log.WriteLine($"ackComplet {ackComplet} - {complement}");
                Thread.Sleep(100);
                ackComplet = Interface.ReadByte();
            }

            var protocolVersion = ((keywordMsb & 0x7F) << 7) + (keywordLsb & 0x7F);
            Log.WriteLine($"Protocol is KW {protocolVersion} (8N1)");

            if (protocolVersion >= 2000)
            {
                ReadComplement(controllerAddress);
            }

            return protocolVersion;
        }

        public byte ReadByte()
        {
            return Interface.ReadByte();
        }

        public void WriteByte(byte b)
        {
            WriteByteAndDiscardEcho(b);
        }

        public void ReadComplement(byte b)
        {
            var expectedComplement = (byte)~b;
            var actualComplement = Interface.ReadByte();
            if (actualComplement != expectedComplement)
            {
                throw new InvalidOperationException(
                    $"Received complement ${actualComplement:X2} but expected ${expectedComplement:X2}");
            }
        }

        /// <summary>
        /// Send a byte at 5 baud manually to the interface. The byte will be sent as
        /// 1 start bit, 7 data bits, 1 parity bit (even or odd), 1 stop bit.
        /// https://www.blafusel.de/obd/obd2_kw1281.html
        /// </summary>
        /// <param name="b">The byte to send.</param>
        /// <param name="evenParity">
        /// False for odd parity (KWP1281), true for even parity (KWP2000).</param>
        private void BitBang5Baud(byte b, bool evenParity)
        {
            const int bitsPerSec = 5;
            long ticksPerSecond = Stopwatch.Frequency;
            long ticksPerBit = ticksPerSecond / bitsPerSec;

            long maxTick;

            // Delay the appropriate amount and then set/clear the TxD line
            void BitBang(bool bit)
            {
                while (Stopwatch.GetTimestamp() < maxTick)
                    ;
                if (bit)
                {
                    Interface.SetBreak(false);
                }
                else
                {
                    Interface.SetBreak(true);
                }

                maxTick += ticksPerBit;
            }

            bool parity = !evenParity; // XORed with each bit to calculate parity bit

            var startTick = maxTick = Stopwatch.GetTimestamp();
            BitBang(false); // Start bit

            for (int i = 0; i < 7; i++)
            {
                bool bit = (b & 1) == 1;
                parity ^= bit;
                b >>= 1;

                BitBang(bit);
            }

            BitBang(parity);

            BitBang(true); // Stop bit

            // Wait for end of stop bit
            long stopTick;
            while ((stopTick = Stopwatch.GetTimestamp()) < maxTick)
                ;

            Log.WriteLine(
                $"Wakeup duration: {(double)(stopTick - startTick) / ticksPerSecond} seconds");
        }

        /// <summary>
        /// Write a byte to the interface and read/discard its echo.
        /// </summary>
        private void WriteByteAndDiscardEcho(byte b)
        {
            Interface.WriteByteRaw(b);
            var echo = Interface.ReadByte();
#if false
            if (echo != b)
            {
                throw new InvalidOperationException($"Wrote 0x{b:X2} to port but echo was 0x{echo:X2}");
            }
#endif
        }

        public KwpCommon(IInterface @interface)
        {
            Interface = @interface;
        }
    }
}
