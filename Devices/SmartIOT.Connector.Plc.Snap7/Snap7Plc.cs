using Sharp7;

namespace SmartIOT.Connector.Plc.Snap7
{
	public class Snap7Plc : Core.Model.Device
	{
		public S7Client S7Client { get; init; }
		public new Snap7PlcConfiguration Configuration => (Snap7PlcConfiguration)base.Configuration;
		public bool IsConnected => S7Client.Connected;

		public Snap7Plc(Snap7PlcConfiguration plcConfiguration) : base(plcConfiguration)
		{
			S7Client = new S7Client();
		}

		public int Connect()
		{
			S7Client.SetConnectionType(Configuration.ConnectionType.Value);
			int err = S7Client.ConnectTo(Configuration.IpAddress, Configuration.Rack, Configuration.Slot);

			if (err == 0)
			{
				int pduLength = S7Client.PduSizeNegotiated;
				if (pduLength > 0)
				{
					PDULength = pduLength;
					SinglePDUWriteBytes = pduLength - 35; // 35 bytes di header nel protocollo ISO/TCP (vedere S7Client#WriteArea())
					SinglePDUReadBytes = pduLength - 18; // 18 bytes di header nel protocollo ISO/TCP (vedere S7Client#ReadArea())
				}
			}

			return err;
		}

		public int Disconnect()
		{
			return S7Client.Disconnect();
		}

		public int ReadBytes(int tagId, int startOffset, byte[] data)
		{
			return S7Client.DBRead(tagId, startOffset, data.Length, data);
		}

		public int WriteBytes(int tagId, int startOffset, byte[] data)
		{
			return S7Client.DBWrite(tagId, startOffset, data.Length, data);
		}

		public int GetCpuInfo(S7Client.S7CpuInfo info)
		{
			return S7Client.GetCpuInfo(ref info);
		}

		public int GetOrderCode(S7Client.S7OrderCode oc)
		{
			return S7Client.GetOrderCode(ref oc);
		}
	}
}
