namespace SmartIOT.Connector.RestApi.Model
{
    public class Connector
    {
        /// <summary>
        /// Index of the connector in SmartIOT.Connector list
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// ConnectionString with connector parameters
        /// </summary>
        public string ConnectionString { get; }

        public Connector(int index, string connectionString)
        {
            Index = index;
            ConnectionString = connectionString;
        }
    }
}
