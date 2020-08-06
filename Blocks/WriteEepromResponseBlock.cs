﻿using System;
using System.Collections.Generic;

namespace BitFab.KW1281Test.Blocks
{
    internal class WriteEepromResponseBlock : Block
    {
        public WriteEepromResponseBlock(List<byte> bytes) : base(bytes)
        {
            Dump();
        }

        private void Dump()
        {
            Console.Write("Received \"Write EEPROM Response\" block:");
            foreach (var b in Body)
            {
                Console.Write($" {b:X2}");
            }

            Console.WriteLine();
        }
    }
}