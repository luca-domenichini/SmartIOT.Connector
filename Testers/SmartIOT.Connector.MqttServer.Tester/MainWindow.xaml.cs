using MQTTnet;
using MQTTnet.Server;
using SmartIOT.Connector.Messages;
using SmartIOT.Connector.Messages.Serializers;
using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace SmartIOT.Connector.MqttServer.Tester;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private MQTTnet.Server.MqttServer? _mqttServer;
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

    private void BtnStartServer_Click(object sender, RoutedEventArgs e)
    {
        if (_mqttServer == null)
        {
            try
            {
                if (rdJsonSerializer.IsChecked == true)
                    _messageSerializer = new JsonSingleMessageSerializer();
                else
                    _messageSerializer = new ProtobufSingleMessageSerializer();

                MqttServerOptionsBuilder serverOptions = new MqttServerOptionsBuilder()
                    .WithDefaultEndpointPort(int.Parse(txtPort.Text));

                _mqttServer = new MqttFactory().CreateMqttServer(serverOptions.Build());

                _mqttServer.ClientConnectedAsync += OnClientConnected;
                _mqttServer.ClientDisconnectedAsync += OnClientDisconnected;
                _mqttServer.InterceptingPublishAsync += OnApplicationMessageReceived;

                _mqttServer.ClientSubscribedTopicAsync += OnClientSubscribedTopic;

                _mqttServer.StartAsync().Wait();

                _deviceStatusTopic = txtDeviceStatusTopic.Text;
                _tagReadTopic = txtTagReadTopic.Text;
                _tagWriteTopic = txtTagWriteTopic.Text;

                txtLogs.Text += "Started\r\n";
            }
            catch (Exception ex)
            {
                txtLogs.Text += $"Exception caught: {ex.Message}\r\n{ex}\r\n";
            }
        }
    }

    private Task OnClientSubscribedTopic(ClientSubscribedTopicEventArgs obj)
    {
        try
        {
            txtLogs.Dispatcher.Invoke(() =>
            {
                var message = $"Client {obj.ClientId} subscribed to topic {obj.TopicFilter.Topic}\r\n";
                txtLogs.Text += message;
            });
        }
        catch (Exception ex)
        {
            txtLogs.Dispatcher.Invoke(() => txtLogs.Text += $"Exception: {ex.Message}\r\n{ex}\r\n");
        }

        return Task.CompletedTask;
    }

    private Task OnApplicationMessageReceived(InterceptingPublishEventArgs e)
    {
        return Task.Run(() =>
        {
            try
            {
                if (_messageSerializer == null)
                    throw new InvalidOperationException("Serializer is null");

                if (IsTopicRoot(_deviceStatusTopic, e.ApplicationMessage.Topic))
                {
                    var msg = _messageSerializer.DeserializeMessage<DeviceEvent>(e.ApplicationMessage.PayloadSegment.Array!);
                    string message = JsonSerializer.Serialize(msg);
                    txtLogs.Dispatcher.Invoke(() => txtLogs.Text += $"RECV DeviceStatus {message}\r\n");
                }
                else if (IsTopicRoot(_tagReadTopic, e.ApplicationMessage.Topic))
                {
                    var msg = _messageSerializer.DeserializeMessage<TagEvent>(e.ApplicationMessage.PayloadSegment.Array!);
                    string message = JsonSerializer.Serialize(msg);
                    txtLogs.Dispatcher.Invoke(() => txtLogs.Text += $"RECV TagRead {message}\r\n");
                }
                else if (IsTopicRoot(_tagWriteTopic, e.ApplicationMessage.Topic))
                {
                    var msg = _messageSerializer.DeserializeMessage<TagEvent>(e.ApplicationMessage.PayloadSegment.Array!);
                    string message = JsonSerializer.Serialize(msg);
                    txtLogs.Dispatcher.Invoke(() => txtLogs.Text += $"RECV TagWrite {message}\r\n");
                }
                else
                {
                    string message = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment.Array!);
                    txtLogs.Dispatcher.Invoke(() => txtLogs.Text += $"RECV from topic {e.ApplicationMessage.Topic}: {message}\r\n");
                }
            }
            catch (Exception ex)
            {
                txtLogs.Dispatcher.Invoke(() => txtLogs.Text += $"Exception deserializing: {ex.Message}\r\n{ex}\r\n");
            }
        });
    }

    private Task OnClientDisconnected(ClientDisconnectedEventArgs e)
    {
        return Task.Run(() =>
        {
            try
            {
                txtLogs.Dispatcher.Invoke(() =>
                {
                    var message = $"Client {e.ClientId} disconnected {e.DisconnectType}\r\n";
                    txtLogs.Text += message;
                });
            }
            catch (Exception ex)
            {
                txtLogs.Dispatcher.Invoke(() => txtLogs.Text += $"Exception: {ex.Message}\r\n{ex}\r\n");
            }
        });
    }

    private Task OnClientConnected(ClientConnectedEventArgs e)
    {
        return Task.Run(() =>
        {
            try
            {
                txtLogs.Dispatcher.Invoke(() =>
                {
                    var message = $"Client {e.ClientId} connected\r\n";
                    txtLogs.Text += message;
                });
            }
            catch (Exception ex)
            {
                txtLogs.Dispatcher.Invoke(() => txtLogs.Text += $"Exception: {ex.Message}\r\n{ex}\r\n");
            }
        });
    }

    private void BtnStopServer_Click(object sender, RoutedEventArgs e)
    {
        if (_mqttServer != null)
        {
            try
            {
                _mqttServer.StopAsync().Wait();
                _mqttServer.Dispose();
                _mqttServer = null!;

                txtLogs.Text += "Stopped\r\n";
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

        if (!topic.EndsWith('/'))
            topic += "/";

        if (subscribed.Contains('/'))
        {
            var root = subscribed[..(subscribed.IndexOf('/') + 1)];
            return topic.StartsWith(root);
        }
        else
        {
            return topic.StartsWith(subscribed);
        }
    }

    private void DoWriteData(string deviceId, string tagId, string topic, int offset, byte[] data)
    {
        TagWriteRequestCommand msg = new TagWriteRequestCommand(deviceId, tagId, offset, data);

        _mqttServer!.InjectApplicationMessage(new InjectedMqttApplicationMessage(
            new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithPayload(_messageSerializer!.SerializeMessage(msg))
                .Build()
            )).Wait();
    }

    private void BtnRequestWrite_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_mqttServer == null || !_mqttServer.IsStarted)
            {
                txtLogs.Text += "Mqtt server is not started. Start it first";
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
