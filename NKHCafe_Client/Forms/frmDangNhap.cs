using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Net.Sockets;


namespace NKHCafe_Client.Forms
{
    public partial class frmDangNhap : Form
    {
        public frmDangNhap()
        {
            InitializeComponent();
        }

        private void frmDangNhap_Load(object sender, EventArgs e)
        {
            LoadMayTram();
        }

        private void LoadMayTram()
        {
            try
            {
                string query = "SELECT IDMay, TenMay FROM MayTram WHERE TrangThai = 'Trong'";
                DataTable dt = KetNoiCSDL.ExecuteQuery(query);

                cboMayTram.DataSource = dt;
                cboMayTram.DisplayMember = "TenMay";
                cboMayTram.ValueMember = "IDMay";
                cboMayTram.SelectedIndex = -1; // Không chọn dòng nào mặc định
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải danh sách máy trạm: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnDangNhap_Click(object sender, EventArgs e)
        {
            string tenDangNhap = txtTenDangNhap.Text.Trim();
            string matKhau = txtMatKhau.Text;

            if (string.IsNullOrWhiteSpace(tenDangNhap) || string.IsNullOrWhiteSpace(matKhau))
            {
                MessageBox.Show("Vui lòng nhập tên đăng nhập và mật khẩu.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cboMayTram.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn máy trạm.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int idMay = Convert.ToInt32(cboMayTram.SelectedValue);

            try
            {
                // 1. Kiểm tra tài khoản hợp lệ
                string query = @"
            SELECT IDTaiKhoan, TenDangNhap, SoDu 
            FROM TaiKhoan 
            WHERE TenDangNhap = @TenDangNhap 
              AND MatKhau = @MatKhau 
              AND LoaiTaiKhoan = 'Client'";

                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@TenDangNhap", tenDangNhap),
            new SqlParameter("@MatKhau", matKhau)
                };

                DataTable result = KetNoiCSDL.ExecuteQuery(query, parameters);

                if (result.Rows.Count == 0)
                {
                    MessageBox.Show("Tên đăng nhập hoặc mật khẩu không đúng.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int idTaiKhoan = Convert.ToInt32(result.Rows[0]["IDTaiKhoan"]);
                string tenDN = result.Rows[0]["TenDangNhap"].ToString();
                decimal soDu = Convert.ToDecimal(result.Rows[0]["SoDu"]);

                // 2. Cập nhật trạng thái máy trạm
                string updateMayQuery = @"
            UPDATE MayTram 
            SET TrangThai = 'Ban', IDTaiKhoan = @IDTaiKhoan 
            WHERE IDMay = @IDMay AND TrangThai = 'Trong'";

                SqlParameter[] updateMayParams = new SqlParameter[]
                {
            new SqlParameter("@IDTaiKhoan", idTaiKhoan),
            new SqlParameter("@IDMay", idMay)
                };

                int rowsAffected = KetNoiCSDL.ExecuteNonQuery(updateMayQuery, updateMayParams);

                if (rowsAffected == 0)
                {
                    MessageBox.Show("Máy đã có người sử dụng, vui lòng chọn máy khác.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    LoadMayTram(); // Tải lại danh sách máy đang rảnh
                    return;
                }

                NotifyServerOpen(idMay);

                // 3. Mở form chính
                this.Hide();
                using (frmClientMain frmMain = new frmClientMain(idTaiKhoan, tenDN, soDu, idMay))
                {
                    frmMain.ShowDialog();
                }
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi đăng nhập: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Client-side
        private void NotifyServerOpen(int idMay)
        {
            try
            {
                TcpClient client = new TcpClient("127.0.0.1", 8888); // IP & Port của server
                NetworkStream stream = client.GetStream();

                string message = $"OPEN:{idMay}";
                byte[] data = Encoding.ASCII.GetBytes(message);
                stream.Write(data, 0, data.Length);

                // (Tùy chọn) Nhận phản hồi
                byte[] buffer = new byte[1024];
                int bytes = stream.Read(buffer, 0, buffer.Length);
                string response = Encoding.ASCII.GetString(buffer, 0, bytes);
                Console.WriteLine("Server response: " + response);

                stream.Close();
                client.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi khi gửi OPEN command: " + ex.Message);
            }
        }


        private void btnThoat_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void cbHienThiMK_CheckedChanged(object sender, EventArgs e)
        {
            txtMatKhau.UseSystemPasswordChar = !cbHienThiMK.Checked;
        }

    }
}