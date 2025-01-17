namespace class_library_exam.Models
{
    public class GameFigure
    {
        public FigureType Type { get; set; }
        public FigureColor Color { get; set; }
        public bool IsUnique { get; set; }

        public GameFigure(FigureType type, FigureColor color, bool isUnique = false)
        {
            Type = type;
            Color = color;
            IsUnique = isUnique;
        }
    }
}