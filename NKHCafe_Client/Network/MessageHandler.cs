namespace NKHCafe_Client.Network
{
    public static class MessageHandler
    {
        public static string HandleResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
                return "Không có phản hồi từ server.";

            string[] parts = response.Split('|');
            if (parts.Length < 2)
                return "Phản hồi không hợp lệ.";

            string status = parts[1];
            string message = parts.Length > 2 ? parts[2] : "";

            if (status == "OK")
                return "✅ " + message;
            else
                return "❌ " + message;
        }
    }
}