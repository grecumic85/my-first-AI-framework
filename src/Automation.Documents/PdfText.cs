using UglyToad.PdfPig;
namespace Automation.Documents;

public interface IPdfTextExtractor { string Read(string file); }
public sealed class PdfTextExtractor : IPdfTextExtractor
{
    // Text extraction only: scanned-image PDFs require a separate OCR adapter.
    public string Read(string file)
    {
        using var pdf = PdfDocument.Open(file);
        return string.Join(Environment.NewLine, pdf.GetPages().Select(page => page.Text));
    }
}
