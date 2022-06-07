namespace SmartIOT.Connector.Plc.S7Net
{
	public class S7NetPlc : Core.Model.Device
	{
		private readonly S7.Net.Plc _plc;

		public new S7NetPlcConfiguration Configuration => (S7NetPlcConfiguration)base.Configuration;

		public S7NetPlc(S7NetPlcConfiguration deviceConfiguration) : base(deviceConfiguration)
		{
			if (deviceConfiguration.Port != null)
				_plc = new S7.Net.Plc(deviceConfiguration.CpuType, deviceConfiguration.IpAddress, deviceConfiguration.Port.Value, deviceConfiguration.Rack, deviceConfiguration.Slot);
			else
				_plc = new S7.Net.Plc(deviceConfiguration.CpuType, deviceConfiguration.IpAddress, deviceConfiguration.Rack, deviceConfiguration.Slot);
		}

		public void Connect()
		{
			_plc.Open();

			int pduLength = _plc.MaxPDUSize;
			if (pduLength > 0)
			{
				PDULength = pduLength;
				SinglePDUWriteBytes = pduLength - 35; // 35 bytes di header nel protocollo ISO/TCP (vedere S7Client#WriteArea())
				SinglePDUReadBytes = pduLength - 18; // 18 bytes di header nel protocollo ISO/TCP (vedere S7Client#ReadArea())
			}
		}
		public void Disconnect()
		{
			_plc.Close();
		}
		public byte[] ReadBytes(int tagId, int startOffset, int length)
		{
			return _plc.ReadBytes(S7.Net.DataType.DataBlock, tagId, startOffset, length);
		}
		public void WriteBytes(int tagId, int startOffset, byte[] bytes)
		{
			_plc.WriteBytes(S7.Net.DataType.DataBlock, tagId, startOffset, bytes);
		}
	}
}
