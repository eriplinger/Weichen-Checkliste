using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Weichen_Checkliste
{
    /// <summary>
    /// Interaktionslogik für BefundAuswahlFenster.xaml
    /// </summary>
    public partial class BefundAuswahlFenster : Window
    {
        public string SelectedItem { get; private set; }

        public BefundAuswahlFenster(List<string> items)
        {
            InitializeComponent();
            ItemListBox.ItemsSource = items;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedItem = ItemListBox.SelectedItem as string;
            DialogResult = true; // Schließt das Fenster und signalisiert Erfolg
        }
    }
}
