using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using Giffer.Properties;
using Microsoft.VisualBasic.FileIO;
using Cursor = System.Windows.Input.Cursor;
using Cursors = System.Windows.Input.Cursors;
using MessageBox = System.Windows.MessageBox;
using SearchOption = System.IO.SearchOption;

namespace Giffer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private bool _switching;
        private bool _autoPlay;
        private readonly Thickness _originalMediaMargin;
        private readonly Brush _originalBackground;
        private readonly Cursor _originalCursor;

        private string Path
        {
            get { return _path ?? (_path = Settings.Path); }
            set
            {
                _path = txt_Directory.Text = Settings.Path = value;
                Settings.Save();
                LoadImage();
            }
        }
        private string _path;

        private List<FileInfo> Images
        {
            get
            {
                try
                {
                    if (_images != null) return _images;
                    if (string.IsNullOrEmpty(Path)) return _images = new List<FileInfo>();
                    _images = GetImagesFromDirectory(new DirectoryInfo(Path));
                    return _images;
                }
                catch (Exception e) {
                    MessageBox.Show("Error gathering images:\n" + e.Message, "Giffer Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return new List<FileInfo>();
                }
            }
        }

        private List<FileInfo> GetImagesFromDirectory(DirectoryInfo dir)
        {
            var list = new List<FileInfo>();
            try
            {
                list.AddRange(dir.GetFiles("*.gif"));
                foreach (var subDir in dir.GetDirectories()) list.AddRange(GetImagesFromDirectory(subDir));
            }
            catch (Exception e) {
                Console.WriteLine("Error gathering files for " + dir.FullName + ":\n" + e.Message);
            }
            return list;
        }

        private List<FileInfo> _images;

        private FileInfo Image
        {
            get { return _image; }
            set
            {
                _image = value;
                LoadImage();
            }
        }
        private FileInfo _image;

        private Uri Uri {  get { return Image == null ? null : new Uri(Image.FullName, UriKind.Absolute); } }
        private static Settings Settings {  get { return Settings.Default; } }

        public MainWindow()
        {
            InitializeComponent();
            if (string.IsNullOrEmpty(Path)) return;
            txt_Directory.Text = Path;
            _originalMediaMargin = media.Margin;
            _originalBackground = Background;
            _originalCursor = Cursor;
            Reset();
        }

        private void Reset()
        {
            media.Source = null;
            media.Visibility = Visibility.Hidden;
            _images = null;
            NextImage();
        }

        private void LoadImage()
        {
            media.Visibility = Visibility.Visible;
            media.Source = Uri;
            media.Play();
        }

        private void NextImage()
        {
            if (Images.Count == 0 || Images.All(i => i == null)) return;
            Image = Images.GetNext();
            while (Image == null) Image = Images.GetNext();
        }

        private void PrevImage()
        {
            if (Images.Count == 0 || Images.All(i => i == null)) return;
            Image = Images.GetPrevious();
            while (Image == null) Image = Images.GetPrevious();
        }

        private void ToggleMinimized() { WindowState = WindowState == WindowState.Minimized ? WindowState.Normal : WindowState.Minimized; }

        private void ToggleFullscreen() { WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized; }

        private void btn_ChangeDirectory_Click(object sender, RoutedEventArgs e)
        {
            media.Source = Uri;
            var path = Path;
            while (!string.IsNullOrEmpty(path) && !Directory.Exists(path)) path = System.IO.Path.GetDirectoryName(path);
            var dlg = new FolderBrowserDialog {SelectedPath = string.IsNullOrEmpty(path) ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) : path};
            if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;
            Path = dlg.SelectedPath;
            Reset();
        }

        private void image_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (_autoPlay) NextImage();
            else
            {
                media.Position = new TimeSpan(0, 0, 1);
                media.Play();
            }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (_switching) return;

            if (WindowState == WindowState.Maximized)
            {
                WindowStyle = WindowStyle.None;
                Background = Brushes.Black;
                media.Margin = new Thickness();
                txt_Directory.Visibility = Visibility.Hidden;
                btn_ChangeDirectory.Visibility = Visibility.Hidden;
                _switching = true;
                WindowState = WindowState.Normal;
                WindowState = WindowState.Maximized;
                _switching = false;
                Cursor = Cursors.None;
            }
            else
            {
                WindowStyle = WindowStyle.SingleBorderWindow;
                media.Margin = _originalMediaMargin;
                Background = _originalBackground;
                txt_Directory.Visibility = Visibility.Visible;
                btn_ChangeDirectory.Visibility = Visibility.Visible;
                Cursor = _originalCursor;
            }
        }

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key) {
                case Key.P:
                case Key.A:
                    _autoPlay = !_autoPlay;
                    break;
                case Key.Home:
                case Key.PageUp:
                case Key.Left:
                    PrevImage();
                    break;
                case Key.End:
                case Key.Right:
                case Key.Space:
                case Key.PageDown:
                    NextImage();
                    break;
                case Key.Down:
                    ToggleMinimized();
                    break;
                case Key.F11:
                case Key.Up:
                case Key.Enter:
                    ToggleFullscreen();
                    break;
                case Key.Escape:
                    if (WindowState == WindowState.Maximized) WindowState = WindowState.Normal;
                    break;
                case Key.Delete:
                case Key.Back:
                    var index = Images.IndexOf(Image);
                    Images[index] = null;
                    FileSystem.DeleteFile(Image.FullName, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                    NextImage();
                    break;
            }
        }

        private void btn_Media_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ToggleFullscreen();
            PrevImage();
            e.Handled = true;
        }

        private void btn_Media_Click(object sender, RoutedEventArgs e)
        {
            if (e.Handled) return;
            NextImage();
            e.Handled = true;
        }
    }
}
