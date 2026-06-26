using System;
using System.Collections.ObjectModel;

namespace StudentMonitor;

public class AssignmentModel
{
	public string? Title { get; set; }

	public string? Description { get; set; }

	public string? Status { get; set; }

	public string? OpenDate { get; set; }

	public string? DueDate { get; set; }

	public string? LastModifiedBy { get; set; }

	public string? ModifiedDate { get; set; }

	public DateTime? StartDate { get; set; }

	public string? StartTime { get; set; }

	public DateTime? EndDate { get; set; }

	public string? EndTime { get; set; }

	public bool IsOnlineExam { get; set; } = true;

	public string? FileName { get; set; }

	public string? ExtractedQuestionsText { get; set; }

	public ObservableCollection<TeacherNote>? Notes { get; set; }
}
