using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace SmartIOT.Connector.Core.Tests
{
	public static class Extensions
	{
		[DoesNotReturn]
		public static void Rethrow(this Exception ex)
		{
			ExceptionDispatchInfo.Throw(ex);
			throw ex;
		}
	}
}
