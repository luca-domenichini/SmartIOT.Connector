namespace SmartIOT.Connector.RestApi.Model
{
    public class TagData
    {
        public int StartOffset { get; }
        public byte[] Bytes { get; }

        public TagData(int startOffset, byte[] bytes)
        {
            StartOffset = startOffset;
            Bytes = bytes;
        }
    }
}
