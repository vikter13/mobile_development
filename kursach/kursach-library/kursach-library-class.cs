using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace KursachLibrary
{
    public class Quiz
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class Question
    {
        public int Id { get; set; }
        public string QuestionText { get; set; }
        public string ImagePath { get; set; }
        public List<string> Answers { get; set; } = new List<string>();
        public List<string> AnswerImagePaths { get; set; } = new List<string>();
        public int CorrectAnswer { get; set; }
    }

    public static class DatabaseHelper
    {
        private const string DatabasePath = "C:\\stud-folder\\mobile_development\\kursach\\database.db";

        public static void InitializeDatabase()
        {
            using var connection = new SQLiteConnection($"Data Source={DatabasePath}");
            connection.Open();

            var createUsersTable = "CREATE TABLE IF NOT EXISTS Users (Id INTEGER PRIMARY KEY, Username TEXT, Password TEXT);";
            var createQuizzesTable = "CREATE TABLE IF NOT EXISTS Quizzes (Id INTEGER PRIMARY KEY, Title TEXT, IsCompleted INTEGER);";
            var createQuestionsTable = @"
            CREATE TABLE IF NOT EXISTS Questions (
                Id INTEGER PRIMARY KEY, 
                QuizId INTEGER, 
                QuestionText TEXT, 
                ImagePath TEXT, 
                Answer1 TEXT, 
                Answer2 TEXT, 
                Answer3 TEXT, 
                Answer4 TEXT, 
                AnswerImage1 TEXT, 
                AnswerImage2 TEXT, 
                AnswerImage3 TEXT, 
                AnswerImage4 TEXT, 
                CorrectAnswer INTEGER
            );";
            var createResultsTable = @"
            CREATE TABLE IF NOT EXISTS Results (
                Id INTEGER PRIMARY KEY, 
                UserId INTEGER, 
                QuizId INTEGER, 
                QuestionId INTEGER, 
                IsCorrect INTEGER
            );";

            using var command = new SQLiteCommand(createUsersTable, connection);
            command.ExecuteNonQuery();
            command.CommandText = createQuizzesTable;
            command.ExecuteNonQuery();
            command.CommandText = createQuestionsTable;
            command.ExecuteNonQuery();
            command.CommandText = createResultsTable;
            command.ExecuteNonQuery();

            command.CommandText = "SELECT COUNT(*) FROM Questions;";
            var count = Convert.ToInt32(command.ExecuteScalar());
            if (count == 0)
            {
                command.CommandText = @"
                INSERT INTO Questions (QuizId, QuestionText, ImagePath, Answer1, Answer2, Answer3, Answer4, AnswerImage1, AnswerImage2, AnswerImage3, AnswerImage4, CorrectAnswer)
                VALUES
                (1, '2 + 2?', NULL, '1', '2', '4', '3', NULL, NULL, NULL, NULL, 2),
                (2, 'What is the capital of Great Britan?', NULL, 'Berlin', 'Madrid', 'Paris', 'London', NULL, NULL, NULL, NULL, 3),
                (3, NULL, 'C:\\stud-folder\\mobile_development\\kursach\\ezhik.jpg', 'ezhik', 'ptiza', 'mishka', 'koshka', NULL, NULL, NULL, NULL, 0),
                (4, 'Who is the China?', NULL, NULL, NULL, NULL, NULL, 'C:\\stud-folder\\mobile_development\\kursach\\ezhik.jpg', 'C:\\stud-folder\\mobile_development\\kursach\\russia.jpg', 'C:\\stud-folder\\mobile_development\\kursach\\china.jpg', 'C:\\stud-folder\\mobile_development\\kursach\\sever-korea.jpg', 2);";
                command.ExecuteNonQuery();
            }
        }

        public static List<Question> GetQuestions(int quizId)
        {
            using var connection = new SQLiteConnection($"Data Source={DatabasePath}");
            connection.Open();

            var selectQuery = @"
            SELECT Id, QuestionText, ImagePath, Answer1, Answer2, Answer3, Answer4, 
                   AnswerImage1, AnswerImage2, AnswerImage3, AnswerImage4, CorrectAnswer 
            FROM Questions WHERE QuizId = @QuizId;";
            using var command = new SQLiteCommand(selectQuery, connection);
            command.Parameters.AddWithValue("@QuizId", quizId);
            using var reader = command.ExecuteReader();

            var questions = new List<Question>();
            while (reader.Read())
            {
                var question = new Question
                {
                    Id = reader.GetInt32(0),
                    QuestionText = reader.IsDBNull(1) ? null : reader.GetString(1),
                    ImagePath = reader.IsDBNull(2) ? null : reader.GetString(2),
                    Answers = new List<string>
                    {
                        reader.IsDBNull(3) ? null : reader.GetString(3),
                        reader.IsDBNull(4) ? null : reader.GetString(4),
                        reader.IsDBNull(5) ? null : reader.GetString(5),
                        reader.IsDBNull(6) ? null : reader.GetString(6)
                    },
                    AnswerImagePaths = new List<string>
                    {
                        reader.IsDBNull(7) ? null : reader.GetString(7),
                        reader.IsDBNull(8) ? null : reader.GetString(8),
                        reader.IsDBNull(9) ? null : reader.GetString(9),
                        reader.IsDBNull(10) ? null : reader.GetString(10)
                    },
                    CorrectAnswer = reader.GetInt32(11)
                };

                questions.Add(question);
            }

            return questions;
        }

        public static void SaveResult(int userId, int quizId, int questionId, bool isCorrect)
        {
            using var connection = new SQLiteConnection($"Data Source={DatabasePath}");
            connection.Open();

            var deleteQuery = "DELETE FROM Results WHERE UserId = @UserId AND QuizId = @QuizId AND QuestionId = @QuestionId;";
            using var deleteCommand = new SQLiteCommand(deleteQuery, connection);
            deleteCommand.Parameters.AddWithValue("@UserId", userId);
            deleteCommand.Parameters.AddWithValue("@QuizId", quizId);
            deleteCommand.Parameters.AddWithValue("@QuestionId", questionId);
            deleteCommand.ExecuteNonQuery();

            var insertQuery = @"
            INSERT INTO Results (UserId, QuizId, QuestionId, IsCorrect) 
            VALUES (@UserId, @QuizId, @QuestionId, @IsCorrect);";
            using var command = new SQLiteCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@QuizId", quizId);
            command.Parameters.AddWithValue("@QuestionId", questionId);
            command.Parameters.AddWithValue("@IsCorrect", isCorrect ? 1 : 0);
            command.ExecuteNonQuery();
        }

        public static int? GetQuizResult(int userId, int quizId)
        {
            using var connection = new SQLiteConnection($"Data Source={DatabasePath}");
            connection.Open();

            var query = "SELECT MIN(IsCorrect) FROM Results WHERE UserId = @UserId AND QuizId = @QuizId;";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@QuizId", quizId);

            var result = command.ExecuteScalar();
            return result == DBNull.Value ? null : (int?)Convert.ToInt32(result);
        }

        public static int GetUserId(string username)
        {
            using var connection = new SQLiteConnection($"Data Source={DatabasePath}");
            connection.Open();

            var query = "SELECT Id FROM Users WHERE Username = @Username;";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);

            var result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : -1;
        }

        public static bool AuthenticateUser(string username, string password)
        {
            using var connection = new SQLiteConnection($"Data Source={DatabasePath}");
            connection.Open();

            var query = "SELECT COUNT(*) FROM Users WHERE Username = @Username AND Password = @Password;";
            using var command = new SQLiteCommand(query, connection);
            command.Parameters.AddWithValue("@Username", username);
            command.Parameters.AddWithValue("@Password", password);

            var count = Convert.ToInt32(command.ExecuteScalar());
            return count > 0;
        }

        public static void AddUser(User user)
        {
            using var connection = new SQLiteConnection($"Data Source={DatabasePath}");
            connection.Open();

            var insertQuery = "INSERT INTO Users (Username, Password) VALUES (@Username, @Password);";
            using var command = new SQLiteCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@Username", user.Username);
            command.Parameters.AddWithValue("@Password", user.Password);
            command.ExecuteNonQuery();
        }

        public static List<Quiz> GetQuizzes()
        {
            using var connection = new SQLiteConnection($"Data Source={DatabasePath}");
            connection.Open();

            var selectQuery = "SELECT Id, Title, IsCompleted FROM Quizzes;";
            using var command = new SQLiteCommand(selectQuery, connection);
            using var reader = command.ExecuteReader();

            var quizzes = new List<Quiz>();
            while (reader.Read())
            {
                quizzes.Add(new Quiz
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    IsCompleted = reader.GetInt32(2) == 1
                });
            }

            return quizzes;
        }
    }
}
