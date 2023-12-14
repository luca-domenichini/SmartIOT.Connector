namespace SmartIOT.Connector.Core.Util;

public static class ConnectionStringParser
{
    public static IDictionary<string, string> ParseTokens(string connectionString)
    {
        var dictionary = new Dictionary<string, string>();

        string[] arr = connectionString.Split("://", 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (arr.Length > 1)
        {
            foreach (string value in arr[1].Split(";", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                string[] keyValue = value.Split("=", 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

                dictionary[keyValue[0].ToLower()] = keyValue.Length > 1 ? keyValue[1] : string.Empty;
            }
        }

        return dictionary;
    }
}
