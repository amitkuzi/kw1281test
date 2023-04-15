using System;
using System.Collections.Generic;

namespace BitFab.KW1281Test.Blocks
{
    public class AckBlock : Block
    {
        public AckBlock(List<byte> bytes) : base(bytes)
        {
            // Dump();
        }

        private void Dump()
        {
            Log.WriteLine("Received ACK block");
        }
    }
}
