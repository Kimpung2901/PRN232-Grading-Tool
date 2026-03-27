namespace Infrastructure;

public partial class TestCase
{
    public int Id { get; set; }

    public int ExamId { get; set; }

    public string FilePath { get; set; } = null!;

    public virtual Exam Exam { get; set; } = null!;
}
