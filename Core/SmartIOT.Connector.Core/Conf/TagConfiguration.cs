﻿using System.Text.Json.Serialization;

namespace SmartIOT.Connector.Core.Conf
{
	public class TagConfiguration
	{
		public int TagId { get; set; }
		[JsonConverter(typeof(JsonStringEnumConverter))] 
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
		public TagConfiguration(int tagId, TagType tagType, int byteOffset, int size, int weight)
		{
			TagId = tagId;
			TagType = tagType;
			ByteOffset = byteOffset;
			Size = size;
			Weight = weight;
		}
	}
}
