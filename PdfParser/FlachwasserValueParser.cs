using System.Text.RegularExpressions;

namespace PdfParser;

public static class FlachwasserValueParser
{
    public static bool TryParseUferabschnittNr(string input, out int uferabschnittNr)
    {
        var parsedNrs = new List<int>();
        var matches = Regex.Matches(input, @"(?:Uferabschnitt Nr\.|de segment).? (\d+)", RegexOptions.Compiled).GetEnumerator();
        while (matches.MoveNext())
        {
            if (matches.Current is Match match &&
                int.TryParse(match.Groups[1].Value, out var result))
            {
                parsedNrs.Add(result);
            }
        }

        if (parsedNrs.Any() && parsedNrs.Distinct().Count() == 1) 
        {
            uferabschnittNr = parsedNrs[0];
            return true;
        }

        uferabschnittNr = default;
        return false;
    }

    public static bool TryParseGesamtNote(string input, out int gesamtNote)
    {
        var match = Regex.Match(input, @"(?:NOTE|TOTALE) = (\d+)", RegexOptions.Compiled);
        if (match.Success)
        {
            var gesamtNoteStr = match.Groups[1].Value;
            return int.TryParse(gesamtNoteStr, out gesamtNote);
        }

        gesamtNote = default;
        return false;
    }
}
