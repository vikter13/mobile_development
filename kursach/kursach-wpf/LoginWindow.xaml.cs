using System.Windows;
using KursachLibrary;

namespace kursach_wpf
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            DatabaseHelper.InitializeDatabase();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text;
            string password = PasswordBox.Password;

            if (DatabaseHelper.AuthenticateUser(username, password))
            {
                var userId = DatabaseHelper.GetUserId(username); 
                MessageBox.Show("Login successful!");
                var mainWindow = new MainWindow(userId); 
                mainWindow.Show();
                Close();
            }
            else
            {
                MessageBox.Show("Invalid username or password.");
            }
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            var registerWindow = new RegisterWindow();
            registerWindow.ShowDialog();
        }
    }
}
