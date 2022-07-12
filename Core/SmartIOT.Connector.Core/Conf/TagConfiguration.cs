namespace SmartIOT.Connector.Core.Conf
{
	public class TagConfiguration
	{
		public string TagId { get; set; } = string.Empty;
		public TagType TagType { get; set; }
		public int ByteOffset { get; set; }
		public int Size { get; set; }
		public int Weight { get; set; }

		public TagConfiguration()
		{

		}

		public TagConfiguration(TagConfiguration configuration)
		{
			TagId = configuration.TagId;
			TagType = configuration.TagType;
			ByteOffset = configuration.ByteOffset;
			Size = configuration.Size;
			Weight = configuration.Weight;
		}
		public TagConfiguration(string tagId, TagType tagType, int byteOffset, int size, int weight)
		{
			TagId = tagId;
			TagType = tagType;
			ByteOffset = byteOffset;
			Size = size;
			Weight = weight;
		}
	}
}
