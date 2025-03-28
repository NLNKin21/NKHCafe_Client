using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NKHCafe_Client.Models
{
    public class ChiTietOrder
    {
        public int IDMon { get; set; }
        public string TenMon { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien => SoLuong * DonGia;

        public ChiTietOrder(int id, string ten, int sl, decimal gia)
        {
            IDMon = id;
            TenMon = ten;
            SoLuong = sl;
            DonGia = gia;
        }
    }
}