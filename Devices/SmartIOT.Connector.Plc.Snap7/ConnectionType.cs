using Sharp7;

namespace SmartIOT.Connector.Plc.Snap7
{
	public sealed class ConnectionType
	{
		public static readonly ConnectionType PG = new ConnectionType() { Value = S7Client.CONNTYPE_PG };
		public static readonly ConnectionType OP = new ConnectionType() { Value = S7Client.CONNTYPE_OP };
		public static readonly ConnectionType BASIC = new ConnectionType() { Value = S7Client.CONNTYPE_BASIC };

		public static ConnectionType Of(S7ConnectionType c)
		{
			return c switch
			{
				S7ConnectionType.PG => PG,
				S7ConnectionType.OP => OP,
				S7ConnectionType.BASIC => BASIC,
				_ => BASIC,
			};
		}

		public ushort Value { get; init; }
	}
}
