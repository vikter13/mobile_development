using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;


namespace ClassLibraryCutPicture
{
    public class ImageHandler
    {
        public Bitmap LoadImage(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty.");

            return new Bitmap(path);
        }

        public Bitmap RotateImage(Bitmap source, float angle)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var rotated = new Bitmap(source.Width, source.Height);
            using (var g = Graphics.FromImage(rotated))
            {
                g.TranslateTransform(source.Width / 2, source.Height / 2);
                g.RotateTransform(angle);
                g.TranslateTransform(-source.Width / 2, -source.Height / 2);
                g.DrawImage(source, new Point(0, 0));
            }
            return rotated;
        }

        public Bitmap[] CutImage(Bitmap source, int rows, int cols)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (rows <= 0 || cols <= 0)
                throw new ArgumentException("Rows and columns must be greater than zero.");

            int cellWidth = source.Width / cols;
            int cellHeight = source.Height / rows;

            var result = new Bitmap[rows * cols];
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    var rect = new Rectangle(col * cellWidth, row * cellHeight, cellWidth, cellHeight);
                    result[row * cols + col] = source.Clone(rect, source.PixelFormat);
                }
            }
            return result;
        }
    }
}