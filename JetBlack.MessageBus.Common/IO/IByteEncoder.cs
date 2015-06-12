namespace JetBlack.MessageBus.Common.IO
{
    public interface IByteEncoder<TData>
    {
        TData Decode(byte[] bytes);
        byte[] Encode(TData data);
    }
}
