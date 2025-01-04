using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace ImageEditor
{
    public class ImageHandler
    {
        private Bitmap _image;

        public void LoadImage(string filePath)
        {
            if (File.Exists(filePath))
            {
                _image = new Bitmap(filePath);
            }
            else
            {
                throw new FileNotFoundException("Image file not found.");
            }
        }

        public Bitmap GetImage() => _image;

        public void SaveImage(string filePath)
        {
            if (_image != null)
            {
                _image.Save(filePath, ImageFormat.Png);
            }
            else
            {
                throw new InvalidOperationException("No image to save.");
            }
        }

        public void RotateImage(float angle)
        {
            if (_image != null)
            {
                using (var matrix = new Matrix())
                {
                    matrix.RotateAt(angle, new PointF(_image.Width / 2, _image.Height / 2));
                    var rect = new Rectangle(0, 0, _image.Width, _image.Height);
                    var points = new[]
                    {
                        new Point(rect.Left, rect.Top),
                        new Point(rect.Right, rect.Top),
                        new Point(rect.Left, rect.Bottom),
                        new Point(rect.Right, rect.Bottom)
                    };
                    matrix.TransformPoints(points);

                    var left = Math.Min(Math.Min(points[0].X, points[1].X), Math.Min(points[2].X, points[3].X));
                    var top = Math.Min(Math.Min(points[0].Y, points[1].Y), Math.Min(points[2].Y, points[3].Y));
                    var right = Math.Max(Math.Max(points[0].X, points[1].X), Math.Max(points[2].X, points[3].X));
                    var bottom = Math.Max(Math.Max(points[0].Y, points[1].Y), Math.Max(points[2].Y, points[3].Y));

                    var rotatedImage = new Bitmap(right - left, bottom - top);
                    using (var g = Graphics.FromImage(rotatedImage))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.TranslateTransform(-left, -top);
                        g.Transform = matrix;
                        g.DrawImage(_image, rect);
                    }
                    _image = rotatedImage;
                }
            }
        }

        public Bitmap CropImage(Rectangle cropArea)
        {
            if (_image != null)
            {
                var croppedImage = new Bitmap(cropArea.Width, cropArea.Height);
                using (var g = Graphics.FromImage(croppedImage))
                {
                    g.DrawImage(_image, new Rectangle(0, 0, cropArea.Width, cropArea.Height),
                        cropArea, GraphicsUnit.Pixel);
                }
                return croppedImage;
            }
            throw new InvalidOperationException("No image to crop.");
        }

        // Метод GetImageSize для получения размера изображения
        public Size GetImageSize() => _image?.Size ?? Size.Empty;
    }
}
