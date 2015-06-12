using System.Text;
using JetBlack.MessageBus.Common.IO;
using Newtonsoft.Json;

namespace JetBlack.MessageBus.Json
{
    public class JsonEncoder<TData> : IByteEncoder<TData>
    {
        public TData Decode(byte[] bytes)
        {
            return JsonConvert.DeserializeObject<TData>(Encoding.UTF8.GetString(bytes));
        }

        public byte[] Encode(TData data)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
        }
    }
}
