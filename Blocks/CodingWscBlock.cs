﻿using System.Collections.Generic;
using System.Linq;

namespace BitFab.KW1281Test.Blocks
{
    public class CodingWscBlock : Block
    {
        public CodingWscBlock(List<byte> bytes) : base(bytes)
        {
            var data = bytes.Skip(4).ToList();

            SoftwareCoding = (data[0] * 256 + data[1]) / 2;
            WorkshopCode = data[2] * 256 + data[3];

            // Workshop codes > 65535 overflow into the low bit of the software coding
            if ((data[1] & 1) == 1)
            {
                WorkshopCode += 65536;
            }
        }

        public override string ToString()
        {
            return $"Software Coding {SoftwareCoding:d5}, Workshop Code: {WorkshopCode:d5}";
        }

        public int SoftwareCoding { get; }

        public int WorkshopCode { get; }
    }
}
