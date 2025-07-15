using System.Collections.Concurrent;

namespace PdfOcr
{
    public class PdfTextExtractor : IAsyncDisposable
    {
        private readonly int _maxDegreeOfParallelism;
        private readonly OcrProcessor _ocrProcessor;

        public PdfTextExtractor(int maxDegreeOfParallelism = 1)
        {
            _maxDegreeOfParallelism = maxDegreeOfParallelism;
            _ocrProcessor = new OcrProcessor(maxDegreeOfParallelism);
        }

        public async Task<string> ExtractTextFromPdf(string pdfFileName)
        {
            ConcurrentBag<(int PageIdx, string Text)> pages = new();

            await foreach (var pageImgGroup in PdfImageConverter.RenderPdfToImageBatchesAsync(pdfFileName, _maxDegreeOfParallelism))
            {
                var pageImgGroupIndexed = pageImgGroup.Select((Image, Index) => (Image, Index));
                await Parallel.ForEachAsync(pageImgGroupIndexed, async (pageImg, _) =>
                {
                    var pageText = await _ocrProcessor.Process(pageImg.Image);
                    pages.Add((pageImg.Index, RemoveLinesWithoutText(pageText)));
                });
            }

            return string.Join(Environment.NewLine, pages);
        }

        private string RemoveLinesWithoutText(string text)
        {
            return text;
        }

        public async ValueTask DisposeAsync()
        {
            await _ocrProcessor.DisposeAsync();
        }
    }
}
