using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
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

            // Установка текста и индекса ответа для каждой кнопки.
            Answer1Button.Content = question.Answers[0];
            Answer1Button.Tag = 1; // Устанавливаем индекс ответа.

            Answer2Button.Content = question.Answers[1];
            Answer2Button.Tag = 2;

            Answer3Button.Content = question.Answers[2];
            Answer3Button.Tag = 3;

            Answer4Button.Content = question.Answers[3];
            Answer4Button.Tag = 4;
        }


        private void AnswerButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button?.Tag == null || !int.TryParse(button.Tag.ToString(), out int selectedAnswer))
            {
                MessageBox.Show("Ошибка обработки ответа. Попробуйте снова.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Проверяем, соответствует ли выбранный ответ правильному.
            var isCorrect = selectedAnswer == _questions[_currentQuestionIndex].CorrectAnswer;

            // Сохраняем результат в базе данных.
            DatabaseHelper.SaveResult(_userId, _quizId, _questions[_currentQuestionIndex].Id, isCorrect);

            // Показываем результат пользователю.
            MessageBox.Show(
                isCorrect ? "Правильно!" : "Неправильно!",
                "Результат",
                MessageBoxButton.OK,
                isCorrect ? MessageBoxImage.Information : MessageBoxImage.Error
            );

            // Переходим к следующему вопросу.
            _currentQuestionIndex++;
            LoadQuestion();
        }


    }
}
