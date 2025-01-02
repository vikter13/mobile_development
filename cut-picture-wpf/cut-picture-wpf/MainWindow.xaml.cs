using System;
using System.Windows;
using ClassLibraryCutPicture;
using System.Windows.Input;

namespace CutPictureWpf
{
    public partial class MainWindow : Window
    {
        private ImageHandler _imageHandler;
        private double _initialWidth, _initialHeight;

        public MainWindow()
        {
            InitializeComponent();
            _imageHandler = new ImageHandler();
        }

        // Загрузка изображения
        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Открываем диалоговое окно для выбора изображения
            var dialog = new Microsoft.Win32.OpenFileDialog();
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    _imageHandler.LoadImage(dialog.FileName);
                    ImageControl.Source = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(dialog.FileName));
                    _initialWidth = _imageHandler.Image.Width;
                    _initialHeight = _imageHandler.Image.Height;
                    UpdateImageInfo();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки изображения: {ex.Message}");
                }
            }
        }

        // Обновление информации о изображении
        private void UpdateImageInfo()
        {
            ImageInfoText.Text = $"Размер изображения: {_imageHandler.Image.Width}x{_imageHandler.Image.Height}";
        }

        // Масштабирование изображения через ползунок
        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Проверяем, загружено ли изображение
            if (_imageHandler?.Image != null)
            {
                var zoomFactor = ZoomSlider.Value;

                // Применяем зум к изображению
                _imageHandler.ZoomImage((float)zoomFactor);

                // Обновляем размер отображаемого изображения
                ImageControl.Width = _initialWidth * zoomFactor;
                ImageControl.Height = _initialHeight * zoomFactor;
            }
            else
            {
                // Если изображение еще не загружено, ничего не делаем
                MessageBox.Show("Сначала загрузите изображение.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        // Сохранение обрезанного изображения
        private void SaveAreaButton_Click(object sender, RoutedEventArgs e)
        {
            if (_imageHandler.Image != null)
            {
                var dialog = new Microsoft.Win32.SaveFileDialog();
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        _imageHandler.SaveCroppedArea(dialog.FileName);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения изображения: {ex.Message}");
                    }
                }
            }
        }
    }
}
