using System;

namespace StudentMonitor;

public class TeacherNote
{
	public string? Id { get; set; }

	public string? NoteContent { get; set; }

	public string? CreatedDate { get; set; }

	public TeacherNote()
	{
		CreatedDate = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
	}
}
