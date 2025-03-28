using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NKHCafe_Client.Utils
{
    public static class Config
    {
        public static string ServerIP = "127.0.0.1"; // hoặc IP LAN của server
        public static int ServerPort = 8888;

        // Nếu muốn load từ App.config, có thể mở rộng sau
    }
}