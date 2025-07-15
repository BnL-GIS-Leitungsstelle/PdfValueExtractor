namespace PdfParser;

public record UferabschnittBewertung(
    int UferabschnittNr,
    int? GesamtNote)
{
    public bool IsValid => 
        UferabschnittNr > 0 && 
        GesamtNote.HasValue && 
        GesamtNote.Value >= 0 && 
        GesamtNote.Value <= 100;
}
