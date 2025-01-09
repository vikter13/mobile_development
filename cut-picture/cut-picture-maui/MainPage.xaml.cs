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
        private int _rows = 2;
        private int _columns = 3;
        private int _horizontalPadding = 10;
        private int _verticalPadding = 10;
        private string _currentImagePath;
        private ImageSource _currentImage;

        public MainPage()
        {
            InitializeComponent();
            InitializeComboBoxes();
        }

        private void InitializeComboBoxes()
        {
            RowsPicker.ItemsSource = Enumerable.Range(1, 10).ToList();
            ColumnsPicker.ItemsSource = Enumerable.Range(1, 10).ToList();
            HorizontalPaddingPicker.ItemsSource = Enumerable.Range(0, 50).ToList();
            VerticalPaddingPicker.ItemsSource = Enumerable.Range(0, 50).ToList();

            RowsPicker.SelectedItem = _rows;
            ColumnsPicker.SelectedItem = _columns;
            HorizontalPaddingPicker.SelectedItem = _horizontalPadding;
            VerticalPaddingPicker.SelectedItem = _verticalPadding;
        }

        private async void OnLoadImageButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var file = await FilePicker.PickAsync();
                if (file != null)
                {
                    _currentImagePath = file.FullPath; // Здесь переменная получает значение
                    _currentImage = ImageSource.FromFile(_currentImagePath);
                    MainImage.Source = _currentImage;

                    // Загружаем изображение в SKBitmap
                    using (var fileStream = File.OpenRead(_currentImagePath))
                    {
                        var bitmap = SKBitmap.Decode(fileStream);

                        // Создаем сетку после загрузки изображения
                        CreateSelectionGrid(bitmap); // Передаем bitmap в метод
                    }
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }


        private void CreateSelectionGrid(SKBitmap bitmap)
        {
            if (bitmap == null)
            {
                DisplayAlert("Warning", "Load an image first.", "OK");
                return;
            }

            _selectionRectangles.Clear();

            float canvasWidth = bitmap.Width;
            float canvasHeight = bitmap.Height;

            // Определяем количество колонок и строк в зависимости от размеров изображения
            int columns = Math.Max(1, (int)(canvasWidth / 100));  // 100px ширина ячейки
            int rows = Math.Max(1, (int)(canvasHeight / 100));    // 100px высота ячейки

            float cellWidth = canvasWidth / columns;
            float cellHeight = canvasHeight / rows;

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < columns; col++)
                {
                    float x = col * cellWidth;
                    float y = row * cellHeight;

                    var rect = new RectangleF(x, y, cellWidth, cellHeight);
                    _selectionRectangles.Add(rect);
                }
            }

            SelectionCanvas.Invalidate();
        }

        private async Task<SKBitmap> CropImageAsync(RectangleF rect, SKBitmap bitmap)
        {
            if (bitmap == null)
            {
                throw new InvalidOperationException("Image is not loaded properly.");
            }

            // Преобразуем координаты в целые числа
            int left = (int)rect.Left;
            int top = (int)rect.Top;
            int right = (int)rect.Right;
            int bottom = (int)rect.Bottom;

            // Проверяем, что прямоугольник в пределах изображения
            if (left < 0 || top < 0 || right > bitmap.Width || bottom > bitmap.Height)
            {
                // Корректируем координаты, если они выходят за пределы изображения
                left = Math.Max(0, left);
                top = Math.Max(0, top);
                right = Math.Min(bitmap.Width, right);
                bottom = Math.Min(bitmap.Height, bottom);
            }

            // Обрезаем изображение
            var cropRect = new SKRectI(left, top, right, bottom);
            var croppedBitmap = new SKBitmap(cropRect.Width, cropRect.Height);
            bitmap.ExtractSubset(croppedBitmap, cropRect);

            return croppedBitmap;
        }



        private void OnCanvasDraw(ICanvas canvas, RectF dirtyRect)
        {
            if (_selectionRectangles.Count == 0)
                return;

            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 2;

            foreach (var rect in _selectionRectangles)
            {
                canvas.DrawRectangle(rect.X, rect.Y, rect.Width, rect.Height);
            }
        }





        // Пример метода сохранения файла
        private async Task<string> SaveFileAsync(string suggestedFileName)
        {
            string filePath = "C:\\Users\\victor\\Downloads\\images-folder";

            try
            {
                // Для мобильных платформ (Android, iOS) можно использовать конкретные папки:
                // Пример использования "Документы" на Android или iOS
                filePath = Path.Combine(FileSystem.AppDataDirectory, suggestedFileName);

                // Если хотите указать пользовательский путь для Windows/Mac, можно сделать так:
                // Пример для Windows: C:\Users\Username\Documents
                if (DeviceInfo.Platform == DevicePlatform.WinUI || DeviceInfo.Platform == DevicePlatform.MacCatalyst)
                {
                    filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), suggestedFileName);
                }

                // Для других платформ можно указать статические пути, например, для загрузок или общего хранилища
                // Если это нужно, добавьте дополнительные проверки для других платформ.

                return filePath;
            }
            catch (Exception ex)
            {
                // Тут можно ловить ошибку, если в мобильных приложениях сохранение не поддерживается
                await DisplayAlert("Error", "Error saving file: " + ex.Message, "OK");
            }

            return filePath;
        }



        private async void OnCropImageButtonClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentImagePath) || _selectionRectangles.Count == 0)
            {
                await DisplayAlert("Error", "Load an image and create a grid first.", "OK");
                return;
            }

            try
            {
                // Выбираем первый прямоугольник для обрезки
                var rect = _selectionRectangles.First(); // Используем первый прямоугольник

                // Загружаем изображение
                using (var fileStream = File.OpenRead(_currentImagePath))
                {
                    var bitmap = SKBitmap.Decode(fileStream);
                    var croppedImage = await CropImageAsync(rect, bitmap); // Передаем rect и bitmap

                    if (croppedImage == null)
                    {
                        await DisplayAlert("Error", "Image cropping failed.", "OK");
                        return;
                    }

                    var filePath = Path.Combine(FileSystem.AppDataDirectory, "cropped_image.png");

                    // Сохраняем обрезанное изображение
                    using (var stream = File.Create(filePath))
                    {
                        croppedImage.Encode(stream, SKEncodedImageFormat.Png, 100);
                    }

                    await DisplayAlert("Success", $"Image saved to {filePath}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void OnSaveAreasButtonClicked(object sender, EventArgs e)
        {
            try
            {
                // Проверяем, есть ли прямоугольники
                if (_selectionRectangles.Count == 0)
                {
                    await DisplayAlert("Error", "No areas to save.", "OK");
                    return;
                }

                if (string.IsNullOrEmpty(_currentImagePath))
                {
                    await DisplayAlert("Error", "No image loaded.", "OK");
                    return;
                }

                // Загружаем исходное изображение
                using (var fileStream = File.OpenRead(_currentImagePath))
                {
                    var bitmap = SKBitmap.Decode(fileStream);

                    // Создаем новое изображение с прямоугольниками
                    var resultBitmap = DrawRectanglesOnImage(bitmap);

                    // Сохраняем результат
                    var filePath = Path.Combine(FileSystem.AppDataDirectory, "image_with_areas.png");
                    using (var stream = File.Create(filePath))
                    {
                        resultBitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
                    }

                    await DisplayAlert("Success", $"Image saved to {filePath}", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private SKBitmap DrawRectanglesOnImage(SKBitmap bitmap)
        {
            // Создаем новый объект SKCanvas для рисования на изображении
            var resultBitmap = new SKBitmap(bitmap.Width, bitmap.Height);
            using (var canvas = new SKCanvas(resultBitmap))
            {
                // Рисуем исходное изображение
                canvas.DrawBitmap(bitmap, 0, 0);

                // Настройки для рисования прямоугольников
                var paint = new SKPaint
                {
                    Color = SKColors.Red.WithAlpha(128), // Полупрозрачный красный цвет
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = 3
                };

                // Рисуем все прямоугольники
                foreach (var rect in _selectionRectangles)
                {
                    var skRect = new SKRect(rect.Left, rect.Top, rect.Right, rect.Bottom);
                    canvas.DrawRect(skRect, paint);
                }
            }

            return resultBitmap;
        }







        private async void OnSaveSettingsButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var settings = new
                {
                    Rows = _rows,
                    Columns = _columns,
                    HorizontalPadding = _horizontalPadding,
                    VerticalPadding = _verticalPadding
                };

                var json = JsonSerializer.Serialize(settings);
                var filePath = Path.Combine(FileSystem.AppDataDirectory, "settings.json");
                await File.WriteAllTextAsync(filePath, json);

                await DisplayAlert("Success", "Settings saved successfully!", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void OnLoadSettingsButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var filePath = Path.Combine(FileSystem.AppDataDirectory, "settings.json");

                if (!File.Exists(filePath))
                {
                    await DisplayAlert("Error", "No saved settings found.", "OK");
                    return;
                }

                var json = await File.ReadAllTextAsync(filePath);
                var settings = JsonSerializer.Deserialize<Settings>(json); // Десериализация в объект Settings

                if (settings != null)
                {
                    _rows = settings.Rows;
                    _columns = settings.Columns;
                    _horizontalPadding = settings.HorizontalPadding;
                    _verticalPadding = settings.VerticalPadding;

                    RowsPicker.SelectedItem = _rows;
                    ColumnsPicker.SelectedItem = _columns;
                    HorizontalPaddingPicker.SelectedItem = _horizontalPadding;
                    VerticalPaddingPicker.SelectedItem = _verticalPadding;

                    // Проверка, если изображение не загружено, то загружаем его
                    if (!string.IsNullOrEmpty(_currentImagePath))
                    {
                        using (var fileStream = File.OpenRead(_currentImagePath))
                        {
                            var bitmap = SKBitmap.Decode(fileStream);
                            CreateSelectionGrid(bitmap);  // Передаем bitmap в метод
                        }
                    }
                    else
                    {
                        await DisplayAlert("Error", "No image loaded. Please load an image first.", "OK");
                    }

                    await DisplayAlert("Success", "Settings loaded successfully!", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }




        private void OnGridSettingsChanged(object sender, EventArgs e)
        {
            if (RowsPicker.SelectedItem != null)
                _rows = (int)RowsPicker.SelectedItem;

            if (ColumnsPicker.SelectedItem != null)
                _columns = (int)ColumnsPicker.SelectedItem;

            if (HorizontalPaddingPicker.SelectedItem != null)
                _horizontalPadding = (int)HorizontalPaddingPicker.SelectedItem;

            if (VerticalPaddingPicker.SelectedItem != null)
                _verticalPadding = (int)VerticalPaddingPicker.SelectedItem;

            // Проверка на наличие загруженного изображения
            if (!string.IsNullOrEmpty(_currentImagePath))
            {
                // Загружаем изображение, чтобы передать его в CreateSelectionGrid
                using (var fileStream = File.OpenRead(_currentImagePath))
                {
                    var bitmap = SKBitmap.Decode(fileStream);
                    CreateSelectionGrid(bitmap);  // Передаем bitmap в метод
                }
            }
            else
            {
                DisplayAlert("Ошибка", "Изображение не загружено.", "OK");
            }
        }





        private Microsoft.Maui.Graphics.IImage CropImage(RectangleF rect)
        {
            // Заглушка: Вернуть обрезанное изображение
            return null; // Добавить код для обрезки изображения
        }

        public class Settings
        {
            public int Rows { get; set; }
            public int Columns { get; set; }
            public int HorizontalPadding { get; set; }
            public int VerticalPadding { get; set; }
        }

    }
}

