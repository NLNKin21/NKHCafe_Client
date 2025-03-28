using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NKHCafe_Client.Forms
{
    public partial class frmChat : System.Windows.Forms.Form
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private bool _isConnected = false;
        private const int ServerPort = 12345; // Cổng mặc định
        private const string DefaultServerIP = "127.0.0.1"; // IP mặc định

        public frmChat()
        {
            InitializeComponent();
        }

        private void frmChat_Load(object sender, EventArgs e)
        {
            txtServerIP.Text = DefaultServerIP; // Gán giá trị mặc định
            txtServerPort.Text = ServerPort.ToString(); // Gán giá trị mặc định
            lblStatus.Text = "Trạng thái: Chưa kết nối";
            // ConnectToServer(); // Không tự động kết nối khi form load
        }
        private void btnConnect_Click(object sender, EventArgs e)
        {
            if (!_isConnected)
            {
                ConnectToServer();
            }
            else
            {
                Disconnect();
            }
        }

        private void ConnectToServer()
        {
            try
            {
                string serverIP = txtServerIP.Text.Trim();
                int serverPort;
                if (!int.TryParse(txtServerPort.Text, out serverPort))
                {
                    MessageBox.Show("Cổng không hợp lệ.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                _client = new TcpClient();
                _client.Connect(serverIP, serverPort);
                _stream = _client.GetStream();
                _isConnected = true;

                _receiveThread = new Thread(ReceiveMessages);
                _receiveThread.Start();
                AppendToChatLog("Đã kết nối đến server!");
                btnConnect.Text = "Ngắt kết nối"; // Đổi text nút
                lblStatus.Text = "Trạng thái: Đã kết nối";

            }
            catch (SocketException ex)
            {
                MessageBox.Show("Lỗi kết nối đến server: " + ex.SocketErrorCode + " - " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối đến server: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void Disconnect()
        {
            _isConnected = false;
            if (_receiveThread != null && _receiveThread.IsAlive)
            {
                _receiveThread.Join();
            }
            if (_stream != null)
            {
                _stream.Close();
            }
            if (_client != null)
            {
                _client.Close();
            }

            btnConnect.Text = "Kết nối";
            lblStatus.Text = "Trạng thái: Chưa kết nối";
            AppendToChatLog("Đã ngắt kết nối.");
        }

        private void ReceiveMessages()
        {
            try
            {
                byte[] buffer = new byte[1024];
                int bytesRead;

                while (_isConnected)
                {
                    bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        AppendToChatLog("Server đã ngắt kết nối.");
                        Disconnect(); // Tự động ngắt kết nối
                        break;
                    }
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        AppendToChatLog("Server: " + message);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_isConnected)
                {
                    MessageBox.Show("Lỗi nhận tin nhắn: " + ex.Message);
                    Disconnect(); // Tự động ngắt kết nối khi có lỗi
                }
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            SendMessage(txtMessage.Text);
            txtMessage.Clear();
        }

        private void SendMessage(string message)
        {
            try
            {
                if (_stream != null && _stream.CanWrite)
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(message);
                    _stream.Write(buffer, 0, buffer.Length);
                    AppendToChatLog("Client: " + message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi gửi tin nhắn: " + ex.Message);
            }
        }
        private delegate void AppendToChatLogDelegate(string text);
        private void AppendToChatLog(string text)
        {
            if (rtbChatLog.InvokeRequired)
            {
                if (!rtbChatLog.IsDisposed)
                {
                    rtbChatLog.Invoke(new AppendToChatLogDelegate(AppendToChatLog), text);
                }
            }
            else
            {
                if (!rtbChatLog.IsDisposed)
                {
                    rtbChatLog.AppendText(text + Environment.NewLine);
                    rtbChatLog.ScrollToCaret();
                }
            }
        }

        private void frmChat_FormClosing(object sender, FormClosingEventArgs e)
        {
            Disconnect(); // Ngắt kết nối khi đóng form
        }
    }
}