using ClosedXML.Excel;
using System.IO;

namespace Weichen_Checkliste
{
    public static class BefundDateien
    {
        public static string Speichern(string zielordner, string weichennummer, string datum,
            string bearbeiter, string status, string kommentar)
        {
            if (string.IsNullOrWhiteSpace(zielordner))
                throw new InvalidOperationException("Bitte den Zielpfad in settings.txt hinterlegen.");

            Directory.CreateDirectory(zielordner);
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(weichennummer + "_" + datum);
            worksheet.Cell(1, 1).Value = "Datum";
            worksheet.Cell(1, 2).Value = "Bearbeiter";
            worksheet.Cell(1, 3).Value = "Status";
            worksheet.Cell(1, 4).Value = "Kommentare";
            worksheet.Cell(2, 1).Value = datum;
            worksheet.Cell(2, 2).Value = bearbeiter;
            worksheet.Cell(2, 3).Value = status;
            worksheet.Cell(2, 4).Value = kommentar;

            string dateiname = weichennummer + "_" + datum;
            string dateipfad = Path.Combine(zielordner, dateiname + ".xlsx");
            int nummer = 1;
            while (File.Exists(dateipfad))
                dateipfad = Path.Combine(zielordner, dateiname + "_" + nummer++ + ".xlsx");
            workbook.SaveAs(dateipfad);
            return dateipfad;
        }

        public static void PruefeGetrenntenPfad(string wartungspfad, params string[] anderePfade)
        {
            if (string.IsNullOrWhiteSpace(wartungspfad) || !Path.IsPathFullyQualified(wartungspfad))
                throw new InvalidOperationException("Bitte einen absoluten Weichenwartung-Pfad in settings.txt hinterlegen.");

            string pfad = Path.TrimEndingDirectorySeparator(Path.GetFullPath(wartungspfad));
            foreach (string andererPfad in anderePfade)
            {
                if (string.IsNullOrWhiteSpace(andererPfad)) continue;
                string anderer = Path.TrimEndingDirectorySeparator(Path.GetFullPath(andererPfad));
                if (pfad.Equals(anderer, StringComparison.OrdinalIgnoreCase)
                    || pfad.StartsWith(Path.EndsInDirectorySeparator(anderer) ? anderer : anderer + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                    || anderer.StartsWith(Path.EndsInDirectorySeparator(pfad) ? pfad : pfad + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Die Weichenwartung-Pfade müssen von den übrigen Befund-, Bilder- und Arbeitsordnern getrennt sein.");
            }
        }

        public static void ArbeitsvorratKopieren(string remote, string lokal)
        {
            PruefeGetrenntenPfad(lokal, remote);
            if (!Directory.Exists(remote))
                throw new IOException("Remote-Arbeitsvorrat für Weichenwartung nicht erreichbar.");
            Directory.CreateDirectory(lokal);
            foreach (string quelle in Directory.GetFiles(remote))
                File.Copy(quelle, Path.Combine(lokal, Path.GetFileName(quelle)), true);
            foreach (string ordner in Directory.GetDirectories(remote))
                ArbeitsvorratKopieren(ordner, Path.Combine(lokal, Path.GetFileName(ordner)));
        }

        public static void Synchronisieren(string lokal, string remote)
        {
            PruefeGetrenntenPfad(remote, lokal);
            if (!Directory.Exists(remote))
                throw new IOException("Remote-Ordner für Weichenwartung nicht erreichbar.");
            if (!Directory.Exists(lokal)) return;

            // Wie bei bisherigen Befunden: lokal nach remote verschieben.
            // Vorhandene Remote-Dateien und der lokale Ordner bleiben erhalten.
            foreach (string quelle in Directory.GetFiles(lokal, "*.xlsx"))
            {
                string ziel = Path.Combine(remote, Path.GetFileName(quelle));
                int nummer = 1;
                while (File.Exists(ziel))
                    ziel = Path.Combine(remote, Path.GetFileNameWithoutExtension(quelle)
                        + "_" + nummer++ + ".xlsx");
                File.Move(quelle, ziel);
            }
        }
    }
}
