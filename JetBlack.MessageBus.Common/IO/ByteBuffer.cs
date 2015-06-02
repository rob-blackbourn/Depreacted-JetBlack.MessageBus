﻿using System.Linq;

namespace JetBlack.MessageBus.Common.IO
{
    public class ByteBuffer
    {
        public ByteBuffer(byte[] bytes, int length)
        {
            Bytes = bytes;
            Length = length;
        }

        public byte[] Bytes { get; private set; }
        public int Length { get; private set; }

        public override string ToString()
        {
            return
                Bytes == null
                    ? string.Format("Length={0}, Capacity=#Null, Bytes=#Null", Length)
                    : string.Format(
                        "Length={0}, Capacity={1}, Bytes=[{2}]",
                        Length,
                        Bytes.Length,
                        string.Join(",", Bytes.Take(Length)));
        }
    }
}
