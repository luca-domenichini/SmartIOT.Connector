namespace SmartIOT.Connector.Core.Util
{
	public static class Extensions
	{
		public static V? GetOrDefault<K, V>(this IDictionary<K, V> dictionary, K key)
		{
			if (dictionary.TryGetValue(key, out var value))
				return value;

			return default;
		}

		public static string? GetFirstNonBlankMessageOrDefault(this Exception exception)
		{
			var ex = exception;
			var message = ex.Message;

			while (string.IsNullOrWhiteSpace(message) && ex != null)
			{
				ex = ex.InnerException;
				message = ex?.Message;
			}

			return message;
		}
	}
}
