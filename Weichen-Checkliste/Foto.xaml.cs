using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;

namespace WpfWebcamApp
{
    public partial class FotoWindow : Window
    {
        private readonly FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private bool isClosing = false; // Flag zum Abbruch von NewFrame

        public FotoWindow()
        {
            InitializeComponent();

            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);

            if (videoDevices.Count == 0)
            {
                MessageBox.Show("Keine Webcam gefunden!");
                Close();
                return;
            }

            videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
            videoSource.NewFrame += Video_NewFrame;
        }

        private void Video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (isClosing) return; // Falls Fenster im Schließen → Frame ignorieren

            try
            {
                using Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();
                using MemoryStream memoryStream = new MemoryStream();
                bitmap.Save(memoryStream, ImageFormat.Bmp);

                // Sichere Kopie des Streams
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = new MemoryStream(memoryStream.ToArray());
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                // Nicht blockierend aktualisieren
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!isClosing && cameraFeed != null)
                        cameraFeed.Source = bitmapImage;
                }));
            }
            catch
            {
                // Fehler ignorieren oder loggen
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (videoSource != null && !videoSource.IsRunning)
                videoSource.Start();
        }

        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            if (cameraFeed.Source is BitmapSource bitmapSource)
                SavePhoto(bitmapSource);
        }

        private void SavePhoto(BitmapSource bitmapSource)
        {
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

            string filePath = $"Foto_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
            using FileStream fileStream = new FileStream(filePath, FileMode.Create);
            encoder.Save(fileStream);

            MessageBox.Show($"Foto gespeichert: {filePath}");
        }

        protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            isClosing = true;
            await StopCameraAsync();
            base.OnClosing(e);
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            isClosing = true;
            await StopCameraAsync();
            Close();
        }

        private async Task StopCameraAsync()
        {
            try
            {
                if (videoSource != null)
                {
                    videoSource.NewFrame -= Video_NewFrame;

                    if (videoSource.IsRunning)
                    {
                        videoSource.SignalToStop();
                        await Task.Run(() => videoSource.WaitForStop()); // verhindert UI-Blockade
                    }
                }
            }
            catch
            {
                // optional: Logging
            }
            finally
            {
                videoSource = null;
                if (cameraFeed != null)
                    cameraFeed.Source = null;
            }
        }
    }
}
