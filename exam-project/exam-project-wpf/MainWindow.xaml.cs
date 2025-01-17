using class_library_exam;
using class_library_exam.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace exam_project_wpf
{
    public partial class MainWindow : Window
    {
        private GameLogic gameLogic;
        private DispatcherTimer gameTimer;
        private SolidColorBrush blueBrush = new SolidColorBrush(Colors.DeepSkyBlue);
        private SolidColorBrush greenBrush = new SolidColorBrush(Colors.MediumSeaGreen);

        public MainWindow()
        {
            InitializeComponent();
            InitializeGame();
        }

        private void InitializeGame()
        {
            gameLogic = new GameLogic();
            DifficultyComboBox.SelectedIndex = 0;

            gameTimer = new DispatcherTimer();
            gameTimer.Interval = TimeSpan.FromSeconds(1);
            gameTimer.Tick += GameTimer_Tick;

            UpdateGameField();
            StartGame();
        }

        private void StartGame()
        {
            gameTimer.Start();
            UpdateUI();
        }

        private void GameTimer_Tick(object sender, EventArgs e)
        {
            gameLogic.DecrementTime();
            UpdateUI();

            if (gameLogic.TimeLeft <= 0)
            {
                gameTimer.Stop();
                MessageBox.Show($"Игра окончена! Ваш счет: {gameLogic.CurrentScore}", "Конец игры");
            }
        }

        private void UpdateUI()
        {
            TimeLeftText.Text = gameLogic.TimeLeft.ToString();
            ScoreText.Text = gameLogic.CurrentScore.ToString();
        }

        private void UpdateGameField()
        {
            GameField.Children.Clear();
            int size = (int)gameLogic.CurrentDifficulty;
            GameField.Columns = size;
            GameField.Rows = size;

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    var figure = gameLogic.GameField[i, j];
                    if (figure.Type == FigureType.Empty)
                    {
                        GameField.Children.Add(new Border());
                        continue;
                    }

                    var button = CreateFigureButton(figure);
                    button.Tag = new Point(i, j);
                    button.Click += FigureButton_Click;
                    GameField.Children.Add(button);
                }
            }
        }

        private Button CreateFigureButton(GameFigure figure)
        {
            var button = new Button
            {
                Width = 60,
                Height = 60,
                Margin = new Thickness(5),
                Background = Brushes.White
            };

            Shape shape;
            if (figure.Type == FigureType.Circle)
            {
                shape = new Ellipse
                {
                    Width = 40,
                    Height = 40,
                    Fill = figure.Color == FigureColor.Blue ? blueBrush : greenBrush
                };
            }
            else
            {
                shape = new Polygon
                {
                    Points = new PointCollection { new Point(20, 0), new Point(40, 40), new Point(0, 40) },
                    Fill = figure.Color == FigureColor.Blue ? blueBrush : greenBrush
                };
            }

            button.Content = shape;
            return button;
        }

        private void FigureButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var position = (Point)button.Tag;
            var result = gameLogic.CheckFigure((int)position.X, (int)position.Y);

            if (result)
            {
                MessageBox.Show("Правильно! Вы нашли уникальную фигуру!", "Успех!");
                gameLogic.ResetGame();
                UpdateGameField();
            }

            UpdateUI();
        }

        private void DifficultyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsInitialized) return;

            var difficulty = (DifficultyLevel)DifficultyComboBox.SelectedIndex;
            gameLogic.SetDifficulty(difficulty);
            UpdateGameField();
        }

        private void NewGame_Click(object sender, RoutedEventArgs e)
        {
            gameLogic.ResetGame();
            UpdateGameField();
            StartGame();
        }
    }
}