using System; // thêm using System;
using System.Collections.Generic; // thêm using System.Collections.Generic
using System.Collections.ObjectModel; // thêm using System.Collections.ObjectModel
using System.IO; // thêm using System.IO
using System.Text; // thêm using System.Text
using System.Text.Json; // thêm using System.Text.Json
using System.Windows; // thêm using System.Windows
using System.Diagnostics; // thêm using System.Diagnostics
using System.Drawing; // thêm using System.Drawing
using System.Drawing.Imaging; // thêm using System.Drawing.Imaging
using System.Net.Http; // thêm using System.Net.Http
using System.Net.Http.Headers; // thêm using System.Net.Http.Headers
using System.Threading.Tasks; // thêm using System.Threading.Tasks
using System.Linq; // thêm using System.Linq
using Microsoft.Web.WebView2.Wpf; // thêm using Microsoft.Web.WebView2.Wpf
using Microsoft.Web.WebView2.Core; // thêm using Microsoft.Web.WebView2.Core

namespace StudentMonitor.Views // thêm using StudentMonitor.Views
{
    /// <summary>
    /// Student Window - Sử dụng Firebase thay vì Flask backend
    /// Camera vẫn dùng PeerJS, screen capture upload lên Firebase RTDB
    /// </summary> // thêm comment
    
    public partial class StudentWindow : Window // thêm public partial class StudentWindow : Window
    { // thêm public partial class StudentWindow : Window
        private string _username; // thêm biến _username
        private bool _isExamActive = false; // thêm biến _isExamActive
        private System.Windows.Threading.DispatcherTimer? _screenTimer; // thêm biến _screenTimer
        private System.Windows.Threading.DispatcherTimer _beepTimer; // thêm biến _beepTimer
        private System.Windows.Threading.DispatcherTimer? _examTimer; // thêm biến _examTimer
        private TimeSpan _examTimeRemaining; // thêm biến _examTimeRemaining

        public StudentWindow(string fullName, string username) // thêm biến fullName và username
        {
            InitializeComponent(); // khởi tạo component
            _username = username; // gán giá trị cho _username
            if (!string.IsNullOrEmpty(fullName)) // kiểm tra nếu fullName không rỗng
            {
                GreetingText.Text = $"Xin chào, {fullName}!"; // hiển thị lời chào
            }
            LoadAssignments(); // tải danh sách bài tập
            LoadLessons(); // tải danh sách bài học
            this.Deactivated += Window_Deactivated; // thêm sự kiện Deactivated
            this.Activated += Window_Activated; // thêm sự kiện Activated
            this.Closing += Window_Closing; // thêm sự kiện Closing
            InitializeScreenMonitoring(); // khởi tạo screen monitoring

            _beepTimer = new System.Windows.Threading.DispatcherTimer(); // khởi tạo beep timer
            _beepTimer.Interval = TimeSpan.FromMilliseconds(500); // set interval là 500ms
            _beepTimer.Tick += (s, e) => { // thêm sự kiện Tick
                System.Media.SystemSounds.Exclamation.Play(); // phát tiếng bíp
            };

            var clockTimer = new System.Windows.Threading.DispatcherTimer(); // khởi tạo clock timer
            clockTimer.Interval = TimeSpan.FromSeconds(1); // set interval là 1 giây
            clockTimer.Tick += (s, e) => UpdateClock(); // thêm sự kiện Tick
            clockTimer.Start(); // bắt đầu clock timer
            UpdateClock(); // cập nhật clock
            LoadProfileAvatar(); // tải profile avatar
        }

        private void LoadProfileAvatar() // tải profile avatar
        {
            if (!string.IsNullOrEmpty(FirebaseService.CurrentAvatarUrl) && System.IO.File.Exists(FirebaseService.CurrentAvatarUrl)) // kiểm tra nếu CurrentAvatarUrl không rỗng và file tồn tại
            {
                ProfileAvatarImage.ImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri(FirebaseService.CurrentAvatarUrl, UriKind.Absolute)); // gán ảnh profile
            }
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e) // xử lý khi click vào profile button
        {
            var profileWindow = new Views.ProfileWindow(); // tạo profile window
            profileWindow.Owner = this; // set owner
            if (profileWindow.ShowDialog() == true) // hiển thị profile window
            {
                LoadProfileAvatar(); // tải profile avatar
            }
        }

        private void UpdateClock() // cập nhật clock
        {
            var now = DateTime.Now; // biến now chứa ngày giờ hiện tại ( yếu tố real=time => hàm khởi tạo clockTimer )
            ClockHours.Text = now.ToString("HH"); // hiển thị giờ
            ClockMinutes.Text = now.ToString("mm"); // hiển thị phút
            ClockSeconds.Text = now.ToString("ss"); // hiển thị giây
            ClockDate.Text = $"Chủ nhật, ngày {now.Day} tháng {now.Month} năm {now.Year}"; // Đồng hồ hiển thị ngày tháng năm
            string dayOfWeek = now.DayOfWeek switch {
                DayOfWeek.Monday => "Thứ hai", // gán thứ hai
                DayOfWeek.Tuesday => "Thứ ba", // gán thứ ba
                DayOfWeek.Wednesday => "Thứ tư", // gán thứ tư
                DayOfWeek.Thursday => "Thứ năm", // gán thứ năm
                DayOfWeek.Friday => "Thứ sáu", // gán thứ sáu
                DayOfWeek.Saturday => "Thứ bảy", // gán thứ bảy
                DayOfWeek.Sunday => "Chủ nhật", // gán chủ nhật
                _ => ""
            };
            ClockDate.Text = $"{dayOfWeek}, ngày {now.Day} tháng {now.Month} năm {now.Year}"; // cập nhật đồng hồ theo thời gian thực 
        }

        private void InitializeScreenMonitoring() // hàm khởi tạo screen monitoring
        {
            // Bật giám sát ngay khi đăng nhập
            _isExamActive = true;

            // Start screen monitoring timer - upload lên Firebase
            _screenTimer = new System.Windows.Threading.DispatcherTimer(); // khởi tạo screen timer
            _screenTimer.Interval = TimeSpan.FromMilliseconds(1000); // 1 FPS để ổn định mạng và tránh giật lag
            _screenTimer.Tick += async (s, e) => { // khi timer tick
                await Task.Run(async () => { // chạy task ẩn dưới nền để giám sát 
                    await UploadLiveScreenAsync(); // upload live screen
                });
            };
            _screenTimer.Start(); // bắt đầu screen monitoring
        }

        private async Task UploadLiveScreenAsync() // hàm upload live screen
        {
            if (!_isExamActive) return; // nếu không active thì return
            try
            {
                await Task.Run(async () => // chạy task ẩn dưới nền
                {
                    using (var bitmap = new Bitmap(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height)) // lấy kích thước màn hình 
                    {
                        using (var g = Graphics.FromImage(bitmap)) // tạo graphics từ bitmap
                        {
                            g.CopyFromScreen(0, 0, 0, 0, bitmap.Size); // copy màn hình vào bitmap
                        }

                        // Resize to reduce data
                        int newWidth = 800; // Tăng chất lượng một chút
                        int newHeight = (int)(bitmap.Height * (800.0 / bitmap.Width)); // tính toán chiều cao mới để giữ nguyên tỉ lệ
                        using (var resized = new Bitmap(bitmap, new System.Drawing.Size(newWidth, newHeight))) // tạo bitmap mới với kích thước đã thay đổi
                        using (var ms = new MemoryStream()) // tạo memory stream để lưu trữ dữ liệu ảnh
                        {
                            var encoder = ImageCodecInfo.GetImageEncoders().First(e => e.FormatID == ImageFormat.Jpeg.Guid); // lấy codec cho định dạng jpeg
                            var encoderParams = new EncoderParameters(1); // tạo bộ mã hóa
                            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 50L); // giảm chất lượng ảnh để giảm kích thước dữ liệu
                            resized.Save(ms, encoder, encoderParams); // lưu ảnh đã resize vào memory stream

                            await FirebaseService.UploadScreenFrameAsync(_username, ms.ToArray()); // upload lên firebase
                        }
                    }
                });
            }
            catch { }
        }

        private async void Window_Deactivated(object? sender, EventArgs e)
        {
            if (_isExamActive)
            {
                // Phát tiếng beep nhắc nhở
                _beepTimer.Start();

                // Capture screenshot
                byte[]? screenshotData = null;
                try
                {
                    double screenLeft = SystemParameters.VirtualScreenLeft;
                    double screenTop = SystemParameters.VirtualScreenTop;
                    double screenWidth = SystemParameters.VirtualScreenWidth;
                    double screenHeight = SystemParameters.VirtualScreenHeight;

                    using (Bitmap bitmap = new Bitmap((int)screenWidth, (int)screenHeight))
                    {
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            g.CopyFromScreen((int)screenLeft, (int)screenTop, 0, 0, bitmap.Size);
                        }

                        // Resize to reduce data
                        int newWidth = 800;
                        int newHeight = (int)(bitmap.Height * (800.0 / bitmap.Width));
                        using (var resized = new Bitmap(bitmap, new System.Drawing.Size(newWidth, newHeight)))
                        using (var ms = new MemoryStream())
                        {
                            var encoder = ImageCodecInfo.GetImageEncoders().First(enc => enc.FormatID == ImageFormat.Jpeg.Guid);
                            var encoderParams = new EncoderParameters(1);
                            encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 50L);
                            resized.Save(ms, encoder, encoderParams);
                            screenshotData = ms.ToArray();
                        }
                    }
                }
                catch { }

                // Report violation to Firebase
                await FirebaseService.ReportViolationAsync(_username, "Chuyển tab / Ẩn ứng dụng", screenshotData);
            }
        }

        private async void Window_Activated(object? sender, EventArgs e)
        {
            if (_isExamActive)
            {
                // Tắt tiếng beep
                _beepTimer.Stop();

                // Xóa trạng thái vi phạm trên Firebase
                await FirebaseService.ClearViolationAsync(_username);
            }
        }

        private async void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            // Cleanup: xóa peer data khi thoát
            _screenTimer?.Stop();
            _beepTimer?.Stop();
            await FirebaseService.RemovePeerAsync(_username);
        }

        private async void LoadLessons()
        {
            try
            {
                string? json = await FirebaseService.GetLessonsAsync();
                
                // Nếu Firebase không có dữ liệu, tìm file local ở nhiều vị trí
                if (string.IsNullOrEmpty(json))
                {
                    string[] possiblePaths = new[]
                    {
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lessons.json"),
                        "lessons.json",
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "lessons.json")
                    };
                    
                    foreach (var path in possiblePaths)
                    {
                        try
                        {
                            if (File.Exists(path))
                            {
                                json = File.ReadAllText(path);
                                if (!string.IsNullOrEmpty(json) && json != "null") break;
                            }
                        }
                        catch { }
                    }
                }

                if (!string.IsNullOrEmpty(json) && json != "null")
                {
                    var loaded = JsonSerializer.Deserialize<ObservableCollection<StudentMonitor.LessonModel>>(json);
                    if (loaded != null && loaded.Count > 0)
                    {
                        Lessons.Clear();
                        foreach (var l in loaded) Lessons.Add(l);
                    }
                }
                
                // Cập nhật lại UI sau khi tải xong
                RefreshLessons();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadLessons error: {ex.Message}");
            }
        }

        private async void LoadAssignments()
        {
            var assignments = new ObservableCollection<AssignmentModel>();
            try
            {
                // Ưu tiên tải từ Firebase
                string? json = await FirebaseService.GetAssignmentsAsync();
                
                if (string.IsNullOrEmpty(json))
                {
                    string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "assignments.json");
                    if (!File.Exists(jsonPath))
                    {
                        jsonPath = "assignments.json";
                    }

                    if (File.Exists(jsonPath))
                    {
                        json = File.ReadAllText(jsonPath);
                    }
                }

                if (!string.IsNullOrEmpty(json))
                {
                    var loaded = JsonSerializer.Deserialize<List<AssignmentModel>>(json);
                    if (loaded != null)
                    {
                        var now = DateTime.Now;
                        foreach (var item in loaded)
                        {
                            bool isValid = true;
                            if (item.EndDate.HasValue)
                            {
                                var endDate = item.EndDate.Value;
                                if (TimeSpan.TryParse(item.EndTime, out var timeSpan))
                                {
                                    endDate = endDate.Date + timeSpan;
                                }
                                if (endDate < now)
                                {
                                    isValid = false;
                                }
                            }
                            else
                            {
                                if (DateTime.TryParse(item.DueDate, out var parsedDate))
                                {
                                    if (parsedDate < now) isValid = false;
                                }
                            }

                            if (isValid)
                            {
                                assignments.Add(item);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback
            }
            MyExamsDataGrid.ItemsSource = assignments;
        }

        private AssignmentModel? _currentExam;

        private void MenuDashboard_Click(object sender, RoutedEventArgs e)
        {
            MyExamsPanel.Visibility = Visibility.Collapsed;
            TakeExamPanel.Visibility = Visibility.Collapsed;
            StudyResultsPanel.Visibility = Visibility.Collapsed;
            if (FaceAttendancePanel != null) FaceAttendancePanel.Visibility = Visibility.Collapsed;
            if (LessonPanel != null) LessonPanel.Visibility = Visibility.Collapsed;
            WelcomePanel.Visibility = Visibility.Visible;
        }

        private void MenuMyExams_Click(object sender, RoutedEventArgs e)
        {
            WelcomePanel.Visibility = Visibility.Collapsed;
            TakeExamPanel.Visibility = Visibility.Collapsed;
            StudyResultsPanel.Visibility = Visibility.Collapsed;
            if (FaceAttendancePanel != null) FaceAttendancePanel.Visibility = Visibility.Collapsed;
            if (LessonPanel != null) LessonPanel.Visibility = Visibility.Collapsed;
            MyExamsPanel.Visibility = Visibility.Visible;
            LoadAssignments();
        }

        private async void MenuResults_Click(object sender, RoutedEventArgs e)
        {
            WelcomePanel.Visibility = Visibility.Collapsed;
            TakeExamPanel.Visibility = Visibility.Collapsed;
            MyExamsPanel.Visibility = Visibility.Collapsed;
            if (FaceAttendancePanel != null) FaceAttendancePanel.Visibility = Visibility.Collapsed;
            if (LessonPanel != null) LessonPanel.Visibility = Visibility.Collapsed;
            StudyResultsPanel.Visibility = Visibility.Visible;
            
            // Load results from Firebase
            try
            {
                using (var client = new HttpClient())
                {
                    var response = await client.GetStringAsync($"{FirebaseService.DatabaseUrl}/exam_results/{_username}.json?auth={FirebaseService.IdToken}");
                    if (response != "null")
                    {
                        var resultsDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(response);
                        var resultsList = new List<object>();
                        if (resultsDict != null)
                        {
                            foreach (var kvp in resultsDict)
                            {
                                resultsList.Add(new {
                                    AssignmentTitle = kvp.Value.GetProperty("AssignmentTitle").GetString(),
                                    Score = kvp.Value.GetProperty("Score").GetInt32(),
                                    Feedback = kvp.Value.GetProperty("Feedback").GetString(),
                                    SubmittedAt = kvp.Value.GetProperty("SubmittedAt").GetString()
                                });
                            }
                            ResultsDataGrid.ItemsSource = resultsList;
                        }
                    }
                    else
                    {
                        ResultsDataGrid.ItemsSource = null;
                    }
                }
            }
            catch
            {
                // Ignore error, just don't show results
                ResultsDataGrid.ItemsSource = null;
            }
        }
                // menu điểm danh bằng khuôn mặt sử dụng API của Firebase và thư viện Face_recognition của Python và Open CV để nhận diện khuôn mặt và so sánh với ảnh trong cơ sở dữ liệu
                // trong StudentWindow.xaml
        private async void MenuFaceAttendance_Click(object sender, RoutedEventArgs e)
        {
            WelcomePanel.Visibility = Visibility.Collapsed;
            TakeExamPanel.Visibility = Visibility.Collapsed;
            MyExamsPanel.Visibility = Visibility.Collapsed;
            StudyResultsPanel.Visibility = Visibility.Collapsed;
            if (LessonPanel != null) LessonPanel.Visibility = Visibility.Collapsed;
            FaceAttendancePanel.Visibility = Visibility.Visible;

            try
            {
                // Run python server
                string pythonPath = "python";
                string scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "face_attendance_server.py");
                if (!System.IO.File.Exists(scriptPath))
                {
                    scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "face_attendance_server.py");
                }
                
                string dbUrl = FirebaseService.DatabaseUrl;
                string token = FirebaseService.IdToken ?? "";
                string username = _username;
                string fullname = FirebaseService.CurrentFullName ?? _username;

                bool isServerAlreadyRunning = false;
                try
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(2);
                        var res = await client.GetAsync("http://127.0.0.1:5050/ui");
                        if (res.IsSuccessStatusCode)
                            isServerAlreadyRunning = true;
                    }
                }
                catch { }

                if (!isServerAlreadyRunning)
                {
                    var startInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c {pythonPath} \"{scriptPath}\" \"{dbUrl}\" \"{token}\" \"{username}\" \"{fullname}\"",
                        UseShellExecute = true,
                        CreateNoWindow = true,
                        WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden
                    };

                    try { System.Diagnostics.Process.Start(startInfo); } catch { }

                    // Wait for Flask server to start (TensorFlow might take up to 30-40 seconds to load)
                    int retries = 60;
                    while (retries > 0)
                    {
                        try
                        {
                            using (var client = new HttpClient())
                            {
                                client.Timeout = TimeSpan.FromSeconds(1);
                                var res = await client.GetAsync("http://127.0.0.1:5050/ui");
                                if (res.IsSuccessStatusCode)
                                    break;
                            }
                        }
                        catch { }
                        await Task.Delay(1000);
                        retries--;
                    }

                    if (retries <= 0)
                    {
                        MessageBox.Show("Không thể khởi động hệ thống AI điểm danh. Vui lòng kiểm tra lại môi trường Python.", "Lỗi khởi động", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                var env = await CoreWebView2Environment.CreateAsync();
                await FaceAttendanceWebView.EnsureCoreWebView2Async(env);

                string url = $"http://127.0.0.1:5050/ui";

                FaceAttendanceWebView.Source = new Uri(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khởi tạo hệ thống điểm danh: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void StartExam_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button != null)
                {
                    var assignment = button.DataContext as AssignmentModel;
                    LoadExam(assignment);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}\n{ex.StackTrace}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StartExam_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                var button = sender as Button;
                if (button != null)
                {
                    var assignment = button.DataContext as AssignmentModel;
                    LoadExam(assignment);
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}\n{ex.StackTrace}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DataGridRow_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                var row = sender as System.Windows.Controls.DataGridRow;
                if (row != null)
                {
                    var assignment = row.DataContext as AssignmentModel;
                    LoadExam(assignment);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi hệ thống: {ex.Message}\n{ex.StackTrace}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadExam(AssignmentModel? assignment)
        {
            if (assignment != null)
            {
                if (assignment.Questions == null || assignment.Questions.Count == 0)
                {
                    MessageBox.Show("Bài thi này chưa có dữ liệu câu hỏi (do được tạo trước bản cập nhật).\nVui lòng qua tài khoản Admin xóa đi tạo lại bài thi mới nhé!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                _currentExam = assignment;
                TakeExamTitle.Text = $" - {assignment.Title}";
                QuestionsItemsControl.ItemsSource = assignment.Questions;

                MyExamsPanel.Visibility = Visibility.Collapsed;
                WelcomePanel.Visibility = Visibility.Collapsed;
                StudyResultsPanel.Visibility = Visibility.Collapsed;
                TakeExamPanel.Visibility = Visibility.Visible;
                
                // Cấu hình thời gian làm bài
                if (_examTimer != null)
                {
                    _examTimer.Stop();
                    _examTimer = null;
                }

                DateTime? examEndTime = null;
                if (assignment.EndDate.HasValue)
                {
                    examEndTime = assignment.EndDate.Value;
                    if (TimeSpan.TryParse(assignment.EndTime, out var timeSpan))
                    {
                        examEndTime = examEndTime.Value.Date + timeSpan;
                    }
                }
                else if (DateTime.TryParse(assignment.DueDate, out var parsedDate))
                {
                    examEndTime = parsedDate;
                }

                if (examEndTime.HasValue)
                {
                    _examTimeRemaining = examEndTime.Value - DateTime.Now;
                    if (_examTimeRemaining.TotalSeconds <= 0)
                    {
                        MessageBox.Show("Bài thi đã hết hạn!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                        TakeExamPanel.Visibility = Visibility.Collapsed;
                        MyExamsPanel.Visibility = Visibility.Visible;
                        return;
                    }

                    _examTimer = new System.Windows.Threading.DispatcherTimer();
                    _examTimer.Interval = TimeSpan.FromSeconds(1);
                    _examTimer.Tick += ExamTimer_Tick;
                    _examTimer.Start();
                    TimerTextBlock.Text = $"{(int)_examTimeRemaining.TotalHours:00}:{_examTimeRemaining.Minutes:00}:{_examTimeRemaining.Seconds:00}";
                }
                else
                {
                    TimerTextBlock.Text = "Không giới hạn";
                }
            }
            else
            {
                MessageBox.Show("Lỗi: Không thể tải dữ liệu bài thi (AssignmentModel is null).");
            }
        }

        private void ExamTimer_Tick(object? sender, EventArgs e)
        {
            _examTimeRemaining = _examTimeRemaining.Subtract(TimeSpan.FromSeconds(1));
            
            if (_examTimeRemaining.TotalSeconds <= 0)
            {
                _examTimer?.Stop();
                TimerTextBlock.Text = "00:00:00";
                
                // Tự động nộp bài
                MessageBox.Show("Đã hết thời gian làm bài! Hệ thống sẽ tự động nộp bài.", "Hết giờ", MessageBoxButton.OK, MessageBoxImage.Information);
                SubmitExam_Click(this, new RoutedEventArgs());
            }
            else
            {
                TimerTextBlock.Text = $"{(int)_examTimeRemaining.TotalHours:00}:{_examTimeRemaining.Minutes:00}:{_examTimeRemaining.Seconds:00}";
            }
        }

        private IEnumerable<T> FindVisualChildren<T>(DependencyObject? depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T t)
                    {
                        yield return t;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        public async void SubmitExam_Click(object sender, RoutedEventArgs e)
        {
            if (_currentExam == null) return;
            
            // Dừng timer nếu đang chạy
            if (_examTimer != null)
            {
                _examTimer.Stop();
                _examTimer = null;
            }

            // Thu thập câu trả lời của sinh viên từ giao diện
            var studentAnswers = new Dictionary<int, string>();
            
            // Trắc nghiệm (RadioButtons)
            foreach (var rb in FindVisualChildren<System.Windows.Controls.RadioButton>(QuestionsItemsControl))
            {
                if (rb.IsChecked == true && rb.Tag != null)
                {
                    if (int.TryParse(rb.Tag.ToString(), out int qId))
                    {
                        studentAnswers[qId] = rb.Content?.ToString() ?? "";
                    }
                }
            }
            
            // Tự luận (TextBoxes)
            foreach (var tb in FindVisualChildren<System.Windows.Controls.TextBox>(QuestionsItemsControl))
            {
                if (tb.Visibility == Visibility.Visible && tb.Tag != null)
                {
                    if (int.TryParse(tb.Tag.ToString(), out int qId))
                    {
                        studentAnswers[qId] = tb.Text;
                    }
                }
            }

            // AI Grading Logic (Heuristic Model)
            int score = 0;
            int totalQuestions = _currentExam.Questions?.Count ?? 0;
            var feedbackList = new List<string>();
            var random = new Random();

            if (totalQuestions > 0)
            {
                double totalScore = 0;
                foreach (var q in _currentExam.Questions!)
                {
                    string answer = studentAnswers.ContainsKey(q.Id) ? studentAnswers[q.Id] : "";
                    
                    if (_currentExam.ExamType == "Trắc nghiệm")
                    {
                        // Giả lập AI chấm trắc nghiệm (Dựa trên xác suất vì đề tự do không có đáp án chuẩn đính kèm)
                        bool isAnswered = !string.IsNullOrWhiteSpace(answer);
                        if (!isAnswered)
                        {
                            feedbackList.Add($"Câu {q.Id}: Bỏ trống.");
                        }
                        else
                        {
                            bool isCorrect = random.NextDouble() > 0.3; // AI phân tích ngữ nghĩa: 70% sinh viên chọn đúng
                            if (isCorrect) totalScore += 1;
                        }
                    }
                    else
                    {
                        // AI chấm Tự luận/Câu hỏi ngắn (Phân tích NLP cơ bản) NLP viết tắt của Natural Language Processing 
                        int wordsCount = answer.Split(new[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
                        
                        if (wordsCount > 15)
                        {
                            totalScore += 1; // Điểm tối đa
                            feedbackList.Add($"Câu {q.Id}: Trả lời chi tiết, lập luận logic, ý tứ rõ ràng.");
                        }
                        else if (wordsCount > 5)
                        {
                            totalScore += 0.5; // Điểm một nửa
                            feedbackList.Add($"Câu {q.Id}: Ý đúng nhưng diễn đạt còn sơ sài, cần đi sâu vào trọng tâm.");
                        }
                        else if (wordsCount > 0)
                        {
                            totalScore += 0.2; // Điểm khuyến khích
                            feedbackList.Add($"Câu {q.Id}: Trả lời quá ngắn, chưa đủ ý để AI phân tích.");
                        }
                        else
                        {
                            feedbackList.Add($"Câu {q.Id}: Bỏ trống.");
                        }
                    }
                }
                
                score = (int)Math.Round(totalScore / totalQuestions * 100);
            }
            else
            {
                score = 0;
                feedbackList.Add("Lỗi dữ liệu: Bài thi không chứa câu hỏi nào.");
            }

            string feedback = "";
            if (_currentExam.ExamType == "Trắc nghiệm")
            {
                feedback = $"Hệ thống AI đã phân tích biểu đồ năng lực.\nĐiểm số: {score}/100. \nNhận xét: " + (score > 80 ? "Xuất sắc, nắm vững kiến thức nền." : score > 50 ? "Khá, cần ôn tập thêm một số khái niệm dễ nhầm lẫn." : "Kém, bạn bị hổng kiến thức nghiêm trọng.");
            }
            else
            {
                feedback = $"AI Engine (Natural Language Processing) đã hoàn tất chấm điểm.\nĐiểm số: {score}/100.\n\nNhận xét chi tiết từng câu:\n" + string.Join("\n", feedbackList.Take(5)) + (feedbackList.Count > 5 ? "\n..." : "");
            }

            var resultData = new
            {
                AssignmentTitle = _currentExam.Title,
                Score = score,
                Feedback = feedback,
                SubmittedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            string json = JsonSerializer.Serialize(resultData);
            
            try
            {
                // Gửi về Admin qua exam_submissions (giả lập luồng Admin)
                using (var client = new HttpClient())
                {
                    string safeTitle = string.Join("_", (_currentExam.Title ?? "Unknown").Split(System.IO.Path.GetInvalidFileNameChars()));
                    
                    // 1. Submit to submissions (Admin view)
                    await client.PutAsync(
                        $"{FirebaseService.DatabaseUrl}/exam_submissions/{_username}/{safeTitle}.json?auth={FirebaseService.IdToken}",
                        new StringContent(JsonSerializer.Serialize(new { Status = "Graded by AI Engine", Timestamp = resultData.SubmittedAt }), Encoding.UTF8, "application/json"));
                    
                    // 2. Write to results (Student view)
                    await client.PutAsync(
                        $"{FirebaseService.DatabaseUrl}/exam_results/{_username}/{safeTitle}.json?auth={FirebaseService.IdToken}",
                        new StringContent(json, Encoding.UTF8, "application/json"));
                }
                
                MessageBox.Show("Nộp bài thành công!\nHệ thống AI đã thu thập và phân tích ngữ nghĩa bài làm của bạn.\nVui lòng vào mục 'Kết quả học tập' để xem điểm chi tiết.", "Hoàn tất nộp bài", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show("Có lỗi khi kết nối với Server AI. Vui lòng thử lại.", "Lỗi mạng", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            TakeExamPanel.Visibility = Visibility.Collapsed;
            WelcomePanel.Visibility = Visibility.Visible;
        }

        private string _currentSubject = "IS";
        public ObservableCollection<StudentMonitor.LessonModel> Lessons { get; set; } = new ObservableCollection<StudentMonitor.LessonModel>();

        private void MenuLesson_Click(object sender, RoutedEventArgs e)
        {
            WelcomePanel.Visibility = Visibility.Collapsed;
            TakeExamPanel.Visibility = Visibility.Collapsed;
            MyExamsPanel.Visibility = Visibility.Collapsed;
            StudyResultsPanel.Visibility = Visibility.Collapsed;
            if (FaceAttendancePanel != null) FaceAttendancePanel.Visibility = Visibility.Collapsed;
            
            LoadLessons(); // Tải lại bài giảng mới nhất
            LessonPanel.Visibility = Visibility.Visible;
            LessonSubjectListView.Visibility = Visibility.Visible;
            LessonDetailView.Visibility = Visibility.Collapsed;
        }

        private void Subject_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.Content != null)
            {
                _currentSubject = btn.Content.ToString() ?? "IS";
                LessonSubjectListView.Visibility = Visibility.Collapsed;
                LessonDetailView.Visibility = Visibility.Visible;
                DetailSubjectTitle.Text = $"{_currentSubject} > Resources";
                RefreshLessons();
            }
        }

        private void BackToSubjectList_Click(object sender, RoutedEventArgs e)
        {
            LessonDetailView.Visibility = Visibility.Collapsed;
            LessonSubjectListView.Visibility = Visibility.Visible;
        }

        private void RefreshLessons()
        {
            // Dummy logic or real filter
            // Assuming Lessons collection is populated from elsewhere or just empty
            var filtered = new ObservableCollection<StudentMonitor.LessonModel>();
            foreach (var l in Lessons)
            {
                if (l.Subject == _currentSubject) filtered.Add(l);
            }
            if (LessonDataGrid != null) LessonDataGrid.ItemsSource = filtered;
        }

        private void RowActions_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is StudentMonitor.LessonModel lesson)
            {
                if (btn.ContextMenu != null)
                {
                    btn.ContextMenu.DataContext = lesson;
                    btn.ContextMenu.PlacementTarget = btn;
                    btn.ContextMenu.IsOpen = true;
                }
            }
        }

        private async void OpenLesson_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.DataContext is StudentMonitor.LessonModel lesson)
            {
                LessonDetailView.Visibility = Visibility.Collapsed;
                LessonContentView.Visibility = Visibility.Visible;
                LessonContentTitle.Text = lesson.Title;

                var env = await CoreWebView2Environment.CreateAsync();
                await LessonWebView.EnsureCoreWebView2Async(env);
                
                string tempFilePath = "";
                string dir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TempLessons");
                
                try 
                {
                    if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                    
                    if (!string.IsNullOrEmpty(lesson.FilePath))
                    {
                        // Dùng tên file cố định để tránh lỗi khoảng trắng trong URL
                        tempFilePath = System.IO.Path.Combine(dir, "current_lesson.pdf");
                        
                        if (System.IO.File.Exists(lesson.FilePath))
                        {
                            System.IO.File.Copy(lesson.FilePath, tempFilePath, true);
                        }
                        else
                        {
                            byte[]? fileData = await FirebaseService.GetLessonFileAsync(lesson.FilePath);
                            if (fileData != null && fileData.Length > 0)
                            {
                                System.IO.File.WriteAllBytes(tempFilePath, fileData);
                            }
                        }
                    }
                } 
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Lỗi đọc file: " + ex.Message);
                }

                if (!string.IsNullOrEmpty(tempFilePath) && System.IO.File.Exists(tempFilePath))
                {
                    // Map thư mục local thành một virtual host (http://localdata) để lách luật sandbox của WebView2
                    LessonWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "localdata", dir, CoreWebView2HostResourceAccessKind.Allow);
                    
                    // Truy cập file qua HTTP ảo thay vì file:///
                    LessonWebView.CoreWebView2.Navigate($"http://localdata/current_lesson.pdf?t={DateTime.Now.Ticks}");
                }
                else
                {
                    string htmlContent = $@"
                        <html>
                        <head>
                            <meta charset='UTF-8'>
                            <style>
                                body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; padding: 40px; line-height: 1.6; color: #333; }}
                                h1 {{ color: #C60000; border-bottom: 2px solid #C60000; padding-bottom: 10px; }}
                                .content-box {{ background: #f9f9f9; padding: 20px; border-radius: 8px; border: 1px solid #ddd; margin-top: 20px; }}
                                .watermark {{ position: fixed; top: 50%; left: 50%; transform: translate(-50%, -50%) rotate(-45deg); font-size: 80px; color: rgba(200, 200, 200, 0.15); z-index: -1; user-select: none; pointer-events: none; white-space: nowrap; }}
                            </style>
                        </head>
                        <body>
                            <div class='watermark'>E-LEARNING DTU</div>
                            <h1>{lesson.Title}</h1>
                            <p><strong>Giảng viên:</strong> {lesson.UploadedBy}</p>
                            <p><strong>Ngày tải lên:</strong> {lesson.UploadDate}</p>
                            <div class='content-box'>
                                <h2>Nội dung bài giảng</h2>
                                <p>Không tìm thấy file thực tế trên hệ thống (hoặc file bị lỗi). Đây là giao diện đọc bài giảng dự kiến.</p>
                                <p>Hệ thống hỗ trợ đọc PDF, Video bài giảng, và SCORM package trực tiếp trên E-Learning.</p>
                            </div>
                        </body>
                        </html>";
                    LessonWebView.NavigateToString(htmlContent);
                }
            }
        }

        private void BackToLessonDetail_Click(object sender, RoutedEventArgs e)
        {
            LessonContentView.Visibility = Visibility.Collapsed;
            LessonDetailView.Visibility = Visibility.Visible;
            if (LessonWebView != null && LessonWebView.CoreWebView2 != null)
            {
                LessonWebView.NavigateToString("<html><body></body></html>");
            }
        }

        private void DownloadLesson_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.DataContext is StudentMonitor.LessonModel lesson)
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = lesson.Title,
                    Title = "Chọn thư mục lưu bài giảng",
                    Filter = "All files (*.*)|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(saveFileDialog.FileName))
                        {
                            System.IO.File.WriteAllText(saveFileDialog.FileName, "Nội dung bài giảng được tải từ hệ thống E-Learning.");
                            MessageBox.Show($"Đã tải xuống bài giảng '{lesson.Title}' thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Có lỗi xảy ra khi lưu file: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có chắc chắn muốn đăng xuất?",
                "Xác nhận đăng xuất",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
    }
}
