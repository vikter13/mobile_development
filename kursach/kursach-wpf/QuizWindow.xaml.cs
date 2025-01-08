using System.Windows;
using KursachLibrary;

namespace kursach_wpf
{
    public partial class QuizWindow : Window
    {
        private int _quizId;

        public QuizWindow(int quizId)
        {
            InitializeComponent();
            _quizId = quizId;
            LoadQuiz();
        }

        private void LoadQuiz()
        {
            var quizzes = DatabaseHelper.GetQuizzes();
            var quiz = quizzes.Find(q => q.Id == _quizId);
            QuizTitle.Text = quiz?.Title ?? "Unknown Quiz";
        }

        private void CompleteQuiz_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Quiz completed!");
            Close();
        }
    }
}
