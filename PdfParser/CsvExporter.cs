using CsvHelper;
using System.Globalization;

namespace PdfParser;

public static class CsvExporter
{
    public static async Task ExportToCsvAsync<T>(IEnumerable<T> records, string fileName) 
        where T: class
    {
        using var writer = new StreamWriter(fileName);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(records);
    }
}