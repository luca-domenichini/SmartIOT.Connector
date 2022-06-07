namespace SmartIOT.Connector.Core.Events
{
	public class ExceptionEventArgs : EventArgs
	{
		public Exception Exception { get; }

		public ExceptionEventArgs(Exception exception)
		{
			Exception = exception;
		}
	}
}
