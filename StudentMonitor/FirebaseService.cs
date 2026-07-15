using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace StudentMonitor
{
    /// <summary>
    /// Firebase REST API Service - Thay thế hoàn toàn Flask backend
    /// Sử dụng Firebase Auth + Realtime Database
    /// </summary>
    public static class FirebaseService
    {
        private static readonly HttpClient _http = new HttpClient();

        // ========== CẤU HÌNH FIREBASE ==========
        // Thay bằng giá trị từ Firebase Console > Project Settings
        public static string ApiKey { get; set; } = "YOUR_FIREBASE_API_KEY";
        public static string ProjectId { get; set; } = "YOUR_PROJECT_ID";
        public static string DatabaseUrl { get; set; } = "https://YOUR_PROJECT_ID-default-rtdb.firebaseio.com";

        // Token sau khi đăng nhập
        public static string? IdToken { get; set; }
        public static string? CurrentUsername { get; set; }
        public static string? CurrentFullName { get; set; }
        public static string? CurrentRole { get; set; }
        public static string? CurrentUserId { get; set; }
        public static string? CurrentStudentId { get; set; }
        public static string? CurrentEmail { get; set; }
        public static string? CurrentHometown { get; set; }
        public static string? CurrentAddress { get; set; }
        public static string? CurrentAvatarUrl { get; set; }

        // ============================================
        // AUTH - Đăng ký / Đăng nhập qua Firebase Auth
        // ============================================

        /// <summary>
        /// Đăng ký tài khoản mới qua Firebase Auth REST API
        /// </summary>
        public static async Task<(bool success, string message)> RegisterAsync(
            string email, string password, string fullName, string username, string studentId, string role)
        {
            try
            {
                // 1. Tạo tài khoản Firebase Auth
                var authPayload = new
                {
                    email = email,
                    password = password,
                    returnSecureToken = true
                };

                var authResponse = await _http.PostAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={ApiKey}",
                    new StringContent(JsonConvert.SerializeObject(authPayload), Encoding.UTF8, "application/json"));

                var authBody = await authResponse.Content.ReadAsStringAsync();
                var authResult = JObject.Parse(authBody);

                if (!authResponse.IsSuccessStatusCode)
                {
                    string errorMessage = authResult["error"]?["message"]?.ToString() ?? "Đăng ký thất bại.";
                    if (errorMessage.Contains("EMAIL_EXISTS"))
                        return (false, "Email đã được sử dụng.");
                    if (errorMessage.Contains("WEAK_PASSWORD"))
                        return (false, "Mật khẩu phải có ít nhất 6 ký tự.");
                    return (false, errorMessage);
                }

                string uid = authResult["localId"]?.ToString() ?? "";
                string token = authResult["idToken"]?.ToString() ?? "";

                // 2. Lưu profile vào Realtime Database
                var profileData = new
                {
                    full_name = fullName,
                    email = email,
                    username = username,
                    student_id = studentId,
                    role = role,
                    is_active = true,
                    created_at = DateTime.UtcNow.ToString("o")
                };

                await _http.PutAsync(
                    $"{DatabaseUrl}/users/{uid}.json?auth={token}",
                    new StringContent(JsonConvert.SerializeObject(profileData), Encoding.UTF8, "application/json"));

                // 3. Tạo mapping username -> uid để tra cứu nhanh
                await _http.PutAsync(
                    $"{DatabaseUrl}/usernames/{username}.json?auth={token}",
                    new StringContent($"\"{uid}\"", Encoding.UTF8, "application/json"));

                return (true, "Đăng ký thành công!");
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Lỗi mạng hoặc kết nối máy chủ: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Đăng nhập qua Firebase Auth REST API
        /// </summary>
        public static async Task<(bool success, string message, string role, string fullName)> LoginAsync(
            string email, string password)
        {
            try
            {
                var payload = new
                {
                    email = email,
                    password = password,
                    returnSecureToken = true
                };

                var response = await _http.PostAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={ApiKey}",
                    new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));

                var body = await response.Content.ReadAsStringAsync();
                var result = JObject.Parse(body);

                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = result["error"]?["message"]?.ToString() ?? "Đăng nhập thất bại.";
                    if (errorMessage.Contains("EMAIL_NOT_FOUND") || errorMessage.Contains("INVALID_PASSWORD")
                        || errorMessage.Contains("INVALID_LOGIN_CREDENTIALS"))
                        return (false, "Email hoặc mật khẩu không đúng.", "", "");
                    if (errorMessage.Contains("USER_DISABLED"))
                        return (false, "Tài khoản đã bị khóa.", "", "");
                    return (false, errorMessage, "", "");
                }

                string uid = result["localId"]?.ToString() ?? "";
                IdToken = result["idToken"]?.ToString() ?? "";
                CurrentUserId = uid;

                // Lấy profile từ Realtime Database
                var getProfileResponse = await _http.GetAsync($"{DatabaseUrl}/users/{uid}.json?auth={IdToken}");
                var profileResponse = await getProfileResponse.Content.ReadAsStringAsync();
                
                if (!getProfileResponse.IsSuccessStatusCode)
                {
                    return (false, $"Lỗi phân quyền từ Firebase (Status: {getProfileResponse.StatusCode}).", "", "");
                }
                
                if (profileResponse == "null" || string.IsNullOrWhiteSpace(profileResponse))
                {
                    return (false, "Profile không tồn tại trên cơ sở dữ liệu Firebase.", "", "");
                }

                var profile = JObject.Parse(profileResponse);

                CurrentUsername = profile["username"]?.ToString() ?? "";
                CurrentFullName = profile["full_name"]?.ToString() ?? "";
                CurrentRole = profile["role"]?.ToString() ?? "student";
                CurrentStudentId = profile["student_id"]?.ToString() ?? "";
                CurrentEmail = profile["email"]?.ToString() ?? "";
                CurrentHometown = profile["hometown"]?.ToString() ?? "";
                CurrentAddress = profile["address"]?.ToString() ?? "";
                CurrentAvatarUrl = profile["avatar_url"]?.ToString() ?? "";

                return (true, "Đăng nhập thành công!", CurrentRole, CurrentFullName);
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Lỗi mạng hoặc kết nối máy chủ: {ex.Message}", "", "");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}", "", "");
            }
        }

        /// <summary>
        /// Đổi mật khẩu Firebase Auth
        /// </summary>
        public static async Task<(bool success, string message)> ChangePasswordAsync(string newPassword)
        {
            try
            {
                var payload = new
                {
                    idToken = IdToken,
                    password = newPassword,
                    returnSecureToken = true
                };

                var response = await _http.PostAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:update?key={ApiKey}",
                    new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));

                var body = await response.Content.ReadAsStringAsync();
                var result = JObject.Parse(body);

                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage = result["error"]?["message"]?.ToString() ?? "Đổi mật khẩu thất bại.";
                    return (false, errorMessage);
                }

                return (true, "Đổi mật khẩu thành công!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        /// <summary>
        /// Cập nhật thông tin profile lên Realtime DB
        /// </summary>
        public static async Task<(bool success, string message)> UpdateProfileAsync(string hometown, string address, string avatarUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(CurrentUserId)) return (false, "Chưa đăng nhập");

                var patchData = new
                {
                    hometown = hometown,
                    address = address,
                    avatar_url = avatarUrl
                };

                var response = await _http.PatchAsync(
                    $"{DatabaseUrl}/users/{CurrentUserId}.json?auth={IdToken}",
                    new StringContent(JsonConvert.SerializeObject(patchData), Encoding.UTF8, "application/json"));

                if (!response.IsSuccessStatusCode)
                {
                    return (false, "Cập nhật Firebase thất bại.");
                }

                CurrentHometown = hometown;
                CurrentAddress = address;
                CurrentAvatarUrl = avatarUrl;
                return (true, "Cập nhật thành công!");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }

        // ============================================
        // MONITORING - Peer Registration & Screen Capture
        // ============================================

        /// <summary>
        /// Đăng ký peer ID cho WebRTC (camera/screen)
        /// </summary>
        public static async Task RegisterPeerAsync(string username, string peerId, string type = "camera")
        {
            try
            {
                var data = new { peer_id = peerId, timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() };
                await _http.PutAsync(
                    $"{DatabaseUrl}/active_peers/{username}/{type}.json?auth={IdToken}",
                    new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
            }
            catch { }
        }

        /// <summary>
        /// Upload ảnh chụp màn hình (base64) lên Realtime Database
        /// </summary>
        public static async Task UploadScreenFrameAsync(string username, byte[] jpegData)
        {
            try
            {
                string base64 = Convert.ToBase64String(jpegData);
                var data = new
                {
                    frame = base64,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                await _http.PutAsync(
                    $"{DatabaseUrl}/screens/{username}.json?auth={IdToken}",
                    new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
            }
            catch { }
        }

        /// <summary>
        /// Báo cáo vi phạm (tab switch)
        /// </summary>
        public static async Task ReportViolationAsync(string username, string violationType, byte[]? screenshotData = null)
        {
            try
            {
                var violationData = new Dictionary<string, object>
                {
                    ["username"] = username,
                    ["full_name"] = CurrentFullName ?? username,
                    ["violation_type"] = violationType,
                    ["captured_at"] = DateTime.UtcNow.ToString("o"),
                    ["is_violating"] = true
                };

                if (screenshotData != null)
                {
                    violationData["screenshot_base64"] = Convert.ToBase64String(screenshotData);
                }

                // Lưu vi phạm vào danh sách
                await _http.PostAsync(
                    $"{DatabaseUrl}/violations.json?auth={IdToken}",
                    new StringContent(JsonConvert.SerializeObject(violationData), Encoding.UTF8, "application/json"));

                // Đánh dấu đang vi phạm trên peer data
                await _http.PutAsync(
                    $"{DatabaseUrl}/active_peers/{username}/is_violating.json?auth={IdToken}",
                    new StringContent("true", Encoding.UTF8, "application/json"));
            }
            catch { }
        }

        /// <summary>
        /// Xóa trạng thái vi phạm
        /// </summary>
        public static async Task ClearViolationAsync(string username)
        {
            try
            {
                await _http.PutAsync(
                    $"{DatabaseUrl}/active_peers/{username}/is_violating.json?auth={IdToken}",
                    new StringContent("false", Encoding.UTF8, "application/json"));
            }
            catch { }
        }

        /// <summary>
        /// Lấy danh sách vi phạm (cho instructor)
        /// </summary>
        public static async Task<List<Dictionary<string, object>>> GetViolationsAsync()
        {
            try
            {
                var response = await _http.GetStringAsync(
                    $"{DatabaseUrl}/violations.json?auth={IdToken}&orderBy=\"$key\"&limitToLast=50");
                var result = JObject.Parse(response);
                var violations = new List<Dictionary<string, object>>();

                if (result != null && result.Type != JTokenType.Null)
                {
                    foreach (var prop in result.Properties())
                    {
                        var v = prop.Value.ToObject<Dictionary<string, object>>() ?? new Dictionary<string, object>();
                        v["id"] = prop.Name;
                        violations.Add(v);
                    }
                }

                violations.Reverse(); // Mới nhất lên đầu
                return violations;
            }
            catch
            {
                return new List<Dictionary<string, object>>();
            }
        }

        /// <summary>
        /// Upload danh sách bài thi lên Firebase
        /// </summary>
        public static async Task UploadAssignmentsAsync(string jsonContent)
        {
            try
            {
                await _http.PutAsync(
                    $"{DatabaseUrl}/assignments.json?auth={IdToken}",
                    new StringContent(jsonContent, Encoding.UTF8, "application/json"));
            }
            catch { }
        }

        /// <summary>
        /// Lấy danh sách bài thi từ Firebase
        /// </summary>
        public static async Task<string?> GetAssignmentsAsync()
        {
            try
            {
                var response = await _http.GetStringAsync($"{DatabaseUrl}/assignments.json?auth={IdToken}");
                if (response == "null") return null;
                return response;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Xóa peer khi sinh viên thoát (cleanup)
        /// </summary>
        public static async Task RemovePeerAsync(string username)
        {
            try
            {
                await _http.DeleteAsync(
                    $"{DatabaseUrl}/active_peers/{username}.json?auth={IdToken}");
                await _http.DeleteAsync(
                    $"{DatabaseUrl}/screens/{username}.json?auth={IdToken}");
            }
            catch { }
        }

        /// <summary>
        /// Upload danh sách bài giảng (Lessons) lên Firebase
        /// </summary>
        public static async Task<bool> UploadLessonsAsync(string jsonContent)
        {
            try
            {
                var response = await _http.PutAsync(
                    $"{DatabaseUrl}/lessons.json?auth={IdToken}",
                    new StringContent(jsonContent, Encoding.UTF8, "application/json"));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Lấy danh sách bài giảng (Lessons) từ Firebase
        /// </summary>
        public static async Task<string?> GetLessonsAsync()
        {
            try
            {
                var response = await _http.GetAsync($"{DatabaseUrl}/lessons.json?auth={IdToken}");
                if (!response.IsSuccessStatusCode) return null;
                var content = await response.Content.ReadAsStringAsync();
                if (content == "null" || string.IsNullOrWhiteSpace(content)) return null;
                return content;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Upload file content as base64 to Firebase Realtime Database
        /// </summary>
        public static async Task<bool> UploadLessonFileAsync(string fileId, byte[] fileData)
        {
            try
            {
                string base64 = Convert.ToBase64String(fileData);
                var data = new { content = base64 };
                var response = await _http.PutAsync(
                    $"{DatabaseUrl}/lesson_files/{fileId}.json?auth={IdToken}",
                    new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json"));
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Download file content from Firebase Realtime Database
        /// </summary>
        public static async Task<byte[]?> GetLessonFileAsync(string fileId)
        {
            try
            {
                var response = await _http.GetStringAsync($"{DatabaseUrl}/lesson_files/{fileId}.json?auth={IdToken}");
                if (response == "null") return null;
                var result = JObject.Parse(response);
                string? base64 = result["content"]?.ToString();
                if (!string.IsNullOrEmpty(base64))
                {
                    return Convert.FromBase64String(base64);
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
