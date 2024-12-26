using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;


using System.Collections.Generic;
using Npgsql;
using BCrypt.Net;

namespace BookCollectionApp
{
    public partial class LoginForm : Form
    {
        private DatabaseManager _databaseManager;
        private BookManager _bookManager;
        private string _currentUserRole;

        public LoginForm()
        {
            _databaseManager = new DatabaseManager();
            _bookManager = new BookManager(_databaseManager);
            InitializeComponent();
        }

        // Обработчик кнопки регистрации
        private void btnRegister_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Имя пользователя и пароль не могут быть пустыми.");
                return;
            }

            try
            {
                // Хэшируем пароль
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

                // Регистрируем пользователя через DatabaseManager
                _databaseManager.RegisterUser(username, passwordHash);
                MessageBox.Show("Регистрация успешна!");
            }
            catch (PostgresException ex) when (ex.SqlState == "23505") // Проверка уникальности имени
            {
                MessageBox.Show("Такой пользователь уже существует.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
        // Обработчик кнопки входа
        // Обработчик кнопки входа
        private void btnLogin_Click(object sender, EventArgs e)
        {
            string username = txtUsername.Text;
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Имя пользователя и пароль не могут быть пустыми.");
                return;
            }

            try
            {
                // Получаем хэш пароля через DatabaseManager
                string storedHash = _databaseManager.GetPasswordHash(username);

                if (!string.IsNullOrEmpty(storedHash))
                {
                    // Проверяем пароль
                    if (BCrypt.Net.BCrypt.Verify(password, storedHash))
                    {
                        // Получаем роль пользователя
                        _currentUserRole = _databaseManager.GetUserRole(username);
                        MessageBox.Show($"Авторизация успешна! Ваша роль: {_currentUserRole}");
                        Form1 mainForm = new Form1(_currentUserRole); // Создаём объект главной формы
                        mainForm.Show(); // Открываем главную форму
                        this.Hide(); // Скрываем текущую форму
                    }
                    else
                    {
                        MessageBox.Show("Неправильный пароль.");
                    }
                }
                else
                {
                    MessageBox.Show("Пользователь не найден.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }
    }
}
