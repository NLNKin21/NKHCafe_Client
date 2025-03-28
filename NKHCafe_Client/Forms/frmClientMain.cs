using NKHCafe_Client.Network;
using NKHCafe_Client.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NKHCafe_Client.Forms
{
    public partial class frmClientMain : System.Windows.Forms.Form
    {
        private int _idTaiKhoan;
        private string _tenDangNhap;
        private decimal _soDu;
        private int _idMay;
        private Timer _timer;
        private DateTime _thoiGianBatDau;
        private bool _daThongBaoHetGio = false; // Biến để kiểm soát thông báo

        private SocketClient _socketClient;



        public frmClientMain(int idTaiKhoan, string tenDangNhap, decimal soDu, int idMay)
        {
            InitializeComponent();
            _idTaiKhoan = idTaiKhoan;
            _tenDangNhap = tenDangNhap;
            _soDu = soDu;
            _idMay = idMay;

            // Load thông tin lên giao diện
            lblTenDangNhap.Text = "Xin chào, " + _tenDangNhap;
            CapNhatSoDuHienThi();

            // Bắt đầu tính giờ
            BatDauTinhGio();

            // Đặt tiêu đề form
            this.Text = "Máy: " + LayTenMay(_idMay);
            
            // Kết nối socket
            _socketClient = new SocketClient();
            
            _socketClient.OnMessageReceived += (response) =>
            {
                string result = MessageHandler.HandleResponse(response);

                // Gọi UI thread để hiển thị MessageBox
                this.Invoke((MethodInvoker)(() =>
                {
                    MessageBox.Show(result, "Phản hồi từ server", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }));
            };


            bool connected = _socketClient.Connect(Config.ServerIP, Config.ServerPort);
            if (!connected)
            {
                MessageBox.Show("Không thể kết nối đến server.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }


        }

        private string LayTenMay(int idMay)
        {
            try
            {
                string query = "SELECT TenMay FROM MayTram WHERE IDMay = @IDMay";
                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@IDMay", idMay)
                };

                object result = KetNoiCSDL.ExecuteScalar(query, parameters);
                return result?.ToString() ?? "Không xác định";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lấy tên máy: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return "Không xác định";
            }
        }

        private void CapNhatSoDuHienThi()
        {
            lblSoDu.Text = "Số dư: " + _soDu.ToString("N0") + " VNĐ";
        }

        private void BatDauTinhGio()
        {
            try
            {
                string query = "SELECT ThoiGianBatDau FROM HoaDon WHERE IDTaiKhoan = @IDTaiKhoan AND IDMay = @IDMay AND TrangThai = 'DangCho'";
                SqlParameter[] parameters = {
            new SqlParameter("@IDTaiKhoan", _idTaiKhoan),
            new SqlParameter("@IDMay", _idMay)
        };

                DataTable result = KetNoiCSDL.ExecuteQuery(query, parameters);

                if (result.Rows.Count > 0)
                {
                    _thoiGianBatDau = Convert.ToDateTime(result.Rows[0]["ThoiGianBatDau"]);
                }
                else
                {
                    string insertQuery = "INSERT INTO HoaDon (IDTaiKhoan, IDMay, ThoiGianBatDau, TrangThai) VALUES (@IDTaiKhoan, @IDMay, @ThoiGianBatDau, 'DangCho')";
                    SqlParameter[] insertParams = {
                new SqlParameter("@IDTaiKhoan", _idTaiKhoan),
                new SqlParameter("@IDMay", _idMay),
                new SqlParameter("@ThoiGianBatDau", DateTime.Now)
            };
                    KetNoiCSDL.ExecuteNonQuery(insertQuery, insertParams);
                    _thoiGianBatDau = DateTime.Now;
                }

                _timer = new Timer();
                _timer.Interval = 60000;
                _timer.Tick += Timer_Tick;
                _timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi bắt đầu tính giờ: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }
        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                TimeSpan thoiGianSuDung = DateTime.Now - _thoiGianBatDau;
                decimal tienSuDung = (decimal)thoiGianSuDung.TotalSeconds * (10000m / 3600m);

                _soDu -= tienSuDung;
                _thoiGianBatDau = DateTime.Now; // Cập nhật lại thời gian bắt đầu


                // Kiểm tra và thông báo trước khi hết giờ
                if (_soDu <= 5000 && !_daThongBaoHetGio) // Ví dụ: Thông báo khi còn dưới 5000
                {
                    MessageBox.Show("Số dư của bạn sắp hết. Vui lòng nạp thêm tiền!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _daThongBaoHetGio = true; // Đánh dấu đã thông báo
                }

                if (_soDu <= 0)
                {
                    _timer.Stop();
                    MessageBox.Show("Hết tiền! Vui lòng nạp thêm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    KetThucPhien();
                    return;
                }

                CapNhatSoDuHienThi();
                lblThoiGian.Text = "Thời gian đã sử dụng: " + thoiGianSuDung.ToString(@"hh\:mm\:ss");
                LoadDanhSachOrder(); //Cập nhật danh sách đã order
            }
            catch (Exception ex)
            {
                _timer.Stop();
                MessageBox.Show("Lỗi trong quá trình tính giờ: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close(); // Hoặc xử lý phù hợp
            }
        }


        private void KetThucPhien()
        {
            try
            {
                // Cập nhật HoaDon
                string updateHDQuery = "UPDATE HoaDon SET ThoiGianKetThuc = @ThoiGianKetThuc, TongTien = @TongTien, TrangThai = 'DaThanhToan' WHERE IDTaiKhoan = @IDTaiKhoan AND IDMay = @IDMay AND TrangThai = 'DangCho'";
                SqlParameter[] updateHDParams = {
            new SqlParameter("@ThoiGianKetThuc", DateTime.Now),
            new SqlParameter("@TongTien", (_soDu > 0 ? 0 : Math.Abs(_soDu))),
            new SqlParameter("@IDTaiKhoan", _idTaiKhoan),
            new SqlParameter("@IDMay", _idMay)
        };
                KetNoiCSDL.ExecuteNonQuery(updateHDQuery, updateHDParams);

                // Cập nhật MayTram
                string updateMayQuery = "UPDATE MayTram SET TrangThai = 'Trong', IDTaiKhoan = NULL WHERE IDMay = @IDMay";
                SqlParameter[] updateMayParams = {
            new SqlParameter("@IDMay", _idMay)
        };
                KetNoiCSDL.ExecuteNonQuery(updateMayQuery, updateMayParams);

                // Cập nhật TaiKhoan
                string updateTKQuery = "UPDATE TaiKhoan SET SoDu = @SoDu WHERE IDTaiKhoan = @IDTaiKhoan";
                SqlParameter[] parametersTK = {
            new SqlParameter("@SoDu", _soDu),
            new SqlParameter("@IDTaiKhoan", _idTaiKhoan)
        };
                KetNoiCSDL.ExecuteNonQuery(updateTKQuery, parametersTK);

                _timer?.Stop();
                _timer?.Dispose();
                _timer = null;

                this.Hide();
                frmDangNhap frm = new frmDangNhap();
                frm.ShowDialog();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi kết thúc phiên: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void btnOrderMon_Click(object sender, EventArgs e)
        {
            frmOrderMon frm = new frmOrderMon(); //Truyền _idTaiKhoan nếu cần
                                                 // gán sự kiện FormClosed cho form Order
            frm.FormClosed += FrmOrderMon_FormClosed;
            frm.ShowDialog(); // ShowDialog để khi đóng form Order, code bên dưới mới chạy tiếp

        }
        private void FrmOrderMon_FormClosed(object sender, FormClosedEventArgs e)
        {
            LoadDanhSachOrder(); // Tải lại danh sách order khi form Order đóng

        }

        private void btnChat_Click(object sender, EventArgs e)
        {
            frmChat frm = new frmChat();
            frm.Show(); // Show hoặc ShowDialog, tùy bạn
        }

        private void btnDangXuat_Click(object sender, EventArgs e)
        {
            KetThucPhien(); // Kết thúc phiên và đăng xuất
        }
        private void LoadDanhSachOrder()
        {
            try
            {
                string getIdHdQuery = "SELECT IDHoaDon FROM HoaDon WHERE IDTaiKhoan = @IDTaiKhoan AND IDMay = @IDMay AND TrangThai = 'DangCho'";
                SqlParameter[] hdParams = {
            new SqlParameter("@IDTaiKhoan", _idTaiKhoan),
            new SqlParameter("@IDMay", _idMay)
        };
                object idHoaDonResult = KetNoiCSDL.ExecuteScalar(getIdHdQuery, hdParams);

                if (idHoaDonResult == null)
                {
                    dgvOrder.DataSource = null;
                    return;
                }

                int idHoaDon = Convert.ToInt32(idHoaDonResult);

                string query = @"
            SELECT td.TenMon, cthd.SoLuong, cthd.DonGia, (cthd.SoLuong * cthd.DonGia) AS ThanhTien
            FROM ChiTietHoaDon cthd
            JOIN ThucDon td ON cthd.IDMon = td.IDMon
            WHERE cthd.IDHoaDon = @IDHoaDon";
                SqlParameter[] parameters = {
            new SqlParameter("@IDHoaDon", idHoaDon)
        };

                DataTable dt = KetNoiCSDL.ExecuteQuery(query, parameters);

                dgvOrder.DataSource = dt;
                dgvOrder.Columns["DonGia"].DefaultCellStyle.Format = "N0";
                dgvOrder.Columns["ThanhTien"].DefaultCellStyle.Format = "N0";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải danh sách order: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void frmClientMain_Load(object sender, EventArgs e)
        {
            LoadDanhSachOrder(); // Tải danh sách đã order khi form load
        }

        private void frmClientMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Ngắt kết nối socket
            if (_socketClient != null)
            {
                _socketClient.Disconnect();
            }

            // Giải phóng timer khi form đóng (tránh lỗi)
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
        }

        public int IDTaiKhoan => _idTaiKhoan;
        public int IDMay => _idMay;
    }
}