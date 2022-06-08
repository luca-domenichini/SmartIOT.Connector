using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Model;
using Sharp7;
using System.Text;
using static Sharp7.S7Client;
using System.Text.RegularExpressions;

namespace SmartIOT.Connector.Plc.Snap7
{
	public class Snap7Driver : IDeviceDriver
	{
		private static readonly Regex RegexDB = new Regex(@"^DB(?<tag>[0-9]*)$");

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

		public int ReadTag(Device plc, Tag tag, byte[] data, int startOffset, int length)
		{
			Snap7Plc p = (Snap7Plc)plc;

			if (int.TryParse(tag.TagId, out int t))
			{
				return p.ReadBytes(t, startOffset, data);
			}
			else
			{
				var match = RegexDB.Match(tag.TagId);
				if (match.Success)
				{
					t = int.Parse(match.Groups["tag"].Value);
					return p.ReadBytes(t, startOffset, data);
				}

				// other tag types can be supported here..
				throw new ArgumentException($"TagId {tag.TagId} not handled. TagId must be in the form \"DB<number>\"");
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

		public int WriteTag(Device plc, Tag tag, byte[] data, int startOffset, int length)
		{
			byte[] bytes = new byte[length];
			Array.Copy(data, startOffset - tag.ByteOffset, bytes, 0, length);

			Snap7Plc p = (Snap7Plc)plc;

			if (int.TryParse(tag.TagId, out int t))
			{
				return p.WriteBytes(t, startOffset, bytes);
			}
			else
			{
				var match = RegexDB.Match(tag.TagId);
				if (match.Success)
				{
					t = int.Parse(match.Groups["tag"].Value);
					return p.WriteBytes(t, startOffset, bytes);
				}

				// other tag types can be supported here..
				throw new ArgumentException($"TagId {tag.TagId} not handled. TagId must be in the form \"DB<number>\"");
			}
		}
	}
}
