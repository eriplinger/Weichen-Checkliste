

using ClosedXML.Excel;
using Microsoft.Win32;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using System.Text;
using WpfWebcamApp;
using DocumentFormat.OpenXml.CustomProperties;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Weichen_Checkliste
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Der Pfad zur Textdatei mit den Einstellungen
        private readonly string settingsFilePath = "settings.txt";
        private string ArbeitsvorratPath;
        private string BefundlistenPath;
        private string RückmeldungsPath;
        private List<string> Befundliste;


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

        public MainWindow()
        {
            InitializeComponent();
            dataTable = new DataTable();

            LoadSettings();

            LoadBefundliste();
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
                        if (line.StartsWith("#"))
                        {
                            continue;
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
        }

        // Methode zum Laden der Einstellungen aus der Textdatei
        private void LoadSettings()
        {
            if (File.Exists(settingsFilePath))
            {
                try
                {
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
                                this.BefundlistenPath = value;
                                Console.WriteLine($"BefundlistenPath: {value}");
                            }
                            else if (key == "RückmeldungsPath")
                            {
                                this.RückmeldungsPath = value;
                                Console.WriteLine($"RückmeldungsPath: {value}");
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
                    $"RückmeldungsPath = {pfad3}"
                };
                // Schreibe die Zeilen in die Datei
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
            openFileDialog.Filter = "CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx";
            openFileDialog.InitialDirectory = ArbeitsvorratPath;

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string extension = Path.GetExtension(filePath).ToLower();

                // Unterscheidung zwischen CSV und Excel basierend auf der Dateiendung
                if (extension == ".csv")
                {
                    dt = LoadCsv(filePath);
                }
                else if (extension == ".xlsx")
                {
                    dt = LoadExcel(filePath);
                }
            }

            if (dt == null)
            {
                MessageBox.Show($"Fehler beim Laden der Datei: ", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
                return;           
            }

            // Füge eine neue Spalte für den Status hinzu
            dt.Columns.Add("Status", typeof(string));

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
            // Registrierung von zusätzlichen Encodings, falls nötig (z.B. für Windows-1252)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            DataTable dt = new DataTable();
            string[] lines = File.ReadAllLines(filePath, Encoding.GetEncoding("Windows-1252"));

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
                Kommentare.Text = "ohne Befund";

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
                    if (!Kommentare.Text.Equals("ohne Befund"))
                    {
                        ampel = "gelb";
                    }
                    SaveToExcel(Anlagennr.Text, iso8601, Bearbeiter.Text, ampel, Kommentare.Text);
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
                    //Todo
                }
            }
        }

        // Funktion zum Speichern in Excel
        private void SaveToExcel(string Weichennummer, string Datum, string Bearbeiter, string Status, string Kommentare)
        {
            try
            {
                // Neues Excel-Workbook und Worksheet erstellen
                using (var workbook = new XLWorkbook())
                {
                    var worksheet = workbook.Worksheets.Add(Weichennummer + "_" + Datum);

                    // Daten in die Zellen schreiben
                    worksheet.Cell(1, 1).Value = "Datum";
                    worksheet.Cell(1, 2).Value = "Bearbeiter";
                    worksheet.Cell(1, 3).Value = "Status";
                    worksheet.Cell(1, 4).Value = "Kommentare";

                    worksheet.Cell(2, 1).Value = Datum;
                    worksheet.Cell(2, 2).Value = Bearbeiter;
                    worksheet.Cell(2, 3).Value = Status;
                    worksheet.Cell(2, 4).Value = Kommentare;

                    // Excel-Datei speichern
                    string filePath = this.RückmeldungsPath + Weichennummer + "_" + Datum + ".xlsx";
                    workbook.SaveAs(filePath);
                }

                MessageBox.Show("Eingaben wurden als Excel gespeichert.", "Erfolg", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Fehler beim Speichern der Excel-Datei: {ex.Message}", "Fehler", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Event-Handler für Fotos
        private void Foto_Click(object sender, EventArgs e)
        {
            //MessageBox.Show("Smile");
            FotoWindow fw = new FotoWindow();
            fw.Activate();
        }

        // Event-Handler für Neuen Befund
        private void BefundNeu_Click(object sender, EventArgs e)
        {
            MessageBox.Show("neuen Befund wählen und einfügen", "neu", MessageBoxButton.OKCancel, MessageBoxImage.Information);
        }

        // Event-Handler für Einstellungen
        private void Einstellungen_Click(object sender, EventArgs e)
        {
            // Beispielhafte Pfade (diese könnten von einer Benutzeroberfläche kommen)
            string newPfad1 = @"C:\Users\eripl\source\repos\Weichen-Checkliste\Weichen-Checkliste\bin\Debug\net8.0-windows";
            string newPfad2 = "D:\\NeuerPfad2";
            string newPfad3 = "E:\\NeuerPfad3";

            // Rufe die Methode zum Speichern der Einstellungen auf
            SaveSettings(newPfad1, newPfad2, newPfad3);
            // Lade die Einstellungen erneut, um sicherzustellen, dass sie angewendet werden
            LoadSettings();
        }
    }
}