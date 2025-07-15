using PdfOcr;
using PdfParser;
using Spectre.Console;

var inputPdf = @"D:\Z70ANIT1\PdfParser\results\Zustand, Erhaltung und Schutz der Ufer des Thuner Sees (Band II) - Pflanzenökologische Merkmale der Uferzonen 1988.pdf";
var outputCsv = @"D:\Z70ANIT1\PdfParser\results\00_thunersee.csv";

AnsiConsole.MarkupLine("[bold green]Initialize...[/]");

ICollection<UferabschnittBewertung> results = Array.Empty<UferabschnittBewertung>();

await AnsiConsole.Progress()
    .HideCompleted(true)
    .Columns(
    [
        new TaskDescriptionColumn(),
        new ProgressBarColumn(),
        new PercentageColumn(),
        new SpinnerColumn()
    ])
    .StartAsync(async ctx =>
    {
        var pdfParseTask = ctx.AddTask($"[green]Parse Pdf[/]");
        pdfParseTask.StartTask();

        results = await new PdfParserRunner().RunAsync(inputPdf, outputCsv, (progress) => pdfParseTask.Increment(progress));
        outputCsv = outputCsv.Replace(".csv", $"_({results.Where(r => r.IsValid).Count()} of {results.Count}).csv");
        await CsvExporter.ExportToCsvAsync(results.OrderBy(r => r.UferabschnittNr), outputCsv);

        pdfParseTask.StopTask();        
    });

var table = new Table();
table.AddColumn("Uferabschnitt Nr.");
table.AddColumn("Gesamtnote");
table.AddColumn("Valid");

foreach (var result in results)
{
    table.AddRow(result.UferabschnittNr.ToString(), result.GesamtNote?.ToString() ?? "NULL", result.IsValid.ToString());
}

AnsiConsole.WriteLine();
AnsiConsole.Write(table);

var successfullyDetected = results.Where(r => r.IsValid).Count();
var failedToDetect = results.Count() - successfullyDetected;
AnsiConsole.Write(new BreakdownChart()
    .Width(60)
    .AddItem($"Successfully detected {successfullyDetected}", successfullyDetected, Color.Green)
    .AddItem($"Failed to detect {failedToDetect}", failedToDetect, Color.Red));
AnsiConsole.WriteLine();
AnsiConsole.MarkupLine($"[green]Exported {results.Count} results to: {outputCsv}.[/]");

Console.ReadLine();

public class PdfParserRunner
{
    public async Task<ICollection<UferabschnittBewertung>> RunAsync(string inputPdf, string outputCsv, Action<double>? reportProgress)
    {
        var maxDegreeOfParallelism = Environment.ProcessorCount * 2;
        var results = new List<UferabschnittBewertung>();
        await using var ocrProcessor = new OcrProcessor(maxDegreeOfParallelism);
        var totalPages = PdfImageConverter.GetTotalPages(inputPdf);

        await foreach (var pageImgGroup in PdfImageConverter.RenderPdfToImageBatchesAsync(inputPdf, maxDegreeOfParallelism))
        {
            await Parallel.ForEachAsync(pageImgGroup, async (pageImg, _) =>
            {
                var pageText = await ocrProcessor.Process(pageImg);
                if (TryParseUferabschnittBewertung(pageText, out var uferabschnittBewertung))
                    results.Add(uferabschnittBewertung);
            });

            if (reportProgress != null)
            {
                var progress = (double)pageImgGroup.Count() / totalPages * 100;
                reportProgress(progress);
            }
        }

        RemoveDuplicatesAndMarkInvalid(results);
        results = OrderResults(results);
        GapfillResults(results);

        return results;
    }

    private bool TryParseUferabschnittBewertung(string pageText, out UferabschnittBewertung uferabschnittBewertung)
    {
        if (FlachwasserValueParser.TryParseUferabschnittNr(pageText, out var uferabschnittNr))
        {
            if (FlachwasserValueParser.TryParseGesamtNote(pageText, out var gesamtNote))
            {
                uferabschnittBewertung = new(uferabschnittNr, gesamtNote);
            }
            else
            {
                uferabschnittBewertung = new(uferabschnittNr, null);
            }

            return true;
        }

        uferabschnittBewertung = null!;
        return false;
    }

    private void GapfillResults(List<UferabschnittBewertung> results)
    {
        for (int i = 0; i < results.Count-1; i++)
        {
            var currRes = results[i];
            var nextRes = results[i + 1];
            while (nextRes.UferabschnittNr - currRes.UferabschnittNr > 1)
            {
                i++;

                currRes = new(currRes.UferabschnittNr + 1, null);
                results.Insert(i, currRes);
            }
        }
    }

    private void RemoveDuplicatesAndMarkInvalid(List<UferabschnittBewertung> results)
    {
        var duplicateKeys = results
            .GroupBy(r => r.UferabschnittNr)
            .Where(r => r.Count() > 1)
            .Select(r => r.Key)
            .ToArray();

        foreach (var duplicateKey in duplicateKeys)
        {
            results.RemoveAll(r => r.UferabschnittNr == duplicateKey);
            results.Add(new(duplicateKey, null));
        }
    }

    private List<UferabschnittBewertung> OrderResults(List<UferabschnittBewertung> results)
    {
        return results
            .OrderBy(r => r.UferabschnittNr)
            .ToList();
    }
}
