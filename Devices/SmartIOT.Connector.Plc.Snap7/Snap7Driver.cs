using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Model;
using Sharp7;
using System.Text;
using static Sharp7.S7Client;

namespace SmartIOT.Connector.Plc.Snap7
{
	public class Snap7Driver : IDeviceDriver
	{
		public string Name => $"{nameof(Snap7Driver)}.{Plc.Name}";
		public Snap7Plc Plc { get; }
		private readonly IList<Snap7Plc> _plcs = new List<Snap7Plc>();


		public Snap7Driver(Snap7Plc plc)
		{
			Plc = plc;
			_plcs.Add(plc);
		}

		public int Connect(Core.Model.Device plc)
		{
			lock (plc)
			{
				Snap7Plc p = (Snap7Plc)plc;
				return p.Connect();
			}
		}

		public int Disconnect(Core.Model.Device plc)
		{
			lock (plc)
			{
				Snap7Plc p = (Snap7Plc)plc;
				return p.Disconnect();
			}
		}

		public string GetErrorMessage(int errorNumber)
		{
			return S7Client.ErrorText(errorNumber);
		}

		public string GetDeviceDescription(Core.Model.Device plc)
		{
			lock (plc)
			{
				Snap7Plc p = (Snap7Plc)plc;
				if (p.IsConnected)
				{
					StringBuilder sb = new StringBuilder();

					S7OrderCode oc = new S7OrderCode();
					S7CpuInfo info = new S7CpuInfo();

					int ret = p.GetCpuInfo(info);
					if (ret == 0)
					{
						sb.Append(info.ASName?.Trim()).Append(" - ");
					}

					ret = p.GetOrderCode(oc);
					if (ret == 0)
					{
						sb.Append(oc.Code);
					}

					return sb.ToString();
				}
				else
					return "PLC not connected";
			}
		}

		public IList<Core.Model.Device> GetDevices(bool enabledOnly)
		{
			return _plcs.Where(x => !enabledOnly || Plc.DeviceStatus != DeviceStatus.DISABLED).Cast<Core.Model.Device>().ToList();
		}

		public int ReadTag(Core.Model.Device plc, Tag tag, byte[] data, int startOffset, int length)
		{
			lock (plc)
			{
				Snap7Plc p = (Snap7Plc)plc;
				return p.ReadBytes(tag.TagId, startOffset, data);
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
			lock (plc)
			{
				byte[] bytes = new byte[length];
				Array.Copy(data, startOffset - tag.ByteOffset, bytes, 0, length);

				Snap7Plc p = (Snap7Plc)plc;
				return p.WriteBytes(tag.TagId, startOffset, bytes);
			}
		}
	}
}
