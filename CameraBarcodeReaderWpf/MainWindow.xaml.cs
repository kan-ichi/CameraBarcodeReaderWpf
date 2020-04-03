using AForge.Video;
using AForge.Video.DirectShow;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using ZXing;

namespace CameraBarcodeReader.Wpf
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private FilterInfoCollection _filterInfoCollection_;
        private VideoCaptureDevice _captureDevice_;
        private Bitmap _readBitmap_;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this._filterInfoCollection_ = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo filterInfo in this._filterInfoCollection_) this.CameraDevice.Items.Add(filterInfo.Name);
            this.CameraDevice.SelectedIndex = 0;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (this._captureDevice_ == null) return;

            // 参考URL：http://robot-factory.blogspot.com/2013/10/c-web.html
            this._captureDevice_.NewFrame -= CaptureDevice_NewFrame;
            this._captureDevice_.SignalToStop();
            this._captureDevice_.WaitForStop();
            this._captureDevice_ = null;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (this._captureDevice_ != null)
            {
                if (this._captureDevice_.IsRunning) return;
            }

            this.BarcodeFormat.Text = string.Empty;
            this.BarcodeText.Text = string.Empty;

            this._captureDevice_ = new VideoCaptureDevice(this._filterInfoCollection_[this.CameraDevice.SelectedIndex].MonikerString);
            this._captureDevice_.NewFrame += CaptureDevice_NewFrame;
            this._captureDevice_.Start();
        }

        private void CaptureDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            if (!(this._captureDevice_.IsRunning)) return;

            this._readBitmap_ = (Bitmap)eventArgs.Frame.Clone();
            if (this._readBitmap_ != null)
            {
                BarcodeReader reader = new BarcodeReader();
                Result result = null;
                try
                {
                    result = reader.Decode(this._readBitmap_);
                }
                catch (Exception ex)
                {
                    return;
                }
                if (result != null)
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        this.BarcodeFormat.Text = result.BarcodeFormat.ToString();
                        this.BarcodeText.Text = result.Text;
                    }));
                    _captureDevice_.SignalToStop();
                }
            }

            // 参考URL：https://ja.coder.work/so/wpf/616847
            try
            {
                System.Drawing.Image img = this._readBitmap_;

                MemoryStream ms = new MemoryStream();
                img.Save(ms, ImageFormat.Bmp);
                ms.Seek(0, SeekOrigin.Begin);
                BitmapImage bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.EndInit();

                bi.Freeze();
                Dispatcher.BeginInvoke(new ThreadStart(delegate
                {
                    this.CameraImage.Source = bi;
                }));
            }
            catch (Exception ex)
            {
                return;
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (this._captureDevice_ == null) return;
            this._captureDevice_.NewFrame -= CaptureDevice_NewFrame;
            this._captureDevice_.SignalToStop();
            this._captureDevice_.WaitForStop();
        }

        private void CopyToClipboardButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetData(DataFormats.Text, this.BarcodeText?.Text ?? string.Empty);
        }
    }
}
