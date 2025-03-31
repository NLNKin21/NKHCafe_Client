using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NKHCafe_Client.Network;

namespace NKHCafe_Client.Forms
{
    public partial class frmChat : Form // Remove System.Windows.Forms. if not needed everywhere
    {
        // Option 1: If frmChat needs its OWN independent connection
        // private SocketClient _socketClient;
        // private bool _isManagingOwnConnection = true; // Flag to differentiate behavior

        // Option 2: If frmChat USES the connection from frmClientMain (Recommended based on previous context)
        private readonly SocketClient _socketClient; // Use readonly if passed via constructor
        private readonly int _idTaiKhoan;
        private readonly string _tenDangNhap;
        private readonly int _idMay; // Maybe needed for context
        private bool _isManagingOwnConnection = false;

        // --- Constructor ---

        // Keep default for Designer compatibility (Optional, can be removed if not needed)
        // public frmChat()
        // {
        //     InitializeComponent();
        //      // Disable controls if opened without parameters
        //      DisableChatControls("Constructor mặc định không dùng.");
        //      _isManagingOwnConnection = false; // Or handle as an error?
        // }

        // Constructor receiving connection and user info from frmClientMain
        public frmChat(int idTaiKhoan, int idMay, string tenDangNhap, SocketClient sharedSocketClient)
        {
            InitializeComponent();

            _idTaiKhoan = idTaiKhoan;
            _idMay = idMay;
            _tenDangNhap = tenDangNhap;
            _socketClient = sharedSocketClient; // Use the shared client
            _isManagingOwnConnection = false;

            this.Text = $"Chat - {_tenDangNhap} (Máy {_idMay})";

            // Subscribe to events of the SHARED socket client
            if (_socketClient != null)
            {
                _socketClient.OnMessageReceived -= HandleSharedSocketMessage; // Unsubscribe first
                _socketClient.OnMessageReceived += HandleSharedSocketMessage;
                _socketClient.OnDisconnected -= HandleSharedSocketDisconnected; // Unsubscribe first
                _socketClient.OnDisconnected += HandleSharedSocketDisconnected;

                // Update UI based on initial state of the shared client
                UpdateChatAvailability(_socketClient.IsConnected);
            }
            else
            {
                // Handle case where null socket is passed (Error)
                DisableChatControls("Lỗi: Không có kết nối hợp lệ.");
                MessageBox.Show("Không thể khởi tạo chat do lỗi kết nối.", "Lỗi Chat", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            // Hide connection controls if using shared connection
            HideConnectionControls();
        }

        // --- UI Helper Methods ---
        private void HideConnectionControls()
        {
            // Hide IP, Port, Connect Button, Status Label related to manual connection
            lblServerIP.Visible = false; // Assuming labels exist
            txtServerIP.Visible = false;
            lblServerPort.Visible = false;
            txtServerPort.Visible = false;
            btnConnect.Visible = false;
            lblStatus.Visible = false; // Or repurpose for shared connection status
        }

        private void UpdateChatAvailability(bool isAvailable)
        {
            if (this.IsDisposed) return;
            if (this.InvokeRequired)
            {
                try { this.Invoke((Action)(() => SetChatControlsEnabled(isAvailable))); } catch { }
            }
            else
            {
                SetChatControlsEnabled(isAvailable);
            }
        }

        private void SetChatControlsEnabled(bool isEnabled)
        {
            if (this.IsDisposed) return;
            txtMessage.Enabled = isEnabled; // Input textbox name
            btnSend.Enabled = isEnabled;    // Send button name
                                            // Optionally update status label if repurposed
                                            // lblStatus.Text = isEnabled ? "Trạng thái: Đã kết nối" : "Trạng thái: Mất kết nối";
        }

        private void DisableChatControls(string reason)
        {
            AppendToChatLog($"*** {reason} ***");
            SetChatControlsEnabled(false);
        }

        // --- Event Handlers for SHARED Socket ---
        private void HandleSharedSocketMessage(string response)
        {
            if (this.InvokeRequired)
            {
                try { this.Invoke((Action)(() => ProcessChatMessage(response))); } catch { }
            }
            else { ProcessChatMessage(response); }
        }

        private void HandleSharedSocketDisconnected()
        {
            if (this.InvokeRequired)
            {
                try { this.Invoke((Action)(() => ProcessChatDisconnection())); } catch { }
            }
            else { ProcessChatDisconnection(); }
        }

        private void ProcessChatDisconnection()
        {
            if (this.IsDisposed) return;
            AppendToChatLog("*** Mất kết nối đến server ***");
            UpdateChatAvailability(false);
        }

        // --- Chat Message Processing ---
        private void ProcessChatMessage(string response)
        {
            if (this.IsDisposed) return;
            if (MessageHandler.ParseServerResponse(response, out string command, out string[] dataParts))
            {
                // Only process commands relevant to chat
                if (command == "CHAT" && dataParts.Length > 0)
                {
                    string sender = "Server";
                    string messageContent = dataParts[0];

                    if (dataParts.Length > 1) // CHAT|Sender|Content format
                    {
                        sender = dataParts[0];
                        messageContent = dataParts[1];
                        // Optional: Don't display own messages echoed back by server
                        if (sender.Equals(_tenDangNhap, StringComparison.OrdinalIgnoreCase)) return;
                    }
                    AppendToChatLog($"{sender}: {messageContent}");
                }
                else if (command == "USER_JOINED" && dataParts.Length > 0)
                { AppendToChatLog($"*** {dataParts[0]} đã tham gia ***"); }
                else if (command == "USER_LEFT" && dataParts.Length > 0)
                { AppendToChatLog($"*** {dataParts[0]} đã rời ***"); }
                // Ignore other commands like BALANCE_UPDATE etc.
            }
        }

        // --- Sending Messages ---
        private void SendChatMessage()
        {
            string message = txtMessage.Text.Trim();
            if (string.IsNullOrEmpty(message)) return;

            if (_socketClient == null || !_socketClient.IsConnected)
            {
                MessageBox.Show("Chưa kết nối đến server.", "Lỗi Kết Nối", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                UpdateChatAvailability(false); // Update UI state
                return;
            }

            // Format using MessageHandler
            string formattedMessage = MessageHandler.CreateChatMessage(message);
            // Optional: Send with sender info if server needs it
            // string formattedMessage = MessageHandler.CreateChatMessage($"{_tenDangNhap}{MessageHandler.DELIMITER}{message}");

            bool sent = _socketClient.Send(formattedMessage);
            if (sent)
            {
                AppendToChatLog($"Bạn: {message}"); // Display own message immediately
                txtMessage.Clear();
                txtMessage.Focus(); // Set focus back to input
            }
            else
            {
                MessageBox.Show("Gửi tin nhắn thất bại. Lỗi kết nối.", "Lỗi Gửi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateChatAvailability(false); // Update UI state
            }
        }

        // --- Appending to Chat Log (UI Update) ---
        private void AppendToChatLog(string text)
        {
            if (rtbChatLog.IsDisposed || this.IsDisposed) return;
            Action appendAction = () => {
                if (!rtbChatLog.IsDisposed)
                { // Double check inside UI thread
                    rtbChatLog.AppendText(text + Environment.NewLine);
                    rtbChatLog.ScrollToCaret();
                }
            };

            if (rtbChatLog.InvokeRequired)
            {
                try { rtbChatLog.Invoke(appendAction); } catch { /* Ignore disposed */ }
            }
            else { appendAction(); }
        }

        // --- UI Event Handlers ---
        private void frmChat_Load(object sender, EventArgs e)
        {
            // Setup initial state (mostly handled in constructor now)
            txtMessage.Focus();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            SendChatMessage();
        }

        private void txtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true; // Prevent newline in textbox
                SendChatMessage();
            }
        }

        private void frmChat_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Unsubscribe from SHARED socket events to prevent memory leaks/errors
            if (_socketClient != null && !_isManagingOwnConnection)
            {
                _socketClient.OnMessageReceived -= HandleSharedSocketMessage;
                _socketClient.OnDisconnected -= HandleSharedSocketDisconnected;
            }
            // If managing own connection, disconnect it (though this scenario is now less likely)
            // else if (_socketClient != null && _isManagingOwnConnection) { _socketClient.Disconnect(); }

            Debug.WriteLine("frmChat closing, detaching event handlers.");
        }

        

        // --- Remove Methods Related to Manual Connection if using Shared Socket ---
        // private void btnConnect_Click(...) { ... }
        // private void ConnectToServer() { ... }
        // private void DisconnectFromServer() { ... }
        // private void UpdateConnectionStatus(...) { ... }
        // private void HandleDisconnected() { ... } // Use HandleSharedSocketDisconnected
        // private void HandleConnectFailed(...) { ... }
        // private void HandleMessageReceived(...) { ... } // Use HandleSharedSocketMessage
    }
}