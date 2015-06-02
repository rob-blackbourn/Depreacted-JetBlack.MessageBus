namespace JetBlack.MessageBus.Common.IO
{
    public interface IByteEncoder<T>
    {
        T Decode(byte[] bytes);
        byte[] Encode(T obj);
    }
}
