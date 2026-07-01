namespace AIQuizGenerator.Models;

public class QuizQuestion
{
    public int Number { get; set; }
    public string Question { get; set; } = "";
    public List<string> Options { get; set; } = new();
    public int CorrectIndex { get; set; }
    public string Explanation { get; set; } = "";
    public int? UserAnswer { get; set; }
}

public class QuizSession
{
    public List<QuizQuestion> Questions { get; set; } = new();
    public string PdfName { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
