using AIQuizGenerator.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

namespace AIQuizGenerator.Services;

public class OpenAIService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIService(HttpClient http, IConfiguration config, ILogger<OpenAIService> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;
    }

    public async Task<List<QuizQuestion>> GenerateQuestionsAsync(string pdfText, int count, string difficulty, string? topic)
    {
        string apiKey = _config["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq API key not configured.");
        string model = _config["Groq:Model"] ?? "llama-3.3-70b-versatile";
        string topicHint = string.IsNullOrWhiteSpace(topic) ? "the content of the document" : topic;

        string prompt = "Generate exactly " + count + " multiple-choice questions about " + topicHint + " at " + difficulty + " difficulty. " +
            "Each question has exactly 4 options, only one correct. " +
            "For the \"explanation\" field, write 2-3 sentences that do BOTH of the following: " +
            "(1) explain clearly why the correct option is right, citing the relevant fact or reasoning from the document, and " +
            "(2) name the single most tempting incorrect option and explain specifically why it is wrong or what misconception would lead someone to pick it. " +
            "Do not just restate the correct answer - actually address the wrong option. " +
            "Return ONLY a JSON array, no markdown, no extra text: " +
            "[{\"number\":1,\"question\":\"...\",\"options\":[\"A\",\"B\",\"C\",\"D\"],\"correctIndex\":0,\"explanation\":\"...\"}]. " +
            "Document:\n\n" + pdfText;

        var requestBody = new
        {
            model = model,
            messages = new[]
            {
                new { role = "system", content = "You are a professional quiz generator and tutor. Always respond with valid JSON only, no markdown fences. Your explanations must address common misconceptions, not just restate the correct answer." },
                new { role = "user", content = prompt }
            },
            temperature = 0.7,
            max_tokens = 3500
        };

        var requestJson = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
        _http.DefaultRequestHeaders.Clear();
        _http.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);

        var response = await _http.PostAsync("https://api.groq.com/openai/v1/chat/completions", content);
        var responseBody = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Groq error: {Body}", responseBody);
            throw new InvalidOperationException("Groq API error: " + response.StatusCode + ". Details: " + responseBody);
        }

        var json = JObject.Parse(responseBody);
        string? rawText = json["choices"]?[0]?["message"]?["content"]?.ToString()?.Trim();
        if (string.IsNullOrEmpty(rawText)) throw new InvalidOperationException("Groq returned empty response.");
        rawText = rawText.Replace("```json", "").Replace("```", "").Trim();

        var questions = JsonConvert.DeserializeObject<List<QuizQuestion>>(rawText) ?? throw new InvalidOperationException("Failed to parse AI response.");
        for (int i = 0; i < questions.Count; i++) questions[i].Number = i + 1;
        return questions;
    }
}
