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
		private ConnectorInterface? _connectorInterface;
		private readonly CancellationTokenSource _stopToken = new CancellationTokenSource();
		private readonly TcpServerClientCollection _clients = new TcpServerClientCollection();

		public event EventHandler<ExceptionEventArgs>? OnException;
		public event EventHandler<TcpServerClientConnectedEventArgs>? Connected;
		public event EventHandler<TcpServerClientDisconnectedEventArgs>? Disconnected;

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

					Disconnected?.Invoke(this, new TcpServerClientDisconnectedEventArgs(tcpClient, ex));
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

		public void Start(IConnector connector, ConnectorInterface connectorInterface)
		{
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

						StartTcpClientHandlerTask(tcpClient);
					}
					catch (OperationCanceledException)
					{
						break;
					}
					catch (Exception ex)
					{
						try
						{
							OnException?.Invoke(this, new ExceptionEventArgs(ex));
						}
						catch
						{
							// ignoring this
						}
					}
				}
			});
		}

		private void StartTcpClientHandlerTask(TcpClient tcpClient)
		{
			// start a new task to handle the communication with a specific client
			Task.Factory.StartNew(() =>
			{
				try
				{
					// locking on tcpClient to handle concurrency on message events vs initialization
					lock (tcpClient)
					{
						_clients.Add(tcpClient);

						_connectorInterface!.InitializationActionDelegate.Invoke((deviceStatusEvents, tagScheduleEvents) =>
						{
							foreach (var deviceStatusEvent in deviceStatusEvents)
							{
								SendSingleMessage(tcpClient, deviceStatusEvent.ToEventMessage());
							}
							foreach (var tagScheduleEvent in tagScheduleEvents)
							{
								SendSingleMessage(tcpClient, tagScheduleEvent.ToEventMessage(true));
							}
						}
						, () => { });
					}

					// reading messages
					while (!_stopToken.IsCancellationRequested)
					{
						try
						{
							var message = _messageSerializer.DeserializeMessage(tcpClient.GetStream());
							if (message == null)
								break;

							if (message is TagWriteRequestCommand c)
							{
								_connectorInterface!.RequestTagWriteDelegate.Invoke(c.DeviceId, c.TagId, c.StartOffset, c.Data);
							}
							else
								throw new NotImplementedException($"Message type {message.GetType().FullName} not handled");
						}
						catch (OperationCanceledException)
						{
							break;
						}
					}
				}
				catch (Exception ex)
				{
					try
					{
						Disconnected?.Invoke(this, new TcpServerClientDisconnectedEventArgs(tcpClient, ex));
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
			});
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
