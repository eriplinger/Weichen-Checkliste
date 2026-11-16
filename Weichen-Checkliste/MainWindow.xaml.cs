

using ClosedXML.Excel;
using DocumentFormat.OpenXml.CustomProperties;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.Win32;
using OpenCvSharp;
using System;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WpfWebcamApp;

namespace Weichen_Checkliste
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        // Der Pfad zur Textdatei mit den Einstellungen
        private readonly string settingsFilePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Weichen\settings.txt";
        private string ArbeitsvorratPath = "";
        private string BefundlistenPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Weichen\";
        private string RückmeldungsPath = "";
        private string WeichenwartungRückmeldungsPath = "";
        private string WeichenwartungArbeitsvorratPath = "";
        private string WeichenwartungSyncPath = "";
        private string startmodus = "Inspektion";
        private bool wartungssynchronisationLaeuft;

        private bool IstWeichenwartung => (Bearbeiter.SelectedItem as ComboBoxItem)?.Content?.ToString() == "Weichenwartung";
        private string AktuellerBefundpfad => IstWeichenwartung ? WeichenwartungRückmeldungsPath : RückmeldungsPath;
        private string lastSavedPhotoPath = "";
        private int bilderZaehler = 0;
        private string BilderPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Weichen\40_Bilder";
        private string SyncPath = @"\\remote-server\folder";
        private bool isConnected = false;
        private List<string> Befundliste = new List<string>();

        private DataTable dataTable;
        //private string AktuellesDatum;
        //private string ausgewählterBearbeiter;
        //private string Anlagennr;
        //private string SAPNr;
        //private string Art;
        //private string Typ;
        //private string Einbauort;
        //private string EinbauUrWeiche;
        //private string Erneuerung;
        //private string Stammgleis;
        //private string Zweiggleis;
        //private string LetzteInstandhaltung;
        //private string Status;
        //private string Kommentare;

        public string AktuelleZeit => DateTime.Now.ToString("HH:mm");

        private DispatcherTimer aktualisierungsTimer;

        public MainWindow()
        {
            InitializeComponent();
            dataTable = new DataTable();

            LoadSettings();
            StartmodusAnwenden();

            LoadBefundliste();

            // Timer für die Uhrzeit bei Aktualisierung
            aktualisierungsTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(60)
            };
            aktualisierungsTimer.Tick += Timer_Tick;
            aktualisierungsTimer.Start();
            Loaded += async (_, _) => await UpdateFileStatusAsync();
        }

        private void StartmodusAnwenden()
        {
            try
            {
                var modus = StartmodusAuswahl.Bestimmen(startmodus, Environment.GetCommandLineArgs().Skip(1));
                Bearbeiter.SelectedItem = modus == Startmodus.Wartung
                    ? Bearbeiter.Items.OfType<ComboBoxItem>().First(item => item.Content?.ToString() == "Weichenwartung")
                    : null;
            }
            catch (ArgumentException ex)
            {
                Bearbeiter.SelectedItem = null;
                MessageBox.Show($"{ex.Message}\nDie Anwendung startet im Inspektionsmodus ohne vorausgewählten Bearbeiter.",
                    "Startmodus", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadBefundliste()
        {

            if (File.Exists(BefundlistenPath))
            {
                try
                {
                    // Lese alle Zeilen aus der Datei
                    string[] lines = File.ReadAllLines(BefundlistenPath);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                        {
                            continue;
                        }
                        else
                        {
                            Befundliste.Add(line);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Laden der Befundliste: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Die Befundlisten-Datei wurde nicht gefunden.");
                
            }
            //MessageBox.Show("Laden von Befunden erfolgreich. Anzahl: " + Befundliste.Count);
        }

        // Methode zum Laden der Einstellungen aus der Textdatei
        private void LoadSettings()
        {
            if (File.Exists(settingsFilePath))
            {
                try
                {
                    // Registrierung von zusätzlichen Encodings, falls nötig (z.B. für Windows-1252)
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                    // Lese alle Zeilen aus der Datei
                    string[] lines = File.ReadAllLines(settingsFilePath);
                    foreach (string line in lines)
                    {
                        if (line.StartsWith("#"))
                        {
                            continue;
                        }
                        // Spalte die Zeilen in Schlüssel und Wert auf
                        string[] keyValue = line.Split('=');
                        if (keyValue.Length == 2)
                        {
                            string key = keyValue[0].Trim();
                            string value = keyValue[1].Trim();

                            // Überprüfe den Schlüssel und wende die Einstellungen an
                            if (key == "ArbeitsvorratPath")
                            {
                                this.ArbeitsvorratPath = value;
                                Console.WriteLine($"ArbeitsvorratPath: {value}");
                            }
                            else if (key == "BefundlistenPath")
                            {
                                this.BefundlistenPath = value + "\\Befundliste.txt";
                                Console.WriteLine($"BefundlistenPath: {value}");
                            }
                            else if (key == "RückmeldungsPath" || key == "RÃ¼ckmeldungsPath")
                            {
                                this.RückmeldungsPath = value;
                                Console.WriteLine($"RückmeldungsPath: {value}");
                            }
                            else if (key == "Startmodus")
                            {
                                startmodus = value;
                            }
                            else if (key == "WeichenwartungArbeitsvorratPath")
                            {
                                WeichenwartungArbeitsvorratPath = value;
                            }
                            else if (key == "WeichenwartungRückmeldungsPath" || key == "WeichenwartungRÃ¼ckmeldungsPath")
                            {
                                WeichenwartungRückmeldungsPath = value;
                            }
                            else if (key == "WeichenwartungSyncPath")
                            {
                                WeichenwartungSyncPath = value;
                            }
                            else if (key == "SyncPath")
                            {
                                this.SyncPath = value;
                                Console.WriteLine($"SyncPath: {value}");
                            }
                            else if (key == "BilderPath")
                            {
                                this.BilderPath = value;
                                Console.WriteLine($"BilderPath: {value}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Laden der Einstellungen: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Die Einstellungen-Datei wurde nicht gefunden. Eine neue Datei wird angelegt, bitte dort die korrekten Pfade hinterlegen und die Anwendung neu starten.");
                SaveSettings("C:\\", "D:\\", "E:\\");
            }
        }

        // Methode zum Speichern der Einstellungen in die Textdatei
        private void SaveSettings(string pfad1, string pfad2, string pfad3)
        {
            try
            {
                // Erstelle den Inhalt der Datei
                string[] lines = {
                    $"# Settingsfile für Weichen-Checkliste",
                    $"ArbeitsvorratPath = {pfad1}",
                    $"BefundlistenPath = {pfad2}",
                    $"RückmeldungsPath = {pfad3}",
                    $"SyncPath = {SyncPath}",
                    $"BilderPath = {BilderPath}",
                    $"WeichenwartungArbeitsvorratPath = {WeichenwartungArbeitsvorratPath}",
                    $"WeichenwartungRückmeldungsPath = {WeichenwartungRückmeldungsPath}",
                    $"WeichenwartungSyncPath = {WeichenwartungSyncPath}",
                    $"Startmodus = {startmodus}"
                };
                // Schreibe die Zeilen in die Datei
                Directory.CreateDirectory(Path.GetDirectoryName(settingsFilePath)!);
                File.WriteAllLines(settingsFilePath, lines);
                MessageBox.Show("Einstellungen wurden erfolgreich gespeichert.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern der Einstellungen: {ex.Message}");
            }
        }

        // Event-Handler für den "Laden"-Button
        private void Laden_Click(object sender, RoutedEventArgs e)
        {
            DataTable dt = new DataTable();

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV und Excel (*.csv;*.xlsx)|*.csv;*.xlsx|" +
                "CSV (*.csv)|*.csv|" +
                "Excel (*.xlsx)|*.xlsx";
            openFileDialog.InitialDirectory = IstWeichenwartung ? WeichenwartungArbeitsvorratPath : ArbeitsvorratPath;

            if (openFileDialog.ShowDialog() != true) return;

            try
            {
                string filePath = openFileDialog.FileName;
                string extension = Path.GetExtension(filePath).ToLowerInvariant();
                dt = extension switch
                {
                    ".csv" => LoadCsv(filePath),
                    ".xlsx" => LoadExcel(filePath),
                    _ => throw new InvalidDataException("Bitte eine CSV- oder XLSX-Datei auswählen.")
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Laden der Datei: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (dt == null)
            {
                MessageBox.Show($"Fehler beim Laden der Datei: ", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;           
            }

            // Füge eine neue Spalte für den Status hinzu
            //dt.Columns.Add("Status", typeof(string));
            DataColumn statusCol = new DataColumn("Status", typeof(string));
            dt.Columns.Add(statusCol);
            statusCol.SetOrdinal(0); // <- Verschiebt sie an die erste Position

            // Setze den Status für jede Zeile auf "Nicht bearbeitet"
            foreach (DataRow row in dt.Rows)
            {
                row["Status"] = "Nicht bearbeitet";
            }

            // DataGrid mit der DataTable füllen
            Arbeitsvorrat.ItemsSource = dt.DefaultView;
        }

        // Funktion zum Laden der CSV-Datei
        private DataTable LoadCsv(string filePath)
        {
            DataTable dt = new DataTable();
            string[] lines = CsvKodierung.ZeilenLesen(filePath);

            if (lines.Length > 0)
            {
                // Erste Zeile enthält die Spaltenüberschriften
                string[] headers = lines[0].Split(';');

                foreach (string header in headers)
                {
                    dt.Columns.Add(new DataColumn(header));
                }

                // Datenzeilen ab der zweiten Zeile hinzufügen
                for (int i = 1; i < lines.Length; i++)
                {
                    string[] rowData = lines[i].Split(';');
                    dt.Rows.Add(rowData);
                }

                return dt;
            }
            return null;
        }


        // Funktion zum Laden der Excel-Datei mit ClosedXML
        private DataTable LoadExcel(string filePath)
        {
            DataTable dt = new DataTable();

            // Excel-Datei mit ClosedXML öffnen
            using (var workbook = new XLWorkbook(filePath))
            {
                // Nimm das erste Arbeitsblatt
                var worksheet = workbook.Worksheets.FirstOrDefault();

                if (worksheet != null)
                {
                    bool headerRow = true;

                    // Durch alle Zeilen und Spalten des Arbeitsblatts iterieren
                    foreach (var row in worksheet.RowsUsed())
                    {
                        if (headerRow)
                        {
                            // Füge die Spaltenüberschriften aus der ersten Zeile hinzu
                            foreach (var cell in row.CellsUsed())
                            {
                                dt.Columns.Add(cell.Value.ToString());
                            }
                            headerRow = false;
                        }
                        else
                        {
                            // Füge die Datenzeilen hinzu
                            DataRow dataRow = dt.NewRow();
                            int cellIndex = 0;

                            foreach (var cell in row.CellsUsed())
                            {
                                dataRow[cellIndex] = cell.Value.ToString();
                                cellIndex++;
                            }

                            dt.Rows.Add(dataRow);
                        }
                    }

                    return dt;

                }
            }
            return null;
        }

        // Event-Handler für den Zeilenklick im DataGrid
        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Arbeitsvorrat.SelectedItem != null)
            {
                DataRowView selectedRow = (DataRowView)Arbeitsvorrat.SelectedItem;

                //string rowData = string.Join(", ", selectedRow.Row.ItemArray);
                //MessageBox.Show($"Ausgewählte Daten: {rowData}");

                
                AktuellesDatum.Text = DateTime.Now.ToString();
                //Bearbeiter.Text = aktuellerBearbeiter;
                Anlagennr.Text = selectedRow["Anlagennr"].ToString();
                SAPNr.Text = selectedRow["SAP-Nr."].ToString();
                Art.Text = selectedRow["Art"].ToString();
                Typ.Text = selectedRow["Typ"].ToString();
                Einbauort.Text = selectedRow["Einbauort"].ToString();
                EinbauUrWeiche.Text = selectedRow["Einbau Ur-Weiche"].ToString();
                Erneuerung.Text = selectedRow["Erneuerung"].ToString();
                Stammgleis.Text = selectedRow["Stammgleis"].ToString();
                Zweiggleis.Text = selectedRow["Zweiggleis"].ToString();
                try
                {
                    LetzteInstandhaltung.Text = selectedRow["LETZTE_INSTANDHALTUNG"].ToString();
                    GW201_ID1.Text = selectedRow["GW201_ID1"].ToString();
                }
                catch (Exception ex)
                {
                    LetzteInstandhaltung.Text = "";
                    GW201_ID1.Text = "";
                    
                }
                Kommentare.Text = "ohne Auffälligkeit";

                if (Bearbeiter.Equals(""))
                {
                    MessageBox.Show($"Ein Bearbeiter muss eingetragen werden");
                }
            }
        }

        // Event-Handler für den "Speichern"-Button
        private void Speichern_Click(object sender, RoutedEventArgs e)
        {
            if (Bearbeiter.Text.Equals("") || AktuellesDatum.Text.Equals("") || Anlagennr.Text.Equals("")) {
                MessageBox.Show("Bitte Datum/Bearbeiter/Anlagennr./Befund ausfüllen. Es konnte nicht gespeichert werden.");
            }
            else
            {
                try
                {
                    string iso8601 = DateOnly.ParseExact(AktuellesDatum.Text, "dd.MM.yyyy", CultureInfo.InvariantCulture).ToString("yyyyMMdd");
                    string ampel = "grün";
                    if (IstWeichenwartung)
                    {
                        PruefeWartungspfade(false);
                        SaveToExcel(Anlagennr.Text, iso8601, Bearbeiter.Text, ampel, "Wartung durchgeführt", WeichenwartungRückmeldungsPath);
                    }
                    else if (Kommentare.Text.Equals("ohne Auffälligkeit"))
                    {
                        SaveToExcel(Anlagennr.Text, iso8601, Bearbeiter.Text, ampel, Kommentare.Text);
                    }
                    else {
                        ampel = "gelb";
                        string[] teile = Kommentare.Text.Split(';');
                        foreach (var item in teile)
                        {
                            SaveToExcel(Anlagennr.Text, iso8601, Bearbeiter.Text, ampel, item);
                        }
                    }
                    try
                    {
                        DataRowView selectedRow = (DataRowView)Arbeitsvorrat.SelectedItem;
                        if (selectedRow != null)
                        {
                            selectedRow["Status"] = "gespeichert";
                            
                        }
                    }
                    catch (Exception ex)
                    {
                        //keine Zeile selektiert. Kein Fehler
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Fehler beim Speichern: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Funktion zum Speichern in Excel
        private void SaveToExcel(string Weichennummer, string Datum, string Bearbeiter, string Status, string Kommentare, string? zielordner = null)
        {
            BefundDateien.Speichern(zielordner ?? RückmeldungsPath, Weichennummer, Datum, Bearbeiter, Status, Kommentare);
            MessageBox.Show("Eingaben wurden als Excel gespeichert.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PruefeWartungspfade(bool mitRemote)
        {
            var anderePfade = new List<string> { RückmeldungsPath, BilderPath, ArbeitsvorratPath };
            if (!string.IsNullOrWhiteSpace(SyncPath))
            {
                anderePfade.Add(Path.Combine(SyncPath, "20_Arbeitsnachbereitung"));
                anderePfade.Add(Path.Combine(SyncPath, "40_Bilder"));
                anderePfade.Add(Path.Combine(SyncPath, "10_Arbeitsvorbereitung"));
            }
            BefundDateien.PruefeGetrenntenPfad(WeichenwartungRückmeldungsPath, anderePfade.ToArray());
            if (!string.IsNullOrWhiteSpace(WeichenwartungArbeitsvorratPath))
            {
                BefundDateien.PruefeGetrenntenPfad(WeichenwartungArbeitsvorratPath, anderePfade.ToArray());
                BefundDateien.PruefeGetrenntenPfad(WeichenwartungArbeitsvorratPath, WeichenwartungRückmeldungsPath);
            }
            if (mitRemote)
            {
                anderePfade.Add(WeichenwartungRückmeldungsPath);
                anderePfade.Add(WeichenwartungArbeitsvorratPath);
                BefundDateien.PruefeGetrenntenPfad(WeichenwartungSyncPath, anderePfade.ToArray());
                BefundDateien.PruefeGetrenntenPfad(WeichenwartungArbeitsvorratPath, WeichenwartungSyncPath);
            }
        }

        private readonly SemaphoreSlim syncSperre = new(1, 1);

        private IReadOnlyList<SynchronisationsAuftrag> SynchronisationsAuftraege()
        {
            return new[]
            {
                new SynchronisationsAuftrag("Inspektion: Arbeitsvorrat",
                    Path.Combine(SyncPath, "10_Arbeitsvorbereitung"), ArbeitsvorratPath, true),
                new SynchronisationsAuftrag("Inspektion: Befunde",
                    Path.Combine(SyncPath, "20_Arbeitsnachbereitung"), RückmeldungsPath, false),
                new SynchronisationsAuftrag("Inspektion: Bilder",
                    Path.Combine(SyncPath, "40_Bilder"), BilderPath, false),
                new SynchronisationsAuftrag("Wartung: Arbeitsvorrat",
                    Path.Combine(WeichenwartungSyncPath, "50_WeichenwartungAV"),
                    WeichenwartungArbeitsvorratPath, true),
                new SynchronisationsAuftrag("Wartung: Befunde",
                    Path.Combine(WeichenwartungSyncPath, "60_WeichenwartungR"),
                    WeichenwartungRückmeldungsPath, false)
            };
        }

        // Event-Handler für Neuen Befund
        private void BefundNeu_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("neuen Befund wählen und einfügen", "neu", MessageBoxButton.OKCancel, MessageBoxImage.Information);

            string neuerBefund = ShowBefundAuswahl(Befundliste);
            if (neuerBefund != null)
            {
                //MessageBox.Show($"Sie haben '{neuerBefund}' ausgewählt.");
                if (Kommentare.Text.Equals("ohne Auffälligkeit") || Kommentare.Text.Equals(""))
                {
                    Kommentare.Text = neuerBefund;
                }else
                {
                    Kommentare.Text += ";\n" + neuerBefund;
                }
            }
            else
            {
                MessageBox.Show("Keine Auswahl getroffen.");
            }
        }

        // Event-Handler für Einstellungen
        private void Einstellungen_Click(object sender, EventArgs e)
        {
            // Beispielhafte Pfade
            string newPfad1 = @"C:\Users\eripl\source\repos\Weichen-Checkliste\Weichen-Checkliste\bin\Debug\net8.0-windows";
            string newPfad2 = "D:\\NeuerPfad2";
            string newPfad3 = "E:\\NeuerPfad3";

            // Rufe die Methode zum Speichern der Einstellungen auf
            SaveSettings(newPfad1, newPfad2, newPfad3);
            // Lade die Einstellungen erneut, um sicherzustellen, dass sie angewendet werden
            LoadSettings();
        }

        public string ShowBefundAuswahl(List<string> items)
        {
            var selectionWindow = new BefundAuswahlFenster(items);
            bool? result = selectionWindow.ShowDialog();

            return result == true ? selectionWindow.SelectedItem : null;
        }

        private async void Foto_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("smile :-)");

            // öffne das Foto-Fenster
            FotoWindow fotoWindow = new FotoWindow(BilderPath, Anlagennr.Text);
            fotoWindow.Owner = this; // Setze das Hauptfenster als Besitzer
            fotoWindow.ShowDialog();
        }


        private async void Timer_Tick(object sender, EventArgs e)
        {
            await UpdateFileStatusAsync();
        }

        private async Task UpdateFileStatusAsync()
        {
            // Kein paralleler Sync, auch nicht durch schnelles Klicken auf "aktualisieren".
            if (!await syncSperre.WaitAsync(0)) return;
            try
            {
                var ergebnisse = await Task.Run(() =>
                    SynchronisationsDienst.Ausfuehren(SynchronisationsAuftraege()));
                var fehler = ergebnisse.Where(e => !e.Erfolgreich).ToList();
                bool inspektionOk = ergebnisse.Where(e => e.Name.StartsWith("Inspektion:"))
                    .All(e => e.Erfolgreich);
                bool wartungOk = ergebnisse.Where(e => e.Name.StartsWith("Wartung:"))
                    .All(e => e.Erfolgreich);

                ConnectionStatus.Text = fehler.Count == 0 ? "Verbunden" : "Teilweise / nicht verbunden";
                StatusMessage.Text = fehler.Count == 0
                    ? $"Synchronisiert um {DateTime.Now:HH:mm}"
                    : $"Synchronisation: {fehler.Count} von {ergebnisse.Count} Datenflüssen fehlerhaft";
                StatusMessage.ToolTip = fehler.Count == 0 ? null
                    : string.Join("\n", fehler.Select(e => e.Name + ": " + e.Fehler));
                WartungStatus.Text = wartungOk ? "Wartung: synchronisiert" : "Wartung: Sync unvollständig";
                WartungStatus.ToolTip = wartungOk ? null
                    : string.Join("\n", fehler.Where(e => e.Name.StartsWith("Wartung:"))
                        .Select(e => e.Name + ": " + e.Fehler));

                FileCount.Text = Directory.Exists(AktuellerBefundpfad)
                    ? Directory.GetFiles(AktuellerBefundpfad).Length.ToString() : "0";
                FotoCount.Text = Directory.Exists(BilderPath)
                    ? Directory.GetFiles(BilderPath).Length.ToString() : "0";
            }
            catch (Exception ex)
            {
                ConnectionStatus.Text = "Nicht verbunden";
                StatusMessage.Text = "Synchronisationsfehler";
                StatusMessage.ToolTip = ex.ToString();
            }
            finally
            {
                syncSperre.Release();
            }
        }

        private string GetNextFileNumber(string directory, string filePattern)
        {
            var files = Directory.GetFiles(directory, filePattern);
            int max = 0;
            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                var parts = name.Split('_');
                if (parts.Length > 0)
                {
                    var last = parts[^1];
                    if (int.TryParse(last, out int num))
                        if (num > max) max = num;
                }
            }
            return (max + 1).ToString("D7");
        }

        private void FotoFolder_Click(object sender, RoutedEventArgs e)
        {
            string pfad = BilderPath; 

            try
            {
                // Prüfen, ob der Ordner existiert, sonst anlegen
                if (!Directory.Exists(pfad))
                {
                    Directory.CreateDirectory(pfad);
                }
                // Öffnet den Windows-Explorer im angegebenen Ordner
                Process.Start("explorer.exe", pfad);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen des Ordners:\n{ex.Message}");
            }
        }

        private void BefundeFolder_Click(object sender, RoutedEventArgs e)
        {
            string pfad = AktuellerBefundpfad;

            try
            {
                // Prüfen, ob der Ordner existiert, sonst anlegen
                if (!Directory.Exists(pfad))
                {
                    Directory.CreateDirectory(pfad);
                }
                // Öffnet den Windows-Explorer im angegebenen Ordner
                Process.Start("explorer.exe", pfad);
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Fehler beim Öffnen des Ordners:\n{ex.Message}");
            }
        }

        private void UpdateStatus_Click(object sender, RoutedEventArgs e)
        {
            _ = UpdateFileStatusAsync();
        }
    }
}
