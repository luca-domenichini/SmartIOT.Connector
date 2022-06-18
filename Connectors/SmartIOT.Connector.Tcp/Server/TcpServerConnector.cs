using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Connector;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Messages;
using SmartIOT.Connector.Messages.Serializers;
using System.Net.Sockets;

namespace SmartIOT.Connector.Tcp.Server
{
	public class TcpServerConnector : AbstractPublisherConnector
	{
		private readonly IStreamMessageSerializer _messageSerializer;
		private new TcpServerConnectorOptions Options => (TcpServerConnectorOptions)base.Options;
		private readonly TcpListener _tcpListener;
		private readonly CancellationTokenSource _stopToken = new CancellationTokenSource();
		private readonly TcpServerClientCollection _clients = new TcpServerClientCollection();

		public TcpServerConnector(TcpServerConnectorOptions options)
			: base(options)
		{
			_messageSerializer = options.MessageSerializer;
			_tcpListener = new TcpListener(System.Net.IPAddress.Any, Options.ServerPort);
		}

		protected override void PublishDeviceStatusEvent(DeviceStatusEvent e)
		{
			BroadcastMessage(e.ToEventMessage());
		}

		protected override void PublishException(Exception exception)
		{
			BroadcastMessage(exception.ToEventMessage());
		}

		protected override void PublishTagScheduleEvent(TagScheduleEvent e)
		{
			BroadcastMessage(e.ToEventMessage());
		}

		private void BroadcastMessage(object message)
		{
			foreach (var tcpClient in _clients)
			{
				try
				{
					SendSingleMessage(tcpClient, message);
				}
				catch (Exception ex)
				{
					CloseTcpClientQuietly(tcpClient);

					ConnectorInterface!.OnConnectorDisconnected(new ConnectorDisconnectedEventArgs(this, $"Client disconnected: {ex.Message}", ex));
				}
			}
		}

		private void SendSingleMessage(TcpClient tcpClient, object message)
		{
			// locking on tcpClient to handle concurrency on message events vs initialization
			lock (tcpClient)
			{
				_messageSerializer.SerializeMessage(tcpClient.GetStream(), message);
			}
		}

		public override void Start(ISmartIOTConnectorInterface connectorInterface)
		{
			base.Start(connectorInterface);

			_tcpListener.Start();

			// spawn a new task for accepting connections
			Task.Run(async () =>
			{
				while (!_stopToken.IsCancellationRequested)
				{
					try
					{
						var tcpClient = await _tcpListener.AcceptTcpClientAsync(_stopToken.Token);

						StartTcpClientHandlerThread(tcpClient);
					}
					catch (OperationCanceledException)
					{
						break;
					}
					catch (Exception ex)
					{
						try
						{
							connectorInterface.OnConnectorException(new ConnectorExceptionEventArgs(this, ex));
						}
						catch
						{
							// ignoring this
						}
					}
				}
			});
		}

		private void StartTcpClientHandlerThread(TcpClient tcpClient)
		{
			// start a new task to handle the communication with a specific client
			new Thread(() =>
			{
				try
				{
					ConnectorInterface!.OnConnectorConnected(new ConnectorConnectedEventArgs(this, "Client connected"));

					// locking on tcpClient to handle concurrency on message events vs initialization
					lock (tcpClient)
					{
						_clients.Add(tcpClient);

						ConnectorInterface!.RunInitializationAction((deviceStatusEvents, tagScheduleEvents) =>
						{
							foreach (var deviceStatusEvent in deviceStatusEvents)
							{
								SendSingleMessage(tcpClient, deviceStatusEvent.ToEventMessage());
							}
							foreach (var tagScheduleEvent in tagScheduleEvents)
							{
								SendSingleMessage(tcpClient, tagScheduleEvent.ToEventMessage(true));
							}
						});
					}

					// reading messages
					while (!_stopToken.IsCancellationRequested)
					{
						var message = _messageSerializer.DeserializeMessage(tcpClient.GetStream());
						if (message == null)
							break;

						HandleMessage(message);
					}
				}
				catch (Exception ex) when (ex is not OperationCanceledException)
				{
					try
					{
						ConnectorInterface!.OnConnectorDisconnected(new ConnectorDisconnectedEventArgs(this, $"Unexpected exception occurred with a client: {ex.Message}", ex));
					}
					catch
					{
						// ignoring this
					}
				}
				finally
				{
					CloseTcpClientQuietly(tcpClient);
				}
			})
			{
				Name = "TcpServer.ClientHandler"
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
							SendSingleMessage(tcpClient, new PingMessage());
						}
					}
					catch (Exception ex)
					{
						ConnectorInterface!.OnConnectorException(new ConnectorExceptionEventArgs(this, ex));
					}
				})
				{
					Name = "TcpServer.Ping",
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

		private void CloseTcpClientQuietly(TcpClient tcpClient)
		{
			_clients.Remove(tcpClient);

			try
			{
				tcpClient.Close();
			}
			catch
			{
				// ignoring this
			}
		}

		public override void Stop()
		{
			base.Stop();

			_stopToken.Cancel();
			_tcpListener.Stop();
		}
	}
}
