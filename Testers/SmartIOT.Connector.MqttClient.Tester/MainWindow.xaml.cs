using SmartIOT.Connector.Messages;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows;
using SmartIOT.Connector.Messages.Serializers;

namespace SmartIOT.Connector.MqttClient.Tester
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private IMqttClient? _mqttClient;
		private ISingleMessageSerializer? _messageSerializer;
		private string? _deviceStatusTopic;
		private string? _tagReadTopic;
		private string? _tagWriteTopic;

		public MainWindow()
		{
			InitializeComponent();
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);

			Application.Current.Shutdown();
		}

		private void BtnClearLogs_Click(object sender, RoutedEventArgs e)
		{
			txtLogs.Text = string.Empty;
		}

		private void BtnConnect_Click(object sender, RoutedEventArgs e)
		{
			if (_mqttClient == null)
			{
				try
				{
					if (rdJsonSerializer.IsChecked == true)
						_messageSerializer = new JsonSingleMessageSerializer();
					else
						_messageSerializer = new ProtobufSingleMessageSerializer();

					var c = new MqttFactory().CreateMqttClient();

					c.UseApplicationMessageReceivedHandler(e =>
					{
						try
						{
							if (IsTopicRoot(_deviceStatusTopic, e.ApplicationMessage.Topic))
							{
								var msg = _messageSerializer.DeserializeMessage<DeviceEvent>(e.ApplicationMessage.Payload);
								string message = JsonSerializer.Serialize(msg);
								txtLogs.Dispatcher.Invoke(() => txtLogs.Text += $"RECV DeviceStatus {message}\r\n");
							}
							else if (IsTopicRoot(_tagReadTopic, e.ApplicationMessage.Topic))
							{
								var msg = _messageSerializer.DeserializeMessage<TagEvent>(e.ApplicationMessage.Payload);
								string message = JsonSerializer.Serialize(msg);
								txtLogs.Dispatcher.Invoke(() => txtLogs.Text += $"RECV TagRead {message}\r\n");
							}
							else if (IsTopicRoot(_tagWriteTopic, e.ApplicationMessage.Topic))
							{
								var msg = _messageSerializer.DeserializeMessage<TagEvent>(e.ApplicationMessage.Payload);
								string message = JsonSerializer.Serialize(msg);
								txtLogs.Dispatcher.Invoke(() => txtLogs.Text += $"RECV TagWrite {message}\r\n");
							}
							else
							{
								string message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
								txtLogs.Dispatcher.Invoke(() => txtLogs.Text += $"RECV from topic {e.ApplicationMessage.Topic}: {message}\r\n");
							}
						}
						catch (Exception ex)
						{
							txtLogs.Dispatcher.Invoke(() => txtLogs.Text += $"Exception deserializing: {ex.Message}\r\n{ex}\r\n");
						}
					});

					var topics = new List<MqttTopicFilter>();

					_deviceStatusTopic = txtDeviceStatusTopic.Text;
					_tagReadTopic = txtTagReadTopic.Text;
					_tagWriteTopic = txtTagWriteTopic.Text;

					if (chkDeviceStatusTopic.IsChecked == true)
					{
						topics.Add(new MqttTopicFilter()
						{
							Topic = txtDeviceStatusTopic.Text,
							QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
						});
					}
					if (chkTagReadTopic.IsChecked == true)
					{
						topics.Add(new MqttTopicFilter()
						{
							Topic = txtTagReadTopic.Text,
							QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
						});
					}
					if (chkTagWriteTopic.IsChecked == true)
					{
						topics.Add(new MqttTopicFilter()
						{
							Topic = txtTagWriteTopic.Text,
							QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
						});
					}

					var server = txtServer.Text;
					int port = 1883;
					if (server.Contains(':'))
					{
						var arr = server.Split(":");
						server = arr[0];
						_ = int.TryParse(arr[1], out port);
					}

					MqttClientOptionsBuilder clientOptions = new MqttClientOptionsBuilder()
						.WithClientId("Tester")
						.WithTcpServer(server, port);

					var result = c.ConnectAsync(clientOptions.Build()).Result;

					if (result.ResultCode == MQTTnet.Client.Connecting.MqttClientConnectResultCode.Success)
					{
						txtLogs.Text += "Connected.\r\n";

						c.SubscribeAsync(topics.ToArray()).Wait();
						
						txtLogs.Text += "Subscribed.\r\n";

						_mqttClient = c;
					}
					else
					{
						txtLogs.Text += $"Connection error: {result.ReasonString}";
					}
				}
				catch (Exception ex)
				{
					txtLogs.Text += $"Exception caught: {ex.Message}\r\n{ex}\r\n";
				}
			}
		}

		private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
		{
			if (_mqttClient != null)
			{
				try
				{
					_mqttClient.DisconnectAsync().Wait();
					_mqttClient.Dispose();
					_mqttClient = null;

					txtLogs.Text += "Disconnected\r\n";
				}
				catch (Exception ex)
				{
					txtLogs.Text += $"Exception caught: {ex.Message}\r\n{ex}\r\n";
				}
			}
		}

		public bool IsTopicRoot(string? subscribed, string topic)
		{
			if (subscribed == null)
				return false;

			if (!topic.EndsWith("/"))
				topic += "/";

			if (subscribed.Contains('/'))
			{
				var root = subscribed[..subscribed.IndexOf('/')];
				return topic.StartsWith(root);
			}
			else
			{
				return topic.StartsWith(subscribed + "/");
			}
		}

		private void DoWriteData(string deviceId, string tagId, string topic, int offset, byte[] data)
		{
			TagWriteRequestCommand msg = new TagWriteRequestCommand(deviceId, tagId, offset, data);

			_mqttClient.PublishAsync(new MqttApplicationMessageBuilder()
				.WithTopic(topic)
				.WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
				.WithPayload(_messageSerializer!.SerializeMessage(msg))
				.Build()
			).Wait();
		}

		private void BtnRequestWrite_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				if (_mqttClient == null || !_mqttClient.IsConnected)
				{
					txtLogs.Text += "Mqtt client is not connected. Connect it first";
					return;
				}

				string deviceId = TxtDeviceId.Text;
				string tagId = TxtTagId.Text;
				var topic = txtTagWriteTopic.Text;
				if (topic.Contains('/'))
					topic = topic[..topic.IndexOf('/')];
				int offset = int.Parse(TxtByteOffset.Text);
				byte[] data = TxtData.Text.Split(",")
					.SelectMany(x => x.Split(" "))
					.SelectMany(x => x.Split(';'))
					.Select(x => byte.TryParse(x, out var b) ? (byte?)b : null)
					.Where(x => x != null)
					.Select(x => x!.Value)
					.ToArray();

				DoWriteData(deviceId, tagId, topic, offset, data);
			}
			catch (Exception ex)
			{
				txtLogs.Text += $"Exception caught: {ex.Message}\r\n{ex}\r\n";
			}
		}
	}
}
