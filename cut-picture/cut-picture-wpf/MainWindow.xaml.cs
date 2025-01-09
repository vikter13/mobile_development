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
        private int _horizontalPadding = 10;
        private int _verticalPadding = 10;

        private Point _dragStartPoint;
        private bool _isDraggingGrid;
        private Canvas _gridCanvas;

        private Point _rightMouseStartPoint;
        private bool _isRightMouseDragging;

        private Point _dragStartPointForRectangle;
        private bool _isDraggingRectangle;
        private Rectangle _draggingRectangle;

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
            HorizontalPaddingComboBox.ItemsSource = Enumerable.Range(0, 50);
            VerticalPaddingComboBox.ItemsSource = Enumerable.Range(0, 50);

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
                _rows = (int)RowsComboBox.SelectedItem;

            if (ColumnsComboBox.SelectedItem != null)
                _columns = (int)ColumnsComboBox.SelectedItem;

            if (HorizontalPaddingComboBox.SelectedItem != null)
                _horizontalPadding = (int)HorizontalPaddingComboBox.SelectedItem;

            if (VerticalPaddingComboBox.SelectedItem != null)
                _verticalPadding = (int)VerticalPaddingComboBox.SelectedItem;

            CreateSelectionGrid();
        }


        private void SaveAreasButton_Click(object sender, RoutedEventArgs e)
        {
            if (_imageHandler.GetImage() == null || _selectionRectangles.Count == 0)
            {
                MessageBox.Show("Нет изображения или областей для сохранения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                    // Получаем изображение
                    var image = _imageHandler.GetImage();

                    // Перебираем все прямоугольники и сохраняем каждую область
                    foreach (var rect in _selectionRectangles)
                    {
                        // Получаем координаты и размеры прямоугольника на канвасе
                        var left = Canvas.GetLeft(rect);
                        var top = Canvas.GetTop(rect);
                        var width = rect.Width;
                        var height = rect.Height;

                        // Проверка на корректность размеров
                        if (width <= 0 || height <= 0) continue; // Пропускаем пустые области

                        // Создаем объект Rectangle для обрезки с координатами относительно изображения
                        var cropRect = new System.Drawing.Rectangle(
                            (int)left,    // X-координата
                            (int)top,     // Y-координата
                            (int)width,   // Ширина
                            (int)height   // Высота
                        );

                        using (var croppedImage = _imageHandler.CropImage(cropRect))
                        {
                            // Сохранение изображения в файл
                            string fileName = System.IO.Path.Combine(baseFolder!, $"{baseFileName}_{index}.png");
                            croppedImage.Save(fileName, ImageFormat.Png);
                            index++;
                        }
                    }

                    MessageBox.Show($"Области успешно сохранены в папку:\n{baseFolder}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении областей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void CreateSelectionGrid()
        {
            SelectionCanvas.Children.Clear();
            _selectionRectangles.Clear();

            if (_imageHandler.GetImage() == null) return;

            _gridCanvas = new Canvas
            {
                Width = SelectionCanvas.ActualWidth,
                Height = SelectionCanvas.ActualHeight,
                Background = System.Windows.Media.Brushes.Transparent
            };
            SelectionCanvas.Children.Add(_gridCanvas);

            double cellWidth = (_gridCanvas.Width - _horizontalPadding * (_columns - 1)) / _columns;
            double cellHeight = (_gridCanvas.Height - _verticalPadding * (_rows - 1)) / _rows;

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    var x = col * (cellWidth + _horizontalPadding);
                    var y = row * (cellHeight + _verticalPadding);

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
                    _gridCanvas.Children.Add(rect);
                    _selectionRectangles.Add(rect);

                    // Добавляем обработчики событий для перетаскивания каждого прямоугольника
                    rect.MouseLeftButtonDown += SelectionRectangle_MouseLeftButtonDown;
                    rect.MouseLeftButtonUp += SelectionRectangle_MouseLeftButtonUp;
                    rect.MouseMove += SelectionRectangle_MouseMove;
                }
            }
        }

        private void SelectionRectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingRectangle) return;

            // Получаем текущую точку мыши
            var currentPoint = e.GetPosition(SelectionCanvas);

            // Находим смещение относительно начальной точки
            var offsetX = currentPoint.X - _dragStartPointForRectangle.X;
            var offsetY = currentPoint.Y - _dragStartPointForRectangle.Y;

            // Перемещаем все прямоугольники
            foreach (var rectangle in _selectionRectangles)
            {
                double newLeft = Canvas.GetLeft(rectangle) + offsetX;
                double newTop = Canvas.GetTop(rectangle) + offsetY;

                // Ограничиваем перемещение прямоугольников в пределах Canvas
                newLeft = Math.Max(0, Math.Min(newLeft, SelectionCanvas.ActualWidth - rectangle.Width));
                newTop = Math.Max(0, Math.Min(newTop, SelectionCanvas.ActualHeight - rectangle.Height));

                Canvas.SetLeft(rectangle, newLeft);
                Canvas.SetTop(rectangle, newTop);
            }

            // Обновляем начальную точку для вычисления смещения
            _dragStartPointForRectangle = currentPoint;
        }


        private void SelectionRectangle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var rect = sender as Rectangle;
            if (rect == null) return;

            // Запоминаем начальную точку перетаскивания
            _dragStartPointForRectangle = e.GetPosition(SelectionCanvas);

            // Устанавливаем флаг, что перетаскивание начато
            _isDraggingRectangle = true;

            // Удерживаем захват мыши на всех прямоугольниках
            foreach (var rectangle in _selectionRectangles)
            {
                rectangle.CaptureMouse();
            }
        }


        private void SelectionRectangle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingRectangle = false;

            // Освобождаем захват всех прямоугольников
            foreach (var rectangle in _selectionRectangles)
            {
                rectangle.ReleaseMouseCapture();
            }
        }



        private void GridCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDraggingGrid) return;

            var currentPoint = e.GetPosition(SelectionCanvas);
            var offsetX = currentPoint.X - _dragStartPoint.X;
            var offsetY = currentPoint.Y - _dragStartPoint.Y;

            double newLeft = Canvas.GetLeft(_gridCanvas) + offsetX;
            double newTop = Canvas.GetTop(_gridCanvas) + offsetY;

            newLeft = Math.Max(0, Math.Min(newLeft, SelectionCanvas.ActualWidth - _gridCanvas.Width));
            newTop = Math.Max(0, Math.Min(newTop, SelectionCanvas.ActualHeight - _gridCanvas.Height));

            Canvas.SetLeft(_gridCanvas, newLeft);
            Canvas.SetTop(_gridCanvas, newTop);

            _dragStartPoint = currentPoint;
        }

    }
}
