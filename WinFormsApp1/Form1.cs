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


        // �������� ��������� ������ ��� ���������� ������ � ���������� ���������� (��������, DataGridView).
        private BindingSource bindingSource = new BindingSource();

        public Form1(string userRole)
        {
            InitializeComponent();  // ������������� ����������� ����� (������������� ������������ ���)
            _userRole = userRole;
            ConfigureAccessByRole();
            _databaseManager = new DatabaseManager();
            _bookManager = new BookManager(_databaseManager);

            // ������������� BindingSource � �������� ��������� ������ ��� DataGridView.
            dataGridBooks.DataSource = bindingSource;

            // ������������� ������������ ������� � DataGridView �� ������ ������� ������� �����.
            dataGridBooks.AutoGenerateColumns = true;
        }
        private void AdjustWindowForQRCode(Bitmap qrCodeImage)
        {
            // ������������ ������ ����
            int qrCodeWidth = qrCodeImage.Width; // ������ QR-����
            int padding = 20; // �������

            // ������������ ����������� ������ �����
            int formBorderWidth = this.Width - this.ClientSize.Width; // ������� ����� ������ ������� � ���������� ��������
            int newFormWidth = 650 + qrCodeWidth + padding;

            // ���� ������� ������ ������ �����������, �����������
            if (this.Width < newFormWidth | this.Width > newFormWidth)
            {
                this.Width = newFormWidth;
            }

            // ��������� QR-���
            pictureBoxQRCode.Location = new System.Drawing.Point(this.ClientSize.Width - qrCodeImage.Width - padding, 10);
            pictureBoxQRCode.Size = qrCodeImage.Size;

            // ��������� ����
            this.PerformLayout();
            this.Update();
        }
        private void btnGenerateQRCode_Click(object sender, EventArgs e)
        {
            if (dataGridBooks.SelectedRows.Count > 0)
            {
                // �������� ��������� �����
                var selectedRow = dataGridBooks.SelectedRows[0];
                Book selectedBook = new Book
                {
                    Title = selectedRow.Cells["Title"].Value.ToString(),
                    Author = selectedRow.Cells["Author"].Value.ToString(),
                    Year = Convert.ToInt32(selectedRow.Cells["Year"].Value)
                };

                try
                {
                    // ��������� URL � QR-����
                    _qrCodeGeneratorHelper = new QRCodeGeneratorHelper();
                    string searchUrl = _qrCodeGeneratorHelper.GenerateSearchUrl(selectedBook);
                    Bitmap qrCodeImage = QRCodeGeneratorHelper.GenerateQRCode(searchUrl);

                    // ������������ ������ ����
                    AdjustWindowForQRCode(qrCodeImage);
                    // ����������� QR-���� � PictureBox
                    pictureBoxQRCode.Image = qrCodeImage;


                    MessageBox.Show("QR-��� ������� ������������!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"������ ��� ��������� QR-����: {ex.Message}");
                }
            }
            else
            {
                MessageBox.Show("�������� ����� �� ������.");
            }
        }

        private void ConfigureAccessByRole()
        {
            // ��������� ��� �������� �������� ���������� � ����������� �� ����
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
                // �������� ������ ���� ����� BookManager.
                List<Book> books = _bookManager.LoadBooks();

                // ����������� ������ ���� � ������, ��������� ��� DataGridView.
                var bindingList = new BindingList<Book>(books);
                var bindingSource = new BindingSource(bindingList, null);

                // ����������� BindingSource � DataGridView.
                dataGridBooks.DataSource = bindingSource;

                // ����������� ����������� �������.
                dataGridBooks.Columns["FileData"].Visible = false; // ������ ���� FileData, ���� ��� �� �����.
                dataGridBooks.Columns["Id"].HeaderText = "ID";
                dataGridBooks.Columns["Title"].HeaderText = "��������";
                dataGridBooks.Columns["Author"].HeaderText = "�����";
                dataGridBooks.Columns["Year"].HeaderText = "���";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"������ ��� ���������� ������ ����: {ex.Message}");
            }
        }




        // ���������� ������� ��� ���������� ����� �����.
        private void btnAddBook_Click(object sender, EventArgs e)
        {
            // ��������� ������ �� ��������� ����� ��� ��������, ������ � ����.
            string title = txtTitle.Text;
            string author = txtAuthor.Text;


            if (int.TryParse(txtYear.Text, out int year))
            {
                // �������� ������� ������ �����
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "PDF Files (*.pdf)|*.pdf|Word Documents (*.docx)|*.docx";
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        // �������� ���� � ���������� �����
                        string filePath = openFileDialog.FileName;

                        try
                        {
                            Book newBook = new Book
                            {
                                Id = Guid.NewGuid(),  // ��������� ����������� �������������� ��� �����
                                Title = title,
                                Author = author,
                                Year = year,
                                FileData = File.ReadAllBytes(filePath)  // ������ ����� � ������ ������
                            };
                            // ���������� �����
                            _bookManager.AddBook(newBook);

                            MessageBox.Show("����� ���������!");
                            UpdateBooks(); // ��������� ������ ����
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"������ ��� ���������� �����: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("������ ������������ ���.");
            }
        }

        // ���������� ������� ��� �������� �����.
        private void btnRemoveBook_Click(object sender, EventArgs e)
        {
            // ��������, ������� �� ������.
            if (dataGridBooks.SelectedRows.Count > 0)
            {
                var selectedRow = dataGridBooks.SelectedRows[0];

                // ��������� ������ � ����� �� ��������� ������.
                Book selectedBook = new Book
                {
                    Id = (Guid)selectedRow.Cells["id"].Value,
                    Title = selectedRow.Cells["title"].Value.ToString(),
                    Author = selectedRow.Cells["author"].Value.ToString(),
                    Year = (int)selectedRow.Cells["year"].Value,
                    // ���� FileData ��������� ������, ��� ��� ��� �� ������������ ��� ��������.
                };

                // �������� ����� ����� BookManager.
                _bookManager.RemoveBook(selectedBook);

                MessageBox.Show("����� �������!");
                UpdateBooks(); // ��������� ������ ����
            }
            else
            {
                // ���� ����� �� �������, �������� ��������� � ������������� ������ �����.
                MessageBox.Show("�������� ����� ��� ��������.");
            }
        }

        // ���������� ������� ��� ������ ����� �� ��������.
        private void btnSearchByTitle_Click(object sender, EventArgs e)
        {
            string title = txtTitle.Text;

            // �������� ������ ����
            List<Book> books = _bookManager.GetBooksByTitle(title);

            // ����������� ������ ���� � DataGridView
            dataGridBooks.DataSource = books;
        }

        private void btnSearchByAuthor_Click(object sender, EventArgs e)
        {
            string author = txtAuthor.Text;

            // �������� ������ ����
            List<Book> books = _bookManager.GetBooksByAuthor(author);

            // ����������� ������ ���� � DataGridView
            dataGridBooks.DataSource = books;
        }

        // ���������� ������� ��� ����������� ���� ����.
        private void btnShowAllBooks_Click(object sender, EventArgs e)
        {
            // ����������� ���� ���� � DataGridView.
            UpdateBooks();  // ������������� ������ ���� ����
        }




        private void btnImportBooks_Click(object sender, EventArgs e)
        {
            // ���� ��� ������ JSON �����
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON Files (*.json)|*.json";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    // ������ ����������� �����
                    string json = File.ReadAllText(openFileDialog.FileName);

                    // �������������� JSON � ������ ����
                    List<Book> books = JsonConvert.DeserializeObject<List<Book>>(json);

                    if (books != null && books.Count > 0)
                    {
                        foreach (var book in books)
                        {
                            // ���������, ���������� �� ����� � ����� `id`
                            if (_bookManager.BookExists(book.Id))
                            {
                                // ���������� ����� `id`, ���� ����� � ����� ��� ����
                                book.Id = Guid.NewGuid();
                            }

                            // ��������� �����
                            _bookManager.AddBook(book);
                        }

                        MessageBox.Show("����� ������� ������������� �� JSON!");
                        UpdateBooks(); // ��������� ������ ����
                    }
                    else
                    {
                        MessageBox.Show("���� JSON �� �������� ���� ��� ������ �����������.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"������ ��� ������� ����: {ex.Message}");
                }
            }
        }


        // ������ ��� �������� ���� � ���� (JSON) Filter = "JSON Files (*.json)|*.json"
        private void btnExportBooks_Click(object sender, EventArgs e)
        {
            try
            {
                // ��������� ������ ���� ���� ����� BookManager
                List<Book> books = _bookManager.LoadBooks();

                if (books.Count == 0)
                {
                    MessageBox.Show("� ���� ������ ��� ���� ��� ��������.");
                    return;
                }

                // ���� ���������� �����
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "JSON Files (*.json)|*.json";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // ������������ ������ ���� � JSON
                    string json = JsonConvert.SerializeObject(books, Formatting.Indented);

                    // ���������� JSON � ����
                    File.WriteAllText(saveFileDialog.FileName, json);
                    MessageBox.Show("������ ���� ������� ������������� � JSON!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"������ ��� �������� ����: {ex.Message}");
            }
        }

        private void btnConvertBook_Click(object sender, EventArgs e)
        {
            // ��������, ������� �� ������ � DataGridView
            if (dataGridBooks.SelectedRows.Count > 0)
            {
                var selectedRow = dataGridBooks.SelectedRows[0];
                Guid bookId = (Guid)selectedRow.Cells["Id"].Value;

                var bookManager = new BookManager();
                // ��������� ����� �� ���� ������
                Book selectedBook = _databaseManager.GetBookById(bookId);

                if (selectedBook != null)
                {
                    // �������� ����������� ���� ��� ������ ������� �����
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "PDF Files (*.pdf)|*.pdf|Word Documents (*.docx)|*.docx";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string selectedFormat = Path.GetExtension(saveFileDialog.FileName).ToLower();

                        // ���� ������ ��������� � �������, ������ ��������� ����
                        if ((selectedFormat == ".pdf" && !bookManager.IsPdfFile(selectedBook)) ||
                            (selectedFormat == ".docx" && !bookManager.IsDocxFile(selectedBook)))
                        {
                            // ���� �����, ������������ ����
                            bookManager.ConvertFile(selectedBook, selectedFormat, saveFileDialog.FileName);
                        }
                        else
                        {
                            // ��������� ���� ��� �����������
                            File.WriteAllBytes(saveFileDialog.FileName, selectedBook.FileData);
                            MessageBox.Show("���� �������� ��� �����������!");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("�������� ����� ��� �����������!");
            }
        }


    }
}