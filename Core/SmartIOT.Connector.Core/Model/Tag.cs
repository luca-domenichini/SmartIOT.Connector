using SmartIOT.Connector.Core.Conf;

namespace SmartIOT.Connector.Core.Model
{
    public class Tag
    {
        public TagConfiguration TagConfiguration { get; }
        public string TagId => TagConfiguration.TagId;
        public TagType TagType => TagConfiguration.TagType;
        public int ByteOffset => TagConfiguration.ByteOffset;
        public int Size => TagConfiguration.Size;
        public bool IsWriteSynchronizationRequested { get; set; }
        public int Weight => TagConfiguration.Weight;

        public bool IsInitialized { get; internal set; }
        public int ErrorCode { get; internal set; }
        public int ErrorCount { get; internal set; }
        public DateTime LastErrorDateTime { get; internal set; }
        public TimeSpan PartialReadTotalTime { get; internal set; }
        public TimeSpan PartialReadMeanTime { get; internal set; }
        public long PartialReadCount { get; internal set; }
        public long SynchronizationCount { get; internal set; }
        public TimeSpan SynchronizationAvgTime { get; internal set; }
        public int WritesCount { get; internal set; }

        internal byte[] Data { get; }
        internal byte[] OldData { get; }
        internal int CurrentReadIndex { get; set; }
        internal DateTime LastDeviceSynchronization { get; set; }
        internal int Points { get; set; }

        public Tag(TagConfiguration tagConfiguration)
        {
            TagConfiguration = tagConfiguration;

            Data = new byte[Size];
            OldData = new byte[Size];

            IsInitialized = false;
        }

        /// <summary>
        /// Questo metodo copia i dati ricevuti in argomento allo startOffset indicato.
        /// Lo startOffset deve essere passato in valore assoluto, quindi se un tag inizia al byte 100
        /// e il metodo intende scrivere i byte dal 110 al 120 passerà come argomenti startOffset = 110 e un array di lunghezza = 11.
        /// In ogni caso, il metodo imposta il flag di richiesta scrittura: se l'array passato in argomento non provoca nessuna variazione
        /// dei dati, il flag viene alzato comunque (e come effetto verrà scritto l'intero tag).
        /// </summary>
        public void RequestTagWrite(byte[] data, int startOffset)
        {
            lock (this)
            {
                Array.Copy(data, 0, Data, startOffset - ByteOffset, data.Length);
                IsWriteSynchronizationRequested = true;
            }
        }

        /// <summary>
        /// This method returns a copy of the current bytes stored in the tag
        /// </summary>
        public byte[] GetData()
        {
            var bytes = new byte[Data.Length];

            Array.Copy(Data, bytes, bytes.Length);

            return bytes;
        }
    }
}
