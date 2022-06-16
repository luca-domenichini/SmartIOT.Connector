using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Connector;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Messages;
using SmartIOT.Connector.Messages.Serializers;
using System.Net.Sockets;

namespace SmartIOT.Connector.Tcp.Server
{
	public class TcpServerEventPublisher : IConnectorEventPublisher
	{
		private readonly IStreamMessageSerializer _messageSerializer;
		private readonly TcpServerEventPublisherOptions _options;
		private readonly TcpListener _tcpListener;
		private IConnector? _connector;
		private ISmartIOTConnectorInterface? _connectorInterface;
		private readonly CancellationTokenSource _stopToken = new CancellationTokenSource();
		private readonly TcpServerClientCollection _clients = new TcpServerClientCollection();

		public TcpServerEventPublisher(IStreamMessageSerializer messageSerializer, TcpServerEventPublisherOptions options)
		{
			_messageSerializer = messageSerializer;
			_options = options;
			_tcpListener = new TcpListener(System.Net.IPAddress.Any, _options.ServerPort);
		}

		public void PublishDeviceStatusEvent(DeviceStatusEvent e)
		{
			BroadcastMessage(e.ToEventMessage());
		}

		public void PublishException(Exception exception)
		{
			BroadcastMessage(exception.ToEventMessage());
		}

		public void PublishTagScheduleEvent(TagScheduleEvent e)
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

					_connectorInterface!.OnConnectorDisconnected(new ConnectorDisconnectedEventArgs(_connector!, $"Client disconnected from TcpServer Connector: {ex.Message}", ex));
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

		public void Start(IConnector connector, ISmartIOTConnectorInterface connectorInterface)
		{
			_connector = connector;
			_connectorInterface = connectorInterface;

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
							connectorInterface.OnConnectorException(new ConnectorExceptionEventArgs(connector, ex));
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
					// locking on tcpClient to handle concurrency on message events vs initialization
					lock (tcpClient)
					{
						_clients.Add(tcpClient);

						_connectorInterface!.RunInitializationAction((deviceStatusEvents, tagScheduleEvents) =>
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
						try
						{
							var message = _messageSerializer.DeserializeMessage(tcpClient.GetStream());
							if (message == null)
								break;

							HandleMessage(message);
						}
						catch (Exception ex) when (ex is OperationCanceledException || ex is ObjectDisposedException)
						{
							break;
						}
					}
				}
				catch (Exception ex)
				{
					try
					{
						_connectorInterface!.OnConnectorDisconnected(new ConnectorDisconnectedEventArgs(_connector!, $"TcpServer Connector: unexpected exception occurred with a client: {ex.Message}", ex));
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
				IsBackground = true,
				Name = "TcpServer.ClientHandler"
			}.Start();
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

		public void Stop()
		{
			_stopToken.Cancel();
			_tcpListener.Stop();
		}
	}
}
