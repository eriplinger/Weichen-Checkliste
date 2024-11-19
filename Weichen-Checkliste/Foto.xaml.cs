using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;

namespace WpfWebcamApp
{
    public partial class FotoWindow : Window
    {
        private FilterInfoCollection videoDevices; // Sammlung aller verfügbaren Videoeingabegeräte (z.B. Webcams)
        private VideoCaptureDevice videoSource;    // Objekt zum Abrufen des Videostreams

        public FotoWindow()
        {
            //InitializeComponent();
            // Alle verfügbaren Videoeingabegeräte abrufen
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevices.Count == 0)
            {
                MessageBox.Show("Keine Webcam gefunden!");
                return;
            }

            // Erstes Gerät als Quelle auswählen
            videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
            videoSource.NewFrame += new NewFrameEventHandler(Video_NewFrame);
        }

        // Ereignishandler für das NewFrame-Ereignis
        private void Video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            // Das Bild von der Kamera abrufen
            Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();

            // In ein BitmapImage umwandeln, um es in WPF anzuzeigen
            MemoryStream memoryStream = new MemoryStream();
            bitmap.Save(memoryStream, ImageFormat.Bmp);
            memoryStream.Seek(0, SeekOrigin.Begin);

            BitmapImage bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();

            bitmapImage.Freeze(); // Erforderlich, um den UI-Thread zu entkoppeln
            Dispatcher.Invoke(() => cameraFeed.Source = bitmapImage); // Bild im UI-Thread anzeigen
        }

        // Startet den Videostream
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (videoSource != null && !videoSource.IsRunning)
            {
                videoSource.Start();
            }
        }

        // Nimmt ein Foto auf und speichert es
        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            if (cameraFeed.Source != null)
            {
                BitmapSource bitmapSource = (BitmapSource)cameraFeed.Source;
                SavePhoto(bitmapSource);
            }
        }

        // Speichert das Foto als JPG-Datei
        private void SavePhoto(BitmapSource bitmapSource)
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            string filePath = "Foto_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".jpg";
            using (FileStream fileStream = new FileStream(filePath, FileMode.Create))
            {
                encoder.Save(fileStream);
            }

            MessageBox.Show($"Foto gespeichert: {filePath}");
        }

        // Beenden des Videostreams beim Schließen des Fensters
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }
            base.OnClosing(e);
        }
    }
}
