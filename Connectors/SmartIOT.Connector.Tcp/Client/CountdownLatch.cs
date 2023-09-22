namespace SmartIOT.Connector.Tcp.Client
{
    internal class CountdownLatch
    {
        private int _count;
        private readonly ManualResetEventSlim _nothingRunning = new ManualResetEventSlim(true);

        public void Increment()
        {
            int count = Interlocked.Increment(ref _count);
            if (count == 1)
                _nothingRunning.Reset();
        }

        public void Decrement()
        {
            int count = Interlocked.Decrement(ref _count);
            if (_count == 0)
            {
                _nothingRunning.Set();
            }
            else if (count < 0)
            {
                throw new InvalidOperationException("Count must be greater than or equal to 0");
            }
        }

        public void WaitUntilZero()
        {
            _nothingRunning.Wait();
        }
    }
}
