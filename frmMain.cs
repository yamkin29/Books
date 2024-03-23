using Npgsql;
using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Books
{
    public partial class FrmMain : Form
    {
        private readonly int _pageSize = 5;  //Размер страницы, используемый для пагинации
        private readonly string _path;
        private readonly string _connstring = String.Format("Server={0};Port={1};" +
                                                            "User Id={2};Password={3};Database={4};", 
            "81.163.31.97", 5433, "postgres",
            "qwerty", "postgres");
        
        private int _currentPageIndex = 1;  //Индекс текущей страницы
        private int _totalPages = 0;  //Общее кол-во страницы
        private int _rowIndex = -1;  //Индекс выбранной строки в DataGridView
        private string _sql;
        
        private DataTable _dataTable; //Таблица для хранения данных
        private NpgsqlConnection _connection;  
        private NpgsqlCommand _command;
        private DataGridViewImageColumn _imageColumn;


        public FrmMain()
        {
            InitializeComponent();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            _connection = new NpgsqlConnection(_connstring);
            CreateImageColumn();           
            
            Select();  //Вызов метода для загрузки данных из базы данных
            CalculateTotalPages();  //Вычисление общего кол-ва страниц
                                    
            dgvData.DataSource = GetCurrentRecords(_currentPageIndex, _connection);

            DisplayingHeaders();

            DisplayingBookTitles();
        }

        private void DisplayingHeaders()
        {
            dgvData.Columns["st_image"].Visible = false;
            dgvData.Columns[0].HeaderText = "image";
            dgvData.Columns[1].HeaderText = "id";
            dgvData.Columns[2].HeaderText = "titlebook";
            dgvData.Columns[3].HeaderText = "author";
            dgvData.Columns[4].HeaderText = "year_of_release";
            dgvData.Columns[5].HeaderText = "isbn";
            dgvData.Columns[6].HeaderText = "short_description";
            dgvData.Columns[7].HeaderText = "genre";
        }

        private void DisplayingBookTitles()
        {
            // Отображение обложки книги
            for (int i = 0; i < dgvData.Rows.Count; i++)
            {
                try
                {
                    byte[] imageData = (byte[])dgvData.Rows[i].Cells["st_image"].Value;
                    if (imageData != null && imageData.Length > 0)
                    {
                        using (MemoryStream ms = new MemoryStream(imageData))
                        {
                            Image image = Image.FromStream(ms);
                            dgvData.Rows[i].Cells["imageColumn"].Value = image;
                        }
                    }
                    else
                    {
                        dgvData.Rows[i].Cells["imageColumn"].Value = null;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("There is no image");
                }
            }
        }

        private void CreateImageColumn()
        {
            _imageColumn = new DataGridViewImageColumn();
            {
                _imageColumn.HeaderText = "image";
                _imageColumn.Name = "imageColumn";
                _imageColumn.ImageLayout = DataGridViewImageCellLayout.Zoom;
                _imageColumn.Width = 100;
            }
            dgvData.Columns.Add(_imageColumn);
        }

        private void Select()
        {
            try
            {
                _connection.Open();
                _sql = $"select * from books order by st_id";
                _command = new NpgsqlCommand(_sql, _connection);
                _dataTable = new DataTable();
                _dataTable.Load(_command.ExecuteReader());
                _connection.Close();
                dgvData.DataSource = null;
                dgvData.DataSource = _dataTable;
                
                DisplayingHeaders();

                DisplayingBookTitles();
            }
            catch (Exception ex)
            {
                _connection.Close();
                MessageBox.Show("Error" + ex.Message);
            }
        }

        private void dgvData_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0)
                {
                    _rowIndex = e.RowIndex;
                    txtTitlebook.Text = dgvData.Rows[e.RowIndex].Cells["st_titlebook"].Value.ToString();
                    txtAuthor.Text = dgvData.Rows[e.RowIndex].Cells["st_author"].Value.ToString();
                    txtYearsofrelease.Text = dgvData.Rows[e.RowIndex].Cells["st_year_of_release"].Value.ToString();
                    txtISBN.Text = dgvData.Rows[e.RowIndex].Cells["st_isbn"].Value.ToString();
                    txtShortdescription.Text = dgvData.Rows[e.RowIndex].Cells["st_short_description"].Value.ToString();
                    txtGenre.Text = dgvData.Rows[e.RowIndex].Cells["st_genre"].Value.ToString();
                    txtImagePath.Text = _path;


                    DisplayingBookTitles();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnInsert_Click(object sender, EventArgs e)
        {
            _rowIndex = -1;
            txtTitlebook.Enabled = txtAuthor.Enabled = txtYearsofrelease.Enabled = txtISBN.Enabled 
                = txtShortdescription.Enabled = txtGenre.Enabled = txtImagePath.Enabled = true;
            txtTitlebook.Text = txtAuthor.Text = txtYearsofrelease.Text = txtISBN.Text 
                = txtShortdescription.Text = txtGenre.Text = txtImagePath.Text = null;
            txtTitlebook.Select();
            btnInsertimage.Enabled = true;
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            if (_rowIndex < 0)
            {
                MessageBox.Show("Please choose book to update");
                return;
            }
            txtTitlebook.Enabled = txtAuthor.Enabled = txtYearsofrelease.Enabled = txtISBN.Enabled 
                = txtShortdescription.Enabled = txtGenre.Enabled = txtImagePath.Enabled = true;
            btnInsertimage.Enabled = true;
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (_rowIndex < 0)
            {
                MessageBox.Show("Please choose book to delete");
                return;
            }

            try
            {
                _connection.Open();
                _sql = @"select * from st_delete(:_id)";
                _command = new NpgsqlCommand(_sql, _connection);
                _command.Parameters.AddWithValue("_id", Convert.ToInt32(dgvData.Rows[_rowIndex].Cells["st_id"].Value.ToString()));
                if((int)_command.ExecuteScalar() == 1)
                {
                    MessageBox.Show("Delete book successfully");
                    _rowIndex = -1;
                    _connection.Close();

                    if (_currentPageIndex == 1)
                    {
                        dgvData.DataSource = GetCurrentRecords(_currentPageIndex, _connection);
                    }
                    else if (_currentPageIndex == _totalPages)
                    {
                        dgvData.DataSource = GetCurrentRecords(_currentPageIndex, _connection);
                    }
                    else if (_currentPageIndex < _totalPages)
                    {
                        _currentPageIndex++;
                        dgvData.DataSource =
                        GetCurrentRecords(_currentPageIndex, _connection);
                    }
                    else if (_currentPageIndex > 1)
                    {
                        _currentPageIndex--;
                        dgvData.DataSource =
                        GetCurrentRecords(_currentPageIndex, _connection);
                    }

                    DisplayingHeaders();
                    
                    DisplayingBookTitles();
                }
            }
            catch (Exception ex)
            {
                _connection.Close();
                MessageBox.Show("Deleted fail. Error: " + ex.Message);
            }
            txtTitlebook.Text = txtAuthor.Text = txtYearsofrelease.Text = txtISBN.Text = txtShortdescription.Text = txtGenre.Text = txtImagePath.Text = null;
            pictureBox.Image = null;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            int result = 0;
            if(_rowIndex < 0)  //insert
            {
                try
                {
                    _connection.Open();
                    _sql = @"select * from st_insert(:_titlebook, :_author, :_year_of_release, :_isbn, :_short_description, :_genre, :_image)";
                    _command = new NpgsqlCommand(_sql, _connection);
                    byte[] imageData = File.ReadAllBytes(txtImagePath.Text);
                    _command.Parameters.AddWithValue("_image", imageData);
                    _command.Parameters.AddWithValue("_titlebook", txtTitlebook.Text);
                    _command.Parameters.AddWithValue("_author", txtAuthor.Text);
                    _command.Parameters.AddWithValue("_year_of_release", txtYearsofrelease.Text);
                    _command.Parameters.AddWithValue("_isbn", txtISBN.Text);
                    _command.Parameters.AddWithValue("_short_description", txtShortdescription.Text);
                    _command.Parameters.AddWithValue("_genre", txtGenre.Text);
                    result = (int)_command.ExecuteScalar();
                    _sql = $"select * from books order by st_id";
                    _command = new NpgsqlCommand(_sql, _connection);
                    _connection.Close();

                    if(result == 1)
                    {
                        MessageBox.Show("Inserted new book succesfully");

                        if (_currentPageIndex == 1)
                        {
                            dgvData.DataSource = GetCurrentRecords(_currentPageIndex, _connection);
                        }
                        else if (_currentPageIndex == _totalPages)
                        {
                            dgvData.DataSource = GetCurrentRecords(_currentPageIndex, _connection);
                        }
                        else if (_currentPageIndex < _totalPages)
                        {
                            _currentPageIndex++;
                            dgvData.DataSource =
                            GetCurrentRecords(_currentPageIndex, _connection);
                        }
                        else if (_currentPageIndex > 1)
                        {
                            _currentPageIndex--;
                            dgvData.DataSource =
                            GetCurrentRecords(_currentPageIndex, _connection);
                        }

                        DisplayingHeaders();
                        
                        DisplayingBookTitles();
                    }
                    else
                    {
                        MessageBox.Show("Inserted book fail");
                    }
                }
                catch (Exception ex)
                {
                    _connection.Close();
                    MessageBox.Show("Inserted fail. Error: " + ex.Message);
                }
                
            }
            else //update
            {
                try
                {
                    _connection.Open();
                    _sql = @"select * from st_update(:_id, :_titlebook, :_author, :_year_of_release, :_isbn, :_short_description, :_genre, :_image)";
                    
                    _command = new NpgsqlCommand(_sql, _connection);
                    byte[] imageData = File.ReadAllBytes(txtImagePath.Text);
                    _command.Parameters.AddWithValue("_image", imageData);
                    _command.Parameters.AddWithValue("_id", Convert.ToInt32(dgvData.Rows[_rowIndex].Cells["st_id"].Value.ToString()));
                    _command.Parameters.AddWithValue("_titlebook", txtTitlebook.Text);
                    _command.Parameters.AddWithValue("_author", txtAuthor.Text);
                    _command.Parameters.AddWithValue("_year_of_release", txtYearsofrelease.Text);
                    _command.Parameters.AddWithValue("_isbn", txtISBN.Text);
                    _command.Parameters.AddWithValue("_short_description", txtShortdescription.Text);
                    _command.Parameters.AddWithValue("_genre", txtGenre.Text);
                    result = (int)_command.ExecuteScalar();
                    _sql = $"select * from books order by st_id";
                    _command = new NpgsqlCommand(_sql, _connection);
                    _connection.Close();
                    if (result == 1)
                    {
                        MessageBox.Show("Updated book succesfully");

                        if (_currentPageIndex == 1)
                        {
                            dgvData.DataSource = GetCurrentRecords(_currentPageIndex, _connection);
                        }
                        else if (_currentPageIndex == _totalPages)
                        {
                            dgvData.DataSource = GetCurrentRecords(_currentPageIndex, _connection);
                        }
                        else if (_currentPageIndex < _totalPages)
                        {
                            _currentPageIndex++;
                            dgvData.DataSource =
                            GetCurrentRecords(_currentPageIndex, _connection);
                        }
                        else if (_currentPageIndex > 1)
                        {
                            _currentPageIndex--;
                            dgvData.DataSource =
                            GetCurrentRecords(_currentPageIndex, _connection);
                        }

                        DisplayingHeaders();
                        
                        DisplayingBookTitles();
                    }
                    else
                    {
                        MessageBox.Show("Updated book fail");
                    }
                    
                }
                catch (Exception ex)
                {
                    _connection.Close();
                    MessageBox.Show("Updated fail. Error: " + ex.Message);
                }
            }

            txtTitlebook.Text = txtAuthor.Text = txtYearsofrelease.Text = txtISBN.Text = txtShortdescription.Text = txtGenre.Text = txtImagePath.Text = null;
            txtTitlebook.Enabled = txtAuthor.Enabled = txtYearsofrelease.Enabled = txtISBN.Enabled = txtShortdescription.Enabled = txtGenre.Enabled = txtImagePath.Enabled = false;
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                dgvData.DataSource = GetCurrentRecords(_currentPageIndex, _connection);

                DisplayingHeaders();
                
                DisplayingBookTitles();
                return;
            }
            
            _connection.Open();
            _sql = $"SELECT * FROM books WHERE st_titlebook ILIKE '%{txtSearch.Text}%' " +
                $"or st_author ILIKE '%{txtSearch.Text}%' or st_genre ILIKE '%{txtSearch.Text}%' " +
                $"or st_year_of_release ILIKE '%{txtSearch.Text}%'";
            _command = new NpgsqlCommand(_sql, _connection);
            var reader = _command.ExecuteReader();
            var dataTable = new DataTable();
            dataTable.Load(reader);

            dgvData.DataSource = dataTable;
            DisplayingHeaders();
            reader.Close();
            _connection.Close();

            DisplayingBookTitles();

        }

        private void CalculateTotalPages()
        {
            if (_dataTable != null && _dataTable.Rows.Count > 0)
            {
                int rowCount = _dataTable.Rows.Count;
                _totalPages = rowCount / _pageSize;
                // Если осталась хотя бы одна строка после вычисления страниц, добавляем еще одну страницу 
                if (rowCount % _pageSize > 0)
                    _totalPages += 1;
            }
        }

        private DataTable GetCurrentRecords(int page, NpgsqlConnection conn)
        {
            DataTable records = new DataTable();
            conn.Open();

            if (page == 1)
            {
                _sql = $"Select * from books order by st_id limit {_pageSize}";
                _command = new NpgsqlCommand(_sql, conn);
                
            }
            else
            {
                int PreviousPageOffSet = (page - 1) * _pageSize;
                
                _sql = $"select * from books order by st_id asc offset {PreviousPageOffSet} limit {_pageSize}";
                _command = new NpgsqlCommand(_sql, conn);
            }
            var reader = _command.ExecuteReader();
            records.Load(reader);
            reader.Close();
            conn.Close();

            return records;
        }

        private void btnNextPage_Click(object sender, EventArgs e)
        {

            if (_currentPageIndex < _totalPages)
            {
                _currentPageIndex++;
                dgvData.DataSource =
                GetCurrentRecords(_currentPageIndex, _connection);

                DisplayingHeaders();
                
                DisplayingBookTitles();
            }
        }

        private void btnPrevPage_Click(object sender, EventArgs e)
        {
            if (_currentPageIndex > 1)
            {
                _currentPageIndex--;
                dgvData.DataSource =
                GetCurrentRecords(_currentPageIndex, _connection);

                DisplayingBookTitles();
            }
        }

        private void btnFirstPage_Click(object sender, EventArgs e)
        {
            _currentPageIndex = 1;
            dgvData.DataSource = GetCurrentRecords(_currentPageIndex, _connection);

            DisplayingHeaders();
            
            DisplayingBookTitles();
        }

        private void btnLastPage_Click(object sender, EventArgs e)
        {
            _currentPageIndex = _totalPages;
            dgvData.DataSource = GetCurrentRecords(_currentPageIndex, _connection);

            DisplayingHeaders();
            
            DisplayingBookTitles();
        }

        private void btnInsertImage_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image Files (*.bmp, *.jpg, *.jpeg, *.png)|*.bmp;*.jpg;*.jpeg;*.png";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string imagePath = openFileDialog.FileName;

                // Чтение данных изображения
                byte[] imageData = File.ReadAllBytes(imagePath);
                
                txtImagePath.Text = imagePath;
            }
        }
    }
}
