using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Model;
using Moq;

namespace SmartIOT.Connector.Mocks
{
	public class MockDeviceDriver : Mock<IDeviceDriver>, IDeviceDriver
	{
		public Core.Model.Device Device { get; }
		public Action? StartInterfaceCallback { get; set; }
		public Action<byte[], int, int>? ReadTagCallback { get; set; }
		public int ConnectReturns { get; set; }
		public int DisconnectReturns { get; set; }
		public int ReadTagReturns { get; set; }
		public int WriteReturns { get; set; }
		public int StartInterfaceReturns { get; set; }
		public int StopInterfaceReturns { get; set; }


		public MockDeviceDriver(Core.Model.Device device, bool setupDefaults = true)
		{
			Device = device;

			if (setupDefaults)
			{
				Setup(x => x.StartInterface()).Returns(() =>
				{
					StartInterfaceCallback?.Invoke();
					return StartInterfaceReturns;
				});
				Setup(x => x.StopInterface()).Returns(() => StopInterfaceReturns);
				Setup(x => x.Connect(It.IsAny<Core.Model.Device>())).Returns(() => ConnectReturns);
				Setup(x => x.Disconnect(It.IsAny<Core.Model.Device>())).Returns(() => DisconnectReturns);
				Setup(x => x.GetDeviceDescription(It.IsAny<Core.Model.Device>())).Returns((Core.Model.Device device) => device.Name);
				Setup(x => x.GetErrorMessage(It.IsAny<int>())).Returns((int err) => $"{err}");
				Setup(x => x.ReadTag(It.IsAny<Core.Model.Device>(), It.IsAny<Tag>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Returns((Core.Model.Device device, Tag tag, byte[] data, int startOffset, int length) =>
				{
					ReadTagCallback?.Invoke(data, startOffset, length);
					return ReadTagReturns;
				});
				Setup(x => x.WriteTag(It.IsAny<Core.Model.Device>(), It.IsAny<Tag>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>())).Returns((Core.Model.Device device, Tag tag, byte[] data, int startOffset, int length) =>
				{
					return WriteReturns;
				});
			}
		}

		public void SetupReadTagAsRandomData()
		{
			Setup(x => x.ReadTag(It.IsAny<Core.Model.Device>(), It.IsAny<Tag>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
				.Returns((Core.Model.Device device, Tag tag, byte[] data, int startOffset, int length) =>
				{
					Thread.Sleep(10); // some spare time to not stress the cpu
					var r = new Random();
					for (int i = 0; i < data.Length; i++)
					{
						byte v;
						do
						{
							v = (byte)r.Next(0, 255);
						} while (v == data[i]);

						data[i] = v;
					}
					return 0;
				});
		}
		public void SetupReadTagAsRandomData(int startOffset, int length)
		{
			Setup(x => x.ReadTag(It.IsAny<Core.Model.Device>(), It.IsAny<Tag>(), It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>()))
				.Returns((Core.Model.Device device, Tag tag, byte[] data, int s, int l) =>
				{
					Thread.Sleep(10); // some spare time to not stress the cpu
					var r = new Random();
					for (int i = 0; i < length; i++)
					{
						byte v;
						do
						{
							v = (byte)r.Next(0, 255);
						} while (v == data[startOffset - tag.ByteOffset + i]);

						data[startOffset - tag.ByteOffset + i] = v;
					}
					return 0;
				});
		}

		public int Connect(Core.Model.Device device)
		{
			return Object.Connect(device);
		}

		public int Disconnect(Core.Model.Device device)
		{
			return Object.Disconnect(device);
		}

		public string GetDeviceDescription(Core.Model.Device device)
		{
			return Object.GetDeviceDescription(device);
		}

		public string GetErrorMessage(int errorNumber)
		{
			return Object.GetErrorMessage(errorNumber);
		}

		public int ReadTag(Core.Model.Device device, Tag tag, byte[] data, int startOffset, int length)
		{
			return Object.ReadTag(device, tag, data, startOffset, length);
		}

		public int StartInterface()
		{
			return Object.StartInterface();
		}

		public int StopInterface()
		{
			return Object.StopInterface();
		}

		public int WriteTag(Core.Model.Device device, Tag tag, byte[] data, int startOffset, int length)
		{
			return Object.WriteTag(device, tag, data, startOffset, length);
		}

		public void ResetInvocations()
		{
			Invocations.Clear();
		}
	}
}