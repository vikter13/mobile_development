using System;
using System.Windows;
using ClassLibraryCutPicture; // Подключаем пространство имен

namespace CutPictureWpf
{
    public partial class MainWindow : Window
    {
        private ImageHandler imageHandler; // Объект для работы с изображением

        public MainWindow()
        {
            InitializeComponent();
            imageHandler = new ImageHandler(); // Инициализация объекта ImageHandler
        }

        private void LoadImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Логика загрузки изображения
            imageHandler.LoadImage("path_to_image.jpg");
            // Покажите изображение в интерфейсе
        }
    }
}
