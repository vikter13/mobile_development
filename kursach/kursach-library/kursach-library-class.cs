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

    public static class DatabaseHelper
    {
        private const string DatabasePath = "database.db";

        public static void InitializeDatabase()
        {
            using var connection = new SQLiteConnection($"Data Source={DatabasePath}");
            connection.Open();

            var createUsersTable = "CREATE TABLE IF NOT EXISTS Users (Id INTEGER PRIMARY KEY, Username TEXT, Password TEXT);";
            var createQuizzesTable = "CREATE TABLE IF NOT EXISTS Quizzes (Id INTEGER PRIMARY KEY, Title TEXT, IsCompleted INTEGER);";

            using var command = new SQLiteCommand(createUsersTable, connection);
            command.ExecuteNonQuery();
            command.CommandText = createQuizzesTable;
            command.ExecuteNonQuery();

            // Add dummy quizzes if empty
            command.CommandText = "SELECT COUNT(*) FROM Quizzes;";
            var count = Convert.ToInt32(command.ExecuteScalar());
            if (count == 0)
            {
                command.CommandText = "INSERT INTO Quizzes (Title, IsCompleted) VALUES ('Quiz 1', 0), ('Quiz 2', 0), ('Quiz 3', 0), ('Quiz 4', 0);";
                command.ExecuteNonQuery();
            }
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
