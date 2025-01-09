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
using Point = System.Windows.Point;
using Rectangle = System.Windows.Shapes.Rectangle;
using System.Drawing;
using System.Drawing.Imaging;

namespace ImageEditor
{
    public partial class MainWindow : Window
    {
        private readonly ImageHandler _imageHandler = new ImageHandler();
        private string? _currentImagePath;
        private double _currentScale = 1.0;

        private readonly List<Rectangle> _selectionRectangles = new();
        private int _rows = 2;
        private int _columns = 3;
        private int _horizontalPadding = 2;
        private int _verticalPadding = 2;

        private Point _rightMouseStartPoint;
        private bool _isRightMouseDragging;

        public MainWindow()
        {
            InitializeComponent();
            InitializeComboBoxes();
            ImageScale.ScaleX = ImageScale.ScaleY = _currentScale;
        }

        private void InitializeComboBoxes()
        {
            RowsComboBox.ItemsSource = Enumerable.Range(1, 10);
            ColumnsComboBox.ItemsSource = Enumerable.Range(1, 10);
            HorizontalPaddingComboBox.ItemsSource = Enumerable.Range(0, 21);
            VerticalPaddingComboBox.ItemsSource = Enumerable.Range(0, 21);

            RowsComboBox.SelectedItem = _rows;
            ColumnsComboBox.SelectedItem = _columns;
            HorizontalPaddingComboBox.SelectedItem = _horizontalPadding;
            VerticalPaddingComboBox.SelectedItem = _verticalPadding;
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settings = new
            {
                Rows = _rows,
                Columns = _columns,
                HorizontalPadding = _horizontalPadding,
                VerticalPadding = _verticalPadding
            };

            var dialog = new SaveFileDialog
            {
                Filter = "JSON files|*.json",
                FileName = "settings.json"
            };

            if (dialog.ShowDialog() == true)
            {
                File.WriteAllText(dialog.FileName, System.Text.Json.JsonSerializer.Serialize(settings));
                MessageBox.Show("Настройки сохранены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LoadSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON files|*.json"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<dynamic>(json);

                    _rows = (int)settings.Rows;
                    _columns = (int)settings.Columns;
                    _horizontalPadding = (int)settings.HorizontalPadding;
                    _verticalPadding = (int)settings.VerticalPadding;

                    RowsComboBox.SelectedItem = _rows;
                    ColumnsComboBox.SelectedItem = _columns;
                    HorizontalPaddingComboBox.SelectedItem = _horizontalPadding;
                    VerticalPaddingComboBox.SelectedItem = _verticalPadding;

                    CreateSelectionGrid();
                    MessageBox.Show("Настройки загружены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки настроек: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void GridSettings_Changed(object sender, RoutedEventArgs e)
        {
            if (RowsComboBox.SelectedItem != null)
            {
                _rows = (int)RowsComboBox.SelectedItem;
            }
            else
            {
                _rows = 0;
            }

            if (ColumnsComboBox.SelectedItem != null)
            {
                _columns = (int)ColumnsComboBox.SelectedItem;
            }
            else
            {
                _columns = 0;
            }

            if (HorizontalPaddingComboBox.SelectedItem != null)
            {
                _horizontalPadding = (int)HorizontalPaddingComboBox.SelectedItem;
            }
            else
            {
                _horizontalPadding = 0;
            }

            if (VerticalPaddingComboBox.SelectedItem != null)
            {
                _verticalPadding = (int)VerticalPaddingComboBox.SelectedItem;
            }
            else
            {
                _verticalPadding = 0;
            }

            CreateSelectionGrid();
        }

        public void SaveAreasButton_Click(object sender, RoutedEventArgs e)
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

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var baseFolder = System.IO.Path.GetDirectoryName(dialog.FileName);
                    var baseFileName = System.IO.Path.GetFileNameWithoutExtension(dialog.FileName);
                    var index = 1;

                    float rotationAngle = (float)ImageRotation.Angle;

                    foreach (var rect in _selectionRectangles)
                    {
                        var left = Canvas.GetLeft(rect);
                        var top = Canvas.GetTop(rect);

                        var cropRect = new System.Drawing.Rectangle(
                                (int)left,
                                (int)top,
                                (int)rect.Width,
                                (int)rect.Height
                        );

                        var imageSize = _imageHandler.GetImageSize();
                        cropRect.X = Math.Max(0, Math.Min(cropRect.X, imageSize.Width - 1));
                        cropRect.Y = Math.Max(0, Math.Min(cropRect.Y, imageSize.Height - 1));
                        cropRect.Width = Math.Min(cropRect.Width, imageSize.Width - cropRect.X);
                        cropRect.Height = Math.Min(cropRect.Height, imageSize.Height - cropRect.Y);

                        using (var croppedImage = _imageHandler.CropImage(cropRect))
                        {
                            Bitmap rotatedImage = RotateImage(croppedImage, rotationAngle);

                            string fileName = System.IO.Path.Combine(baseFolder!, $"{baseFileName}_{index}.png");
                            rotatedImage.Save(fileName, ImageFormat.Png);
                        }

                        index++;
                    }

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

        private Bitmap RotateImage(Bitmap image, float angle)
        {
            Bitmap rotatedBitmap = new Bitmap(image.Width, image.Height);
            rotatedBitmap.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (Graphics g = Graphics.FromImage(rotatedBitmap))
            {
                g.Clear(System.Drawing.Color.Transparent);
                g.RotateTransform(angle);
                g.DrawImage(image, new System.Drawing.Point(0, 0));
            }

            return rotatedBitmap;
        }

        private void ScrollViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                _currentScale *= 1.1;
            else
                _currentScale *= 0.9;

            ImageScale.ScaleX = ImageScale.ScaleY = _currentScale;
        }

        private void ScrollViewer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer == null) return;

            _rightMouseStartPoint = e.GetPosition(scrollViewer);
            _isRightMouseDragging = true;
        }

        private void ScrollViewer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isRightMouseDragging = false;
        }

        private void ScrollViewer_MouseMove(object sender, MouseEventArgs e)
        {
            var scrollViewer = sender as ScrollViewer;
            if (scrollViewer == null) return;

            if (_isRightMouseDragging)
            {
                var currentMousePosition = e.GetPosition(scrollViewer);
                var delta = _rightMouseStartPoint - currentMousePosition;

                scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + delta.X);
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + delta.Y);

                _rightMouseStartPoint = currentMousePosition;
            }
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
                    MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void DisplayImage()
        {
            if (_imageHandler.GetImage() is { } bitmap)
            {
                using var memory = new MemoryStream();
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

        private void UpdateImageInfo()
        {
            var size = _imageHandler.GetImageSize();
            ImageInfoTextBlock.Text = $"Размер изображения: {size.Width}x{size.Height}\nМасштаб: {_currentScale:P0}";
        }
        private void RotationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.NewValue != e.OldValue)
            {
                ImageRotation.Angle = e.NewValue;
            }
        }

        public void CreateSelectionGrid()
        {
            SelectionCanvas.Children.Clear();
            _selectionRectangles.Clear();

            if (_imageHandler.GetImage() == null) return;

            var imageSize = _imageHandler.GetImageSize();
            var canvasWidth = SelectionCanvas.ActualWidth;
            var canvasHeight = SelectionCanvas.ActualHeight;

            if (canvasWidth <= 0 || canvasHeight <= 0) return;

            var cellWidth = imageSize.Width / _columns;
            var cellHeight = imageSize.Height / _rows;

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    var x = col * cellWidth;
                    var y = row * cellHeight;

                    var rect = new Rectangle
                    {
                        Width = cellWidth,
                        Height = cellHeight,
                        Stroke = System.Windows.Media.Brushes.Black,
                        StrokeThickness = 2,
                        Fill = System.Windows.Media.Brushes.Transparent
                    };

                    Canvas.SetLeft(rect, x);
                    Canvas.SetTop(rect, y);
                    SelectionCanvas.Children.Add(rect);
                    _selectionRectangles.Add(rect);
                }
            }
        }
    }
}
