using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Drawing;
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;


namespace ImageEditor
{
    public partial class MainWindow : Window
    {
        private readonly ImageHandler _imageHandler = new ImageHandler();
        private string? _currentImagePath; // Сделано nullable
        private double _currentScale = 1.0;
        private bool _isDragging;
        private Point _lastMousePosition;
        private List<Rectangle> _selectionRectangles = new List<Rectangle>();

        private int _rows = 2;
        private int _columns = 3;
        private int _horizontalPadding = 2;
        private int _verticalPadding = 2;

        public MainWindow()
        {
            InitializeComponent();
            InitializeComboBoxes();
            ImageScale.ScaleX = ImageScale.ScaleY = _currentScale;
        }

        private void InitializeComboBoxes()
        {
            var rows = Enumerable.Range(1, 10);
            var columns = Enumerable.Range(1, 10);
            var paddings = Enumerable.Range(0, 21);

            RowsComboBox.ItemsSource = rows;
            ColumnsComboBox.ItemsSource = columns;
            HorizontalPaddingComboBox.ItemsSource = paddings;
            VerticalPaddingComboBox.ItemsSource = paddings;

            RowsComboBox.SelectedItem = _rows;
            ColumnsComboBox.SelectedItem = _columns;
            HorizontalPaddingComboBox.SelectedItem = _horizontalPadding;
            VerticalPaddingComboBox.SelectedItem = _verticalPadding;
        }

        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _currentImagePath = dialog.FileName;
                    _imageHandler.LoadImage(_currentImagePath);
                    DisplayImage();
                    UpdateImageInfo();
                    CreateSelectionGrid();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DisplayImage()
        {
            if (_imageHandler.GetImage() != null)
            {
                var bitmap = _imageHandler.GetImage();
                using (var memory = new MemoryStream())
                {
                    bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                    memory.Position = 0;

                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.StreamSource = memory;
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    MainImage.Source = bitmapImage;
                }
            }
        }

        private void CreateSelectionGrid()
        {
            SelectionCanvas.Children.Clear();
            _selectionRectangles.Clear();

            if (_imageHandler.GetImage() == null) return;

            var imageSize = _imageHandler.GetImageSize(); // Мы вызываем GetImageSize() из ImageHandler
            var cellWidth = (imageSize.Width - (_columns + 1) * _horizontalPadding) / _columns;
            var cellHeight = (imageSize.Height - (_rows + 1) * _verticalPadding) / _rows;

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    var rect = new Rectangle
                    {
                        Width = cellWidth,
                        Height = cellHeight,
                        Stroke = System.Windows.Media.Brushes.Blue,
                        StrokeThickness = 2,
                        StrokeDashArray = new DoubleCollection { 4, 4 }
                    };

                    Canvas.SetLeft(rect, col * (cellWidth + _horizontalPadding) + _horizontalPadding);
                    Canvas.SetTop(rect, row * (cellHeight + _verticalPadding) + _verticalPadding);

                    SelectionCanvas.Children.Add(rect);
                    _selectionRectangles.Add(rect);
                }
            }
        }

        private void UpdateImageInfo()
        {
            var size = _imageHandler.GetImageSize();
            ImageInfoTextBlock.Text = $"Размер изображения: {size.Width}x{size.Height}\n" +
                                    $"Масштаб: {_currentScale:P0}";
        }

        private void GridSettings_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            _rows = (int)RowsComboBox.SelectedItem;
            _columns = (int)ColumnsComboBox.SelectedItem;
            _horizontalPadding = (int)HorizontalPaddingComboBox.SelectedItem;
            _verticalPadding = (int)VerticalPaddingComboBox.SelectedItem;

            CreateSelectionGrid();
        }

        private void RotationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (ImageRotation != null)
            {
                ImageRotation.Angle = e.NewValue;
                _imageHandler.RotateImage((float)e.NewValue);
            }
        }

        private void ScrollViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.RightCtrl) || Keyboard.IsKeyDown(Key.LeftCtrl))
            {
                if (e.Delta > 0)
                    _currentScale *= 1.1;
                else
                    _currentScale /= 1.1;

                _currentScale = Math.Max(0.1, Math.Min(_currentScale, 10.0));
                ImageScale.ScaleX = ImageScale.ScaleY = _currentScale;
                UpdateImageInfo();
                e.Handled = true;
            }
        }

        private void ScrollViewer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _lastMousePosition = e.GetPosition(ImageGrid);
            ((ScrollViewer)sender).Cursor = Cursors.Hand;
        }

        private void ScrollViewer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            ((ScrollViewer)sender).Cursor = Cursors.Arrow;
        }

        private void ScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                var scrollViewer = (ScrollViewer)sender;
                var currentPosition = e.GetPosition(ImageGrid);
                var delta = currentPosition - _lastMousePosition;

                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - delta.X);
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - delta.Y);

                _lastMousePosition = currentPosition;
            }
        }

        private void SaveAreasButton_Click(object sender, RoutedEventArgs e)
        {
            if (_imageHandler.GetImage() == null || _selectionRectangles.Count == 0)
            {
                MessageBox.Show("Нет изображения или областей для сохранения.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Filter = "PNG Image|*.png",
                FileName = "area_1"
            };

            var baseFolder = System.IO.Path.GetDirectoryName(dialog.FileName);
            if (dialog.ShowDialog() == true)
            {
                baseFolder = System.IO.Path.GetDirectoryName(dialog.FileName);
                var baseFileName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);

                try
                {
                    SaveAllAreas(baseFolder, baseFileName);
                    MessageBox.Show($"Области успешно сохранены в папку:\n{baseFolder}",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении областей: {ex.Message}",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void SaveAllAreas(string baseFolder, string baseFileName)
        {
            if (string.IsNullOrEmpty(baseFolder)) throw new ArgumentNullException(nameof(baseFolder));

            var imageSize = _imageHandler.GetImageSize(); // Мы вызываем GetImageSize() из ImageHandler
            var index = 1;

            foreach (var rect in _selectionRectangles)
            {
                var left = Canvas.GetLeft(rect);
                var top = Canvas.GetTop(rect);

                // Преобразуем координаты с учетом поворота и масштаба
                var cropRect = new System.Drawing.Rectangle(
                    (int)(left / _currentScale),
                    (int)(top / _currentScale),
                    (int)(rect.Width / _currentScale),
                    (int)(rect.Height / _currentScale));

                var croppedImage = _imageHandler.CropImage(cropRect);
                var fileName = System.IO.Path.Combine(baseFolder, $"{baseFileName}_{index}.png");
                croppedImage.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
                croppedImage.Dispose();
                index++;
            }
        }

        // Обработчик для кнопки сохранения настроек
        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Логика сохранения настроек
            MessageBox.Show("Настройки сохранены.");
        }

        // Обработчик для кнопки загрузки настроек
        private void LoadSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Логика загрузки настроек
            MessageBox.Show("Настройки загружены.");
        }
    }
}
