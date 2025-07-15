using SkiaSharp;
using System.Collections.Concurrent;

namespace PdfOcr;

public static class PdfImageConverter
{
    public static int GetTotalPages(string pdfFileName)
    {
        using var pdfStream = new FileStream(pdfFileName, FileMode.Open, FileAccess.Read);
        return PDFtoImage.Conversion.GetPageCount(pdfStream);
    }

    public static async IAsyncEnumerable<byte[]> RenderPdfToImagesAsync(string pdfFileName)
    {
        using var pdfFileStream = new FileStream(pdfFileName, FileMode.Open, FileAccess.Read);

        var renderOptions = new PDFtoImage.RenderOptions(PdfConstants.Dpi);
        await foreach (var bitmapStream in PDFtoImage.Conversion.ToImagesAsync(pdfFileStream, options: renderOptions))
        {
            using var bitmap = SKImage.FromBitmap(bitmapStream);
            using var image = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            yield return image.ToArray();
        } 
    }

    public static async IAsyncEnumerable<byte[][]> RenderPdfToImageBatchesAsync(
        string pdfFileName,
        int batchSize)
    {       
        var lastPageNumber = GetTotalPages(pdfFileName);
        var currIdx = 0;

        while (currIdx < lastPageNumber) 
        {
            ConcurrentBag<(int, byte[])> results = new();
            var range = Enumerable.Range(currIdx, Math.Min(batchSize, lastPageNumber-currIdx)).ToArray();

            await Parallel.ForEachAsync(range, async (i, _) =>
            {
                await using var pdfFileStream = new FileStream(pdfFileName, FileMode.Open, FileAccess.Read);

                var renderOptions = new PDFtoImage.RenderOptions(PdfConstants.Dpi);
                using var bitmapStream = PDFtoImage.Conversion.ToImage(pdfFileStream, i, options: renderOptions);
                using var bitmap = SKImage.FromBitmap(bitmapStream);
                using var image = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                results.Add((i, image.ToArray()));
            });

            currIdx = range.Last() + 1;
            yield return results
                .OrderBy(r => r.Item1)
                .Select(r => r.Item2)
                .ToArray();
        }
    }
}
