using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Data;

namespace BookCollectionApp
{
    public class DatabaseManager
    {
        private static readonly string ConnectionString;

        static DatabaseManager()
        {
            ConnectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
        }
        public void InsertBook(Book book)
        {
            string query = "INSERT INTO books (id, title, author, year, file_data) VALUES (@id, @title, @author, @year, @file_data)";
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("id", book.Id);
                    command.Parameters.AddWithValue("title", book.Title);
                    command.Parameters.AddWithValue("author", book.Author);
                    command.Parameters.AddWithValue("year", book.Year);
                    command.Parameters.AddWithValue("file_data", book.FileData);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<Book> GetAllBooks()
        {
            string query = "SELECT id, title, author, year, file_data FROM books";
            List<Book> books = new List<Book>();
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        books.Add(new Book
                        {
                            Id = reader.GetGuid(reader.GetOrdinal("id")),
                            Title = reader.GetString(reader.GetOrdinal("title")),
                            Author = reader.GetString(reader.GetOrdinal("author")),
                            Year = reader.GetInt32(reader.GetOrdinal("year")),
                            FileData = reader["file_data"] as byte[] // Если null, вернет null
                        });
                    }
                }
            }
            return books;
        }

        public void DeleteBook(Book book)
        {
            string query = "DELETE FROM books WHERE id = @id";
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("id", book.Id);
                    command.ExecuteNonQuery();
                }
            }
        }

        public List<Book> SearchBooksByTitle(string title)
        {
            string query = "SELECT id, title, author, year, file_data FROM books WHERE title ILIKE @title";
            List<Book> books = new List<Book>();

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("title", "%" + title + "%");
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            books.Add(new Book
                            {
                                Id = reader.GetGuid(0),
                                Title = reader.GetString(1),
                                Author = reader.GetString(2),
                                Year = reader.GetInt32(3),
                                FileData = reader["file_data"] as byte[] // Чтение файла, если он есть
                            });
                        }
                    }
                }
            }

            return books;
        }

        public List<Book> SearchBooksByAuthor(string author)
        {
            string query = "SELECT id, title, author, year, file_data FROM books WHERE author ILIKE @author";
            List<Book> books = new List<Book>();

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("author", "%" + author + "%");
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            books.Add(new Book
                            {
                                Id = reader.GetGuid(0),
                                Title = reader.GetString(1),
                                Author = reader.GetString(2),
                                Year = reader.GetInt32(3),
                                FileData = reader["file_data"] as byte[] // Чтение файла, если он есть
                            });
                        }
                    }
                }
            }

            return books;
        }

        public bool BookExists(Guid bookId)
        {
            string query = "SELECT COUNT(1) FROM books WHERE id = @id";
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("id", bookId);
                    return Convert.ToInt32(command.ExecuteScalar()) > 0;
                }
            }
        }

        public Book GetBookById(Guid bookId)
        {
            Book book = null;
            string query = "SELECT id, title, author, year, file_data FROM books WHERE id = @id";

            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("id", bookId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            book = new Book
                            {
                                Id = reader.GetGuid(0),
                                Title = reader.GetString(1),
                                Author = reader.GetString(2),
                                Year = reader.GetInt32(3),
                                FileData = (byte[])reader["file_data"]
                            };
                        }
                    }
                }
            }

            return book;
        }

        // Метод для регистрации пользователя
        public void RegisterUser(string username, string passwordHash)
        {
            string query = "INSERT INTO users (username, password_hash) VALUES (@username, @passwordHash)";
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("username", username);
                    command.Parameters.AddWithValue("passwordHash", passwordHash);
                    command.Parameters.AddWithValue("role", "User");
                    command.ExecuteNonQuery();
                }
            }
        }

        // Метод для получения хэша пароля по имени пользователя
        public string GetPasswordHash(string username)
        {
            string query = "SELECT password_hash FROM users WHERE username = @username";
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("username", username);
                    var result = command.ExecuteScalar();
                    return result?.ToString(); // Возвращаем строку или null, если пользователь не найден
                }
            }
        }

        public string GetUserRole(string username)
        {
            string query = "SELECT role FROM users WHERE username = @username";
            using (var connection = new NpgsqlConnection(ConnectionString))
            {
                connection.Open();
                using (var command = new NpgsqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("username", username);
                    var result = command.ExecuteScalar();
                    return result?.ToString(); // Возвращаем роль или null, если пользователь не найден
                }
            }
        }
    }
}