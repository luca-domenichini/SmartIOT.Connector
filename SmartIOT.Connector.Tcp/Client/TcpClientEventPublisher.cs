using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Connector;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Messages;
using SmartIOT.Connector.Messages.Serializers;
using System.Net.Sockets;

namespace SmartIOT.Connector.Tcp.Client
{
	public class TcpClientEventPublisher : IConnectorEventPublisher
	{
		private TcpClient? TcpClient { get; set; }

		private readonly IStreamMessageSerializer _messageSerializer;
		private readonly TcpClientEventPublisherOptions _options;
		private readonly CancellationTokenSource _stopToken = new CancellationTokenSource();
		private readonly ManualResetEventSlim _reconnectTaskTerminated = new ManualResetEventSlim();
		private readonly CountdownLatch _readers = new CountdownLatch();
		private ConnectorInterface? _connectorInteface;

		public event EventHandler<ExceptionEventArgs>? OnException;
		public event EventHandler<TcpClientConnectionFailedEventArgs>? ConnectionFailed;
		public event EventHandler<TcpClientDisconnectedEventArgs>? Disconnected;
		public event EventHandler<TcpClientConnectedEventArgs>? Connected;

		public TcpClientEventPublisher(IStreamMessageSerializer messageSerializer, TcpClientEventPublisherOptions options)
		{
			_messageSerializer = messageSerializer;
			_options = options;
		}


		public void PublishDeviceStatusEvent(DeviceStatusEvent e)
		{
			PublishDeviceStatusEvent(e, TcpClient);
		}
		private void PublishDeviceStatusEvent(DeviceStatusEvent e, TcpClient? tcpClient)
		{
			SendMessage(tcpClient, e.ToEventMessage());
		}

		public void PublishException(Exception exception)
		{
			PublishException(exception, TcpClient);
		}
		private void PublishException(Exception exception, TcpClient? tcpClient)
		{
			SendMessage(tcpClient, new ExceptionEvent(exception.Message, exception.ToString()));
		}

		public void PublishTagScheduleEvent(TagScheduleEvent e)
		{
			PublishTagScheduleEvent(e, TcpClient, false);
		}
		private void PublishTagScheduleEvent(TagScheduleEvent e, TcpClient? tcpClient, bool isInitializationData)
		{
			SendMessage(tcpClient, e.ToEventMessage(isInitializationData));
		}

		private void SendMessage(TcpClient? tcpClient, object message)
		{
			if (tcpClient != null && tcpClient.Connected)
			{
				try
				{
					_messageSerializer.SerializeMessage(tcpClient.GetStream(), message);
				}
				catch (Exception ex)
				{
					Disconnected?.Invoke(this, new TcpClientDisconnectedEventArgs(_options.ServerAddress, _options.ServerPort, ex));
					tcpClient.Close();

					throw;
				}
			}
		}

		public void Start(IConnector connector, ConnectorInterface connectorInterface)
		{
			_connectorInteface = connectorInterface;

			Task.Factory.StartNew(async () =>
			{
				try
				{
					while (!_stopToken.IsCancellationRequested)
					{
						try
						{
							await Task.Delay(_options.ReconnectDelay, _stopToken.Token);

							if (!(TcpClient?.Connected ?? true))
							{
								TcpClient?.Close();
								TcpClient = null;
							}

							if (TcpClient == null)
							{
								var tcpClient = new TcpClient();
								ConfigureTcpClient(tcpClient);

								try
								{
									await tcpClient.ConnectAsync(_options.ServerAddress, _options.ServerPort, _stopToken.Token);
								}
								catch (Exception ex)
								{
									tcpClient.Close();
									tcpClient = null;

									if (ex is not OperationCanceledException)
										ConnectionFailed?.Invoke(this, new TcpClientConnectionFailedEventArgs(_options.ServerAddress, _options.ServerPort, ex));
								}

								if (tcpClient != null && tcpClient.Connected)
								{
									try
									{
										// send initialization events
										connectorInterface.InitializationActionDelegate.Invoke((deviceStatusEvents, tagEvents) =>
										{
											foreach (var e in deviceStatusEvents)
											{
												PublishDeviceStatusEvent(e, tcpClient);
											}
											foreach (var e in tagEvents)
											{
												PublishTagScheduleEvent(e, tcpClient, true);
											}
										}
										, () => { });

										Connected?.Invoke(this, new TcpClientConnectedEventArgs(_options.ServerAddress, _options.ServerPort));

										connectorInterface.ConnectedDelegate.Invoke(connector, $"TcpClient Connector connected to server {_options.ServerAddress}:{_options.ServerPort}");

										StartReadMessagesTask(tcpClient);

										TcpClient = tcpClient;
									}
									catch (Exception ex)
									{
										tcpClient.Close();
										tcpClient = null;

										Disconnected?.Invoke(this, new TcpClientDisconnectedEventArgs(_options.ServerAddress, _options.ServerPort, ex));
									}
								}
							}
						}
						catch (OperationCanceledException)
						{
							// stop request: do nothing
						}
						catch (Exception ex)
						{
							// unhandled exception: signal via event
							try
							{
								OnException?.Invoke(this, new ExceptionEventArgs(ex));
							}
							catch
							{
								// ignore this exception
							}
						}
					}
				}
				finally
				{
					_reconnectTaskTerminated.Set();
				}
			});
		}

		private void StartReadMessagesTask(TcpClient tcpClient)
		{
			_readers.Increment();

			Task.Factory.StartNew(() =>
			{
				try
				{
					while (!_stopToken.IsCancellationRequested)
					{
						object? message = _messageSerializer.DeserializeMessage(tcpClient.GetStream());
						// null here means "EOF": socket is closed
						if (message == null)
							break;

						HandleMessage(message);
					}
				}
				catch (Exception ex)
				{
					if (tcpClient.Connected)
					{
						tcpClient.Close();

						Disconnected?.Invoke(this, new TcpClientDisconnectedEventArgs(_options.ServerAddress, _options.ServerPort, ex));
					}
				}
				finally
				{
					_readers.Decrement();
				}
			});
		}

		private void HandleMessage(object message)
		{
			switch (message)
			{
				case TagWriteRequestCommand c:
					HandleTagWriteRequestCommand(c);
					break;
				default:
					throw new InvalidOperationException($"Message type {message.GetType().FullName} not managed");
			}
		}

		private void HandleTagWriteRequestCommand(TagWriteRequestCommand c)
		{
			_connectorInteface?.RequestTagWriteDelegate.Invoke(c.DeviceId, c.TagId, c.StartOffset, c.Data);
		}

		private void ConfigureTcpClient(TcpClient tcpClient)
		{
			// TODO configure tcp parameters
		}

		public void Stop()
		{
			_stopToken.Cancel();
			TcpClient?.Close();

			_readers.WaitUntilZero();
			_reconnectTaskTerminated.Wait();
		}
	}
}
