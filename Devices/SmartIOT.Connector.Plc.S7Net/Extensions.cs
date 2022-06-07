using S7.Net;

namespace SmartIOT.Connector.Plc.S7Net
{
	public static class Extensions
	{
		public static string GetErrorMessage(this PlcException exception)
		{
			return $"[{exception.ErrorCode}] {exception.Message}";
		}
	}
}
