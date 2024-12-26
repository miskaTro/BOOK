using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using Npgsql;
using Newtonsoft.Json;
using Aspose.Pdf;
using Aspose.Words;
using System.Xml;

namespace BookCollectionApp
{
    public partial class Form1 : Form
    {

        private DatabaseManager _databaseManager;
        private BookManager _bookManager;
        private QRCodeGeneratorHelper _qrCodeGeneratorHelper;
        private readonly string _userRole;


        // Создание источника данных для связывания данных с элементами управления (например, DataGridView).
        private BindingSource bindingSource = new BindingSource();

        public Form1(string userRole)
        {
            InitializeComponent();  // Инициализация компонентов формы (автоматически генерируемый код)
            _userRole = userRole;
            ConfigureAccessByRole();
            _databaseManager = new DatabaseManager();
            _bookManager = new BookManager(_databaseManager);

            // Устанавливаем BindingSource в качестве источника данных для DataGridView.
            dataGridBooks.DataSource = bindingSource;

            // Автоматически генерировать столбцы в DataGridView на основе свойств объекта книги.
            dataGridBooks.AutoGenerateColumns = true;
        }
        private void AdjustWindowForQRCode(Bitmap qrCodeImage)
        {
            // Рассчитываем ширину окна
            int qrCodeWidth = qrCodeImage.Width; // Ширина QR-кода
            int padding = 20; // Отступы

            // Рассчитываем необходимую ширину формы
            int formBorderWidth = this.Width - this.ClientSize.Width; // Разница между полной шириной и клиентской областью
            int newFormWidth = 650 + qrCodeWidth + padding;

            // Если текущая ширина меньше необходимой, увеличиваем
            if (this.Width < newFormWidth | this.Width > newFormWidth)
            {
                this.Width = newFormWidth;
            }

            // Размещаем QR-код
            pictureBoxQRCode.Location = new System.Drawing.Point(this.ClientSize.Width - qrCodeImage.Width - padding, 10);
            pictureBoxQRCode.Size = qrCodeImage.Size;

            // Обновляем окно
            this.PerformLayout();
            this.Update();
        }
        private void btnGenerateQRCode_Click(object sender, EventArgs e)
        {
            if (dataGridBooks.SelectedRows.Count > 0)
            {
                // Получаем выбранную книгу
                var selectedRow = dataGridBooks.SelectedRows[0];
                Book selectedBook = new Book
                {
                    Title = selectedRow.Cells["Title"].Value.ToString(),
                    Author = selectedRow.Cells["Author"].Value.ToString(),
                    Year = Convert.ToInt32(selectedRow.Cells["Year"].Value)
                };

                try
                {
                    // Генерация URL и QR-кода
                    _qrCodeGeneratorHelper = new QRCodeGeneratorHelper();
                    string searchUrl = _qrCodeGeneratorHelper.GenerateSearchUrl(selectedBook);
                    Bitmap qrCodeImage = QRCodeGeneratorHelper.GenerateQRCode(searchUrl);

                    // Подстраиваем ширину окна
                    AdjustWindowForQRCode(qrCodeImage);
                    // Отображение QR-кода в PictureBox
                    pictureBoxQRCode.Image = qrCodeImage;


                    MessageBox.Show("QR-код успешно сгенерирован!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при генерации QR-кода: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("Выберите книгу из списка.");
            }
        }

        private void ConfigureAccessByRole()
        {
            // Отключаем или скрываем элементы интерфейса в зависимости от роли
            if (_userRole == "Admin")
            {
                btnImport.Enabled = true;
                btnExport.Enabled = true;
                btnConvert.Enabled = true;
            }
            else if (_userRole == "User")
            {
                btnImport.Enabled = false;
                btnExport.Enabled = false;
                btnConvert.Enabled = false;
            }
        }
        private void UpdateBooks()
        {
            try
            {
                // Получаем список книг через BookManager.
                List<Book> books = _bookManager.LoadBooks();

                // Преобразуем список книг в формат, пригодный для DataGridView.
                var bindingList = new BindingList<Book>(books);
                var bindingSource = new BindingSource(bindingList, null);

                // Привязываем BindingSource к DataGridView.
                dataGridBooks.DataSource = bindingSource;

                // Настраиваем отображение колонок.
                dataGridBooks.Columns["FileData"].Visible = false; // Скрыть поле FileData, если оно не нужно.
                dataGridBooks.Columns["Id"].HeaderText = "ID";
                dataGridBooks.Columns["Title"].HeaderText = "Название";
                dataGridBooks.Columns["Author"].HeaderText = "Автор";
                dataGridBooks.Columns["Year"].HeaderText = "Год";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении списка книг: {ex.Message}");
            }
        }




        // Обработчик события для добавления новой книги.
        private void btnAddBook_Click(object sender, EventArgs e)
        {
            // Получение данных из текстовых полей для названия, автора и года.
            string title = txtTitle.Text;
            string author = txtAuthor.Text;


            if (int.TryParse(txtYear.Text, out int year))
            {
                // Открытие диалога выбора файла
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "PDF Files (*.pdf)|*.pdf|Word Documents (*.docx)|*.docx";
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // Получаем путь к выбранному файлу
                        string filePath = openFileDialog.FileName;

                        try
                        {
                            Book newBook = new Book
                            {
                                Id = Guid.NewGuid(),  // Генерация уникального идентификатора для книги
                                Title = title,
                                Author = author,
                                Year = year,
                                FileData = File.ReadAllBytes(filePath)  // Чтение файла в массив байтов
                            };
                            // Добавление книги
                            _bookManager.AddBook(newBook);

                            MessageBox.Show("Книга добавлена!");
                            UpdateBooks(); // Обновляем список книг
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при добавлении книги: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Введен некорректный год.");
            }
        }

        // Обработчик события для удаления книги.
        private void btnRemoveBook_Click(object sender, EventArgs e)
        {
            // Проверка, выбрана ли строка.
            if (dataGridBooks.SelectedRows.Count > 0)
            {
                var selectedRow = dataGridBooks.SelectedRows[0];

                // Получение данных о книге из выбранной строки.
                Book selectedBook = new Book
                {
                    Id = (Guid)selectedRow.Cells["id"].Value,
                    Title = selectedRow.Cells["title"].Value.ToString(),
                    Author = selectedRow.Cells["author"].Value.ToString(),
                    Year = (int)selectedRow.Cells["year"].Value,
                    // Поле FileData оставляем пустым, так как оно не используется при удалении.
                };

                // Удаление книги через BookManager.
                _bookManager.RemoveBook(selectedBook);

                MessageBox.Show("Книга удалена!");
                UpdateBooks(); // Обновляем список книг
            }
            else
            {
                // Если книга не выбрана, показать сообщение о необходимости выбора книги.
                MessageBox.Show("Выберите книгу для удаления.");
            }
        }

        // Обработчик события для поиска книги по названию.
        private void btnSearchByTitle_Click(object sender, EventArgs e)
        {
            string title = txtTitle.Text;

            // Получаем список книг
            List<Book> books = _bookManager.GetBooksByTitle(title);

            // Привязываем список книг к DataGridView
            dataGridBooks.DataSource = books;
        }

        private void btnSearchByAuthor_Click(object sender, EventArgs e)
        {
            string author = txtAuthor.Text;

            // Получаем список книг
            List<Book> books = _bookManager.GetBooksByAuthor(author);

            // Привязываем список книг к DataGridView
            dataGridBooks.DataSource = books;
        }

        // Обработчик события для отображения всех книг.
        private void btnShowAllBooks_Click(object sender, EventArgs e)
        {
            // Отображение всех книг в DataGridView.
            UpdateBooks();  // Перезагружаем список всех книг
        }




        private void btnImportBooks_Click(object sender, EventArgs e)
        {
            // Окно для выбора JSON файла
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON Files (*.json)|*.json";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // Чтение содержимого файла
                    string json = File.ReadAllText(openFileDialog.FileName);

                    // Десериализация JSON в список книг
                    List<Book> books = JsonConvert.DeserializeObject<List<Book>>(json);

                    if (books != null && books.Count > 0)
                    {
                        foreach (var book in books)
                        {
                            // Проверяем, существует ли книга с таким `id`
                            if (_bookManager.BookExists(book.Id))
                            {
                                // Генерируем новый `id`, если книга с таким уже есть
                                book.Id = Guid.NewGuid();
                            }

                            // Добавляем книгу
                            _bookManager.AddBook(book);
                        }

                        MessageBox.Show("Книги успешно импортированы из JSON!");
                        UpdateBooks(); // Обновляем список книг
                    }
                    else
                    {
                        MessageBox.Show("Файл JSON не содержит книг или данные некорректны.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при импорте книг: {ex.Message}");
                }
            }
        }


        // Кнопка для экспорта книг в файл (JSON) Filter = "JSON Files (*.json)|*.json"
        private void btnExportBooks_Click(object sender, EventArgs e)
        {
            try
            {
                // Извлекаем список всех книг через BookManager
                List<Book> books = _bookManager.LoadBooks();

                if (books.Count == 0)
                {
                    MessageBox.Show("В базе данных нет книг для экспорта.");
                    return;
                }

                // Окно сохранения файла
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "JSON Files (*.json)|*.json";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Сериализация списка книг в JSON
                    string json = JsonConvert.SerializeObject(books, Formatting.Indented);

                    // Сохранение JSON в файл
                    File.WriteAllText(saveFileDialog.FileName, json);
                    MessageBox.Show("Список книг успешно экспортирован в JSON!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте книг: {ex.Message}");
            }
        }

        private void btnConvertBook_Click(object sender, EventArgs e)
        {
            // Проверка, выбрана ли строка в DataGridView
            if (dataGridBooks.SelectedRows.Count > 0)
            {
                var selectedRow = dataGridBooks.SelectedRows[0];
                Guid bookId = (Guid)selectedRow.Cells["Id"].Value;

                var bookManager = new BookManager();
                // Извлекаем книгу из базы данных
                Book selectedBook = _databaseManager.GetBookById(bookId);

                if (selectedBook != null)
                {
                    // Открытие диалогового окна для выбора формата файла
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "PDF Files (*.pdf)|*.pdf|Word Documents (*.docx)|*.docx";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string selectedFormat = Path.GetExtension(saveFileDialog.FileName).ToLower();

                        // Если формат совпадает с текущим, просто сохраняем файл
                        if ((selectedFormat == ".pdf" && !bookManager.IsPdfFile(selectedBook)) ||
                            (selectedFormat == ".docx" && !bookManager.IsDocxFile(selectedBook)))
                        {
                            // Если нужно, конвертируем файл
                            bookManager.ConvertFile(selectedBook, selectedFormat, saveFileDialog.FileName);
                        }
                        else
                        {
                            // Сохраняем файл без конвертации
                            File.WriteAllBytes(saveFileDialog.FileName, selectedBook.FileData);
                            MessageBox.Show("Файл сохранен без конвертации!");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите книгу для конвертации!");
            }
        }


    }
}