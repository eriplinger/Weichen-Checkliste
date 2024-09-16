

using ClosedXML.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using Irony;
using Microsoft.Win32;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Weichen_Checkliste
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DataTable dataTable;
        //private string AktuellesDatum;
        private string Bearbeiter;
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
        }


        // Event-Handler für den "Laden"-Button
        private void Laden_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV files (*.csv)|*.csv|Excel files (*.xlsx)|*.xlsx";

            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string extension = Path.GetExtension(filePath).ToLower();

                // Unterscheidung zwischen CSV und Excel basierend auf der Dateiendung
                if (extension == ".csv")
                {
                    LoadCsv(filePath);
                }
                else if (extension == ".xlsx")
                {
                    LoadExcel(filePath);
                }
            }
        }

        // Funktion zum Laden der CSV-Datei
        private void LoadCsv(string filePath)
        {
            DataTable dt = new DataTable();
            string[] lines = File.ReadAllLines(filePath);

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

                // DataGrid mit der DataTable füllen
                Arbeitsvorrat.ItemsSource = dt.DefaultView;
            }
        }


        // Funktion zum Laden der Excel-Datei mit ClosedXML
        private void LoadExcel(string filePath)
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

                    // DataGrid mit der DataTable füllen
                    Arbeitsvorrat.ItemsSource = dt.DefaultView;
                }
            }
        }

        // Event-Handler für den Zeilenklick im DataGrid
        private void dataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Arbeitsvorrat.SelectedItem != null)
            {
                DataRowView selectedRow = (DataRowView)Arbeitsvorrat.SelectedItem;

                string rowData = string.Join(", ", selectedRow.Row.ItemArray);
                MessageBox.Show($"Ausgewählte Daten: {rowData}");

                Anlagennr.Text = selectedRow["Anlagennr"].ToString();
                Art.Text = selectedRow["Art"].ToString();
                Typ.Text = selectedRow["Typ"].ToString();
                Einbauort.Text = selectedRow["Einbauort"].ToString();
                EinbauUrWeiche.Text = selectedRow["Einbau Ur-Weiche"].ToString();
                Erneuerung.Text = selectedRow["Erneuerung"].ToString();
                Stammgleis.Text = selectedRow["Stammgleis"].ToString();
                Zweiggleis.Text = selectedRow["Zweiggleis"].ToString();
                LetzteInstandhaltung.Text = selectedRow["LETZTE_INSTANDHALTUNG"].ToString();
                Status.Text = selectedRow["GW201_ID1"].ToString();
                Anlagennr.Text = selectedRow["Anlagennr"].ToString();
                Anlagennr.Text = selectedRow["Anlagennr"].ToString();
                Anlagennr.Text = selectedRow["Anlagennr"].ToString();

                //SAP - Nr
                //Art
                //Typ
                //Einbauort
                //EinbauUrWeiche
                //Erneuerung
                //Stammgleis
                //Zweiggleis
                //LetzteInstandhaltung
                //Status
            }
        }

        // Event-Handler für den "Speichern"-Button
        private void Speichern_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("noch nicht implementiert");
        }
    }
}