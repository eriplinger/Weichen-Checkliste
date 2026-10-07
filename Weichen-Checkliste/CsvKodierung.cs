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
                // ANSI auf westeuropäischen Windows-Systemen entspricht Windows-1252.
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                kodierung = Encoding.GetEncoding(1252, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
                text = kodierung.GetString(daten);
            }

            using var reader = new StringReader(text);
            var zeilen = new List<string>();
            string? zeile;
            while ((zeile = reader.ReadLine()) != null) zeilen.Add(zeile);
            return zeilen.ToArray();
        }
    }
}
