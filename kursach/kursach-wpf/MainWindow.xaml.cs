using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using KursachLibrary;

namespace kursach_wpf
{
    public partial class MainWindow : Window
    {
        private readonly int _userId;

        public MainWindow(int userId)
        {
            InitializeComponent();
            _userId = userId;
            UpdateButtonStatus();
        }

        private void QuizButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as FrameworkElement;
            var quizId = int.Parse(button.Tag.ToString());
            var quizWindow = new QuizWindow(quizId, _userId);
            quizWindow.ShowDialog();
            UpdateButtonStatus(); // Обновляем кнопки после завершения викторины.
        }

        private void UpdateButtonStatus()
        {
            foreach (var child in QuizGrid.Children)
            {
                if (child is Button button)
                {
                    var quizId = int.Parse(button.Tag.ToString());
                    var result = DatabaseHelper.GetQuizResult(_userId, quizId);

                    button.Background = result switch
                    {
                        null => Brushes.White,
                        0 => Brushes.Red,
                        1 => Brushes.Green,
                        _ => Brushes.White
                    };
                }
            }
        }
    }
}
