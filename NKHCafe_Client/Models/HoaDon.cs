using System;

namespace NKHCafe_Client.Models
{
    public class HoaDon
    {
        public int IDHoaDon { get; set; }
        public int IDTaiKhoan { get; set; }
        public int IDMay { get; set; }
        public DateTime ThoiGianBatDau { get; set; }
        public DateTime? ThoiGianKetThuc { get; set; }
        public decimal TongTien { get; set; }
        public string TrangThai { get; set; }
    }
}