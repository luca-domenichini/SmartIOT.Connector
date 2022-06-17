using SmartIOT.Connector.Messages;
using SmartIOT.Connector.Messages.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace SmartIOT.Connector.TcpServer.Tester
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private TcpListener? _tcpListener;
		private IList<TcpClient> _clients = new List<TcpClient>();
		private IStreamMessageSerializer? _messageSerializer;

		public MainWindow()
		{
			InitializeComponent();
		}

		private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
		{
			txtLogs.Text = string.Empty;
		}

		private void BtnStartServer_Click(object sender, RoutedEventArgs e)
		{
			if (_tcpListener == null)
			{
				try
				{
					if (rdJsonSerializer.IsChecked == true)
						_messageSerializer = new JsonStreamMessageSerializer();
					else
						_messageSerializer = new ProtobufStreamMessageSerializer();

					_tcpListener = new TcpListener(System.Net.IPAddress.Any, int.Parse(txtPort.Text));
					_tcpListener.Start();

					var tcpClient = _tcpListener.AcceptTcpClient();

					StartTaskForClient(tcpClient);

					txtLogs.Text += "Started\r\n";
				}
				catch (Exception ex)
				{
					txtLogs.Text += $"Exception caught: {ex.Message}\r\n{ex}\r\n";
				}
			}
		}

		private void StartTaskForClient(TcpClient tcpClient)
		{
			Task.Factory.StartNew(() =>
			{
				try
				{
					Dispatcher.Invoke(() => txtLogs.Text += $"Client connected\r\n");
					_clients.Add(tcpClient);

					while (tcpClient.Connected)
					{
						var msg = _messageSerializer!.DeserializeMessage(tcpClient.GetStream());

						if (msg is DeviceEvent)
						{
							string message = JsonSerializer.Serialize(msg);
							Dispatcher.Invoke(() => txtLogs.Text += $"RECV DeviceStatus {message}\r\n");
						}
						else if (msg is TagEvent)
						{
							string message = JsonSerializer.Serialize(msg);
							Dispatcher.Invoke(() => txtLogs.Text += $"RECV TagRead {message}\r\n");
						}
						else
						{
							Dispatcher.Invoke(() => txtLogs.Text += $"RECV unknown message\r\n");
						}
					}
				}
				catch (Exception ex)
				{
					Dispatcher.Invoke(() => txtLogs.Text += $"Exception caught: {ex.Message}\r\n{ex}\r\n");
					tcpClient.Close();
				}
				finally
				{
					_clients.Remove(tcpClient);
					Dispatcher.Invoke(() => txtLogs.Text += $"Client disconnected\r\n");
				}
			});
		}

		private void BtnStopServer_Click(object sender, RoutedEventArgs e)
		{
			if (_tcpListener != null)
			{
				try
				{
					_tcpListener.Stop();
					_tcpListener = null;

					txtLogs.Text += "Stopped\r\n";
				}
				catch (Exception ex)
				{
					txtLogs.Text += $"Exception caught: {ex.Message}\r\n{ex}\r\n";
				}
			}
		}

		private void DoWriteData(string deviceId, string tagId, int offset, byte[] data)
		{
			TagWriteRequestCommand msg = new TagWriteRequestCommand(deviceId, tagId, offset, data);

			foreach (var client in _clients)
			{
				_messageSerializer!.SerializeMessage(client.GetStream(), msg);
			}
		}

		private void BtnRequestWrite_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (_tcpListener == null)
				{
					txtLogs.Text += "Server is not started. Start it first";
					return;
				}

				string deviceId = TxtDeviceId.Text;
				string tagId = TxtTagId.Text;
				int offset = int.Parse(TxtByteOffset.Text);
				byte[] data = TxtData.Text.Split(",")
					.SelectMany(x => x.Split(" "))
					.SelectMany(x => x.Split(';'))
					.Select(x => byte.TryParse(x, out var b) ? (byte?)b : null)
					.Where(x => x != null)
					.Select(x => x!.Value)
					.ToArray();

				DoWriteData(deviceId, tagId, offset, data);
			}
			catch (Exception ex)
			{
				txtLogs.Text += $"Exception caught: {ex.Message}\r\n{ex}\r\n";
			}
		}
	}
}
