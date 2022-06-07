namespace SmartIOT.Connector.Core.Queue
{
	public abstract class AggregatingQueue<T> where T : class
	{
		private readonly Queue<T> _queue = new Queue<T>();
		private readonly ManualResetEventSlim _eventAvailable = new ManualResetEventSlim();

		protected AggregatingQueue()
		{

		}

		public bool IsAggregationEnabled { get; protected init; } = true;

		/// <summary>
		/// Se gli argomenti sono aggregabili, il metodo deve ritornare l'elemento aggregato.
		/// Se gli argomenti non sono aggregabili, il metodo può ritorna null.
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
			if (IsAggregationEnabled)
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
			else
			{
				lock (_queue)
				{
					if (_queue.Any())
					{
						return _queue.Dequeue();
					}

					_eventAvailable.Reset();

					return default;
				}
			}
		}
	}
}
