using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using KursachLibrary;

namespace kursach_wpf
{
    public partial class QuizWindow : Window
    {
        private List<Question> _questions;
        private int _currentQuestionIndex;
        private int _quizId;
        private int _userId;

        public QuizWindow(int quizId, int userId)
        {
            InitializeComponent();
            _quizId = quizId;
            _userId = userId;
            _questions = DatabaseHelper.GetQuestions(quizId);

            if (_questions.Count == 0)
            {
                MessageBox.Show("Вопросов в этом квизе нет!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
                return;
            }

            _currentQuestionIndex = 0;
            LoadQuestion();
        }

        private void LoadQuestion()
        {
            if (_currentQuestionIndex >= _questions.Count)
            {
                MessageBox.Show("Квиз завершён!", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
                return;
            }

            var question = _questions[_currentQuestionIndex];
            QuestionText.Text = question.QuestionText;

            if (!string.IsNullOrEmpty(question.ImagePath) && System.IO.File.Exists(question.ImagePath))
            {
                QuestionImage.Source = new BitmapImage(new Uri(question.ImagePath, UriKind.Absolute));
                QuestionImage.Visibility = Visibility.Visible;
            }
            else
            {
                QuestionImage.Visibility = Visibility.Collapsed;
            }

            SetButtonContent(Answer1Button, Answer1Image, question.Answers[0], question.AnswerImagePaths[0], 0);
            SetButtonContent(Answer2Button, Answer2Image, question.Answers[1], question.AnswerImagePaths[1], 1);
            SetButtonContent(Answer3Button, Answer3Image, question.Answers[2], question.AnswerImagePaths[2], 2);
            SetButtonContent(Answer4Button, Answer4Image, question.Answers[3], question.AnswerImagePaths[3], 3);
        }

        private void SetButtonContent(Button button, Image image, string answerText, string imagePath, int answerIndex)
        {
            if (!string.IsNullOrEmpty(imagePath) && System.IO.File.Exists(imagePath))
            {
                image.Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                image.Visibility = Visibility.Visible;
                button.Content = image; 
            }
            else if (!string.IsNullOrEmpty(answerText))
            {
                image.Visibility = Visibility.Collapsed;
                button.Content = answerText; 
            }
            else
            {
                image.Visibility = Visibility.Collapsed;
                button.Content = string.Empty;
            }

            button.Tag = answerIndex;
        }


        private void AnswerButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button?.Tag == null || !int.TryParse(button.Tag.ToString(), out int selectedAnswer))
            {
                MessageBox.Show("Ошибка обработки ответа. Попробуйте снова.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var isCorrect = selectedAnswer == _questions[_currentQuestionIndex].CorrectAnswer;

            DatabaseHelper.SaveResult(_userId, _quizId, _questions[_currentQuestionIndex].Id, isCorrect);

            MessageBox.Show(
                isCorrect ? "Правильно!" : "Неправильно!",
                "Результат",
                MessageBoxButton.OK,
                isCorrect ? MessageBoxImage.Information : MessageBoxImage.Error
            );

            Close();
        }
    }
}
