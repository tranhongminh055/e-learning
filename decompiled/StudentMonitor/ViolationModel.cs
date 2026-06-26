namespace StudentMonitor;

public class ViolationModel
{
	public int id { get; set; }

	public string? full_name { get; set; }

	public string? username { get; set; }

	public string? violation_type { get; set; }

	public string? screenshot_path { get; set; }

	public string? captured_at { get; set; }
}
