using UglyToad.PdfPig;
using System.Text;

namespace AIQuizGenerator.Services;

public class PdfExtractorService
{
    public string ExtractText(Stream pdfStream)
    {
        using var ms = new MemoryStream();
        pdfStream.CopyTo(ms);
        ms.Position = 0;

        using var pdf = PdfDocument.Open(ms.ToArray());
        var sb = new StringBuilder();

        foreach (var page in pdf.GetPages())
        {
            sb.AppendLine(page.Text);
        }

        var text = sb.ToString().Trim();
        if (text.Length > 12000)
            text = text.Substring(0, 12000);

        return text;
    }
}
