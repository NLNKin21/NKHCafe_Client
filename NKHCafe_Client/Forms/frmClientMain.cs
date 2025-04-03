using NKHCafe_Client.Network;
using NKHCafe_Client.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NKHCafe_Client.Forms
{
    public partial class frmClientMain : Form
    {
        // --- Member Variables ---
        private readonly int _idTaiKhoan;
        private readonly string _tenDangNhap;
        private decimal _soDu;
        private readonly int _idMay;
        private Timer _timer;
        private DateTime _thoiGianBatDau;
        private bool _daThongBaoHetGio = false;
        private SocketClient _socketClient;
        private bool _isManuallyClosing = false;


        private DateTime _lastChargeTime; // Thời điểm trừ tiền gần nhất
        private const decimal PhiMoi2Phut = 250m; // Số tiền trừ mỗi 2 phút

        public int IDTaiKhoan => _idTaiKhoan;
        public int IDMay => _idMay;


        // --- Constructor ---
        public frmClientMain(int idTaiKhoan, string tenDangNhap, decimal soDu, int idMay)
        {
            InitializeComponent();

            _idTaiKhoan = idTaiKhoan;
            _tenDangNhap = tenDangNhap;
            _soDu = soDu;
            _idMay = idMay;

            lblTenDangNhap.Text = "Xin chào, " + _tenDangNhap;
            CapNhatSoDuHienThi();
            string tenMay = LayTenMay(_idMay); // Get machine name once
            this.Text = $"Máy: {tenMay} - Đang kết nối...";

            InitializeSocketClient();
            LoadOrderData();
            if (_socketClient != null && _socketClient.IsConnected)
            {
                BatDauTinhGio();
                UpdateUIForConnectedState(true);
                this.Text = $"Máy: {tenMay}"; // Update title on success
                //LoadDanhSachOrder(); // Load orders after connection and timer start
                LoadOrderData();
            }
            else
            {
                UpdateUIForConnectedState(false);
                this.Text = $"Máy: {tenMay} - Mất kết nối"; // Update title on failure
            }
        }

        // --- Socket Initialization and Handlers ---
        private void InitializeSocketClient()
        {
            _socketClient = new SocketClient();
            _socketClient.OnMessageReceived += SocketClient_OnMessageReceived;
            _socketClient.OnDisconnected += SocketClient_OnDisconnected;
            _socketClient.OnConnectFailed += SocketClient_OnConnectFailed;

            Debug.WriteLine("Attempting to connect...");
            bool connected = _socketClient.Connect(Config.ServerIP, Config.ServerPort);

            if (connected)
            {
                Debug.WriteLine("Initial connection successful.");
                string connectMsg = MessageHandler.CreateClientConnectMessage(_idTaiKhoan, _idMay);
                _socketClient.Send(connectMsg);
            }
            else
            {
                Debug.WriteLine("Initial connection failed immediately.");
            }
        }

        private void SocketClient_OnMessageReceived(string response)
        {
            if (this.InvokeRequired)
            {
                try { this.Invoke((Action)(() => HandleServerMessage(response))); }
                catch (ObjectDisposedException) { /* Form closing, ignore */ }
                catch (Exception ex) { Debug.WriteLine($"Invoke Error in OnMessageReceived: {ex.Message}"); }
            }
            else
            {
                HandleServerMessage(response);
            }
        }

        private void SocketClient_OnDisconnected()
        {
            Debug.WriteLine("Received OnDisconnected event.");
            if (this.InvokeRequired)
            {
                try { this.Invoke((Action)(() => ProcessDisconnection())); }
                catch (ObjectDisposedException) { /* Form closing, ignore */ }
                catch (Exception ex) { Debug.WriteLine($"Invoke Error in OnDisconnected: {ex.Message}"); }
            }
            else
            {
                ProcessDisconnection();
            }
        }

        private void ProcessDisconnection()
        {
            if (this.IsDisposed) return; // Check if form is already disposed
            UpdateUIForConnectedState(false);
            _timer?.Stop();
            if (!_isManuallyClosing) // Don't show if user initiated logout
            {
                MessageBox.Show("Mất kết nối đến server!", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }


        private void SocketClient_OnConnectFailed(string errorMessage)
        {
            Debug.WriteLine($"Received OnConnectFailed event: {errorMessage}");
            if (this.InvokeRequired)
            {
                try { this.Invoke((Action)(() => ProcessConnectionFailure(errorMessage))); }
                catch (ObjectDisposedException) { /* Form closing, ignore */ }
                catch (Exception ex) { Debug.WriteLine($"Invoke Error in OnConnectFailed: {ex.Message}"); }
            }
            else
            {
                ProcessConnectionFailure(errorMessage);
            }
        }

        private void ProcessConnectionFailure(string errorMessage)
        {
            if (this.IsDisposed) return;
            UpdateUIForConnectedState(false);
            MessageBox.Show($"Không thể kết nối đến server: {errorMessage}", "Lỗi Kết Nối", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        // --- Message Handling Logic ---
        private void HandleServerMessage(string response)
        {
            if (this.IsDisposed) return; // Prevent action on disposed form
            if (MessageHandler.ParseServerResponse(response, out string command, out string[] dataParts))
            {
                Debug.WriteLine($"Handling command: {command}");
                switch (command)
                {
                    case "BALANCE_UPDATE":
                        if (dataParts.Length >= 1 && decimal.TryParse(dataParts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal newBalance))
                        {
                            _soDu = newBalance;
                            CapNhatSoDuHienThi();
                            _daThongBaoHetGio = false; // Reset warning flag on balance update
                            MessageBox.Show($"Số dư cập nhật: {_soDu:N0} VNĐ", "Nạp Tiền Thành Công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else { Debug.WriteLine($"Invalid BALANCE_UPDATE data: {response}"); }
                        break;
                    case "FORCE_LOGOUT":
                        MessageBox.Show("Bạn đã bị đăng xuất.", "Thông Báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        KetThucPhien(false); // Server likely handled DB updates
                        break;
                    case "ORDER_CONFIRMATION":
                        if (dataParts.Length > 0)
                            MessageBox.Show($"Server xác nhận: {dataParts[0]}", "Xác nhận Order", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadDanhSachOrder(); // Refresh order list
                        break;
                    case "SERVER_MESSAGE":
                        if (dataParts.Length > 0)
                            MessageBox.Show($"Server: {dataParts[0]}", "Thông báo Server", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    // Add other cases as needed
                    default:
                        Debug.WriteLine($"[frmClientMain] Received unhandled command: {command}");
                        break;
                }
            }
            else
            {
                Debug.WriteLine($"[frmClientMain] Failed to parse server response: {response}");
            }
        }

        // --- UI Update Helpers ---
        private void UpdateUIForConnectedState(bool isConnected)
        {
            if (this.IsDisposed) return; // Check before invoking
            if (this.InvokeRequired)
            {
                try { this.Invoke((Action)(() => UpdateUIElements(isConnected))); }
                catch (ObjectDisposedException) { /* Form closing, ignore */ }
                catch (Exception ex) { Debug.WriteLine($"Invoke Error in UpdateUIForConnectedState: {ex.Message}"); }
            }
            else
            {
                UpdateUIElements(isConnected);
            }
        }

        private void UpdateUIElements(bool isConnected)
        {
            if (this.IsDisposed) return; // Double check inside the UI thread action

            btnOrderMon.Enabled = isConnected;
            btnNapTien.Enabled = isConnected;
            btnChat.Enabled = isConnected;
            // Disable/Enable other controls dependent on connection

            string tenMay = LayTenMay(_idMay); // Get machine name again for title update
            if (!isConnected)
            {
                lblThoiGian.Text = "Mất kết nối";
                if (this.Controls.ContainsKey("lblThoiGianConLai"))
                    this.Controls["lblThoiGianConLai"].Text = "";
                this.Text = $"Máy: {tenMay} - Mất kết nối";
            }
            else
            {
                // Reset time label if needed when reconnected (or handled by timer start)
                this.Text = $"Máy: {tenMay}";
            }
        }

        private void CapNhatSoDuHienThi()
        {
            if (lblSoDu.IsDisposed) return;
            Action updateAction = () => lblSoDu.Text = "Số dư: " + _soDu.ToString("N0") + " VNĐ";

            if (lblSoDu.InvokeRequired)
            {
                try { lblSoDu.Invoke(updateAction); }
                catch (ObjectDisposedException) { /* Control closing, ignore */ }
                catch (Exception ex) { Debug.WriteLine($"Invoke Error in CapNhatSoDuHienThi: {ex.Message}"); }
            }
            else
            {
                updateAction();
            }
        }

        // --- Data Access Helpers ---
        private string LayTenMay(int idMay)
        {
            try
            {
                string query = "SELECT TenMay FROM MayTram WHERE IDMay = @IDMay";
                SqlParameter[] parameters = { new SqlParameter("@IDMay", SqlDbType.Int) { Value = idMay } };
                object result = KetNoiCSDL.ExecuteScalar(query, parameters);
                return result?.ToString() ?? "Không xác định";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi LayTenMay (ID: {idMay}): {ex.Message}");
                return "Lỗi tên máy";
            }
        }

        private void LoadDanhSachOrder()
        {
            Debug.WriteLine("Loading ordered items...");
            try
            {
                string getIdHdQuery = @"SELECT TOP 1 IDHoaDon FROM HoaDon
                                        WHERE IDTaiKhoan = @IDTaiKhoan AND IDMay = @IDMay AND TrangThai = 'DangCho'
                                        ORDER BY ThoiGianBatDau DESC";
                SqlParameter[] hdParams = {
                    new SqlParameter("@IDTaiKhoan", SqlDbType.Int) { Value = _idTaiKhoan },
                    new SqlParameter("@IDMay", SqlDbType.Int) { Value = _idMay }
                };
                object idHoaDonResult = KetNoiCSDL.ExecuteScalar(getIdHdQuery, hdParams);

                DataTable dt = new DataTable(); // Create empty table by default
                if (idHoaDonResult != null && idHoaDonResult != DBNull.Value)
                {
                    int idHoaDon = Convert.ToInt32(idHoaDonResult);
                    Debug.WriteLine($"Found active HoaDon ID: {idHoaDon}");
                    string query = @"SELECT td.TenMon, cthd.SoLuong, cthd.DonGia, (cthd.SoLuong * cthd.DonGia) AS ThanhTien
                                     FROM ChiTietHoaDon cthd JOIN ThucDon td ON cthd.IDMon = td.IDMon
                                     WHERE cthd.IDHoaDon = @IDHoaDon";
                    SqlParameter[] parameters = { new SqlParameter("@IDHoaDon", SqlDbType.Int) { Value = idHoaDon } };
                    dt = KetNoiCSDL.ExecuteQuery(query, parameters);
                    Debug.WriteLine($"Loaded {dt.Rows.Count} ordered items from DB.");
                }
                else { Debug.WriteLine("No active HoaDon found for current session."); }

                // Update DataGridView on UI thread
                if (dgvOrder.IsDisposed) return;
                if (dgvOrder.InvokeRequired)
                {
                    try { dgvOrder.Invoke((Action)(() => BindOrderDataToGrid(dt))); }
                    catch (ObjectDisposedException) { /* Control closing, ignore */ }
                    catch (Exception ex) { Debug.WriteLine($"Invoke Error in LoadDanhSachOrder: {ex.Message}"); }
                }
                else
                {
                    BindOrderDataToGrid(dt);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi khi tải danh sách order: {ex.Message}");
                // Clear grid on error
                if (!dgvOrder.IsDisposed)
                {
                    if (dgvOrder.InvokeRequired) { try { dgvOrder.Invoke((Action)(() => dgvOrder.DataSource = null)); } catch { } }
                    else { dgvOrder.DataSource = null; }
                }
            }
        }

        private void BindOrderDataToGrid(DataTable dataTable)
        {
            if (dgvOrder.IsDisposed) return;
            dgvOrder.DataSource = dataTable;
            if (dgvOrder.Columns.Contains("TenMon")) dgvOrder.Columns["TenMon"].HeaderText = "Tên Món";
            if (dgvOrder.Columns.Contains("SoLuong")) dgvOrder.Columns["SoLuong"].HeaderText = "SL";
            if (dgvOrder.Columns.Contains("DonGia")) { dgvOrder.Columns["DonGia"].HeaderText = "Đơn Giá"; dgvOrder.Columns["DonGia"].DefaultCellStyle.Format = "N0"; }
            if (dgvOrder.Columns.Contains("ThanhTien")) { dgvOrder.Columns["ThanhTien"].HeaderText = "Thành Tiền"; dgvOrder.Columns["ThanhTien"].DefaultCellStyle.Format = "N0"; }
            dgvOrder.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvOrder.ClearSelection();
            Debug.WriteLine("Order list updated in DataGridView.");
        }


        // --- Timer Logic ---
        private void BatDauTinhGio()
        {
            if (_timer != null && _timer.Enabled) return;
            Debug.WriteLine("Starting usage timer...");
            try
            {
                string query = @"SELECT TOP 1 ThoiGianBatDau FROM HoaDon
                                 WHERE IDTaiKhoan = @IDTaiKhoan AND IDMay = @IDMay AND TrangThai = 'DangCho'
                                 ORDER BY ThoiGianBatDau DESC";
                SqlParameter[] parameters = {
                    new SqlParameter("@IDTaiKhoan", SqlDbType.Int) { Value = _idTaiKhoan },
                    new SqlParameter("@IDMay", SqlDbType.Int) { Value = _idMay }
                };
                object result = KetNoiCSDL.ExecuteScalar(query, parameters);

                if (result != null && result != DBNull.Value) { _thoiGianBatDau = Convert.ToDateTime(result); }
                else
                {
                    _thoiGianBatDau = DateTime.Now;
                    string insertQuery = @"INSERT INTO HoaDon (IDTaiKhoan, IDMay, ThoiGianBatDau, TrangThai)
                                           VALUES (@IDTaiKhoan, @IDMay, @ThoiGianBatDau, 'DangCho');";
                    SqlParameter[] insertParams = {
                        new SqlParameter("@IDTaiKhoan", _idTaiKhoan), new SqlParameter("@IDMay", _idMay),
                        new SqlParameter("@ThoiGianBatDau", _thoiGianBatDau) };
                    KetNoiCSDL.ExecuteNonQuery(insertQuery, insertParams);
                }

                if (_timer == null)
                {
                    _timer = new Timer { Interval = 1000 };
                    _timer.Tick += Timer_Tick;
                }
                _timer.Start();
                Debug.WriteLine("Timer started.");
                Timer_Tick(null, EventArgs.Empty); // Update UI immediately
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi BatDauTinhGio: {ex.Message}");
                MessageBox.Show("Lỗi khi bắt đầu tính giờ: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (this.IsDisposed) return;

            TimeSpan thoiGianSuDung = DateTime.Now - _thoiGianBatDau;

            // ✅ Giả sử: 250 VNĐ = 1 phút
            double tongPhutConLai = (double)_soDu / 250.0;
            TimeSpan thoiGianConLai = TimeSpan.FromMinutes(tongPhutConLai) - thoiGianSuDung;

            if (thoiGianConLai < TimeSpan.Zero)
                thoiGianConLai = TimeSpan.Zero;

            // ✅ Cập nhật UI
            lblThoiGian.Text = $"Thời gian sử dụng: {thoiGianSuDung:hh\\:mm\\:ss}";
            lblThoiGianConLai.Text = $"Thời gian còn lại: {thoiGianConLai:hh\\:mm\\:ss}";

            // Khởi tạo mốc nếu lần đầu
            if (_lastChargeTime == default)
                _lastChargeTime = _thoiGianBatDau;

            // ✅ Trừ tiền mỗi 2 phút (hoặc 1 phút nếu bạn muốn)
            if ((DateTime.Now - _lastChargeTime).TotalMinutes >= 1)
            {
                if (_soDu >= PhiMoi2Phut)
                {
                    _soDu -= PhiMoi2Phut;
                    CapNhatSoDuHienThi();
                    _lastChargeTime = DateTime.Now;

                    Debug.WriteLine($"Đã trừ {PhiMoi2Phut:N0} VNĐ. Số dư còn lại: {_soDu:N0}");
                }
                else if (!_daThongBaoHetGio)
                {
                    _daThongBaoHetGio = true;
                    MessageBox.Show("Số dư không đủ, vui lòng nạp thêm để tiếp tục sử dụng!", "Hết số dư", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _timer.Stop();
                    // Gọi logout hoặc xử lý tắt máy tại đây nếu cần
                }
            }
        }

        // --- Session Ending ---
        private void KetThucPhien(bool performClientDbUpdates = true)
        {
            if (_isManuallyClosing) return; // Prevent double execution if already closing
            _isManuallyClosing = true;
            _timer?.Stop();
            Debug.WriteLine($"Ending session. Client DB updates: {performClientDbUpdates}");

            if (performClientDbUpdates)
            {
                try
                {
                    TimeSpan tongThoiGianSuDung = DateTime.Now - _thoiGianBatDau;
                    decimal giaMoiGio = 15000m;
                    decimal tongTienSuDung = Math.Max(0, (decimal)tongThoiGianSuDung.TotalHours * giaMoiGio);
                    tongTienSuDung = Math.Round(tongTienSuDung, 0);

                    decimal soDuBanDau = 0;
                    // Simplified: Assume _soDu before deduction was correct enough
                    // Or fetch initial if critical:
                    // string getInitialBalanceQuery = "SELECT SoDu FROM TaiKhoan WHERE IDTaiKhoan = @IDTaiKhoan"; ...
                    soDuBanDau = _soDu + tongTienSuDung; // Estimate based on current _soDu before final calc

                    decimal soDuCuoiCung = Math.Max(0, soDuBanDau - tongTienSuDung);

                    string updateHDQuery = @"UPDATE HoaDon SET ThoiGianKetThuc = @ThoiGianKetThuc, TongTien = @TongTien, TrangThai = 'DaThanhToan'
                                             WHERE IDTaiKhoan = @IDTaiKhoan AND IDMay = @IDMay AND TrangThai = 'DangCho'";
                    SqlParameter[] updateHDParams = { new SqlParameter("@ThoiGianKetThuc", DateTime.Now), new SqlParameter("@TongTien", tongTienSuDung), new SqlParameter("@IDTaiKhoan", _idTaiKhoan), new SqlParameter("@IDMay", _idMay) };
                    KetNoiCSDL.ExecuteNonQuery(updateHDQuery, updateHDParams);

                    string updateMayQuery = "UPDATE MayTram SET TrangThai = 'Trong', IDTaiKhoan = NULL WHERE IDMay = @IDMay";
                    SqlParameter[] updateMayParams = { new SqlParameter("@IDMay", _idMay) };
                    KetNoiCSDL.ExecuteNonQuery(updateMayQuery, updateMayParams);

                    string updateTKQuery = "UPDATE TaiKhoan SET SoDu = @SoDu WHERE IDTaiKhoan = @IDTaiKhoan";
                    SqlParameter[] parametersTK = { new SqlParameter("@SoDu", soDuCuoiCung), new SqlParameter("@IDTaiKhoan", _idTaiKhoan) };
                    KetNoiCSDL.ExecuteNonQuery(updateTKQuery, parametersTK);

                    Debug.WriteLine($"Session DB records updated. Final Balance: {soDuCuoiCung:N0}");
                }
                catch (Exception ex) { Debug.WriteLine($"Lỗi KetThucPhien DB update: {ex.Message}"); MessageBox.Show("Lỗi khi cập nhật dữ liệu cuối phiên: " + ex.Message, "Lỗi DB", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            }

            _socketClient?.Disconnect();
            // Timer dispose is handled in FormClosing

            // Transition to Login Form
            this.Hide();
            frmDangNhap frmLogin = Application.OpenForms.OfType<frmDangNhap>().FirstOrDefault() ?? new frmDangNhap();
            if (!frmLogin.Visible) frmLogin.Show(); else frmLogin.BringToFront();
            this.Close(); // Close this form instance
        }

        // --- UI Event Handlers ---
        private void btnNapTien_Click(object sender, EventArgs e)
        {
            if (_socketClient == null || !_socketClient.IsConnected) { MessageBox.Show("Chưa kết nối đến server.", "Lỗi Kết Nối", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            frmNapTien frm = new frmNapTien(_idTaiKhoan, _idMay, _tenDangNhap, _socketClient);
            frm.ShowDialog();
        }

        private void btnOrderMon_Click(object sender, EventArgs e)
        {
            if (_socketClient == null || !_socketClient.IsConnected) { MessageBox.Show("Chưa kết nối đến server.", "Lỗi Kết Nối", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            frmOrderMon frm = new frmOrderMon(_idTaiKhoan, _idMay, _socketClient);
            frm.FormClosed += FrmOrderMon_FormClosed;
            frm.ShowDialog();
        }

        private void FrmOrderMon_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Refresh the order list after the order form closes, regardless of outcome
            LoadDanhSachOrder();
        }

        private void btnChat_Click(object sender, EventArgs e)
        {
            if (_socketClient == null || !_socketClient.IsConnected) { MessageBox.Show("Chưa kết nối đến server.", "Lỗi Kết Nối", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            // Consider making chat form singleton or manage instances properly
            frmChat frm = new frmChat(_idTaiKhoan, _idMay, _tenDangNhap, _socketClient);
            frm.Show();
        }

        private void btnDangXuat_Click(object sender, EventArgs e)
        {
            var confirmResult = MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?", "Xác Nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirmResult == DialogResult.Yes)
            {
                string logoutMsg = MessageHandler.CreateLogoutRequestMessage(_idTaiKhoan, _idMay);
                _socketClient?.Send(logoutMsg); // Notify server (optional)
                KetThucPhien(); // End session with client DB updates
            }
        }

        private void frmClientMain_Load(object sender, EventArgs e)
        {
            // Load initial order list if connected and timer started
            // This is now handled inside the constructor after connection check
            // if (_socketClient != null && _socketClient.IsConnected && _timer != null && _timer.Enabled)
            // {
            //     LoadDanhSachOrder();
            // }

            // Add remaining time label if not present (moved from timer for initial setup)
            if (!this.Controls.ContainsKey("lblThoiGianConLai"))
            {
                Label lblTimeRemaining = new Label
                {
                    Name = "lblThoiGianConLai",
                    Text = "Còn lại: --:--:--",
                    AutoSize = true,
                    Location = new Point(lblThoiGian.Location.X, lblThoiGian.Location.Y + 25) // Adjust position as needed
                };
                this.Controls.Add(lblTimeRemaining);
                lblTimeRemaining.BringToFront();
            }
        }

        private void frmClientMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_isManuallyClosing) // If closed via 'X' button, not logout button
            {
                // Optionally ask for confirmation
                // var result = MessageBox.Show("Đóng cửa sổ sẽ đăng xuất bạn. Tiếp tục?", "Xác nhận đóng", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                // if (result == DialogResult.No) {
                //     e.Cancel = true;
                //     return;
                // }
                // If closing proceeds, treat it like a logout
                Debug.WriteLine("Form closing via X, initiating session end...");
                KetThucPhien(); // End session properly
                e.Cancel = true; // Prevent the default close, KetThucPhien will handle it
                return; // Exit after calling KetThucPhien
            }
            // If _isManuallyClosing is true, KetThucPhien already handled cleanup.
            // Dispose timer here ensures it's always disposed on form close.
            _timer?.Dispose();
            _timer = null;
            Debug.WriteLine("frmClientMain disposed.");
        }

      

        private void btnDangXuat_Click_1(object sender, EventArgs e)
        {
            var confirmResult = MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?", "Xác Nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirmResult == DialogResult.Yes)
            {
                string logoutMsg = MessageHandler.CreateLogoutRequestMessage(_idTaiKhoan, _idMay);
                _socketClient?.Send(logoutMsg); // Notify server (optional)
                KetThucPhien(); // End session with client DB updates
            }
        }

        private void dgvOrder_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
        private void LoadOrderData()
        {
            // Chuỗi kết nối đến SQL Server - sửa lại theo cấu hình của bạn
            string connectionString = @"Data Source=LAPTOP-5V6TA3CH\NGUYENLONGNHAT;Initial Catalog=QLTiemNET;Integrated Security=True";

            // Câu truy vấn SQL
            string query = @"SELECT TOP (1000) [IDChiTiet], [IDHoaDon], [IDMon], [SoLuong], [DonGia], [ThanhTien], [ThoiGianDatMon]
                     FROM [QLTiemNet].[dbo].[ChiTietHoaDon]";

            // Sử dụng using để đảm bảo tài nguyên được giải phóng đúng cách
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                try
                {
                    SqlDataAdapter adapter = new SqlDataAdapter(query, connection);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    // Gán nguồn dữ liệu cho DataGridView
                    dgvOrder.DataSource = dt;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi tải dữ liệu: " + ex.Message);
                }
            }
        }
    } 
} 