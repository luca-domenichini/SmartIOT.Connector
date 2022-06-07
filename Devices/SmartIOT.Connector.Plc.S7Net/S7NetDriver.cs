using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Model;
using S7.Net;

namespace SmartIOT.Connector.Plc.S7Net
{
	public class S7NetDriver : IDeviceDriver
	{
		public string Name => $"{nameof(S7NetDriver)}.{Plc.Name}";
		public S7NetPlc Plc { get; }
		private readonly IList<S7NetPlc> _plcs = new List<S7NetPlc>();


		public S7NetDriver(S7NetPlc plc)
		{
			Plc = plc;
			_plcs.Add(plc);
		}

		public int Connect(Core.Model.Device plc)
		{
			try
			{
				lock (plc)
				{
					S7NetPlc p = (S7NetPlc)plc;
					p.Connect();
				}

				return 0;
			}
			catch (PlcException ex)
			{
				throw new DeviceDriverException(ex.GetErrorMessage(), ex);
			}
		}

		public int Disconnect(Core.Model.Device plc)
		{
			try
			{
				lock (plc)
				{
					S7NetPlc p = (S7NetPlc)plc;
					p.Disconnect();
				}

				return 0;
			}
			catch (PlcException ex)
			{
				throw new DeviceDriverException(ex.GetErrorMessage(), ex);
			}
		}

		public string GetErrorMessage(int errorNumber)
		{
			return $"Error {errorNumber}";
		}

		public string GetDeviceDescription(Core.Model.Device plc)
		{
			return plc.Name;
		}

		public IList<Core.Model.Device> GetDevices(bool enabledOnly)
		{
			return _plcs.Where(x => !enabledOnly || Plc.DeviceStatus != DeviceStatus.DISABLED).Cast<Core.Model.Device>().ToList();
		}

		public int ReadTag(Core.Model.Device plc, Tag tag, byte[] data, int startOffset, int length)
		{
			try
			{
				lock (plc)
				{
					S7NetPlc p = (S7NetPlc)plc;
					byte[] bytes = p.ReadBytes(tag.TagId, startOffset, length);

					Array.Copy(bytes, 0, data, startOffset - tag.ByteOffset, bytes.Length);
				}

				return 0;
			}
			catch (PlcException ex)
			{
				throw new DeviceDriverException(ex.GetErrorMessage(), ex);
			}
		}

		public int StartInterface()
		{
			return 0;
		}

		public int StopInterface()
		{
			return 0;
		}

		public int WriteTag(Core.Model.Device plc, Tag tag, byte[] data, int startOffset, int length)
		{
			try
			{
				lock (plc)
				{
					byte[] bytes = new byte[length];
					Array.Copy(data, startOffset - tag.ByteOffset, bytes, 0, length);

					S7NetPlc p = (S7NetPlc)plc;
					p.WriteBytes(tag.TagId, startOffset, bytes);
				}

				return 0;
			}
			catch (PlcException ex)
			{
				throw new DeviceDriverException(ex.GetErrorMessage(), ex);
			}
		}
	}
}
