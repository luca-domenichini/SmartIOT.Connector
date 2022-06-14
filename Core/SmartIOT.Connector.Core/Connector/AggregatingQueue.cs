namespace SmartIOT.Connector.Core.Connector
{
	public abstract class AggregatingQueue<T> where T : class
	{
		private readonly Queue<T> _queue = new Queue<T>();
		private readonly ManualResetEventSlim _eventAvailable = new ManualResetEventSlim();

		protected AggregatingQueue()
		{

		}

		/// <summary>
		/// This method must return an aggregated item if possible, or default if the 2 items are not aggregable.
		/// </summary>
		protected abstract T? Aggregate(T item1, T item2);

		public void Push(T item)
		{
			lock (_queue)
			{
				_queue.Enqueue(item);
				_eventAvailable.Set();
			}
		}

		public T PopWait(CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested)
			{
				T? t = PopOrDefault();
				if (t != default)
					return t;

				_eventAvailable.Wait(cancellationToken);
			}

			throw new OperationCanceledException(cancellationToken);
		}

		public T? PopOrDefault()
		{
			lock (_queue)
			{
				T? item = default;

				while (_queue.Any())
				{
					if (item == default)
					{
						item = _queue.Dequeue();
					}
					else
					{
						var t = _queue.Peek();
						var aggregate = Aggregate(item, t);
						if (aggregate != default)
						{
							item = aggregate;
							_queue.Dequeue();
						}
						else
							break;
					}
				}

				if (_queue.Count == 0)
					_eventAvailable.Reset();

				return item;
			}
		}
	}
}
