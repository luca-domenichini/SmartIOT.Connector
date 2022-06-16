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
		private IConnector? _connector;
		private ISmartIOTConnectorInterface? _connectorInterface;

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
					// locking on tcpClient to handle concurrency on message events vs initialization
					lock (tcpClient)
					{
						_messageSerializer.SerializeMessage(tcpClient.GetStream(), message);
					}
				}
				catch (Exception ex)
				{
					tcpClient.Close();
					_connectorInterface!.OnConnectorDisconnected(new ConnectorDisconnectedEventArgs(_connector!, $"TcpClient disconnected from host {_options.ServerAddress}:{_options.ServerPort}: {ex.Message}", ex));
				}
			}
		}

		public void Start(IConnector connector, ISmartIOTConnectorInterface connectorInterface)
		{
			_connector = connector;
			_connectorInterface = connectorInterface;

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
										connectorInterface.OnConnectorConnectionFailed(new ConnectorConnectionFailedEventArgs(connector, $"TcpClient Connector failed to connect to host {_options.ServerAddress}:{_options.ServerPort}: {ex.Message}", ex));
								}

								if (tcpClient != null && tcpClient.Connected)
								{
									try
									{
										// locking on tcpClient to handle concurrency on message events vs initialization
										lock (tcpClient)
										{
											TcpClient = tcpClient;

											// send initialization events
											connectorInterface.RunInitializationAction((deviceStatusEvents, tagEvents) =>
											{
												foreach (var e in deviceStatusEvents)
												{
													PublishDeviceStatusEvent(e, tcpClient);
												}
												foreach (var e in tagEvents)
												{
													PublishTagScheduleEvent(e, tcpClient, true);
												}
											});

											connectorInterface.OnConnectorConnected(new ConnectorConnectedEventArgs(connector, $"TcpClient Connector connected to server {_options.ServerAddress}:{_options.ServerPort}"));

											StartReadMessagesTask(tcpClient);
										}
									}
									catch (Exception ex)
									{
										tcpClient.Close();
										tcpClient = null;

										_connectorInterface!.OnConnectorDisconnected(new ConnectorDisconnectedEventArgs(connector, $"TcpClient Connector disconnected from host {_options.ServerAddress}:{_options.ServerPort}: {ex.Message}", ex));
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
							// unhandled exception: signal via interface
							try
							{
								_connectorInterface!.OnConnectorException(new ConnectorExceptionEventArgs(connector, ex));
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

						_connectorInterface!.OnConnectorDisconnected(new ConnectorDisconnectedEventArgs(_connector!, $"TcpClient Connector disconnected from host {_options.ServerAddress}:{_options.ServerPort}: {ex.Message}", ex));
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
			_connectorInterface!.RequestTagWrite(c.DeviceId, c.TagId, c.StartOffset, c.Data);
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
