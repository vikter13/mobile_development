using class_library_exam.Models;

namespace class_library_exam
{
    public class GameLogic
    {
        public DifficultyLevel CurrentDifficulty { get; private set; }
        public int CurrentScore { get; private set; }
        public int Attempts { get; private set; }
        public int TimeLeft { get; private set; }
        public GameFigure[,] GameField { get; private set; }
        private Random random;

        public GameLogic()
        {
            random = new Random();
            CurrentDifficulty = DifficultyLevel.Easy;
            ResetGame();
        }

        public void ResetGame()
        {
            CurrentScore = 0;
            Attempts = 0;
            TimeLeft = 60;
            InitializeGameField();
        }

        public void SetDifficulty(DifficultyLevel difficulty)
        {
            CurrentDifficulty = difficulty;
            ResetGame();
        }

        public bool CheckFigure(int row, int col)
        {
            if (GameField[row, col]?.IsUnique == true)
            {
                CurrentScore++;
                return true;
            }

            Attempts++;
            return false;
        }

        public void DecrementTime()
        {
            if (TimeLeft > 0)
                TimeLeft--;
        }

        private void InitializeGameField()
        {
            int size = (int)CurrentDifficulty;
            GameField = new GameFigure[size, size];

            FillFieldWithRandomFigures();
            AddUniqueFigure();
        }

        private void FillFieldWithRandomFigures()
        {
            int size = GameField.GetLength(0);

            FigureType mainType = random.Next(2) == 0 ? FigureType.Circle : FigureType.Triangle;
            FigureColor mainColor = random.Next(2) == 0 ? FigureColor.Blue : FigureColor.Green;

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (random.Next(5) == 0)
                    {
                        GameField[i, j] = new GameFigure(FigureType.Empty, mainColor);
                    }
                    else
                    {
                        GameField[i, j] = new GameFigure(mainType, mainColor);
                    }
                }
            }
        }

        private void AddUniqueFigure()
        {
            int size = GameField.GetLength(0);
            List<(int, int)> emptyCells = new List<(int, int)>();

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    if (GameField[i, j].Type == FigureType.Empty)
                    {
                        emptyCells.Add((i, j));
                    }
                }
            }

            if (emptyCells.Count > 0)
            {
                var (row, col) = emptyCells[random.Next(emptyCells.Count)];
                FigureType uniqueType = random.Next(2) == 0 ? FigureType.Circle : FigureType.Triangle;
                FigureColor uniqueColor = random.Next(2) == 0 ? FigureColor.Blue : FigureColor.Green;

                GameField[row, col] = new GameFigure(uniqueType, uniqueColor, true);
            }
            else
            {
                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        if (GameField[i, j].Type != FigureType.Empty)
                        {
                            FigureType uniqueType = random.Next(2) == 0 ? FigureType.Circle : FigureType.Triangle;
                            FigureColor uniqueColor = random.Next(2) == 0 ? FigureColor.Blue : FigureColor.Green;

                            GameField[i, j] = new GameFigure(uniqueType, uniqueColor, true);
                            return;
                        }
                    }
                }
            }
        }
    }
}
