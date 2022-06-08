using ProtoBuf;

namespace SmartIOT.Connector.Messages
{
	[ProtoContract]
	public class TagEvent
	{
		[ProtoMember(1)]
		public string DeviceId { get; set; } = string.Empty;
		[ProtoMember(2)]
		public string TagId { get; set; } = string.Empty;
		[ProtoMember(3)]
		public int StartOffset { get; set; }
		[ProtoMember(4)]
		public byte[]? Data { get; set; }
		[ProtoMember(5)]
		public bool IsInitializationEvent { get; set; }
		[ProtoMember(6)]
		public int ErrorNumber { get; set; }
		[ProtoMember(7)]
		public string? Description { get; set; }

		public static TagEvent CreateTagDataEvent(string deviceId, string tagId, int startOffset, byte[] data, bool isInitializationData = false)
		{
			return new TagEvent(deviceId, tagId, startOffset, data, isInitializationData);
		}
		public static TagEvent CreateTagStatusEvent(string deviceId, string tagId, int errorNumber, string description)
		{
			return new TagEvent(deviceId, tagId, errorNumber, description);
		}

		public TagEvent()
		{

		}

		private TagEvent(string deviceId, string tagId, int startOffset, byte[] data, bool isInitializationData)
		{
			DeviceId = deviceId;
			TagId = tagId;
			StartOffset = startOffset;
			Data = data;
			IsInitializationEvent = isInitializationData;
		}
		private TagEvent(string deviceId, string tagId, int errorNumber, string description)
		{
			DeviceId = deviceId;
			TagId = tagId;
			ErrorNumber = errorNumber;
			Description = description;
		}

		public override string? ToString()
		{
			if (Data != null)
				return $"[{nameof(TagEvent)}] Device {DeviceId}, Tag {TagId}, StartOffset {StartOffset}, Data[{Data.Length}]";

			return $"[{nameof(TagEvent)}] Device {DeviceId}, Tag {TagId}, Error: {ErrorNumber} '{Description}'";
		}
	}
}