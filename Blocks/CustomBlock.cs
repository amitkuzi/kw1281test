﻿using System.Collections.Generic;

namespace BitFab.KW1281Test.Blocks
{
    public class CustomBlock : Block
    {
        public CustomBlock(List<byte> bytes) : base(bytes)
        {
            // Dump();
        }

        private void Dump()
        {
            Log.Write("Received Custom block:");
            for (var i = 3; i < Bytes.Count - 1; i++)
            {
                Log.Write($" {Bytes[i]:X2}");
            }

            Log.WriteLine();
        }
    }
}