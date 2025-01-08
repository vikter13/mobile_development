using System.Windows;
using KursachLibrary;

namespace kursach_wpf
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void Register_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text;
            string password = PasswordBox.Password;

            if (!string.IsNullOrWhiteSpace(username) && !string.IsNullOrWhiteSpace(password))
            {
                DatabaseHelper.AddUser(new User { Username = username, Password = password });
                MessageBox.Show("Registration successful!");
                Close();
            }
            else
            {
                MessageBox.Show("Please fill in all fields.");
            }
        }
    }
}
