using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ImageEditor
{
    public partial class MainPage : ContentPage
    {
        private List<RectangleF> _selectionRectangles = new();
        private string _currentImagePath;
        private SKBitmap _currentBitmap;
        private int _rows = 2;
        private int _columns = 3;
        private int _horizontalPadding = 10;
        private int _verticalPadding = 10;
        private float _zoom = 1.0f;
        private float _rotation = 0.0f;

        public MainPage()
        {
            InitializeComponent();
            InitializeUI();
            SetupZoomHandler();
        }

        private void InitializeUI()
        {
            RowsPicker.ItemsSource = Enumerable.Range(1, 10).ToList();
            ColumnsPicker.ItemsSource = Enumerable.Range(1, 10).ToList();
            HorizontalPaddingPicker.ItemsSource = Enumerable.Range(0, 100).ToList();
            VerticalPaddingPicker.ItemsSource = Enumerable.Range(0, 100).ToList();

            RowsPicker.SelectedItem = _rows;
            ColumnsPicker.SelectedItem = _columns;
            HorizontalPaddingPicker.SelectedItem = _horizontalPadding;
            VerticalPaddingPicker.SelectedItem = _verticalPadding;

            // Add rotation slider value changed handler
            RotationSlider.ValueChanged += (s, e) =>
            {
                _rotation = (float)e.NewValue;
                MainImage.Rotation = _rotation;
                SelectionCanvas.Rotation = _rotation;
                UpdateSelectionGrid();
            };
        }

        private void SetupZoomHandler()
        {
            // Add pinch gesture recognizer for zoom
            var pinchGesture = new PinchGestureRecognizer();
            pinchGesture.PinchUpdated += (s, e) =>
            {
                switch (e.Status)
                {
                    case GestureStatus.Started:
                        // Store the current scale when the gesture begins
                        break;
                    case GestureStatus.Running:
                        // Update the scale based on the gesture
                        _zoom = Math.Max(0.1f, Math.Min(5.0f, _zoom * (float)e.Scale));
                        MainImage.Scale = _zoom;
                        SelectionCanvas.Scale = _zoom;
                        break;
                }
            };

            ImageGrid.GestureRecognizers.Add(pinchGesture);
        }

        private async void OnLoadImageButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var result = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Выберите изображение",
                    FileTypes = FilePickerFileType.Images
                });

                if (result != null)
                {
                    _currentImagePath = result.FullPath;

                    using (var stream = await result.OpenReadAsync())
                    {
                        _currentBitmap = SKBitmap.Decode(stream);
                        MainImage.Source = ImageSource.FromFile(_currentImagePath);

                        await DisplayAlert("Информация о изображении",
                            $"Размер: {_currentBitmap.Width}x{_currentBitmap.Height}\n" +
                            $"Формат: {Path.GetExtension(_currentImagePath)}", "OK");

                        UpdateSelectionGrid();
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private void UpdateSelectionGrid()
        {
            if (_currentBitmap == null) return;

            _selectionRectangles.Clear();

            float imageWidth = _currentBitmap.Width;
            float imageHeight = _currentBitmap.Height;

            float cellWidth = (imageWidth - (_columns + 1) * _horizontalPadding) / _columns;
            float cellHeight = (imageHeight - (_rows + 1) * _verticalPadding) / _rows;

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    float x = col * (cellWidth + _horizontalPadding) + _horizontalPadding;
                    float y = row * (cellHeight + _verticalPadding) + _verticalPadding;

                    _selectionRectangles.Add(new RectangleF(x, y, cellWidth, cellHeight));
                }
            }

            SelectionCanvas.Invalidate();
        }


        private void OnCanvasDraw(object sender, ICanvas canvas)
        {
            if (_currentBitmap == null || _selectionRectangles.Count == 0)
                return;

            canvas.StrokeColor = Colors.Red;
            canvas.StrokeSize = 2;

            foreach (var rect in _selectionRectangles)
            {
                canvas.DrawRectangle(rect.X, rect.Y, rect.Width, rect.Height);
            }
        }

        private async void OnSaveAreasButtonClicked(object sender, EventArgs e)
        {
            if (_currentBitmap == null || _selectionRectangles.Count == 0)
            {
                await DisplayAlert("Ошибка", "Сначала загрузите изображение и создайте сетку", "OK");
                return;
            }

            try
            {
                string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "ImageEditor");
                Directory.CreateDirectory(basePath);

                int areaIndex = 1;
                foreach (var rect in _selectionRectangles)
                {
                    var croppedBitmap = CropImage(_currentBitmap, rect);
                    if (croppedBitmap != null)
                    {
                        string fileName = $"area_{areaIndex}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                        string fullPath = Path.Combine(basePath, fileName);

                        using (var fileStream = File.OpenWrite(fullPath))
                        {
                            croppedBitmap.Encode(SKEncodedImageFormat.Png, 100).SaveTo(fileStream);
                        }
                        areaIndex++;
                    }
                }

                await DisplayAlert("Успех", $"Сохранено {_selectionRectangles.Count} областей в папку {basePath}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private SKBitmap CropImage(SKBitmap source, RectangleF rect)
        {
            // Создаем новый битмап для обрезанного изображения
            SKRectI cropRect = new SKRectI(
                (int)rect.X,
                (int)rect.Y,
                (int)(rect.X + rect.Width),
                (int)(rect.Y + rect.Height)
            );

            // Проверяем границы
            cropRect.Left = Math.Max(0, cropRect.Left);
            cropRect.Top = Math.Max(0, cropRect.Top);
            cropRect.Right = Math.Min(source.Width, cropRect.Right);
            cropRect.Bottom = Math.Min(source.Height, cropRect.Bottom);

            // Создаем новый битмап и копируем в него область
            var croppedBitmap = new SKBitmap(cropRect.Width, cropRect.Height);
            source.ExtractSubset(croppedBitmap, cropRect);

            return croppedBitmap;
        }

        private void OnGridSettingsChanged(object sender, EventArgs e)
        {
            if (sender is Picker picker)
            {
                if (picker.SelectedItem is int value)
                {
                    if (picker == RowsPicker)
                        _rows = value;
                    else if (picker == ColumnsPicker)
                        _columns = value;
                    else if (picker == HorizontalPaddingPicker)
                        _horizontalPadding = value;
                    else if (picker == VerticalPaddingPicker)
                        _verticalPadding = value;

                    UpdateSelectionGrid();
                }
            }
        }

        private async void OnSaveSettingsButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var settings = new Settings
                {
                    Rows = _rows,
                    Columns = _columns,
                    HorizontalPadding = _horizontalPadding,
                    VerticalPadding = _verticalPadding,
                    Zoom = _zoom,
                    Rotation = _rotation
                };

                string json = JsonSerializer.Serialize(settings);
                string path = Path.Combine(FileSystem.AppDataDirectory, "settings.json");
                await File.WriteAllTextAsync(path, json);

                await DisplayAlert("Успех", "Настройки сохранены", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        private async void OnLoadSettingsButtonClicked(object sender, EventArgs e)
        {
            try
            {
                string path = Path.Combine(FileSystem.AppDataDirectory, "settings.json");
                if (!File.Exists(path))
                {
                    await DisplayAlert("Ошибка", "Файл настроек не найден", "OK");
                    return;
                }

                string json = await File.ReadAllTextAsync(path);
                var settings = JsonSerializer.Deserialize<Settings>(json);

                if (settings != null)
                {
                    _rows = settings.Rows;
                    _columns = settings.Columns;
                    _horizontalPadding = settings.HorizontalPadding;
                    _verticalPadding = settings.VerticalPadding;
                    _zoom = settings.Zoom;
                    _rotation = settings.Rotation;

                    RowsPicker.SelectedItem = _rows;
                    ColumnsPicker.SelectedItem = _columns;
                    HorizontalPaddingPicker.SelectedItem = _horizontalPadding;
                    VerticalPaddingPicker.SelectedItem = _verticalPadding;

                    MainImage.Scale = _zoom;
                    MainImage.Rotation = _rotation;
                    SelectionCanvas.Scale = _zoom;
                    SelectionCanvas.Rotation = _rotation;

                    UpdateSelectionGrid();
                    await DisplayAlert("Успех", "Настройки загружены", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Ошибка", ex.Message, "OK");
            }
        }

        public class Settings
        {
            public int Rows { get; set; }
            public int Columns { get; set; }
            public int HorizontalPadding { get; set; }
            public int VerticalPadding { get; set; }
            public float Zoom { get; set; }
            public float Rotation { get; set; }
        }
    }
}