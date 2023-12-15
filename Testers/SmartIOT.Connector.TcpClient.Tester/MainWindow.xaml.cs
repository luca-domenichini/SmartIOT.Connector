#pragma warning disable S2589 // Boolean expressions should not be gratuitous

using SmartIOT.Connector.Messages;
using SmartIOT.Connector.Messages.Serializers;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Windows;

namespace SmartIOT.Connector.TcpClient.Tester;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private System.Net.Sockets.TcpClient? _tcpClient;
    private IStreamMessageSerializer? _messageSerializer;

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
        if (_tcpClient == null)
        {
            try
            {
                if (rdJsonSerializer.IsChecked == true)
                    _messageSerializer = new JsonStreamMessageSerializer();
                else
                    _messageSerializer = new ProtobufStreamMessageSerializer();

                var server = txtServer.Text;
                int port = 1883;
                if (server.Contains(':'))
                {
                    var arr = server.Split(":");
                    server = arr[0];
                    _ = int.TryParse(arr[1], out port);
                }

                _tcpClient = new System.Net.Sockets.TcpClient();
                _tcpClient.Connect(server, port);

                txtLogs.Text += "Connected.\r\n";

                StartReadThread();
            }
            catch (Exception ex)
            {
                txtLogs.Text += $"Exception caught: {ex.Message}\r\n{ex}\r\n";
            }
        }
    }

    private void StartReadThread()
    {
        new Thread(() =>
        {
            try
            {
                while (true)
                {
                    if (_tcpClient == null)
                        break;

                    var msg = _messageSerializer!.DeserializeMessage(_tcpClient.GetStream());

                    if (msg is null)
                    {
                        txtLogs.Dispatcher.Invoke(() => txtLogs.Text += "Disconnected\r\n");
                        break;
                    }
                    else if (msg is DeviceEvent)
                    {
                        string message = JsonSerializer.Serialize(msg);
                        txtLogs.Dispatcher.Invoke(() => txtLogs.Text += $"RECV DeviceStatus {message}\r\n");
                    }
                    else if (msg is TagEvent)
                    {
                        string message = JsonSerializer.Serialize(msg);
                        txtLogs.Dispatcher.Invoke(() => txtLogs.Text += $"RECV TagRead {message}\r\n");
                    }
                    else if (msg is PingMessage)
                    {
                        Dispatcher.Invoke(() => txtLogs.Text += "RECV Ping\r\n");
                    }
                    else
                    {
                        txtLogs.Dispatcher.Invoke(() => txtLogs.Text += $"RECV unknown message\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                Dispatcher.InvokeAsync(() => txtLogs.Text += $"Exception caught: {ex.Message}\r\n{ex}");
                _tcpClient?.Close();

                _tcpClient = null;
            }
        }).Start();
    }

    private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
    {
        if (_tcpClient != null)
        {
            try
            {
                _tcpClient.Close();
                _tcpClient = null;

                txtLogs.Text += "Disconnected\r\n";
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

        _messageSerializer!.SerializeMessage(_tcpClient!.GetStream(), msg);
    }

    private void BtnRequestWrite_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_tcpClient == null || !_tcpClient.Connected)
            {
                txtLogs.Text += "Client is not connected. Connect it first";
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
