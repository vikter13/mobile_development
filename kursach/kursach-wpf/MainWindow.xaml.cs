using System.Windows;
using KursachLibrary;

namespace kursach_wpf
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DatabaseHelper.InitializeDatabase();
        }

        private void QuizButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as FrameworkElement;
            var quizId = int.Parse(button.Tag.ToString());
            var quizWindow = new QuizWindow(quizId);
            quizWindow.ShowDialog();
        }
    }
}
