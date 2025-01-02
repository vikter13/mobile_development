using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ClassLibraryCutPicture
{
    public class ImageHandler
    {
        public Bitmap Image { get; private set; }
        public Rectangle SelectionRectangle { get; set; }
        public float ZoomLevel { get; set; } = 1.0f;  // Степень зума

        // Загрузка изображения
        public void LoadImage(string path)
        {
            if (File.Exists(path))
            {
                Image = new Bitmap(path);
            }
            else
            {
                throw new FileNotFoundException("Изображение не найдено.");
            }
        }

        // Поворот изображения
        public void RotateImage(float angle)
        {
            if (Image != null)
            {
                Image = new Bitmap(Image); // Создаем новый объект Bitmap для применения изменений
                Image.RotateFlip(RotateFlipType.RotateNoneFlipNone); // Пример поворота, для реализации можно доработать
            }
        }

        // Сохранение части изображения
        public void SaveCroppedArea(string path)
        {
            if (Image != null && SelectionRectangle.Width > 0 && SelectionRectangle.Height > 0)
            {
                using (Bitmap croppedImage = Image.Clone(SelectionRectangle, Image.PixelFormat))
                {
                    croppedImage.Save(path, ImageFormat.Jpeg);
                }
            }
        }

        // Масштабирование изображения
        public void ZoomImage(float zoomFactor)
        {
            ZoomLevel *= zoomFactor;
        }
    }
}
