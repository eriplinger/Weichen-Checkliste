using ClosedXML.Excel;
using Microsoft.VisualBasic.FileIO;
using System.Data;
using System.IO;
using System.Text;

namespace Weichen_Checkliste
{
    public static class ArbeitsvorratImport
    {
        private static readonly Encoding Utf8 = new UTF8Encoding(false, true);
        private static readonly Encoding Windows1252;

        static ArbeitsvorratImport()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Windows1252 = Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        }

        public static string Dekodieren(byte[] daten, string quellkodierung = "Auto")
        {
            if (daten.AsSpan().StartsWith(new byte[] { 0xFF, 0xFE, 0x00, 0x00 }))
                return new UTF32Encoding(false, false, true).GetString(daten, 4, daten.Length - 4);
            if (daten.AsSpan().StartsWith(new byte[] { 0x00, 0x00, 0xFE, 0xFF }))
                return new UTF32Encoding(true, false, true).GetString(daten, 4, daten.Length - 4);
            if (daten.AsSpan().StartsWith(new byte[] { 0xFF, 0xFE }))
                return new UnicodeEncoding(false, false, true).GetString(daten, 2, daten.Length - 2);
            if (daten.AsSpan().StartsWith(new byte[] { 0xFE, 0xFF }))
                return new UnicodeEncoding(true, false, true).GetString(daten, 2, daten.Length - 2);
            if (daten.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }))
                return Utf8.GetString(daten, 3, daten.Length - 3);
            if (!quellkodierung.Equals("Auto", StringComparison.OrdinalIgnoreCase))
            {
                Encoding kodierung = quellkodierung.Trim().ToUpperInvariant() switch
                {
                    "UTF8" or "UTF-8" => Utf8,
                    "WINDOWS1252" or "WINDOWS-1252" => Windows1252,
                    "IBM850" or "CP850" => Encoding.GetEncoding(850, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback),
                    _ => throw new InvalidDataException("CsvEncoding muss Auto, UTF8, Windows1252 oder IBM850 sein.")
                };
                return kodierung.GetString(daten);
            }
            try { return Utf8.GetString(daten); }
            catch (DecoderFallbackException) { return Windows1252.GetString(daten); }
        }

        public static string Bereinigen(string text)
        {
            // Bereits falsch interpretierte UTF-8-Daten nur dann als ganzen Text
            // zurückwandeln, wenn Windows-1252 -> UTF-8 verlustfrei möglich ist.
            for (int durchlauf = 0; durchlauf < 3; durchlauf++)
            {
                if (!text.Any(zeichen => zeichen is 'Â' or 'Ã' or 'â' or 'ï' or 'ð')) break;
                try
                {
                    string repariert = Utf8.GetString(Windows1252.GetBytes(text));
                    if (repariert == text) break;
                    text = repariert;
                }
                catch (EncoderFallbackException) { break; }
                catch (DecoderFallbackException) { break; }
            }

            for (int i = 0; i < text.Length; i++)
            {
                char zeichen = text[i];
                if (zeichen == '\uFFFD' || (char.IsControl(zeichen) && zeichen != '\t' && zeichen != '\r' && zeichen != '\n'))
                    throw new InvalidDataException("Der Text enthält beschädigte oder nicht unterstützte Zeichen. Bitte die Quelldatei erneut als UTF-8 bzw. XLSX exportieren.");
                if (char.IsHighSurrogate(zeichen) && i + 1 < text.Length && char.IsLowSurrogate(text[i + 1])) { i++; continue; }
                if (char.IsSurrogate(zeichen)) throw new InvalidDataException("Ungültiges Unicode-Zeichen in der Quelldatei.");
            }
            return text;
        }

        public static DataTable CsvLaden(string pfad, string quellkodierung = "Auto")
        {
            using var reader = new StringReader(Dekodieren(File.ReadAllBytes(pfad), quellkodierung));
            using var parser = new TextFieldParser(reader) { HasFieldsEnclosedInQuotes = true, TrimWhiteSpace = false };
            parser.SetDelimiters(";");
            if (parser.EndOfData) throw new InvalidDataException("Die CSV-Datei ist leer.");
            var tabelle = new DataTable();
            foreach (string name in parser.ReadFields()!) tabelle.Columns.Add(Bereinigen(name));
            while (!parser.EndOfData)
            {
                string[] felder = parser.ReadFields()!;
                if (felder.Length != tabelle.Columns.Count)
                    throw new InvalidDataException($"CSV-Zeile bei Zeile {parser.LineNumber}: Die Anzahl der Felder passt nicht zur Kopfzeile.");
                tabelle.Rows.Add(felder.Select(Bereinigen).ToArray());
            }
            return tabelle;
        }

        public static DataTable ExcelLaden(string pfad)
        {
            using var workbook = new XLWorkbook(pfad);
            var sheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidDataException("Die Excel-Datei enthält kein Arbeitsblatt.");
            var kopf = sheet.FirstRowUsed() ?? throw new InvalidDataException("Das Arbeitsblatt ist leer.");
            int letzteSpalte = kopf.LastCellUsed()!.Address.ColumnNumber;
            var tabelle = new DataTable();
            for (int spalte = 1; spalte <= letzteSpalte; spalte++)
                tabelle.Columns.Add(Bereinigen(kopf.Cell(spalte).GetString()));
            foreach (var zeile in sheet.RowsUsed().Where(zeile => zeile.RowNumber() > kopf.RowNumber()))
            {
                var daten = tabelle.NewRow();
                for (int spalte = 1; spalte <= letzteSpalte; spalte++)
                    daten[spalte - 1] = Bereinigen(zeile.Cell(spalte).GetString());
                tabelle.Rows.Add(daten);
            }
            return tabelle;
        }
    }
}
