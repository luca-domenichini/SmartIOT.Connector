using SmartIOT.Connector.Messages;
using Xunit;

namespace SmartIOT.Connector.Core.Tests
{
	public class ProtobufMessageSerializerTests
	{
		[Fact]
		public void Test_parser()
		{
			TagEvent e = TagEvent.CreateTagDataEvent("1", 20, 0, new byte[] { 1, 2, 3, 4, 5 });

			var serializer = new ProtobufMessageSerializer();

			byte[] bytes = serializer.SerializeMessage(e);
			TagEvent? e2 = serializer.DeserializeMessage<TagEvent>(bytes);

			Assert.NotNull(e2);
			Assert.Equal(e.DeviceId, e2!.DeviceId);
			Assert.Equal(e.TagId, e2.TagId);
			Assert.Equal(e.StartOffset, e2.StartOffset);
			Assert.Equal(e.Data, e2.Data);
			Assert.Equal(e.Description, e2.Description);
			Assert.Equal(e.IsInitializationEvent, e2.IsInitializationEvent);
		}
	}
}
