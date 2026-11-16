using System.Security.Cryptography;

namespace Weichen_Checkliste
{
    public sealed record SynchronisationsAuftrag(
        string Name, string RemotePfad, string LokalPfad, bool IstArbeitsvorrat);

    public sealed record SynchronisationsErgebnis(
        string Name, bool Erfolgreich, int Uebertragen, int LokalGeloescht, string? Fehler);

    /// <summary>
    /// Synchronisiert jeweils einen dedizierten lokalen Ordner mit einem zentralen Ordner.
    /// Arbeitsvorrat: Remote ist fuehrend. Rueckmeldungen: lokal -> Remote, letzter Upload gewinnt.
    /// Jede Aufgabe wird unabhaengig ausgefuehrt; Fehler lassen lokale Rueckmeldungen unberuehrt.
    /// </summary>
    public static class SynchronisationsDienst
    {
        public static IReadOnlyList<SynchronisationsErgebnis> Ausfuehren(
            IEnumerable<SynchronisationsAuftrag> auftraege)
        {
            var ergebnisse = new List<SynchronisationsErgebnis>();
            foreach (var auftrag in auftraege)
            {
                try
                {
                    PruefePfade(auftrag);
                    if (!Directory.Exists(auftrag.RemotePfad))
                        throw new IOException($"Remote-Ordner nicht erreichbar: {auftrag.RemotePfad}");

                    (int uebertragen, int geloescht) = auftrag.IstArbeitsvorrat
                        ? Spiegeln(auftrag.RemotePfad, auftrag.LokalPfad)
                        : Hochladen(auftrag.LokalPfad, auftrag.RemotePfad);

                    ergebnisse.Add(new(auftrag.Name, true, uebertragen, geloescht, null));
                }
                catch (Exception ex)
                {
                    ergebnisse.Add(new(auftrag.Name, false, 0, 0, ex.Message));
                }
            }
            return ergebnisse;
        }

        private static void PruefePfade(SynchronisationsAuftrag auftrag)
        {
            if (string.IsNullOrWhiteSpace(auftrag.LokalPfad)
                || string.IsNullOrWhiteSpace(auftrag.RemotePfad)
                || !Path.IsPathFullyQualified(auftrag.LokalPfad)
                || !Path.IsPathFullyQualified(auftrag.RemotePfad))
                throw new InvalidOperationException(
                    $"Ungueltiger oder fehlender absoluter Pfad fuer {auftrag.Name}.");

            string lokal = Path.TrimEndingDirectorySeparator(Path.GetFullPath(auftrag.LokalPfad));
            string remote = Path.TrimEndingDirectorySeparator(Path.GetFullPath(auftrag.RemotePfad));
            if (IstGleichOderUnterordner(lokal, remote) || IstGleichOderUnterordner(remote, lokal))
                throw new InvalidOperationException(
                    $"Quell- und Zielverzeichnis duerfen sich nicht ueberlappen: {auftrag.Name}.");
        }

        private static bool IstGleichOderUnterordner(string pfad, string basis)
        {
            return pfad.Equals(basis, StringComparison.OrdinalIgnoreCase)
                || pfad.StartsWith(basis + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Erst nach einem vollstaendig erfolgreichen Download werden nicht mehr vorhandene
        /// lokale Arbeitsvorratsdateien entfernt. Bei Offlinebetrieb bleibt der Cache bestehen.
        /// </summary>
        private static (int, int) Spiegeln(string remote, string lokal)
        {
            // Erst die Remote-Auflistung abschliessen. Ist sie nicht moeglich, lokal nichts loeschen.
            string[] remoteDateien = Directory.GetFiles(remote, "*", SearchOption.AllDirectories);
            Directory.CreateDirectory(lokal);

            int uebertragen = 0;
            int geloescht = 0;
            var erwartet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string quelle in remoteDateien)
            {
                string relativ = Path.GetRelativePath(remote, quelle);
                string ziel = Path.Combine(lokal, relativ);
                erwartet.Add(Path.GetFullPath(ziel));
                Directory.CreateDirectory(Path.GetDirectoryName(ziel)!);

                if (File.Exists(ziel) && DateienGleich(quelle, ziel))
                    continue;

                string temporaer = TemporarerPfad(ziel);
                try
                {
                    File.Copy(quelle, temporaer);
                    if (!DateienGleich(quelle, temporaer))
                        throw new IOException($"Quelle waehrend des Kopierens veraendert: {quelle}");
                    File.Move(temporaer, ziel, true);
                    uebertragen++;
                }
                finally
                {
                    if (File.Exists(temporaer)) File.Delete(temporaer);
                }
            }

            foreach (string datei in Directory.GetFiles(lokal, "*", SearchOption.AllDirectories))
            {
                if (erwartet.Contains(Path.GetFullPath(datei))) continue;
                File.Delete(datei);
                geloescht++;
            }
            // Das Stammverzeichnis bleibt immer erhalten.
            foreach (string ordner in Directory.GetDirectories(lokal, "*", SearchOption.AllDirectories)
                .OrderByDescending(p => p.Length))
            {
                if (!Directory.EnumerateFileSystemEntries(ordner).Any())
                    Directory.Delete(ordner);
            }
            return (uebertragen, geloescht);
        }

        /// <summary>
        /// Veroeffentlicht Dateien auf dem Share unter unveraendertem Dateinamen.
        /// Ein gleichnamiges Remote-Dokument wird ersetzt (last completed upload wins).
        /// Die lokale Datei wird erst nach erfolgreicher Veroeffentlichung geloescht.
        /// </summary>
        private static (int, int) Hochladen(string lokal, string remote)
        {
            if (!Directory.Exists(lokal)) return (0, 0);
            string[] lokaleDateien = Directory.GetFiles(lokal, "*", SearchOption.AllDirectories);
            int uebertragen = 0;
            int geloescht = 0;
            foreach (string quelle in lokaleDateien)
            {
                string ziel = Path.Combine(remote, Path.GetRelativePath(lokal, quelle));
                Directory.CreateDirectory(Path.GetDirectoryName(ziel)!);
                string temporaer = TemporarerPfad(ziel);
                try
                {
                    // Exklusiv lesen: Noch schreibende Anwendungen koennen nicht
                    // versehentlich eine halbfertige Datei hochladen.
                    using (var eingang = new FileStream(quelle, FileMode.Open, FileAccess.Read, FileShare.None))
                    {
                        using (var ausgang = new FileStream(temporaer, FileMode.CreateNew,
                            FileAccess.ReadWrite, FileShare.None))
                        {
                            eingang.CopyTo(ausgang);
                            ausgang.Flush(true);
                            eingang.Position = 0;
                            ausgang.Position = 0;
                            if (!SHA256.HashData(eingang).SequenceEqual(SHA256.HashData(ausgang)))
                                throw new IOException($"Pruefsumme der Uebertragung stimmt nicht: {quelle}");
                        }

                        // Umbenennen innerhalb desselben Remote-Verzeichnisses:
                        // kein geloeschtes Alt-Dokument vor gesichertem Upload.
                        File.Move(temporaer, ziel, true);
                    }
                    uebertragen++;
                    File.Delete(quelle);
                    geloescht++;
                }
                finally
                {
                    if (File.Exists(temporaer)) File.Delete(temporaer);
                }
            }
            foreach (string ordner in Directory.GetDirectories(lokal, "*", SearchOption.AllDirectories)
                .OrderByDescending(p => p.Length))
            {
                if (!Directory.EnumerateFileSystemEntries(ordner).Any())
                    Directory.Delete(ordner);
            }
            return (uebertragen, geloescht);
        }

        private static bool DateienGleich(string pfadA, string pfadB)
        {
            if (new FileInfo(pfadA).Length != new FileInfo(pfadB).Length)
                return false;
            using var a = File.OpenRead(pfadA);
            using var b = File.OpenRead(pfadB);
            return SHA256.HashData(a).SequenceEqual(SHA256.HashData(b));
        }

        private static string TemporarerPfad(string ziel) =>
            ziel + ".sync-" + Guid.NewGuid().ToString("N") + ".tmp";
    }
}
