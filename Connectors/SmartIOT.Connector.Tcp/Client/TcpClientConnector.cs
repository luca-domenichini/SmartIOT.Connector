using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Connector;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Messages;
using SmartIOT.Connector.Messages.Serializers;
using System.Net.Sockets;

namespace SmartIOT.Connector.Tcp.Client
{
	public class TcpClientConnector : AbstractPublisherConnector
	{
		private new TcpClientConnectorOptions Options => (TcpClientConnectorOptions)base.Options;

		private TcpClient? _tcpClient;
		private readonly IStreamMessageSerializer _messageSerializer;
		private readonly CancellationTokenSource _stopToken = new CancellationTokenSource();
		private readonly ManualResetEventSlim _reconnectTaskTerminated = new ManualResetEventSlim();
		private readonly CountdownLatch _clients = new CountdownLatch();

		public TcpClientConnector(TcpClientConnectorOptions options)
			: base(options)
		{
			_messageSerializer = options.MessageSerializer;
		}


		protected override void PublishDeviceStatusEvent(DeviceStatusEvent e)
		{
			PublishDeviceStatusEvent(e, _tcpClient);
		}
		private void PublishDeviceStatusEvent(DeviceStatusEvent e, TcpClient? tcpClient)
		{
			SendMessage(tcpClient, e.ToEventMessage());
		}

		protected override void PublishException(Exception exception)
		{
			PublishException(exception, _tcpClient);
		}
		private void PublishException(Exception exception, TcpClient? tcpClient)
		{
			SendMessage(tcpClient, new ExceptionEvent(exception.Message, exception.ToString()));
		}

		protected override void PublishTagScheduleEvent(TagScheduleEvent e)
		{
			PublishTagScheduleEvent(e, _tcpClient, false);
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
					ConnectorInterface!.OnConnectorDisconnected(new ConnectorDisconnectedEventArgs(this, $"Disconnected from server {Options.ServerAddress}:{Options.ServerPort}: {ex.Message}", ex));
				}
			}
		}

		public override void Start(ISmartIOTConnectorInterface connectorInterface)
		{
			base.Start(connectorInterface);

			Task.Factory.StartNew(async () =>
			{
				try
				{
					while (!_stopToken.IsCancellationRequested)
					{
						try
						{
							if (!(_tcpClient?.Connected ?? true))
							{
								_tcpClient?.Close();
								_tcpClient = null;
							}

							if (_tcpClient == null)
							{
								var tcpClient = new TcpClient();
								ConfigureTcpClient(tcpClient);

								try
								{
									await tcpClient.ConnectAsync(Options.ServerAddress, Options.ServerPort, _stopToken.Token);
								}
								catch (Exception ex)
								{
									tcpClient.Close();
									tcpClient = null;

									if (ex is not OperationCanceledException)
										connectorInterface.OnConnectorConnectionFailed(new ConnectorConnectionFailedEventArgs(this, $"TcpClient Connector failed to connect to host {Options.ServerAddress}:{Options.ServerPort}: {ex.Message}", ex));
								}

								if (tcpClient != null && tcpClient.Connected)
								{
									try
									{
										// locking on tcpClient to handle concurrency on message events vs initialization
										lock (tcpClient)
										{
											_tcpClient = tcpClient;

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

											connectorInterface.OnConnectorConnected(new ConnectorConnectedEventArgs(this, $"Connected to server {Options.ServerAddress}:{Options.ServerPort}"));

											StartHandlingTcpClient(tcpClient);
										}
									}
									catch (Exception ex)
									{
										tcpClient.Close();
										tcpClient = null;

										ConnectorInterface!.OnConnectorDisconnected(new ConnectorDisconnectedEventArgs(this, $"Disconnected from server {Options.ServerAddress}:{Options.ServerPort}: {ex.Message}", ex));
									}
								}
							}

							await Task.Delay(Options.ReconnectInterval, _stopToken.Token);
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
								ConnectorInterface!.OnConnectorException(new ConnectorExceptionEventArgs(this, ex));
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

		private void StartHandlingTcpClient(TcpClient tcpClient)
		{
			_clients.Increment();

			// processing thread
			new Thread(() =>
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

						ConnectorInterface!.OnConnectorDisconnected(new ConnectorDisconnectedEventArgs(this, $"Disconnected from server {Options.ServerAddress}:{Options.ServerPort}: {ex.Message}", ex));
					}
				}
				finally
				{
					_clients.Decrement();
				}
			})
			{
				Name = "TcpClient.Reader"
			}.Start();

			// ping thread
			if (Options.PingInterval > TimeSpan.Zero)
			{
				new Thread(() =>
				{
					try
					{
						while (!_stopToken.Token.WaitHandle.WaitOne(5000))
						{
							lock (tcpClient)
							{
								_messageSerializer.SerializeMessage(tcpClient.GetStream(), new PingMessage());
							}
						}
					}
					catch (Exception ex)
					{
						ConnectorInterface!.OnConnectorException(new ConnectorExceptionEventArgs(this, ex));
					}
				})
				{
					Name = "TcpClient.Ping",
					IsBackground = true
				}.Start();
			}
		}

		private void HandleMessage(object message)
		{
			switch (message)
			{
				case TagWriteRequestCommand c:
					HandleTagWriteRequestCommand(c);
					break;
				case PingMessage:
					break;
				default:
					throw new InvalidOperationException($"Message type {message.GetType().FullName} not managed");
			}
		}

		private void HandleTagWriteRequestCommand(TagWriteRequestCommand c)
		{
			ConnectorInterface!.RequestTagWrite(c.DeviceId, c.TagId, c.StartOffset, c.Data);
		}

		private void ConfigureTcpClient(TcpClient tcpClient)
		{
			
		}

		public override void Stop()
		{
			base.Stop();

			_stopToken.Cancel();
			_tcpClient?.Close();

			_clients.WaitUntilZero();
			_reconnectTaskTerminated.Wait();
		}
	}
}
