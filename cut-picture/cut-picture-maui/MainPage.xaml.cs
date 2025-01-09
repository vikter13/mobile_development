using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
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
        private Image _currentImage;

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

            // Установка значений по умолчанию, если они еще не установлены
            RowsPicker.SelectedItem = RowsPicker.SelectedItem ?? _rows;
            ColumnsPicker.SelectedItem = ColumnsPicker.SelectedItem ?? _columns;
            HorizontalPaddingPicker.SelectedItem = HorizontalPaddingPicker.SelectedItem ?? _horizontalPadding;
            VerticalPaddingPicker.SelectedItem = VerticalPaddingPicker.SelectedItem ?? _verticalPadding;
        }


        private async void OnLoadImageButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var file = await FilePicker.PickAsync();
                if (file != null)
                {
                    _currentImagePath = file.FullPath;
                    var image = ImageSource.FromFile(_currentImagePath);
                    MainImage.Source = image;
                    CreateSelectionGrid();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
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

                // Запрос на выбор места для сохранения файла через FileSystem
                var filePath = Path.Combine(FileSystem.AppDataDirectory, "settings.json");

                // Записываем данные в файл
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
                var file = await FilePicker.PickAsync();
                if (file != null)
                {
                    var json = File.ReadAllText(file.FullPath);
                    var settings = JsonSerializer.Deserialize<dynamic>(json);

                    _rows = (int)settings.Rows;
                    _columns = (int)settings.Columns;
                    _horizontalPadding = (int)settings.HorizontalPadding;
                    _verticalPadding = (int)settings.VerticalPadding;

                    RowsPicker.SelectedItem = _rows;
                    ColumnsPicker.SelectedItem = _columns;
                    HorizontalPaddingPicker.SelectedItem = _horizontalPadding;
                    VerticalPaddingPicker.SelectedItem = _verticalPadding;

                    CreateSelectionGrid();
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

            CreateSelectionGrid();
        }



        private void CreateSelectionGrid()
        {
            // Очистка списка прямоугольников
            _selectionRectangles.Clear();

            if (_currentImage == null)
                return;

            // Приведение к типу float
            var cellWidth = (float)((SelectionCanvas.Width - _horizontalPadding * (_columns - 1)) / _columns);
            var cellHeight = (float)((SelectionCanvas.Height - _verticalPadding * (_rows - 1)) / _rows);

            for (int row = 0; row < _rows; row++)
            {
                for (int col = 0; col < _columns; col++)
                {
                    // Приведение координат x и y к типу float
                    var x = (float)(col * (cellWidth + _horizontalPadding));
                    var y = (float)(row * (cellHeight + _verticalPadding));

                    var rect = new RectangleF(x, y, cellWidth, cellHeight);
                    _selectionRectangles.Add(rect);
                }
            }

            // Перерисовка `GraphicsView`
            SelectionCanvas.Invalidate();
        }

        // Обработчик события Draw
        private void OnCanvasDraw(ICanvas canvas, RectF dirtyRect)
        {
            // Установка цвета
            canvas.StrokeColor = Microsoft.Maui.Graphics.Colors.Black;
            canvas.StrokeSize = 2;

            // Рисование всех прямоугольников
            foreach (var rect in _selectionRectangles)
            {
                // Передаем координаты x, y, ширину и высоту вместо прямоугольника
                canvas.DrawRectangle(rect.X, rect.Y, rect.Width, rect.Height);
            }
        }

        private async void OnSaveAreasButtonClicked(object sender, EventArgs e)
        {
            try
            {
                // Подготовка данных для сохранения
                var areas = _selectionRectangles.Select(rect => new
                {
                    X = rect.X,
                    Y = rect.Y,
                    Width = rect.Width,
                    Height = rect.Height
                }).ToList();

                // Сериализация в JSON
                var json = JsonSerializer.Serialize(areas);

                // Запрос на выбор места для сохранения файла через FileSystem
                var filePath = Path.Combine(FileSystem.AppDataDirectory, "areas.json");

                // Запись данных в файл
                await File.WriteAllTextAsync(filePath, json);

                await DisplayAlert("Success", "Areas saved successfully!", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }

        private async void OnLoadAreasButtonClicked(object sender, EventArgs e)
        {
            try
            {
                // Открываем диалог выбора файла
                var file = await FilePicker.PickAsync();
                if (file != null)
                {
                    var json = await File.ReadAllTextAsync(file.FullPath);

                    // Десериализация JSON
                    var areas = JsonSerializer.Deserialize<List<dynamic>>(json);

                    // Очистка существующих прямоугольников
                    _selectionRectangles.Clear();

                    // Восстановление прямоугольников из данных
                    foreach (var area in areas)
                    {
                        var rect = new RectangleF((float)area.X, (float)area.Y, (float)area.Width, (float)area.Height);
                        _selectionRectangles.Add(rect);
                    }

                    // Перерисовка области
                    SelectionCanvas.Invalidate();

                    await DisplayAlert("Success", "Areas loaded successfully!", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
        }


        private ImageSource CropImage(RectangleF rect)
        {
            // Implement the logic to crop the image based on the rectangle selection
            return null; // Placeholder
        }
    }
}
