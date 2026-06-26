using System.Windows; // lớp xử lý sự kiện winform và wpf
using System.Windows.Controls; // lớp xử lý sự kiện winform và wpf
using Microsoft.Win32; // lớp xử lý sự kiện winform và wpf
using System.Collections.ObjectModel; // lớp xử lý sự kiện winform và wpf
using System.IO; // lớp xử lý sự kiện winform và wpf
using System.Text.Json; // lớp xử lý sự kiện winform và wpf
using System.Collections.Generic; // lớp xử lý sự kiện winform và wpf
using System.Diagnostics; // lớp xử lý sự kiện winform và wpf
using System.Net.Http; // lớp xử lý sự kiện winform và wpf
using System.Threading.Tasks; // lớp xử lý sự kiện winform và wpf
using Microsoft.Web.WebView2.Core; // lớp xử lý sự kiện winform và wpf

namespace StudentMonitor // không gian tên StudentMonitor
{
    public class TeacherNote // lớp xử lý sự kiện winform và wpf
    {
        public string? Id { get; set; } // thuộc tính ID
        public string? NoteContent { get; set; } // thuộc tính nội dung ghi chú
        public string? CreatedDate { get; set; } // thuộc tính ngày tạo

        public TeacherNote() // hàm tạo
        {
            CreatedDate = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm"); // gán giá trị cho thuộc tính ngày tạo theo định dạng dd/MM/yyyy HH:mm
        }
    }

    public class QuestionModel // lớp xử lý sự kiện winform và wpf
    {
        public int Id { get; set; } // thuộc tính ID
        public string? Text { get; set; } // thuộc tính nội dung câu hỏi
        public List<string>? Options { get; set; } // thuộc tính danh sách các đáp án
        public string? CorrectAnswer { get; set; } // thuộc tính đáp án đúng
    }

    public class AssignmentModel // lớp xử lý sự kiện winform và wpf
    {
        public string? Title { get; set; } // thuộc tính tiêu đề
        public string? Description { get; set; } // thuộc tính mô tả
        public string? Status { get; set; } // thuộc tính trạng thái
        public string? OpenDate { get; set; } // thuộc tính ngày mở
        public string? DueDate { get; set; } // thuộc tính ngày hết hạn
        public string? LastModifiedBy { get; set; } // thuộc tính người sửa
        public string? ModifiedDate { get; set; } // thuộc tính ngày sửa
        
        // Additional properties for editing
        public System.DateTime? StartDate { get; set; } // thuộc tính ngày bắt đầu
        public string? StartTime { get; set; } // thuộc tính giờ bắt đầu
        public System.DateTime? EndDate { get; set; } // thuộc tính ngày kết thúc
        public string? EndTime { get; set; } // thuộc tính giờ kết thúc
        public bool IsOnlineExam { get; set; } = true; // thuộc tính kiểm tra trực tuyến
        public string? ExamType { get; set; } // "Trắc nghiệm", "Câu hỏi ngắn", "Tự luận"
        public string? FileName { get; set; } // thuộc tính tên tệp
        public string? ExtractedQuestionsText { get; set; } // thuộc tính nội dung câu hỏi đã trích xuất
        public List<QuestionModel>? Questions { get; set; } // thuộc tính danh sách các câu hỏi
        public ObservableCollection<TeacherNote>? Notes { get; set; } // thuộc tính danh sách các ghi chú
    }

    public class ActiveStudentModel // lớp xử lý sự kiện winform và wpf kích hoạt mô hình sinh viên
    {
        public string? username { get; set; } // thuộc tính tên người dùng
        public string? full_name { get; set; } // thuộc tính tên đầy đủ
        public string? camera_peer_id { get; set; } // thuộc tính ID của camera
        public string? screen_peer_id { get; set; } // thuộc tính ID của màn hình
    }

    public class ViolationModel // lớp xử lý sự kiện winform và wpf mô hình vi phạm
    {
        public string id { get; set; } = ""; // thuộc tính ID
        public string? full_name { get; set; } // thuộc tính tên đầy đủ
        public string? username { get; set; } // thuộc tính tên người dùng
        public string? violation_type { get; set; } // thuộc tính loại vi phạm
        public string? screenshot_base64 { get; set; } // thuộc tính ảnh chụp màn hình  
        public string? captured_at { get; set; } // thuộc tính thời gian chụp

        public string captured_at_display // thuộc tính hiển thị thời gian chụp
        {
            get 
            {
                if (System.DateTime.TryParse(captured_at, out var dt)) // phân tích chuỗi thời gian (nếu hệ thống lưu là UTC thì chuyển sang giờ địa phương)
                    return dt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"); // chuyển sang định dạng dd/MM/yyyy HH:mm:ss
                return captured_at ?? ""; // trả về chuỗi rỗng nếu không có dữ liệu
            }
        }

        public System.Windows.Media.Imaging.BitmapImage? image_source // thuộc tính hiển thị ảnh chụp màn hình
        {
            get
            {
                if (string.IsNullOrEmpty(screenshot_base64)) return null; // nếu không có dữ liệu ảnh chụp màn hình trả về null
                try
                {
                    byte[] binaryData = System.Convert.FromBase64String(screenshot_base64); // chuyển chuỗi base64 sang mảng byte
                    var bi = new System.Windows.Media.Imaging.BitmapImage(); // tạo đối tượng BitmapImage
                    bi.BeginInit(); // bắt đầu khởi tạo BitmapImage
                    bi.StreamSource = new System.IO.MemoryStream(binaryData); // gán nguồn cho BitmapImage
                    bi.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad; // gán tùy chọn bộ nhớ cho BitmapImage
                    bi.EndInit(); // kết thúc khởi tạo BitmapImage
                    return bi; // trả về BitmapImage
                }
                catch { return null; } // nếu có lỗi trả về null
            }
        }
    }

    public class StudentGradeModel // lớp xử lý sự kiện winform và wpf mô hình điểm của sinh viên
    {
        public string? Username { get; set; } // thuộc tính tên người dùng
        public string? AssignmentTitle { get; set; } // thuộc tính tiêu đề bài tập
        public double Score { get; set; } // thuộc tính điểm số
        public string? Feedback { get; set; } // thuộc tính phản hồi
        public string? SubmittedAt { get; set; } // thuộc tính nộp bài xử lý check xem sinh viên nộp bài đúng hạn không 
    }

    public class AttendanceRecordModel // lớp xử lý sự kiện winform và wpf mô hình điểm danh
    {
        public string? Username { get; set; } // thuộc tính tên người dùng
        public string? Fullname { get; set; } // thuộc tính tên đầy đủ
        public double Similarity { get; set; } // thuộc tính so sánh
        public bool IsMatch { get; set; } // thuộc tính kiểm tra
        public string? Verdict { get; set; } // thuộc tính xử lý kết quả
        public string? Timestamp { get; set; } // thuộc tính thời gian

        // thuộc tính hiển thị được sửa từ các thuộc tính trên 
        public string SimilarityDisplay => Similarity.ToString("0.0") + "%"; // hiển thị so sánh thông thường + hiển thị dưới dạng số sánh phần trăm 
        public string TimestampDisplay // hiển thị thời gian
        { 
            get
            {
                if (DateTime.TryParse(Timestamp, out var dt)) // phân tích chuỗi thời gian
                    return dt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss"); // chuyển sang định dạng dd/MM/yyyy HH:mm:ss
                return Timestamp ?? ""; // trả về chuỗi rỗng nếu không có dữ liệu
            }
        }
    }

    public class LessonModel // lớp xử lý sự kiện winform và wpf mô hình bài giảng
    {
        public string? Title { get; set; } // thuộc tính tiêu đề bài giảng dùng để hiển thị tiêu đề bài giảng
        public string? Subject { get; set; } // thuộc tính môn học dùng để hiển thị môn học 
        public string? Access { get; set; } // thuộc tính truy cập dung để hiển thị bài giảng công khai hay riêng tư 
        public string? UploadDate { get; set; } // thuộc tính ngày tải lên để sinh viên và giảng viên biết ngay tải lên bài giảng
        public string? UploadedBy { get; set; } // thuộc tính người tải lên hiển thị người đăng dưới dạng upload By
        public string? Size { get; set; } // thuộc tính kích thước file dùng để cho hệ thống biết định dạng file, dung lượng file 
        public string? FilePath { get; set; } // thuộc tính đường dẫn file dùng để hiển thị 1 popup windown nổi cho phép sinh viên chọn nơi lưu trữ file 
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window // lớp xử lý sự kiện winform và wpf của giảng viên (wpf ở đây viết tắt là windows presentation foundation (trình bày windowns bằng công nghệ c#))
    {
        public ObservableCollection<TeacherNote> Notes { get; set; } // đây là danh sách ghi chú (note lại hành vi gian lận của sinh viên)
        public ObservableCollection<LessonModel> Lessons { get; set; } // đây là danh sách bài giảng được load từ admin -> database firebase -> sinh viên
        private AssignmentModel? _currentEditingAssignment = null; // đây là bài giảng status editting (tức là bài giảng đang chỉnh sửa), nếu không có bài giảng nào được chọn thì trả về null

        private string _currentSubject = "IS"; // đây là môn học hiện tại theo học kỳ

        public MainWindow() // lớp window chạy đầu tiên khi mở ứng dụng (home screen)
        {
            // khởi tạo các control trong windown
            InitializeComponent(); // ở đây là đang khởi tạo các component khi phần mềm khởi chạy
            Notes = new ObservableCollection<TeacherNote>(); // ở đây là đang khởi tạo danh sách ghi chú (note lại hành vi gian lận của sinh viên)
            NotesDataGrid.ItemsSource = Notes; // gán danh sách ghi chú cho NotesDataGrid (dữ liệu ghi chú dạng lưới )
            
            Lessons = new ObservableCollection<LessonModel>(); // ở đây là môn họ và đây là phần leson cho giảng viên dùng để upload giáo trình lên
            LessonDataGrid.ItemsSource = Lessons; // gán danh sách bài giảng cho LessonDataGrid (dữ liệu bài giảng dạng lưới )
            
            LoadAssignments(); // tải danh sách bài giảng
            LoadLessons(); // tải danh sách bài giảng
            
            // kiểm tra xem giảng viên có quyền xóa bài thi không (nếu đúng role admin thì cho xoá, còn không phải admin thì k cho xoá)
            CancelExamSessionButton.Visibility = Visibility.Visible; // nếu đúng role admin thì cho xoá, còn không phải admin thì k cho xoá

            var clockTimer = new System.Windows.Threading.DispatcherTimer(); // tạo bộ đếm thời gian (timer) để cập nhật thời gian thực
            clockTimer.Interval = System.TimeSpan.FromSeconds(1); // đặt khoảng thời gian là 1 giây 
            clockTimer.Tick += (s, e) => UpdateClock(); // khi bộ đếm thời gian hết 1 giây thì gọi hàm UpdateClock
            clockTimer.Start(); // bắt đầu bộ đếm thời gian
            UpdateClock(); // cập nhật thời gian 
            LoadProfileAvatar();
        }

        private void LoadProfileAvatar()
        {
            if (!string.IsNullOrEmpty(FirebaseService.CurrentAvatarUrl) && System.IO.File.Exists(FirebaseService.CurrentAvatarUrl))
            {
                ProfileAvatarImage.ImageSource = new System.Windows.Media.Imaging.BitmapImage(new System.Uri(FirebaseService.CurrentAvatarUrl, System.UriKind.Absolute));
            }
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            var profileWindow = new Views.ProfileWindow();
            profileWindow.Owner = this;
            if (profileWindow.ShowDialog() == true)
            {
                LoadProfileAvatar();
            }
        }

        private void UpdateClock()
        {
            var now = System.DateTime.Now;
            ClockHours.Text = now.ToString("HH");
            ClockMinutes.Text = now.ToString("mm");
            ClockSeconds.Text = now.ToString("ss");
            string dayOfWeek = now.DayOfWeek switch {
                System.DayOfWeek.Monday => "Thứ hai",
                System.DayOfWeek.Tuesday => "Thứ ba",
                System.DayOfWeek.Wednesday => "Thứ tư",
                System.DayOfWeek.Thursday => "Thứ năm",
                System.DayOfWeek.Friday => "Thứ sáu",
                System.DayOfWeek.Saturday => "Thứ bảy",
                System.DayOfWeek.Sunday => "Chủ nhật",
                _ => ""
            };
            ClockDate.Text = $"{dayOfWeek}, ngày {now.Day} tháng {now.Month} năm {now.Year}";
        }

        private async void LoadLessons()
        {
            try
            {
                string? json = await FirebaseService.GetLessonsAsync();
                if (string.IsNullOrEmpty(json) && File.Exists("lessons.json"))
                {
                    json = File.ReadAllText("lessons.json");
                    if (!string.IsNullOrEmpty(json))
                    {
                        _ = FirebaseService.UploadLessonsAsync(json);
                    }
                }

                if (!string.IsNullOrEmpty(json) && json != "null")
                {
                    var loaded = JsonSerializer.Deserialize<ObservableCollection<LessonModel>>(json);
                    if (loaded != null)
                    {
                        Lessons.Clear();
                        foreach (var l in loaded) Lessons.Add(l);
                    }
                }
            }
            catch { }
        }

        private async void SaveLessons()
        {
            try
            {
                string json = JsonSerializer.Serialize(Lessons);
                
                // Lưu file local
                string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lessons.json");
                File.WriteAllText(localPath, json);
                
                // Cũng lưu ở thư mục project nếu khác
                try { File.WriteAllText("lessons.json", json); } catch { }
                
                // Upload lên Firebase
                bool uploaded = await FirebaseService.UploadLessonsAsync(json);
                if (!uploaded)
                {
                    MessageBox.Show("Lưu bài giảng local thành công nhưng không thể đồng bộ lên Firebase.\nSinh viên trên máy khác sẽ không thấy bài giảng này.", 
                        "Cảnh báo đồng bộ", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu bài giảng: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void LoadAssignments()
        {
            var assignments = new ObservableCollection<AssignmentModel>();
            try
            {
                // Ưu tiên tải từ Firebase
                string? json = await FirebaseService.GetAssignmentsAsync();
                
                // Fallback xuống file cục bộ nếu Firebase rỗng hoặc lỗi
                if (string.IsNullOrEmpty(json) && File.Exists("assignments.json"))
                {
                    json = File.ReadAllText("assignments.json");
                    if (!string.IsNullOrEmpty(json) && json.Length > 5)
                    {
                        // Đồng bộ file nội bộ lên Firebase luôn
                        _ = FirebaseService.UploadAssignmentsAsync(json);
                    }
                }

                if (!string.IsNullOrEmpty(json))
                {
                    var loaded = JsonSerializer.Deserialize<List<AssignmentModel>>(json);
                    if (loaded != null)
                    {
                        var now = System.DateTime.Now;
                        foreach (var item in loaded)
                        {
                            bool isValid = true;
                            if (item.EndDate.HasValue)
                            {
                                var endDate = item.EndDate.Value;
                                if (System.TimeSpan.TryParse(item.EndTime, out var timeSpan))
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
                                if (System.DateTime.TryParse(item.DueDate, out var parsedDate))
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
                // Fallback on error
            }
            AssignmentsDataGrid.ItemsSource = assignments;
        }

        private async void SaveAssignmentsList()
        {
            try
            {
                var assignments = AssignmentsDataGrid.ItemsSource as ObservableCollection<AssignmentModel>;
                if (assignments != null)
                {
                    string json = JsonSerializer.Serialize(assignments);
                    File.WriteAllText("assignments.json", json); // Lưu cục bộ
                    await FirebaseService.UploadAssignmentsAsync(json); // Đồng bộ lên Firebase
                }
            }
            catch
            {
                // Fallback on error
            }
        }

        private void ClearExamForm()
        {
            _currentEditingAssignment = null; // Xóa thông tin bài tập đang chỉnh sửa 
            UploadExamHeader.Text = "Upload Đề Thi"; // tiêu đề cửa sổ upload đề thi
            ExamTitleTextBox.Text = ""; // tiêu đề của đề thi
            ExamDescriptionTextBox.Text = ""; // mô tả của đề thi
            ExamTypeComboBox.SelectedIndex = 0; // loại của đề thi
            StartDatePicker.SelectedDate = null; // ngày bắt đầu của đề thi
            StartTimeTextBox.Text = "08:00"; // thời gian bắt đầu của đề thi
            EndDatePicker.SelectedDate = null; // ngày kết thúc của đề thi
            EndTimeTextBox.Text = "10:00"; // thời gian kết thúc của đề thi
            RadioOnline.IsChecked = true; // radio button online
            SelectedFileNameTextBlock.Text = "Kéo thả file đề thi vào đây hoặc nhấn để duyệt file"; // tên file của đề thi
            SelectedFileNameTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(102, 102, 102)); // màu của tên file đề thi
            ExtractedQuestionsPanel.Visibility = Visibility.Collapsed; // ẩn panel câu hỏi 
            if (CancelExamSessionButton != null) CancelExamSessionButton.Visibility = Visibility.Collapsed; // ẩn nút hủy phiên
            Notes.Clear(); // xóa ghi chú
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e) // lớp xử lý sự kiện click nút đăng xuất
        {
            var result = MessageBox.Show( // hàm hiện hộp thoại xác nhận 
                "Bạn có chắc chắn muốn đăng xuất?",
                "Xác nhận đăng xuất",
                MessageBoxButton.YesNo, // nút yes và no
                MessageBoxImage.Question); // icon question

            if (result == MessageBoxResult.Yes) // nếu chọn yes
            {
                var loginWindow = new Views.LoginWindow(); // tạo cửa sổ đăng nhập mới
                loginWindow.Show(); // hiển thị cửa sổ đăng nhập mới
                this.Close(); // đóng cửa sổ hiện tại
            }
        }

        private void MenuUploadExam_Click(object sender, RoutedEventArgs e) // lớp xử lý sự kiện click nút upload đề thi
        {
            WelcomePanel.Visibility = Visibility.Collapsed; // ẩn panel chào mừng
            if (ExamListPanel != null) ExamListPanel.Visibility = Visibility.Collapsed; // ẩn panel danh sách đề thi
            if (AntiCheatPanel != null) AntiCheatPanel.Visibility = Visibility.Collapsed; // ẩn panel chống gian lận
            if (MonitorStudentsPanel != null) MonitorStudentsPanel.Visibility = Visibility.Collapsed; // ẩn panel giám sát học sinh
            if (StudentGradesPanel != null) StudentGradesPanel.Visibility = Visibility.Collapsed; // ẩn panel bảng điểm
            if (AttendancePanel != null) AttendancePanel.Visibility = Visibility.Collapsed; // ẩn panel điểm danh
            if (LessonPanel != null) LessonPanel.Visibility = Visibility.Collapsed; // ẩn panel bài giảng
            if (EvidencePanel != null) EvidencePanel.Visibility = Visibility.Collapsed; // ẩn panel bằng chứng
            UploadExamPanel.Visibility = Visibility.Visible; // hiển thị panel upload đề thi
            ClearExamForm(); // xóa form upload đề thi
        }

        private void MenuHome_Click(object sender, RoutedEventArgs e) // lớp xử lý sự kiện click nút trang chủ
        {
            UploadExamPanel.Visibility = Visibility.Collapsed; // ẩn panel upload đề thi
            ExamListPanel.Visibility = Visibility.Collapsed; // ẩn panel danh sách đề thi
            AntiCheatPanel.Visibility = Visibility.Collapsed; // ẩn panel chống gian lận
            if (MonitorStudentsPanel != null) MonitorStudentsPanel.Visibility = Visibility.Collapsed; // ẩn panel giám sát học sinh
            if (StudentGradesPanel != null) StudentGradesPanel.Visibility = Visibility.Collapsed; // ẩn panel bảng điểm
            if (AttendancePanel != null) AttendancePanel.Visibility = Visibility.Collapsed; // ẩn panel điểm danh
            if (LessonPanel != null) LessonPanel.Visibility = Visibility.Collapsed; // ẩn panel bài giảng
            if (EvidencePanel != null) EvidencePanel.Visibility = Visibility.Collapsed; // ẩn panel bằng chứng
            WelcomePanel.Visibility = Visibility.Visible; // hiển thị panel chào mừng
        }

        private void MenuAssignmentList_Click(object sender, RoutedEventArgs e)
        {
            WelcomePanel.Visibility = Visibility.Collapsed;
            UploadExamPanel.Visibility = Visibility.Collapsed;
            AntiCheatPanel.Visibility = Visibility.Collapsed;
            if (MonitorStudentsPanel != null) MonitorStudentsPanel.Visibility = Visibility.Collapsed;
            if (StudentGradesPanel != null) StudentGradesPanel.Visibility = Visibility.Collapsed;
            if (AttendancePanel != null) AttendancePanel.Visibility = Visibility.Collapsed;
            if (LessonPanel != null) LessonPanel.Visibility = Visibility.Collapsed;
            if (EvidencePanel != null) EvidencePanel.Visibility = Visibility.Collapsed;
            ExamListPanel.Visibility = Visibility.Visible;

            if (AssignmentsDataGrid.ItemsSource == null)
            {
                var assignments = new ObservableCollection<AssignmentModel>();
                AssignmentsDataGrid.ItemsSource = assignments;
            }
        }

        private async void MenuMonitorStudents_Click(object sender, RoutedEventArgs e)
        {
            WelcomePanel.Visibility = Visibility.Collapsed;
            UploadExamPanel.Visibility = Visibility.Collapsed;
            ExamListPanel.Visibility = Visibility.Collapsed;
            AntiCheatPanel.Visibility = Visibility.Collapsed;
            if (StudentGradesPanel != null) StudentGradesPanel.Visibility = Visibility.Collapsed;
            if (AttendancePanel != null) AttendancePanel.Visibility = Visibility.Collapsed;
            if (LessonPanel != null) LessonPanel.Visibility = Visibility.Collapsed;
            if (EvidencePanel != null) EvidencePanel.Visibility = Visibility.Collapsed;
            MonitorStudentsPanel.Visibility = Visibility.Visible;
            
            var env = await CoreWebView2Environment.CreateAsync();
            await CameraMonitorWebView.EnsureCoreWebView2Async(env);

            CameraMonitorWebView.NavigationCompleted -= MonitorWebView_NavigationCompleted;
            CameraMonitorWebView.NavigationCompleted += MonitorWebView_NavigationCompleted;

            // Load trang giám sát từ Firebase Hosting
            CameraMonitorWebView.Source = new Uri($"{AppConfig.ApiBaseUrl}/instructor_monitor.html?v=" + System.DateTime.Now.Ticks + "&mode=screen");
        }

        private async void MenuAntiCheat_Click(object sender, RoutedEventArgs e)
        {
            WelcomePanel.Visibility = Visibility.Collapsed;
            UploadExamPanel.Visibility = Visibility.Collapsed;
            ExamListPanel.Visibility = Visibility.Collapsed;
            if (MonitorStudentsPanel != null) MonitorStudentsPanel.Visibility = Visibility.Collapsed;
            if (StudentGradesPanel != null) StudentGradesPanel.Visibility = Visibility.Collapsed;
            if (AttendancePanel != null) AttendancePanel.Visibility = Visibility.Collapsed;
            if (LessonPanel != null) LessonPanel.Visibility = Visibility.Collapsed;
            if (EvidencePanel != null) EvidencePanel.Visibility = Visibility.Collapsed;
            AntiCheatPanel.Visibility = Visibility.Visible;
            
            var env = await CoreWebView2Environment.CreateAsync();
            await GlobalMonitorWebView.EnsureCoreWebView2Async(env);

            GlobalMonitorWebView.NavigationCompleted -= MonitorWebView_NavigationCompleted;
            GlobalMonitorWebView.NavigationCompleted += MonitorWebView_NavigationCompleted;

            // Load trang giám sát màn hình từ Firebase Hosting
            GlobalMonitorWebView.Source = new Uri($"{AppConfig.ApiBaseUrl}/instructor_monitor.html?v=" + System.DateTime.Now.Ticks + "&mode=screen");
        }

        private async void MenuStudentGrades_Click(object sender, RoutedEventArgs e)
        {
            WelcomePanel.Visibility = Visibility.Collapsed;
            UploadExamPanel.Visibility = Visibility.Collapsed;
            ExamListPanel.Visibility = Visibility.Collapsed;
            AntiCheatPanel.Visibility = Visibility.Collapsed;
            if (MonitorStudentsPanel != null) MonitorStudentsPanel.Visibility = Visibility.Collapsed;
            if (AttendancePanel != null) AttendancePanel.Visibility = Visibility.Collapsed;
            if (LessonPanel != null) LessonPanel.Visibility = Visibility.Collapsed;
            if (EvidencePanel != null) EvidencePanel.Visibility = Visibility.Collapsed;
            StudentGradesPanel.Visibility = Visibility.Visible;

            await LoadStudentGradesAsync();
        }

        private async void MenuEvidence_Click(object sender, RoutedEventArgs e)
        {
            WelcomePanel.Visibility = Visibility.Collapsed;
            UploadExamPanel.Visibility = Visibility.Collapsed;
            ExamListPanel.Visibility = Visibility.Collapsed;
            AntiCheatPanel.Visibility = Visibility.Collapsed;
            if (MonitorStudentsPanel != null) MonitorStudentsPanel.Visibility = Visibility.Collapsed;
            if (StudentGradesPanel != null) StudentGradesPanel.Visibility = Visibility.Collapsed;
            if (AttendancePanel != null) AttendancePanel.Visibility = Visibility.Collapsed;
            if (LessonPanel != null) LessonPanel.Visibility = Visibility.Collapsed;
            EvidencePanel.Visibility = Visibility.Visible;

            await LoadEvidenceAsync();
        }

        private async void RefreshEvidence_Click(object sender, RoutedEventArgs e)
        {
            await LoadEvidenceAsync();
        }

        private async Task LoadEvidenceAsync()
        {
            try
            {
                var violationsList = await FirebaseService.GetViolationsAsync();
                var items = new List<ViolationModel>();
                foreach (var v in violationsList)
                {
                    items.Add(new ViolationModel
                    {
                        id = v.ContainsKey("id") ? v["id"]?.ToString() ?? "" : "",
                        full_name = v.ContainsKey("full_name") ? v["full_name"]?.ToString() : "",
                        username = v.ContainsKey("username") ? v["username"]?.ToString() : "",
                        violation_type = v.ContainsKey("violation_type") ? v["violation_type"]?.ToString() : "",
                        screenshot_base64 = v.ContainsKey("screenshot_base64") ? v["screenshot_base64"]?.ToString() : "",
                        captured_at = v.ContainsKey("captured_at") ? v["captured_at"]?.ToString() : ""
                    });
                }
                EvidenceListView.ItemsSource = items;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Lỗi tải minh chứng: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadStudentGradesAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    string json = await client.GetStringAsync($"{FirebaseService.DatabaseUrl}/exam_results.json?auth={FirebaseService.IdToken}");
                    if (!string.IsNullOrEmpty(json) && json != "null")
                    {
                        var resultsDict = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, StudentGradeModel>>>(json);
                        var list = new List<StudentGradeModel>();
                        
                        if (resultsDict != null)
                        {
                            foreach (var userKvp in resultsDict)
                            {
                                foreach (var examKvp in userKvp.Value)
                                {
                                    var grade = examKvp.Value;
                                    grade.Username = userKvp.Key; // Use username as key for now
                                    // Try fetch fullname if available, this could be extended later
                                    list.Add(grade);
                                }
                            }
                        }
                        
                        // Sort by latest submitted
                        list.Sort((a, b) => 
                        {
                            if (DateTime.TryParse(a.SubmittedAt, out var da) && DateTime.TryParse(b.SubmittedAt, out var db))
                                return db.CompareTo(da);
                            return string.Compare(b.SubmittedAt, a.SubmittedAt);
                        });

                        GradesDataGrid.ItemsSource = list;
                    }
                    else
                    {
                        GradesDataGrid.ItemsSource = new List<StudentGradeModel>();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải bảng điểm: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            var items = GradesDataGrid.ItemsSource as List<StudentGradeModel>;
            if (items == null || items.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV (Comma delimited)|*.csv",
                FileName = "BangDiemSinhVien_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new System.Text.StringBuilder();
                    // Write BOM for UTF-8 Excel support
                    sb.Append('\uFEFF');
                    
                    sb.AppendLine("MSSV/Username,Bài thi,Điểm,Nhận xét (AI Feedback),Ngày nộp");

                    foreach (var item in items)
                    {
                        string username = EscapeCsvField(item.Username);
                        string title = EscapeCsvField(item.AssignmentTitle);
                        string score = item.Score.ToString();
                        string feedback = EscapeCsvField(item.Feedback);
                        string date = EscapeCsvField(item.SubmittedAt);

                        sb.AppendLine($"{username},{title},{score},{feedback},{date}");
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                    MessageBox.Show("Xuất file thành công!\n\nBạn có thể mở file này bằng Excel.", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Open file
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = dialog.FileName,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi lưu file: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string EscapeCsvField(string? field)
        {
            if (string.IsNullOrEmpty(field)) return "";
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
        }

        private async void MenuAttendance_Click(object sender, RoutedEventArgs e)
        {
            WelcomePanel.Visibility = Visibility.Collapsed;
            UploadExamPanel.Visibility = Visibility.Collapsed;
            ExamListPanel.Visibility = Visibility.Collapsed;
            AntiCheatPanel.Visibility = Visibility.Collapsed;
            if (MonitorStudentsPanel != null) MonitorStudentsPanel.Visibility = Visibility.Collapsed;
            if (StudentGradesPanel != null) StudentGradesPanel.Visibility = Visibility.Collapsed;
            if (LessonPanel != null) LessonPanel.Visibility = Visibility.Collapsed;
            if (EvidencePanel != null) EvidencePanel.Visibility = Visibility.Collapsed;
            AttendancePanel.Visibility = Visibility.Visible;

            await LoadAttendanceAsync();
        }

        private async void RefreshAttendance_Click(object sender, RoutedEventArgs e)
        {
            await LoadAttendanceAsync();
        }

        private async Task LoadAttendanceAsync()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    string json = await client.GetStringAsync($"{FirebaseService.DatabaseUrl}/attendance.json?auth={FirebaseService.IdToken}");
                    if (!string.IsNullOrEmpty(json) && json != "null")
                    {
                        var allData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, System.Text.Json.JsonElement>>>(json);
                        var list = new List<AttendanceRecordModel>();

                        if (allData != null)
                        {
                            foreach (var userKvp in allData)
                            {
                                foreach (var recordKvp in userKvp.Value)
                                {
                                    try
                                    {
                                        var el = recordKvp.Value;
                                        list.Add(new AttendanceRecordModel
                                        {
                                            Username = el.TryGetProperty("username", out var u) ? u.GetString() : userKvp.Key,
                                            Fullname = el.TryGetProperty("fullname", out var f) ? f.GetString() : "",
                                            Similarity = el.TryGetProperty("similarity", out var s) ? s.GetDouble() : 0,
                                            IsMatch = el.TryGetProperty("is_match", out var m) && m.GetBoolean(),
                                            Verdict = el.TryGetProperty("verdict", out var v) ? v.GetString() : "",
                                            Timestamp = el.TryGetProperty("timestamp", out var t) ? t.GetString() : ""
                                        });
                                    }
                                    catch { }
                                }
                            }
                        }

                        // Sort by timestamp descending
                        list.Sort((a, b) =>
                        {
                            if (DateTime.TryParse(a.Timestamp, out var da) && DateTime.TryParse(b.Timestamp, out var db))
                                return db.CompareTo(da);
                            return string.Compare(b.Timestamp, a.Timestamp);
                        });

                        AttendanceDataGrid.ItemsSource = list;
                    }
                    else
                    {
                        AttendanceDataGrid.ItemsSource = new List<AttendanceRecordModel>();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải dữ liệu điểm danh: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportAttendanceToExcel_Click(object sender, RoutedEventArgs e)
        {
            var items = AttendanceDataGrid.ItemsSource as List<AttendanceRecordModel>;
            if (items == null || items.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV (Comma delimited)|*.csv",
                FileName = "DiemDanh_KhuonMat_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var sb = new System.Text.StringBuilder();
                    sb.Append('\uFEFF');
                    sb.AppendLine("MSSV/Username,Họ tên,Độ khớp (%),Kết quả,Thời gian");

                    foreach (var item in items)
                    {
                        sb.AppendLine($"{EscapeCsvField(item.Username)},{EscapeCsvField(item.Fullname)},{item.SimilarityDisplay},{EscapeCsvField(item.Verdict)},{item.TimestampDisplay}");
                    }

                    File.WriteAllText(dialog.FileName, sb.ToString(), System.Text.Encoding.UTF8);
                    MessageBox.Show("Xuất file điểm danh thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = dialog.FileName,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi lưu file: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void MonitorWebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            var webView = sender as Microsoft.Web.WebView2.Wpf.WebView2;
            if (webView?.CoreWebView2 != null)
            {
                // Inject script to continuously hide any camera containers since elements are added dynamically
                string script = @"
                    setInterval(() => {
                        document.querySelectorAll('.video-container').forEach(el => {
                            if (el.innerHTML.includes('Camera') || el.querySelector('video')) {
                                el.style.display = 'none';
                            }
                        });
                    }, 500);
                ";
                await webView.CoreWebView2.ExecuteScriptAsync(script);
            }
        }

        private void RadioOnline_Checked(object sender, RoutedEventArgs e)
        {
            if (SelectedFileNameTextBlock != null && SelectedFileNameTextBlock.Text != "Kéo thả file đề thi vào đây hoặc nhấn để duyệt file")
            {
                if (ExtractedQuestionsPanel != null)
                {
                    ExtractedQuestionsPanel.Visibility = Visibility.Visible;
                }
            }
        }

        private void RadioOnline_Unchecked(object sender, RoutedEventArgs e)
        {
            if (ExtractedQuestionsPanel != null)
            {
                ExtractedQuestionsPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void ExamTypeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (SelectedFileNameTextBlock != null && SelectedFileNameTextBlock.Text != "Kéo thả file đề thi vào đây hoặc nhấn để duyệt file" && ExtractedQuestionsPanel != null && ExtractedQuestionsPanel.Visibility == Visibility.Visible)
            {
                string examType = ((ComboBoxItem)ExamTypeComboBox.SelectedItem)?.Content?.ToString() ?? "Trắc nghiệm";
                var extracted = ExtractQuestionsFromFile(SelectedFileNameTextBlock.Text, examType);

                string resultText = $"• {extracted.Count} câu {examType.ToLower()}\n";
                if (extracted.Count == 0) resultText = "• Không thể trích xuất nội dung hợp lệ hoặc không có câu hỏi nào (Vui lòng kiểm tra file)";

                ExtractedQuestionsSummary.Text = resultText.TrimEnd('\n');
            }
        }

        private void BrowseFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Tệp tin (.pdf, .docx)|*.pdf;*.docx|Tất cả các tệp (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                SelectedFileNameTextBlock.Text = openFileDialog.FileName;
                SelectedFileNameTextBlock.Foreground = System.Windows.Media.Brushes.Black;
                
                if (RadioOnline != null && RadioOnline.IsChecked == true)
                {
                    ExtractedQuestionsPanel.Visibility = Visibility.Visible;
                    
                    string examType = ((ComboBoxItem)ExamTypeComboBox.SelectedItem)?.Content?.ToString() ?? "Trắc nghiệm";
                    var extracted = ExtractQuestionsFromFile(openFileDialog.FileName, examType);

                    string resultText = $"• {extracted.Count} câu {examType.ToLower()}\n";
                    if (extracted.Count == 0) resultText = "• Không thể trích xuất nội dung hợp lệ hoặc không có câu hỏi nào (Vui lòng kiểm tra file)";

                    ExtractedQuestionsSummary.Text = resultText.TrimEnd('\n');
                }
            }
        }

        private void SaveExamButton_Click(object sender, RoutedEventArgs e)
        {
            // Lấy tên file để làm tiêu đề bài tập
            string fileName = SelectedFileNameTextBlock.Text;
            if (fileName == "Kéo thả file đề thi vào đây hoặc nhấn để duyệt file")
            {
                fileName = string.IsNullOrWhiteSpace(ExamTitleTextBox.Text) ? "New Exam" : ExamTitleTextBox.Text;
            }
            else
            {
                fileName = string.IsNullOrWhiteSpace(ExamTitleTextBox.Text) ? System.IO.Path.GetFileNameWithoutExtension(fileName) : ExamTitleTextBox.Text;
            }

            // Lấy danh sách hiện tại hoặc tạo mới
            var assignments = AssignmentsDataGrid.ItemsSource as ObservableCollection<AssignmentModel>;
            if (assignments == null)
            {
                assignments = new ObservableCollection<AssignmentModel>();
            }

            string openDateStr = StartDatePicker.SelectedDate.HasValue 
                ? StartDatePicker.SelectedDate.Value.ToString("MMM d, yyyy") + $", {StartTimeTextBox.Text}" 
                : System.DateTime.Now.ToString("MMM d, yyyy, h:mm tt");
                
            string dueDateStr = EndDatePicker.SelectedDate.HasValue 
                ? EndDatePicker.SelectedDate.Value.ToString("MMM d, yyyy") + $", {EndTimeTextBox.Text}" 
                : System.DateTime.Now.AddDays(7).ToString("MMM d, yyyy, h:mm tt");

            string examType = ((ComboBoxItem)ExamTypeComboBox.SelectedItem)?.Content?.ToString() ?? "Trắc nghiệm";

            var extractedQuestions = ExtractQuestionsFromFile(SelectedFileNameTextBlock.Text, examType);
            
            // Nếu không trích xuất được gì (có thể do lỗi, hoặc user đang edit mà file gốc đã bị xóa/di chuyển)
            if (extractedQuestions.Count == 0)
            {
                if (_currentEditingAssignment != null && _currentEditingAssignment.Questions != null && _currentEditingAssignment.Questions.Count > 0 && _currentEditingAssignment.ExamType == examType)
                {
                    // Giữ lại câu hỏi cũ nếu đang edit và type không đổi
                    extractedQuestions = _currentEditingAssignment.Questions;
                }
                else
                {
                    // Thêm câu hỏi dự phòng
                    extractedQuestions.Add(new QuestionModel { 
                        Id = 1, 
                        Text = "Không thể tự động trích xuất nội dung từ file. Vui lòng xem file đính kèm để làm bài.",
                        Options = examType == "Trắc nghiệm" ? new List<string> { "A. Đáp án A", "B. Đáp án B", "C. Đáp án C", "D. Đáp án D" } : null
                    });
                }
            }

            if (_currentEditingAssignment == null)
            {
                // Thêm bài thi mới vào đầu danh sách
                var newAssignment = new AssignmentModel
                {
                    Title = fileName,
                    Description = ExamDescriptionTextBox.Text,
                    Status = "Just Uploaded",
                    OpenDate = openDateStr,
                    DueDate = dueDateStr,
                    LastModifiedBy = "CURRENT INSTRUCTOR",
                    ModifiedDate = System.DateTime.Now.ToString("MMM d, yyyy, h:mm tt"),
                    StartDate = StartDatePicker.SelectedDate,
                    StartTime = StartTimeTextBox.Text,
                    EndDate = EndDatePicker.SelectedDate,
                    EndTime = EndTimeTextBox.Text,
                    IsOnlineExam = RadioOnline.IsChecked == true,
                    ExamType = examType,
                    FileName = SelectedFileNameTextBlock.Text,
                    ExtractedQuestionsText = ExtractedQuestionsPanel.Visibility == Visibility.Visible ? ExtractedQuestionsSummary.Text : null,
                    Questions = extractedQuestions,
                    Notes = new ObservableCollection<TeacherNote>(Notes)
                };
                assignments.Insert(0, newAssignment);
            }
            else
            {
                // Cập nhật bài thi hiện tại
                _currentEditingAssignment.Title = fileName;
                _currentEditingAssignment.Description = ExamDescriptionTextBox.Text;
                _currentEditingAssignment.OpenDate = openDateStr;
                _currentEditingAssignment.DueDate = dueDateStr;
                _currentEditingAssignment.ModifiedDate = System.DateTime.Now.ToString("MMM d, yyyy, h:mm tt");
                _currentEditingAssignment.StartDate = StartDatePicker.SelectedDate;
                _currentEditingAssignment.StartTime = StartTimeTextBox.Text;
                _currentEditingAssignment.EndDate = EndDatePicker.SelectedDate;
                _currentEditingAssignment.EndTime = EndTimeTextBox.Text;
                _currentEditingAssignment.IsOnlineExam = RadioOnline.IsChecked == true;
                _currentEditingAssignment.ExamType = examType;
                _currentEditingAssignment.FileName = SelectedFileNameTextBlock.Text;
                _currentEditingAssignment.ExtractedQuestionsText = ExtractedQuestionsPanel.Visibility == Visibility.Visible ? ExtractedQuestionsSummary.Text : null;
                _currentEditingAssignment.Questions = extractedQuestions;
                
                _currentEditingAssignment.Notes?.Clear();
                if (_currentEditingAssignment.Notes == null) _currentEditingAssignment.Notes = new ObservableCollection<TeacherNote>();
                foreach (var note in Notes)
                {
                    _currentEditingAssignment.Notes.Add(note);
                }
                
                // Refresh datagrid
                AssignmentsDataGrid.Items.Refresh();
            }

            AssignmentsDataGrid.ItemsSource = assignments;
            SaveAssignmentsList();

            // Chuyển giao diện
            UploadExamPanel.Visibility = Visibility.Collapsed;
            ExamListPanel.Visibility = Visibility.Visible;
        }

        private List<QuestionModel> ExtractQuestionsFromFile(string filePath, string examType)
        {
            var questions = new List<QuestionModel>();
            try
            {
                if (string.IsNullOrEmpty(filePath) || !System.IO.File.Exists(filePath))
                    return questions;
                
                string text = "";
                
                // Copy to temp file to avoid "File in use" errors if Word has it open
                string tempPath = System.IO.Path.GetTempFileName();
                System.IO.File.Copy(filePath, tempPath, true);

                try
                {
                    if (filePath.EndsWith(".docx", System.StringComparison.OrdinalIgnoreCase))
                    {
                        using (System.IO.Compression.ZipArchive archive = System.IO.Compression.ZipFile.OpenRead(tempPath))
                        {
                            var entry = archive.GetEntry("word/document.xml");
                            if (entry != null)
                            {
                                using (var stream = entry.Open())
                                using (var reader = new System.IO.StreamReader(stream))
                                {
                                    string xml = reader.ReadToEnd();
                                    var pMatches = System.Text.RegularExpressions.Regex.Matches(xml, @"<w:p[^>]*>(.*?)</w:p>");
                                    var sb = new System.Text.StringBuilder();
                                    foreach (System.Text.RegularExpressions.Match match in pMatches)
                                    {
                                        string pText = System.Text.RegularExpressions.Regex.Replace(match.Groups[1].Value, "<.*?>", "");
                                        if (!string.IsNullOrWhiteSpace(pText)) sb.AppendLine(pText.Trim());
                                    }
                                    text = sb.ToString();
                                }
                            }
                        }
                    }
                    else if (filePath.EndsWith(".pdf", System.StringComparison.OrdinalIgnoreCase))
                    {
                        using (var pdfReader = new iText.Kernel.Pdf.PdfReader(tempPath))
                        using (var pdfDocument = new iText.Kernel.Pdf.PdfDocument(pdfReader))
                        {
                            var sb = new System.Text.StringBuilder();
                            for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                            {
                                var strategy = new iText.Kernel.Pdf.Canvas.Parser.Listener.LocationTextExtractionStrategy();
                                string pageText = iText.Kernel.Pdf.Canvas.Parser.PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(i), strategy);
                                sb.AppendLine(pageText);
                            }
                            text = sb.ToString();
                        }
                    }
                    else // fallback txt
                    {
                        text = System.IO.File.ReadAllText(tempPath);
                    }
                }
                finally
                {
                    try { System.IO.File.Delete(tempPath); } catch { }
                }

                // Parse text based on examType
                var lines = text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                
                QuestionModel? currentQuestion = null;
                int idCounter = 1;

                // Regex patterns
                string questionRegex = @"^(?:(?:(?:Câu|Question|Bài|Câu hỏi)\s*\d+[\.\:\-\/]\s*|\d+[\.\:\-\/]\s+)(?:\(\d+(?:\.\d+)?\s*Points?\)\s*)?|(?:\b\d+\s*)?\(\d+(?:\.\d+)?\s*Points?\)\s*)";
                string optionRegex = @"^[A-F][\.\)\/\-]\s*";
                bool hasOptions = System.Text.RegularExpressions.Regex.IsMatch(text, optionRegex, System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                if (examType == "Trắc nghiệm")
                {
                    // Luôn duyệt qua các dòng để bắt "Câu X:"
                    foreach (var line in lines)
                    {
                        string tLine = line.Trim();
                        if (System.Text.RegularExpressions.Regex.IsMatch(tLine, questionRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        {
                            if (currentQuestion != null) questions.Add(currentQuestion);
                            string cleanText = System.Text.RegularExpressions.Regex.Replace(tLine, questionRegex, "").Trim();
                            currentQuestion = new QuestionModel { Id = idCounter++, Text = cleanText, Options = new List<string>() };
                        }
                        else if (System.Text.RegularExpressions.Regex.IsMatch(tLine, optionRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                        {
                            if (currentQuestion == null)
                                currentQuestion = new QuestionModel { Id = idCounter++, Text = "Nội dung câu hỏi:", Options = new List<string>() };
                            if (currentQuestion.Options == null) currentQuestion.Options = new List<string>();
                            currentQuestion.Options.Add(tLine);
                        }
                        else
                        {
                            if (currentQuestion != null)
                            {
                                if (currentQuestion.Options == null || currentQuestion.Options.Count == 0)
                                    currentQuestion.Text += (string.IsNullOrEmpty(currentQuestion.Text) ? "" : "\n") + tLine;
                            }
                            else
                                currentQuestion = new QuestionModel { Id = idCounter++, Text = tLine, Options = new List<string>() };
                        }
                    }
                    if (currentQuestion != null) questions.Add(currentQuestion);

                    // Hậu xử lý (Retroactive Heuristic): 
                    // Những câu Trắc nghiệm không tìm thấy dấu hiệu A, B, C, D (Options.Count == 0)
                    // sẽ tự động cắt 4 dòng cuối của Text làm đáp án
                    foreach (var q in questions)
                    {
                        if (q.Options == null || q.Options.Count == 0)
                        {
                            if (!string.IsNullOrEmpty(q.Text))
                            {
                                var qLines = q.Text.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                                if (qLines.Length >= 2)
                                {
                                    int optionCount = System.Math.Min(4, qLines.Length - 1);
                                    int questionLinesCount = qLines.Length - optionCount;
                                    
                                    q.Text = string.Join("\n", qLines.Take(questionLinesCount).Select(l => l.Trim()));
                                    q.Options = qLines.Skip(questionLinesCount).Select((opt, idx) => $"{(char)('A' + idx)}. {opt.Trim()}").ToList();
                                }
                                else if (qLines.Length == 1)
                                {
                                    // File quá ngắn hoặc lỗi định dạng, tạo option mock để không bị lỗi UI hiển thị TextBox
                                    q.Options = new List<string> { "A. Đáp án 1", "B. Đáp án 2", "C. Đáp án 3", "D. Đáp án 4" };
                                }
                            }
                            else
                            {
                                q.Options = new List<string> { "A. Đáp án 1", "B. Đáp án 2", "C. Đáp án 3", "D. Đáp án 4" };
                            }
                        }
                    }
                }
                else // Tự luận hoặc Câu hỏi ngắn
                {
                    if (examType == "Câu hỏi ngắn")
                    {
                        foreach (var line in lines)
                        {
                            string tLine = line.Trim();
                            if (string.IsNullOrWhiteSpace(tLine)) continue;
                            
                            // Tự động bỏ số thứ tự cũ (nếu có) và chèn lại theo Id
                            string cleanText = System.Text.RegularExpressions.Regex.Replace(tLine, questionRegex, "").Trim();
                            if (!string.IsNullOrEmpty(cleanText))
                            {
                                questions.Add(new QuestionModel { Id = idCounter++, Text = cleanText });
                            }
                        }
                    }
                    else
                    {
                        foreach (var line in lines)
                        {
                            string tLine = line.Trim();
                            if (System.Text.RegularExpressions.Regex.IsMatch(tLine, questionRegex, System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                            {
                                if (currentQuestion != null) questions.Add(currentQuestion);
                                string cleanText = System.Text.RegularExpressions.Regex.Replace(tLine, questionRegex, "").Trim();
                                currentQuestion = new QuestionModel { Id = idCounter++, Text = cleanText };
                            }
                            else
                            {
                                if (currentQuestion != null)
                                    currentQuestion.Text += (string.IsNullOrEmpty(currentQuestion.Text) ? "" : "\n") + tLine;
                                else
                                    currentQuestion = new QuestionModel { Id = idCounter++, Text = tLine };
                            }
                        }
                        if (currentQuestion != null) questions.Add(currentQuestion);
                    }
                }
            }
            catch { }

            return questions;
        }

        private void AssignmentTitle_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var textBlock = sender as TextBlock;
            if (textBlock != null)
            {
                var assignment = textBlock.DataContext as AssignmentModel;
                if (assignment != null)
                {
                    _currentEditingAssignment = assignment;
                    UploadExamHeader.Text = "Chỉnh sửa Đề Thi";
                    if (CancelExamSessionButton != null) CancelExamSessionButton.Visibility = Visibility.Visible;
                    
                    ExamTitleTextBox.Text = assignment.Title;
                    ExamDescriptionTextBox.Text = assignment.Description;
                    
                    StartDatePicker.SelectedDate = assignment.StartDate;
                    StartTimeTextBox.Text = assignment.StartTime ?? "08:00";
                    EndDatePicker.SelectedDate = assignment.EndDate;
                    EndTimeTextBox.Text = assignment.EndTime ?? "10:00";
                    
                    RadioOnline.IsChecked = assignment.IsOnlineExam;
                    RadioOffline.IsChecked = !assignment.IsOnlineExam;

                    if (!string.IsNullOrEmpty(assignment.ExamType))
                    {
                        foreach (ComboBoxItem item in ExamTypeComboBox.Items)
                        {
                            if (item.Content.ToString() == assignment.ExamType)
                            {
                                item.IsSelected = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        ExamTypeComboBox.SelectedIndex = 0;
                    }
                    
                    SelectedFileNameTextBlock.Text = string.IsNullOrEmpty(assignment.FileName) ? "Kéo thả file đề thi vào đây hoặc nhấn để duyệt file" : assignment.FileName;
                    if (SelectedFileNameTextBlock.Text != "Kéo thả file đề thi vào đây hoặc nhấn để duyệt file")
                    {
                        SelectedFileNameTextBlock.Foreground = System.Windows.Media.Brushes.Black;
                    }
                    
                    if (!string.IsNullOrEmpty(assignment.ExtractedQuestionsText))
                    {
                        ExtractedQuestionsPanel.Visibility = Visibility.Visible;
                        ExtractedQuestionsSummary.Text = assignment.ExtractedQuestionsText;
                    }
                    else
                    {
                        ExtractedQuestionsPanel.Visibility = Visibility.Collapsed;
                    }
                    
                    Notes.Clear();
                    if (assignment.Notes != null)
                    {
                        foreach (var note in assignment.Notes)
                        {
                            Notes.Add(note);
                        }
                    }

                    // Chuyển lại màn hình UploadExamPanel để giảng viên chỉnh sửa
                    WelcomePanel.Visibility = Visibility.Collapsed;
                    ExamListPanel.Visibility = Visibility.Collapsed;
                    UploadExamPanel.Visibility = Visibility.Visible;
                }
            }
        }

        private void MenuLesson_Click(object sender, RoutedEventArgs e)
        {
            WelcomePanel.Visibility = Visibility.Collapsed;
            UploadExamPanel.Visibility = Visibility.Collapsed;
            ExamListPanel.Visibility = Visibility.Collapsed;
            MonitorStudentsPanel.Visibility = Visibility.Collapsed;
            StudentGradesPanel.Visibility = Visibility.Collapsed;
            AttendancePanel.Visibility = Visibility.Collapsed;
            if (EvidencePanel != null) EvidencePanel.Visibility = Visibility.Collapsed;
            LessonPanel.Visibility = Visibility.Visible;
            RefreshLessons();
        }

        private void TabSubject_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.RadioButton rb && rb.IsChecked == true)
            {
                if (rb.Name == "TabIS") _currentSubject = "IS";
                else if (rb.Name == "TabCS401") _currentSubject = "CS 401";
                else if (rb.Name == "TabDIS") _currentSubject = "DIS";
                
                RefreshLessons();
            }
        }

        private void RefreshLessons()
        {
            if (Lessons == null) return;
            // Filter by subject visually or load from somewhere.
            // For now, let's just clear and show fake data or load real if implemented.
            var filtered = new ObservableCollection<LessonModel>();
            foreach (var l in Lessons)
            {
                if (l.Subject == _currentSubject) filtered.Add(l);
            }
            if (LessonDataGrid != null) LessonDataGrid.ItemsSource = filtered;
        }

        private async void UploadLesson_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Tài liệu (*.pdf;*.docx;*.pptx)|*.pdf;*.docx;*.pptx|Tất cả các file (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                string fileName = System.IO.Path.GetFileName(openFileDialog.FileName);
                string fileId = System.Guid.NewGuid().ToString();

                var newLesson = new LessonModel
                {
                    Title = fileName,
                    Subject = _currentSubject,
                    UploadDate = System.DateTime.Now.ToString("dd/MM/yyyy"),
                    UploadedBy = "Giảng viên",
                    Size = Math.Max(1, new System.IO.FileInfo(openFileDialog.FileName).Length / 1024) + " KB",
                    Access = "Public",
                    FilePath = fileId // Save Firebase fileId instead of local path
                };

                Lessons.Add(newLesson);
                SaveLessons();
                RefreshLessons();
                
                // Upload file content to Firebase asynchronously
                try
                {
                    byte[] fileData = System.IO.File.ReadAllBytes(openFileDialog.FileName);
                    await FirebaseService.UploadLessonFileAsync(fileId, fileData);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi upload nội dung file lên Firebase: " + ex.Message, "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                MessageBox.Show("Upload môn học thành công!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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

        private void RowActions_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.DataContext is LessonModel lesson)
            {
                if (btn.ContextMenu != null)
                {
                    btn.ContextMenu.DataContext = lesson;
                    btn.ContextMenu.PlacementTarget = btn;
                    btn.ContextMenu.IsOpen = true;
                }
            }
        }

        private void TopActions_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void RemoveSingleLesson_Click(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.MenuItem menuItem && menuItem.DataContext is LessonModel lesson)
            {
                if (MessageBox.Show($"Bạn có chắc muốn xóa '{lesson.Title}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    Lessons.Remove(lesson);
                    SaveLessons();
                    RefreshLessons();
                }
            }
        }

        private void CancelUploadExam_Click(object sender, RoutedEventArgs e)
        {
            UploadExamPanel.Visibility = Visibility.Collapsed;
            WelcomePanel.Visibility = Visibility.Visible;
            _currentEditingAssignment = null;
        }

        private void CancelExamSession_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEditingAssignment == null) return;
            
            if (MessageBox.Show($"Bạn có chắc chắn muốn HỦY BUỔI THI '{_currentEditingAssignment.Title}' không?\nHành động này không thể hoàn tác!", "CẢNH BÁO", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                // Find and remove the exam
                var assignments = AssignmentsDataGrid.ItemsSource as ObservableCollection<AssignmentModel>;
                if (assignments != null)
                {
                    AssignmentModel? toRemove = null;
                    foreach(var a in assignments)
                    {
                        if (a.Title == _currentEditingAssignment.Title) { toRemove = a; break; }
                    }
                    if (toRemove != null) assignments.Remove(toRemove);
                    
                    // Save to local file
                    try
                    {
                        var json = JsonSerializer.Serialize(assignments);
                        System.IO.File.WriteAllText("assignments.json", json);
                        _ = FirebaseService.UploadAssignmentsAsync(json);
                    }
                    catch { }
                }
                
                MessageBox.Show("Đã hủy buổi thi thành công.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                UploadExamPanel.Visibility = Visibility.Collapsed;
                ExamListPanel.Visibility = Visibility.Visible;
                _currentEditingAssignment = null;
            }
        }
    }
}
