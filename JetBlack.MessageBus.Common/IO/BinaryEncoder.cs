using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace JetBlack.MessageBus.Common.IO
{
    public class BinaryEncoder<T> : IByteEncoder<T>
    {
        static readonly BinaryFormatter BinaryFormatter = new BinaryFormatter();

        public T Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length <= 0)
                return default(T);

            using (var stream = new MemoryStream(bytes))
            {
                return (T)BinaryFormatter.Deserialize(stream);
            }
        }

        public byte[] Encode(T obj)
        {
            using (var stream = new MemoryStream())
            {
                BinaryFormatter.Serialize(stream, obj);
                stream.Flush();
                return stream.GetBuffer();
            }
        }
    }
}
