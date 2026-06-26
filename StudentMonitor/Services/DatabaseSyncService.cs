using System;
using System.Threading.Tasks;
using MySqlConnector;
using Microsoft.Data.SqlClient;

namespace StudentMonitor.Services
{
    /// <summary>
    /// Đồng bộ dữ liệu tài khoản lên MySQL và SQL Server khi đăng nhập/đăng ký thành công.
    /// </summary>
    public static class DatabaseSyncService
    {
        // ========== Connection Strings ==========
        private static string MySqlConnectionString { get; set; } =
            "Server=localhost;Database=E-LEARNING;User=root;Password=Hieuthi22032005;CharSet=utf8mb4;";

        private static string SqlServerConnectionString { get; set; } =
            "Server=localhost\\SQLEXPRESS;Database=E-LEARNING;User Id=sa;Password=123456;TrustServerCertificate=True;";

        /// <summary>
        /// Đồng bộ thông tin user lên cả MySQL và SQL Server sau khi login/register thành công.
        /// Nếu user đã tồn tại (theo email) thì cập nhật, chưa có thì insert.
        /// </summary>
        public static async Task SyncUserAsync(string fullName, string email, string username,
            string studentId, string role, string rawPassword)
        {
            string passwordHash = ComputeSha256Hash(rawPassword);

            // Chạy song song cả 2 DB, không block UI
            var t1 = SyncToMySqlAsync(fullName, email, username, studentId, role, passwordHash);
            var t2 = SyncToSqlServerAsync(fullName, email, username, studentId, role, passwordHash);
            
            try { await Task.WhenAll(t1, t2); }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseSync] Error: {ex.Message}");
            }
        }

        private static string ComputeSha256Hash(string rawData)
        {
            if (string.IsNullOrEmpty(rawData)) return "";
            using (System.Security.Cryptography.SHA256 sha256Hash = System.Security.Cryptography.SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawData));
                System.Text.StringBuilder builder = new System.Text.StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // ========== MySQL ==========
        private static async Task SyncToMySqlAsync(string fullName, string email, string username,
            string studentId, string role, string passwordHash)
        {
            try
            {
                using var conn = new MySqlConnection(MySqlConnectionString);
                await conn.OpenAsync();

                // Kiểm tra user đã tồn tại chưa (theo email)
                string checkSql = "SELECT id FROM users WHERE email = @email LIMIT 1";
                using var checkCmd = new MySqlCommand(checkSql, conn);
                checkCmd.Parameters.AddWithValue("@email", email);
                var existingId = await checkCmd.ExecuteScalarAsync();

                if (existingId != null)
                {
                    // Cập nhật thông tin (không thay đổi is_active)
                    string updateSql = @"UPDATE users SET full_name=@full_name, username=@username, 
                        student_id=@student_id, role=@role" + 
                        (string.IsNullOrEmpty(passwordHash) ? "" : ", password_hash=@password_hash") +
                        ", updated_at=NOW() WHERE email=@email";
                    using var updateCmd = new MySqlCommand(updateSql, conn);
                    updateCmd.Parameters.AddWithValue("@full_name", fullName);
                    updateCmd.Parameters.AddWithValue("@username", username);
                    updateCmd.Parameters.AddWithValue("@student_id", studentId);
                    updateCmd.Parameters.AddWithValue("@role", role);
                    if (!string.IsNullOrEmpty(passwordHash))
                        updateCmd.Parameters.AddWithValue("@password_hash", passwordHash);
                    updateCmd.Parameters.AddWithValue("@email", email);
                    await updateCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    // Insert mới
                    string insertSql = @"INSERT INTO users (full_name, email, username, student_id, password_hash, role, is_active, created_at, updated_at)
                        VALUES (@full_name, @email, @username, @student_id, @password_hash, @role, 1, NOW(), NOW())";
                    using var insertCmd = new MySqlCommand(insertSql, conn);
                    insertCmd.Parameters.AddWithValue("@full_name", fullName);
                    insertCmd.Parameters.AddWithValue("@email", email);
                    insertCmd.Parameters.AddWithValue("@username", username);
                    insertCmd.Parameters.AddWithValue("@student_id", studentId);
                    insertCmd.Parameters.AddWithValue("@password_hash", passwordHash);
                    insertCmd.Parameters.AddWithValue("@role", role);
                    await insertCmd.ExecuteNonQueryAsync();
                }

                System.Diagnostics.Debug.WriteLine($"[MySQL] Synced user: {username}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MySQL] Sync error: {ex.Message}");
            }
        }

        // ========== SQL Server ==========
        private static async Task SyncToSqlServerAsync(string fullName, string email, string username,
            string studentId, string role, string passwordHash)
        {
            try
            {
                using var conn = new SqlConnection(SqlServerConnectionString);
                await conn.OpenAsync();

                // Kiểm tra user đã tồn tại chưa (theo email)
                string checkSql = "SELECT TOP 1 id FROM users WHERE email = @email";
                using var checkCmd = new SqlCommand(checkSql, conn);
                checkCmd.Parameters.AddWithValue("@email", email);
                var existingId = await checkCmd.ExecuteScalarAsync();

                if (existingId != null)
                {
                    // Cập nhật thông tin
                    string updateSql = @"UPDATE users SET full_name=@full_name, username=@username, 
                        student_id=@student_id, role=@role" +
                        (string.IsNullOrEmpty(passwordHash) ? "" : ", password_hash=@password_hash") +
                        ", updated_at=GETDATE() WHERE email=@email";
                    using var updateCmd = new SqlCommand(updateSql, conn);
                    updateCmd.Parameters.AddWithValue("@full_name", fullName);
                    updateCmd.Parameters.AddWithValue("@username", username);
                    updateCmd.Parameters.AddWithValue("@student_id", studentId);
                    updateCmd.Parameters.AddWithValue("@role", role);
                    if (!string.IsNullOrEmpty(passwordHash))
                        updateCmd.Parameters.AddWithValue("@password_hash", passwordHash);
                    updateCmd.Parameters.AddWithValue("@email", email);
                    await updateCmd.ExecuteNonQueryAsync();
                }
                else
                {
                    // Insert mới
                    string insertSql = @"INSERT INTO users (full_name, email, username, student_id, password_hash, role, is_active, created_at, updated_at)
                        VALUES (@full_name, @email, @username, @student_id, @password_hash, @role, 1, GETDATE(), GETDATE())";
                    using var insertCmd = new SqlCommand(insertSql, conn);
                    insertCmd.Parameters.AddWithValue("@full_name", fullName);
                    insertCmd.Parameters.AddWithValue("@email", email);
                    insertCmd.Parameters.AddWithValue("@username", username);
                    insertCmd.Parameters.AddWithValue("@student_id", studentId);
                    insertCmd.Parameters.AddWithValue("@password_hash", passwordHash);
                    insertCmd.Parameters.AddWithValue("@role", role);
                    await insertCmd.ExecuteNonQueryAsync();
                }

                System.Diagnostics.Debug.WriteLine($"[SQLServer] Synced user: {username}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SQLServer] Sync error: {ex.Message}");
            }
        }

        public static async Task UpdateUserProfileAsync(string email, string hometown, string address, string avatarUrl)
        {
            try
            {
                var t1 = Task.Run(async () =>
                {
                    using var conn = new MySqlConnection(MySqlConnectionString);
                    await conn.OpenAsync();
                    var cmd = new MySqlCommand("UPDATE users SET hometown=@h, address=@a, avatar_url=@v WHERE email=@e", conn);
                    cmd.Parameters.AddWithValue("@h", hometown);
                    cmd.Parameters.AddWithValue("@a", address);
                    cmd.Parameters.AddWithValue("@v", avatarUrl);
                    cmd.Parameters.AddWithValue("@e", email);
                    await cmd.ExecuteNonQueryAsync();
                });

                var t2 = Task.Run(async () =>
                {
                    using var conn = new SqlConnection(SqlServerConnectionString);
                    await conn.OpenAsync();
                    var cmd = new SqlCommand("UPDATE users SET hometown=@h, address=@a, avatar_url=@v WHERE email=@e", conn);
                    cmd.Parameters.AddWithValue("@h", hometown);
                    cmd.Parameters.AddWithValue("@a", address);
                    cmd.Parameters.AddWithValue("@v", avatarUrl);
                    cmd.Parameters.AddWithValue("@e", email);
                    await cmd.ExecuteNonQueryAsync();
                });

                await Task.WhenAll(t1, t2);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseSync] Update profile error: {ex.Message}");
            }
        }

        public static async Task UpdatePasswordAsync(string email, string rawPassword)
        {
            string passwordHash = ComputeSha256Hash(rawPassword);
            if (string.IsNullOrEmpty(passwordHash)) return;

            try
            {
                var t1 = Task.Run(async () =>
                {
                    using var conn = new MySqlConnection(MySqlConnectionString);
                    await conn.OpenAsync();
                    var cmd = new MySqlCommand("UPDATE users SET password_hash=@p WHERE email=@e", conn);
                    cmd.Parameters.AddWithValue("@p", passwordHash);
                    cmd.Parameters.AddWithValue("@e", email);
                    await cmd.ExecuteNonQueryAsync();
                });

                var t2 = Task.Run(async () =>
                {
                    using var conn = new SqlConnection(SqlServerConnectionString);
                    await conn.OpenAsync();
                    var cmd = new SqlCommand("UPDATE users SET password_hash=@p WHERE email=@e", conn);
                    cmd.Parameters.AddWithValue("@p", passwordHash);
                    cmd.Parameters.AddWithValue("@e", email);
                    await cmd.ExecuteNonQueryAsync();
                });

                await Task.WhenAll(t1, t2);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DatabaseSync] Update password error: {ex.Message}");
            }
        }
    }
}
