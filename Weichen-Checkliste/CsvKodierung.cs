using System.IO;
using System.Text;

namespace Weichen_Checkliste
{
    public static class CsvKodierung
    {
        public static string[] ZeilenLesen(string dateipfad)
        {
            byte[] daten = File.ReadAllBytes(dateipfad);
            Encoding kodierung;
            int offset = 0;
            if (daten.AsSpan().StartsWith(new byte[] { 0xFF, 0xFE, 0x00, 0x00 }))
            {
                kodierung = new UTF32Encoding(false, false, true);
                offset = 4;
            }
            else if (daten.AsSpan().StartsWith(new byte[] { 0x00, 0x00, 0xFE, 0xFF }))
            {
                kodierung = new UTF32Encoding(true, false, true);
                offset = 4;
            }
            else if (daten.AsSpan().StartsWith(new byte[] { 0xFF, 0xFE }))
            {
                kodierung = new UnicodeEncoding(false, false, true);
                offset = 2;
            }
            else if (daten.AsSpan().StartsWith(new byte[] { 0xFE, 0xFF }))
            {
                kodierung = new UnicodeEncoding(true, false, true);
                offset = 2;
            }
            else
            {
                kodierung = new UTF8Encoding(false, true);
                if (daten.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF })) offset = 3;
            }

            string text;
            try
            {
                text = kodierung.GetString(daten, offset, daten.Length - offset);
            }
            catch (DecoderFallbackException) when (offset == 0)
            {
                text = LegacyDekodieren(daten);
            }

            using var reader = new StringReader(text);
            var zeilen = new List<string>();
            string? zeile;
            while ((zeile = reader.ReadLine()) != null) zeilen.Add(zeile);
            return zeilen.ToArray();
        }

        private static string LegacyDekodieren(byte[] daten)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string Dekodieren(int codepage) => Encoding.GetEncoding(codepage,
                EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback).GetString(daten);

            string windowsText = Dekodieren(1252);
            string besterText = windowsText;
            int windowsBewertung = DeutscheTextBewertung(windowsText);
            int besteBewertung = windowsBewertung;
            // IBM850 ist die übliche westeuropäische DOS-Codepage; bei Gleichstand
            // mit IBM437 hat sie Vorrang. Gleiche deutsche Zeichen ergeben denselben Text.
            foreach (int codepage in new[] { 850, 437 })
            {
                string kandidat = Dekodieren(codepage);
                int bewertung = DeutscheTextBewertung(kandidat);
                if (bewertung > besteBewertung && bewertung - windowsBewertung >= 4)
                {
                    besterText = kandidat;
                    besteBewertung = bewertung;
                }
            }
            return besterText;
        }

        private static int DeutscheTextBewertung(string text)
        {
            // Sprachheuristik für deutsche Exporte, keine Worterkennung oder Ersetzung.
            // Bei fehlender bzw. schwacher Evidenz bleibt Windows-1252 der Standard.
            int bewertung = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char zeichen = text[i];
                if (zeichen is 'ä' or 'ö' or 'ü' or 'Ä' or 'Ö' or 'Ü' or 'ß')
                    bewertung += 2;
                else if (char.IsControl(zeichen) && zeichen != '\r' && zeichen != '\n' && zeichen != '\t')
                    bewertung -= 8;
                else if (zeichen >= '\u2500' && zeichen <= '\u259F')
                    bewertung -= 4; // DOS-Rahmenzeichen sind in Tabellenwerten untypisch.
                else if (zeichen > 127 && char.IsLetter(zeichen))
                    bewertung -= 2;
                else if (zeichen is '„' or '“' or '”' or '‘' or '’')
                {
                    // Anführungszeichen innerhalb eines Wortes sprechen für eine
                    // falsch gelesene DOS-Codepage; normale Zitate bleiben neutral.
                    if (i > 0 && i + 1 < text.Length
                        && char.IsLetter(text[i - 1]) && char.IsLetter(text[i + 1]))
                        bewertung -= 6;
                }
            }
            return bewertung;
        }
    }
}
