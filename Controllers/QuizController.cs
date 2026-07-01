using AIQuizGenerator.Models;
using AIQuizGenerator.Services;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizGenerator.Controllers;

public class QuizController : Controller
{
    private readonly QuizService _quiz;
    private readonly ILogger<QuizController> _logger;
    private const string SessionKey = "QuizData";

    public QuizController(QuizService quiz, ILogger<QuizController> logger)
    {
        _quiz = quiz;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Upload() => View();

    [HttpPost]
    public async Task<IActionResult> Generate(IFormFile pdfFile, int questionCount = 5, string difficulty = "medium", string? topic = null)
    {
        if (pdfFile == null || pdfFile.Length == 0)
        {
            TempData["Error"] = "Please upload a PDF file.";
            return RedirectToAction("Upload");
        }

        if (!pdfFile.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Only PDF files are supported.";
            return RedirectToAction("Upload");
        }

        try
        {
            using var stream = pdfFile.OpenReadStream();
            var questions = await _quiz.GenerateFromPdfAsync(stream, questionCount, difficulty, topic);
            HttpContext.Session.SetString(SessionKey, QuizService.SerializeQuiz(questions));
            HttpContext.Session.SetString("PdfName", pdfFile.FileName);
            return RedirectToAction("Take");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating quiz");
            TempData["Error"] = ex.Message;
            return RedirectToAction("Upload");
        }
    }

    [HttpGet]
    public IActionResult Take()
    {
        var json = HttpContext.Session.GetString(SessionKey);
        if (string.IsNullOrEmpty(json))
            return RedirectToAction("Upload");

        var questions = QuizService.DeserializeQuiz(json);
        if (questions == null || questions.Count == 0)
            return RedirectToAction("Upload");

        return View(questions);
    }

    [HttpPost]
    public IActionResult Submit(Dictionary<string, int> answers, int elapsedSeconds = 0)
    {
        var json = HttpContext.Session.GetString(SessionKey);
        if (string.IsNullOrEmpty(json))
            return RedirectToAction("Upload");

        var questions = QuizService.DeserializeQuiz(json)!;

        foreach (var q in questions)
        {
            if (answers.TryGetValue("q" + q.Number, out int ans))
                q.UserAnswer = ans;
        }

        var (score, total) = QuizService.ScoreQuiz(questions);
        HttpContext.Session.SetString(SessionKey, QuizService.SerializeQuiz(questions));
        HttpContext.Session.SetInt32("Score", score);
        HttpContext.Session.SetInt32("Total", total);
        HttpContext.Session.SetInt32("ElapsedSeconds", elapsedSeconds);

        return RedirectToAction("Result");
    }

    [HttpGet]
    public IActionResult Result()
    {
        var json = HttpContext.Session.GetString(SessionKey);
        if (string.IsNullOrEmpty(json))
            return RedirectToAction("Upload");

        var questions = QuizService.DeserializeQuiz(json)!;
        int score = HttpContext.Session.GetInt32("Score") ?? 0;
        int total = HttpContext.Session.GetInt32("Total") ?? questions.Count;

        ViewBag.Score = score;
        ViewBag.Total = total;
        ViewBag.PdfName = HttpContext.Session.GetString("PdfName") ?? "Document";
        ViewBag.ElapsedSeconds = HttpContext.Session.GetInt32("ElapsedSeconds") ?? 0;

        return View(questions);
    }
}
