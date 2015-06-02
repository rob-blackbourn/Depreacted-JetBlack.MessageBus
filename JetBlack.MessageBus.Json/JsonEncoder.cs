using System.Text;
using JetBlack.MessageBus.Common.IO;
using Newtonsoft.Json;

namespace JetBlack.MessageBus.Json
{
    public class JsonEncoder<T> : IByteEncoder<T>
    {
        public T Decode(byte[] bytes)
        {
            var s = Encoding.UTF8.GetString(bytes);
            var obj = JsonConvert.DeserializeObject<T>(s);
            return obj;
        }

        public byte[] Encode(T obj)
        {
            var s = JsonConvert.SerializeObject(obj);
            var bytes = Encoding.UTF8.GetBytes(s);
            return bytes;
        }
    }
}
