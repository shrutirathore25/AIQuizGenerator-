using AIQuizGenerator.Models;
using Newtonsoft.Json;

namespace AIQuizGenerator.Services;

public class QuizService
{
    private readonly PdfExtractorService _pdf;
    private readonly OpenAIService _ai;

    public QuizService(PdfExtractorService pdf, OpenAIService ai)
    {
        _pdf = pdf;
        _ai = ai;
    }

    public async Task<List<QuizQuestion>> GenerateFromPdfAsync(Stream pdfStream, int count, string difficulty, string? topic)
    {
        string text = _pdf.ExtractText(pdfStream);
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("No readable text found in the PDF. Please use a text-based PDF.");

        return await _ai.GenerateQuestionsAsync(text, count, difficulty, topic);
    }

    public static string SerializeQuiz(List<QuizQuestion> questions)
        => JsonConvert.SerializeObject(questions);

    public static List<QuizQuestion>? DeserializeQuiz(string json)
        => JsonConvert.DeserializeObject<List<QuizQuestion>>(json);

    public static (int score, int total) ScoreQuiz(List<QuizQuestion> questions)
    {
        int score = questions.Count(q => q.UserAnswer.HasValue && q.UserAnswer.Value == q.CorrectIndex);
        return (score, questions.Count);
    }
}
