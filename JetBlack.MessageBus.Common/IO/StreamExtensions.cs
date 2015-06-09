using System;
using System.IO;
using System.Net;
using System.Text;

namespace JetBlack.MessageBus.Common.IO
{
    public static class StreamExtensions
    {
        #region Reading

        public static int Read(this Stream stream, byte[] value)
        {
            return stream.Read(value, 0, value.Length);
        }

        public static int Read(this Stream stream, byte[] value, int off, int len)
        {
            return stream.Read(value, off, len);
        }

        public static bool ReadBoolean(this Stream stream)
        {
            return (stream.ReadByte() != 0);
        }

        public static byte ReadByte(this Stream stream)
        {
            int ch = stream.ReadByte();
            if (ch < 0)
                throw new EndOfStreamException();
            return (byte)ch;
        }

        public static char ReadChar(this Stream stream)
        {
            return PortableBitConverter.ToChar(stream.ReadFully(new byte[2]), 0);
        }

        public static double ReadDouble(this Stream stream)
        {
            return BitConverter.Int64BitsToDouble(stream.ReadInt64());
        }

        public static float ReadFloat(this Stream stream)
        {
            return PortableBitConverter.ToFloat(stream.ReadFully(new byte[4]), 0);
        }

        public static short ReadInt16(this Stream stream)
        {
            return PortableBitConverter.ToInt16(stream.ReadFully(new byte[2]), 0);
        }

        public static ushort ReadUInt16(this Stream stream)
        {
            return (ushort)PortableBitConverter.ToInt16(stream.ReadFully(new byte[2]), 0);
        }

        public static int ReadInt32(this Stream stream)
        {
            return PortableBitConverter.ToInt32(stream.ReadFully(new byte[4]), 0);
        }

        public static long ReadInt64(this Stream stream)
        {
            return PortableBitConverter.ToInt64(stream.ReadFully(new byte[8]), 0);
        }

        public static IPAddress ReadIPAddress(this Stream stream)
        {
            var len = stream.ReadInt32();
            var address = new byte[len];
            stream.ReadFully(address);
            return new IPAddress(address);
        }

        public static string ReadString(this Stream stream)
        {
            var len = stream.ReadInt32();
            return Encoding.UTF8.GetString(stream.ReadFully(new byte[len]));
        }

        public static byte[] ReadByteArray(this Stream stream)
        {
            var nbytes = stream.ReadInt32();
            var data = new byte[nbytes];
            var offset = 0;
            while (nbytes > 0)
            {
                var bytesRead = stream.Read(data, offset, nbytes);
                if (bytesRead == 0)
                    throw new EndOfStreamException();
                nbytes -= bytesRead;
                offset += bytesRead;
            }
            return data;
        }

        static byte[] ReadFully(this Stream stream, byte[] buf)
        {
            return stream.ReadFully(buf, 0, buf.Length);
        }

        static byte[] ReadFully(this Stream stream, byte[] buf, int off, int len)
        {
            if (len < 0)
                throw new IndexOutOfRangeException();

            int n = 0;
            while (n < len)
            {
                int count = stream.Read(buf, off + n, len - n);
                if (count < 0)
                    throw new EndOfStreamException();
                n += count;
            }

            return buf;
        }

        public static DateTime ReadDate(this Stream stream)
        {
            return PortableBitConverter.Int64ToDate(stream.ReadInt64());
        }

        #endregion

        #region Writing

        public static void Write(this Stream stream, bool value)
        {
            stream.WriteByte((byte)(value ? 1 : 0));
        }

        public static void Write(this Stream stream, byte value)
        {
            stream.WriteByte(value);
        }

        public static void Write(this Stream stream, char value)
        {
            stream.Write(PortableBitConverter.GetBytes(value), 0, 2);
        }

        public static void Write(this Stream stream, int value)
        {
            stream.Write(PortableBitConverter.GetBytes(value), 0, 4);
        }

        public static void Write(this Stream stream, long value)
        {
            stream.Write(PortableBitConverter.GetBytes(value), 0, 8);
        }

        public static void Write(this Stream stream, short value)
        {
            stream.Write(PortableBitConverter.GetBytes(value), 0, 2);
        }

        public static void Write(this Stream stream, ushort value)
        {
            stream.Write(PortableBitConverter.GetBytes(value), 0, 2);
        }

        public static void Write(this Stream stream, float value)
        {
            stream.Write(PortableBitConverter.GetBytes(value), 0, 4);
        }

        public static void Write(this Stream stream, double value)
        {
            stream.Write(BitConverter.DoubleToInt64Bits(value));
        }

        public static void Write(this Stream stream, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            stream.Write(bytes.Length);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void Write(this Stream stream, DateTime value)
        {
            stream.Write(PortableBitConverter.DateToInt64(value));
        }

        public static void Write(this Stream stream, IPAddress ipAddress)
        {
            byte[] address = ipAddress.GetAddressBytes();
            stream.Write(address.Length);
            stream.Write(address, 0, address.Length);
        }

        public static void Write(this Stream stream, byte[] value)
        {
            if (value == null)
                stream.Write(0);
            else
            {
                stream.Write(value.Length);
                stream.Write(value, 0, value.Length);
            }
        }

        #endregion
    }
}
