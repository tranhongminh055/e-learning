using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using StudentMonitor.Views;

namespace StudentMonitor;

public class MainWindow : Window, IComponentConnector, IStyleConnector
{
	private AssignmentModel? _currentEditingAssignment = null;

	private DispatcherTimer? _screenRefreshTimer;

	private string? _currentLiveUsername;

	internal Border WelcomePanel;

	internal Border UploadExamPanel;

	internal TextBlock UploadExamHeader;

	internal TextBox ExamTitleTextBox;

	internal TextBox ExamDescriptionTextBox;

	internal DatePicker StartDatePicker;

	internal TextBox StartTimeTextBox;

	internal DatePicker EndDatePicker;

	internal TextBox EndTimeTextBox;

	internal RadioButton RadioOffline;

	internal RadioButton RadioOnline;

	internal TextBlock SelectedFileNameTextBlock;

	internal Border ExtractedQuestionsPanel;

	internal TextBlock ExtractedQuestionsSummary;

	internal DataGrid NotesDataGrid;

	internal Border ExamListPanel;

	internal DataGrid AssignmentsDataGrid;

	internal Border MonitorStudentsPanel;

	internal WebView2 GlobalMonitorWebView;

	internal DataGrid ViolationsDataGrid;

	private bool _contentLoaded;

	public ObservableCollection<TeacherNote> Notes { get; set; }

	public MainWindow()
	{
		InitializeComponent();
		Notes = new ObservableCollection<TeacherNote>();
		NotesDataGrid.ItemsSource = Notes;
		LoadAssignments();
	}

	private void LoadAssignments()
	{
		ObservableCollection<AssignmentModel> observableCollection = new ObservableCollection<AssignmentModel>();
		try
		{
			if (File.Exists("assignments.json"))
			{
				string json = File.ReadAllText("assignments.json");
				List<AssignmentModel> list = JsonSerializer.Deserialize<List<AssignmentModel>>(json);
				if (list != null)
				{
					DateTime now = DateTime.Now;
					foreach (AssignmentModel item in list)
					{
						bool flag = true;
						DateTime result2;
						if (item.EndDate.HasValue)
						{
							DateTime dateTime = item.EndDate.Value;
							if (TimeSpan.TryParse(item.EndTime, out var result))
							{
								dateTime = dateTime.Date + result;
							}
							if (dateTime < now)
							{
								flag = false;
							}
						}
						else if (DateTime.TryParse(item.DueDate, out result2) && result2 < now)
						{
							flag = false;
						}
						if (flag)
						{
							observableCollection.Add(item);
						}
					}
				}
			}
		}
		catch
		{
		}
		AssignmentsDataGrid.ItemsSource = observableCollection;
	}

	private void SaveAssignmentsList()
	{
		try
		{
			if (AssignmentsDataGrid.ItemsSource is ObservableCollection<AssignmentModel> value)
			{
				string contents = JsonSerializer.Serialize(value);
				File.WriteAllText("assignments.json", contents);
			}
		}
		catch
		{
		}
	}

	private void ClearExamForm()
	{
		_currentEditingAssignment = null;
		UploadExamHeader.Text = "Upload Đề Thi";
		ExamTitleTextBox.Text = "";
		ExamDescriptionTextBox.Text = "";
		StartDatePicker.SelectedDate = null;
		StartTimeTextBox.Text = "08:00";
		EndDatePicker.SelectedDate = null;
		EndTimeTextBox.Text = "10:00";
		RadioOnline.IsChecked = true;
		SelectedFileNameTextBlock.Text = "Kéo thả file đề thi vào đây hoặc nhấn để duyệt file";
		SelectedFileNameTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 102));
		ExtractedQuestionsPanel.Visibility = Visibility.Collapsed;
		Notes.Clear();
	}

	private void LogoutButton_Click(object sender, RoutedEventArgs e)
	{
		MessageBoxResult messageBoxResult = MessageBox.Show("Bạn có chắc chắn muốn đăng xuất?", "Xác nhận đăng xuất", MessageBoxButton.YesNo, MessageBoxImage.Question);
		if (messageBoxResult == MessageBoxResult.Yes)
		{
			LoginWindow loginWindow = new LoginWindow();
			loginWindow.Show();
			Close();
		}
	}

	private void MenuUploadExam_Click(object sender, RoutedEventArgs e)
	{
		WelcomePanel.Visibility = Visibility.Collapsed;
		if (ExamListPanel != null)
		{
			ExamListPanel.Visibility = Visibility.Collapsed;
		}
		UploadExamPanel.Visibility = Visibility.Visible;
		ClearExamForm();
	}

	private void MenuAssignmentList_Click(object sender, RoutedEventArgs e)
	{
		WelcomePanel.Visibility = Visibility.Collapsed;
		UploadExamPanel.Visibility = Visibility.Collapsed;
		MonitorStudentsPanel.Visibility = Visibility.Collapsed;
		ExamListPanel.Visibility = Visibility.Visible;
		if (AssignmentsDataGrid.ItemsSource == null)
		{
			ObservableCollection<AssignmentModel> itemsSource = new ObservableCollection<AssignmentModel>();
			AssignmentsDataGrid.ItemsSource = itemsSource;
		}
	}

	private async void MenuMonitorStudents_Click(object sender, RoutedEventArgs e)
	{
		WelcomePanel.Visibility = Visibility.Collapsed;
		UploadExamPanel.Visibility = Visibility.Collapsed;
		ExamListPanel.Visibility = Visibility.Collapsed;
		MonitorStudentsPanel.Visibility = Visibility.Visible;
		CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions("--unsafely-treat-insecure-origin-as-secure=http://192.168.110.53:5000");
		CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, null, options);
		await GlobalMonitorWebView.EnsureCoreWebView2Async(env);
		GlobalMonitorWebView.Source = new Uri("http://192.168.110.53:5000/instructor_monitor");
		LoadViolations();
	}

	private async void LoadViolations()
	{
		try
		{
			using HttpClient client = new HttpClient();
			List<ViolationModel> violations = JsonSerializer.Deserialize<List<ViolationModel>>(await client.GetStringAsync("http://192.168.110.53:5000/api/monitor/violations"));
			ViolationsDataGrid.ItemsSource = violations;
		}
		catch
		{
		}
	}

	private void RefreshViolations_Click(object sender, RoutedEventArgs e)
	{
		LoadViolations();
	}

	private void ViewScreenshot_Click(object sender, MouseButtonEventArgs e)
	{
		if (sender is TextBlock { DataContext: ViolationModel dataContext } && !string.IsNullOrEmpty(dataContext.screenshot_path))
		{
			string text = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)) ?? "", "..", "Backend", dataContext.screenshot_path);
			if (File.Exists(text))
			{
				Process.Start(new ProcessStartInfo(text)
				{
					UseShellExecute = true
				});
			}
			else
			{
				MessageBox.Show("Ảnh minh chứng được lưu tại backend: " + dataContext.screenshot_path, "Thông tin", MessageBoxButton.OK, MessageBoxImage.Asterisk);
			}
		}
	}

	private void RadioOnline_Checked(object sender, RoutedEventArgs e)
	{
		if (SelectedFileNameTextBlock != null && SelectedFileNameTextBlock.Text != "Kéo thả file đề thi vào đây hoặc nhấn để duyệt file" && ExtractedQuestionsPanel != null)
		{
			ExtractedQuestionsPanel.Visibility = Visibility.Visible;
		}
	}

	private void RadioOnline_Unchecked(object sender, RoutedEventArgs e)
	{
		if (ExtractedQuestionsPanel != null)
		{
			ExtractedQuestionsPanel.Visibility = Visibility.Collapsed;
		}
	}

	private void BrowseFileButton_Click(object sender, RoutedEventArgs e)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.Filter = "Tệp tin (.pdf, .docx)|*.pdf;*.docx|Tất cả các tệp (*.*)|*.*";
		if (openFileDialog.ShowDialog() != true)
		{
			return;
		}
		SelectedFileNameTextBlock.Text = openFileDialog.FileName;
		SelectedFileNameTextBlock.Foreground = Brushes.Black;
		if (RadioOnline == null || RadioOnline.IsChecked != true)
		{
			return;
		}
		ExtractedQuestionsPanel.Visibility = Visibility.Visible;
		int num = 0;
		int num2 = 0;
		try
		{
			if (openFileDialog.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
			{
				using ZipArchive zipArchive = ZipFile.OpenRead(openFileDialog.FileName);
				ZipArchiveEntry entry = zipArchive.GetEntry("word/document.xml");
				if (entry != null)
				{
					using Stream stream = entry.Open();
					using StreamReader streamReader = new StreamReader(stream);
					string input = streamReader.ReadToEnd();
					string input2 = Regex.Replace(input, "<.*?>", " ");
					int count = Regex.Matches(input2, "\\bA\\.").Count;
					int count2 = Regex.Matches(input2, "\\bB\\.").Count;
					int count3 = Regex.Matches(input2, "\\bC\\.").Count;
					int count4 = Regex.Matches(input2, "\\bD\\.").Count;
					if (count > 0 && count2 > 0)
					{
						num = Math.Min(count, Math.Min(count2, Math.Min(count3, count4)));
					}
					if (num == 0)
					{
						int count5 = Regex.Matches(input2, "(?i)(Câu\\s+\\d+|Thời gian|Năm|Hội nghị|Tháng|Tại sao|Phân tích|Nêu)").Count;
						num2 = Math.Max(1, count5);
						if (num2 == 0)
						{
							num2 = 5;
						}
					}
					else
					{
						num2 = 0;
					}
				}
			}
		}
		catch
		{
			num = 0;
			num2 = 0;
		}
		string text = "";
		if (num > 0)
		{
			text += $"• {num} câu trắc nghiệm\n";
		}
		if (num2 > 0)
		{
			text += $"• {num2} câu tự luận/ngắn\n";
		}
		if (string.IsNullOrEmpty(text))
		{
			text = "• Đã phân tích nội dung (Chưa phân loại cụ thể)";
		}
		ExtractedQuestionsSummary.Text = text.TrimEnd('\n');
	}

	private void SaveExamButton_Click(object sender, RoutedEventArgs e)
	{
		string text = SelectedFileNameTextBlock.Text;
		text = ((!(text == "Kéo thả file đề thi vào đây hoặc nhấn để duyệt file")) ? (string.IsNullOrWhiteSpace(ExamTitleTextBox.Text) ? Path.GetFileNameWithoutExtension(text) : ExamTitleTextBox.Text) : (string.IsNullOrWhiteSpace(ExamTitleTextBox.Text) ? "New Exam" : ExamTitleTextBox.Text));
		ObservableCollection<AssignmentModel> observableCollection = AssignmentsDataGrid.ItemsSource as ObservableCollection<AssignmentModel>;
		if (observableCollection == null)
		{
			observableCollection = new ObservableCollection<AssignmentModel>();
		}
		string openDate = (StartDatePicker.SelectedDate.HasValue ? (StartDatePicker.SelectedDate.Value.ToString("MMM d, yyyy") + ", " + StartTimeTextBox.Text) : DateTime.Now.ToString("MMM d, yyyy, h:mm tt"));
		string dueDate = (EndDatePicker.SelectedDate.HasValue ? (EndDatePicker.SelectedDate.Value.ToString("MMM d, yyyy") + ", " + EndTimeTextBox.Text) : DateTime.Now.AddDays(7.0).ToString("MMM d, yyyy, h:mm tt"));
		if (_currentEditingAssignment == null)
		{
			AssignmentModel item = new AssignmentModel
			{
				Title = text,
				Description = ExamDescriptionTextBox.Text,
				Status = "Just Uploaded",
				OpenDate = openDate,
				DueDate = dueDate,
				LastModifiedBy = "CURRENT INSTRUCTOR",
				ModifiedDate = DateTime.Now.ToString("MMM d, yyyy, h:mm tt"),
				StartDate = StartDatePicker.SelectedDate,
				StartTime = StartTimeTextBox.Text,
				EndDate = EndDatePicker.SelectedDate,
				EndTime = EndTimeTextBox.Text,
				IsOnlineExam = (RadioOnline.IsChecked == true),
				FileName = SelectedFileNameTextBlock.Text,
				ExtractedQuestionsText = ((ExtractedQuestionsPanel.Visibility == Visibility.Visible) ? ExtractedQuestionsSummary.Text : null),
				Notes = new ObservableCollection<TeacherNote>(Notes)
			};
			observableCollection.Insert(0, item);
		}
		else
		{
			_currentEditingAssignment.Title = text;
			_currentEditingAssignment.Description = ExamDescriptionTextBox.Text;
			_currentEditingAssignment.OpenDate = openDate;
			_currentEditingAssignment.DueDate = dueDate;
			_currentEditingAssignment.ModifiedDate = DateTime.Now.ToString("MMM d, yyyy, h:mm tt");
			_currentEditingAssignment.StartDate = StartDatePicker.SelectedDate;
			_currentEditingAssignment.StartTime = StartTimeTextBox.Text;
			_currentEditingAssignment.EndDate = EndDatePicker.SelectedDate;
			_currentEditingAssignment.EndTime = EndTimeTextBox.Text;
			_currentEditingAssignment.IsOnlineExam = RadioOnline.IsChecked == true;
			_currentEditingAssignment.FileName = SelectedFileNameTextBlock.Text;
			_currentEditingAssignment.ExtractedQuestionsText = ((ExtractedQuestionsPanel.Visibility == Visibility.Visible) ? ExtractedQuestionsSummary.Text : null);
			_currentEditingAssignment.Notes?.Clear();
			if (_currentEditingAssignment.Notes == null)
			{
				_currentEditingAssignment.Notes = new ObservableCollection<TeacherNote>();
			}
			foreach (TeacherNote note in Notes)
			{
				_currentEditingAssignment.Notes.Add(note);
			}
			AssignmentsDataGrid.Items.Refresh();
		}
		AssignmentsDataGrid.ItemsSource = observableCollection;
		SaveAssignmentsList();
		UploadExamPanel.Visibility = Visibility.Collapsed;
		ExamListPanel.Visibility = Visibility.Visible;
	}

	private void AssignmentTitle_Click(object sender, MouseButtonEventArgs e)
	{
		if (!(sender is TextBlock { DataContext: AssignmentModel dataContext }))
		{
			return;
		}
		_currentEditingAssignment = dataContext;
		UploadExamHeader.Text = "Chỉnh sửa Đề Thi";
		ExamTitleTextBox.Text = dataContext.Title;
		ExamDescriptionTextBox.Text = dataContext.Description;
		StartDatePicker.SelectedDate = dataContext.StartDate;
		StartTimeTextBox.Text = dataContext.StartTime ?? "08:00";
		EndDatePicker.SelectedDate = dataContext.EndDate;
		EndTimeTextBox.Text = dataContext.EndTime ?? "10:00";
		RadioOnline.IsChecked = dataContext.IsOnlineExam;
		RadioOffline.IsChecked = !dataContext.IsOnlineExam;
		SelectedFileNameTextBlock.Text = (string.IsNullOrEmpty(dataContext.FileName) ? "Kéo thả file đề thi vào đây hoặc nhấn để duyệt file" : dataContext.FileName);
		if (SelectedFileNameTextBlock.Text != "Kéo thả file đề thi vào đây hoặc nhấn để duyệt file")
		{
			SelectedFileNameTextBlock.Foreground = Brushes.Black;
		}
		if (!string.IsNullOrEmpty(dataContext.ExtractedQuestionsText))
		{
			ExtractedQuestionsPanel.Visibility = Visibility.Visible;
			ExtractedQuestionsSummary.Text = dataContext.ExtractedQuestionsText;
		}
		else
		{
			ExtractedQuestionsPanel.Visibility = Visibility.Collapsed;
		}
		Notes.Clear();
		if (dataContext.Notes != null)
		{
			foreach (TeacherNote note in dataContext.Notes)
			{
				Notes.Add(note);
			}
		}
		WelcomePanel.Visibility = Visibility.Collapsed;
		ExamListPanel.Visibility = Visibility.Collapsed;
		UploadExamPanel.Visibility = Visibility.Visible;
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.8.0")]
	public void InitializeComponent()
	{
		if (!_contentLoaded)
		{
			_contentLoaded = true;
			Uri resourceLocator = new Uri("/StudentMonitor;component/mainwindow.xaml", UriKind.Relative);
			Application.LoadComponent(this, resourceLocator);
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.8.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IComponentConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 1:
			((Button)target).Click += LogoutButton_Click;
			break;
		case 2:
			((Button)target).Click += MenuUploadExam_Click;
			break;
		case 3:
			((Button)target).Click += MenuAssignmentList_Click;
			break;
		case 4:
			((Button)target).Click += MenuMonitorStudents_Click;
			break;
		case 5:
			WelcomePanel = (Border)target;
			break;
		case 6:
			UploadExamPanel = (Border)target;
			break;
		case 7:
			UploadExamHeader = (TextBlock)target;
			break;
		case 8:
			ExamTitleTextBox = (TextBox)target;
			break;
		case 9:
			ExamDescriptionTextBox = (TextBox)target;
			break;
		case 10:
			StartDatePicker = (DatePicker)target;
			break;
		case 11:
			StartTimeTextBox = (TextBox)target;
			break;
		case 12:
			EndDatePicker = (DatePicker)target;
			break;
		case 13:
			EndTimeTextBox = (TextBox)target;
			break;
		case 14:
			RadioOffline = (RadioButton)target;
			break;
		case 15:
			RadioOnline = (RadioButton)target;
			RadioOnline.Checked += RadioOnline_Checked;
			RadioOnline.Unchecked += RadioOnline_Unchecked;
			break;
		case 16:
			SelectedFileNameTextBlock = (TextBlock)target;
			break;
		case 17:
			((Button)target).Click += BrowseFileButton_Click;
			break;
		case 18:
			ExtractedQuestionsPanel = (Border)target;
			break;
		case 19:
			ExtractedQuestionsSummary = (TextBlock)target;
			break;
		case 20:
			NotesDataGrid = (DataGrid)target;
			break;
		case 21:
			((Button)target).Click += SaveExamButton_Click;
			break;
		case 22:
			ExamListPanel = (Border)target;
			break;
		case 23:
			AssignmentsDataGrid = (DataGrid)target;
			break;
		case 25:
			MonitorStudentsPanel = (Border)target;
			break;
		case 26:
			((Button)target).Click += RefreshViolations_Click;
			break;
		case 27:
			GlobalMonitorWebView = (WebView2)target;
			break;
		case 28:
			ViolationsDataGrid = (DataGrid)target;
			break;
		default:
			_contentLoaded = true;
			break;
		}
	}

	[DebuggerNonUserCode]
	[GeneratedCode("PresentationBuildTasks", "10.0.8.0")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	void IStyleConnector.Connect(int connectionId, object target)
	{
		switch (connectionId)
		{
		case 24:
			((TextBlock)target).MouseLeftButtonUp += AssignmentTitle_Click;
			break;
		case 29:
			((TextBlock)target).MouseLeftButtonUp += ViewScreenshot_Click;
			break;
		}
	}
}
