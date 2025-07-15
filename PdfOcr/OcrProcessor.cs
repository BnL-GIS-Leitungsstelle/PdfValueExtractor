using System.Threading.Channels;
using Tesseract;

namespace PdfOcr;

public class OcrProcessor : IAsyncDisposable
{
    private readonly Channel<TesseractEngine> _enginePool;

    public OcrProcessor(int enginePoolSize = 1)
    {
        _enginePool = Channel.CreateBounded<TesseractEngine>(enginePoolSize);
        for (int i = 0; i < enginePoolSize; i++)
        {
            var engine = new TesseractEngine(@"./tessdata", "deu+ita+fra+eng", EngineMode.Default);
            engine.SetVariable("debug_file", "NUL");
            _enginePool.Writer.TryWrite(engine);
        }
    }

    public async ValueTask<string> Process(byte[] image)
    {
        var engine = await _enginePool.Reader.ReadAsync();

        try
        {
            using var img = Pix.LoadFromMemory(image);
            img.XRes = PdfConstants.Dpi;
            img.YRes = PdfConstants.Dpi;

            using var page = engine.Process(img);
            return page.GetText();
        }
        finally
        {
            await _enginePool.Writer.WriteAsync(engine);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _enginePool.Writer.Complete();
        await foreach (var engine in _enginePool.Reader.ReadAllAsync())
        {
            engine.Dispose();
        }
    }
}