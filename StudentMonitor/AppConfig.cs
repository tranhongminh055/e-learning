using System;
using System.IO;
using System.Text.Json;

namespace StudentMonitor
{
    public static class AppConfig
    {
        // Firebase Configuration
        // Thay bằng giá trị từ Firebase Console > Project Settings
        public static string FirebaseApiKey { get; private set; } = "AIzaSyAUUfwKLCFt0Ksp2Y6GLICl63eXspZ2YTs";
        public static string FirebaseProjectId { get; private set; } = "vibio-8391c";
        public static string FirebaseDatabaseUrl { get; private set; } = "https://vibio-8391c-default-rtdb.firebaseio.com";

        // URL hosting cho các trang giám sát (Firebase Hosting)
        public static string ApiBaseUrl { get; private set; } = "https://vibio-8391c.web.app";

        public static void LoadConfig()
        {
            // Luôn đồng bộ giá trị mặc định sang FirebaseService trước
            FirebaseService.ApiKey = FirebaseApiKey;
            FirebaseService.ProjectId = FirebaseProjectId;
            FirebaseService.DatabaseUrl = FirebaseDatabaseUrl;

            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    using (JsonDocument doc = JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty("FirebaseApiKey", out JsonElement apiKeyEl))
                        {
                            string val = apiKeyEl.GetString() ?? "";
                            if (!string.IsNullOrEmpty(val)) FirebaseApiKey = val;
                        }
                        if (doc.RootElement.TryGetProperty("FirebaseProjectId", out JsonElement projEl))
                        {
                            string val = projEl.GetString() ?? "";
                            if (!string.IsNullOrEmpty(val)) FirebaseProjectId = val;
                        }
                        if (doc.RootElement.TryGetProperty("FirebaseDatabaseUrl", out JsonElement dbEl))
                        {
                            string val = dbEl.GetString() ?? "";
                            if (!string.IsNullOrEmpty(val)) FirebaseDatabaseUrl = val;
                        }
                        if (doc.RootElement.TryGetProperty("ApiBaseUrl", out JsonElement urlEl))
                        {
                            string val = urlEl.GetString() ?? "";
                            if (!string.IsNullOrEmpty(val)) ApiBaseUrl = val.TrimEnd('/');
                        }
                    }
                }
                else
                {
                    // Tạo file config mẫu
                    var defaultConfig = new
                    {
                        FirebaseApiKey = FirebaseApiKey,
                        FirebaseProjectId = FirebaseProjectId,
                        FirebaseDatabaseUrl = FirebaseDatabaseUrl,
                        ApiBaseUrl = ApiBaseUrl
                    };
                    string defaultJson = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(configPath, defaultJson);
                }

                // Đồng bộ sang FirebaseService
                FirebaseService.ApiKey = FirebaseApiKey;
                FirebaseService.ProjectId = FirebaseProjectId;
                FirebaseService.DatabaseUrl = FirebaseDatabaseUrl;
            }
            catch
            {
                // Dùng mặc định nếu lỗi
            }
        }
    }
}
