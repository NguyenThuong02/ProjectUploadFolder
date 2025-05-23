using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileStorageClient
{
    public partial class MainForm : Form
    {
        private readonly FileStorageClient _client;
        private string _currentDirectory = "";
        private string _username = "";
        // Thêm biến để lưu trữ biểu tượng
        private ImageList _imageList;

        public MainForm()
        {
            InitializeComponent();
            _client = new FileStorageClient("127.0.0.1", 8888);
            ConnectToServer();
        }

        private async void ConnectToServer()
        {
            bool connected = await _client.ConnectAsync();
            if (!connected)
            {
                MessageBox.Show("Không thể kết nối đến server. Ứng dụng sẽ thoát.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Ứng dụng lưu trữ file";
            this.Size = new Size(900, 650);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += MainForm_FormClosing;
            this.Icon = SystemIcons.Application;

            // Tạo ImageList cho các biểu tượng
            _imageList = new ImageList();
            _imageList.ColorDepth = ColorDepth.Depth32Bit;
            _imageList.ImageSize = new Size(16, 16);
            _imageList.Images.Add(SystemIcons.Application); // Index 0: Folder icon (thay thế SystemIcons.Folder)
            _imageList.Images.Add(SystemIcons.WinLogo); // Index 1: File icon

            // Panel đăng nhập
            Panel loginPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Name = "loginPanel",
                BackColor = Color.WhiteSmoke
            };

            Label lblTitle = new Label
            {
                Text = "ỨNG DỤNG LƯU TRỮ FILE",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.DarkBlue,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 80
            };

            Panel buttonPanel = new Panel
            {
                Width = 250,
                Height = 150,
                Location = new Point((loginPanel.Width - 250) / 2, 150),
                Anchor = AnchorStyles.None
            };

            Button btnLogin = new Button
            {
                Text = "Đăng nhập",
                Size = new Size(200, 45),
                Location = new Point(25, 0),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.RoyalBlue,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            Button btnRegister = new Button
            {
                Text = "Đăng ký",
                Size = new Size(200, 45),
                Location = new Point(25, 60),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnRegister.FlatAppearance.BorderSize = 0;
            btnRegister.Click += BtnRegister_Click;

            buttonPanel.Controls.Add(btnLogin);
            buttonPanel.Controls.Add(btnRegister);

            loginPanel.Controls.Add(lblTitle);
            loginPanel.Controls.Add(buttonPanel);

            // Panel quản lý file
            Panel filePanel = new Panel
            {
                Dock = DockStyle.Fill,
                Visible = false,
                Name = "filePanel",
                BackColor = Color.White
            };

            // Toolbar
            ToolStrip toolStrip = new ToolStrip
            {
                BackColor = Color.WhiteSmoke,
                RenderMode = ToolStripRenderMode.System,
                GripStyle = ToolStripGripStyle.Hidden
            };
            
            // Thêm nút Back vào đầu thanh công cụ
            ToolStripButton btnBack = new ToolStripButton("Quay lại");
            btnBack.Image = SystemIcons.Information.ToBitmap(); // Thay thế SystemIcons.ArrowLeft
            btnBack.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            btnBack.Click += BtnBack_Click;
            toolStrip.Items.Add(btnBack);
            
            // Thêm dấu phân cách
            toolStrip.Items.Add(new ToolStripSeparator());
            
            ToolStripButton btnUpload = new ToolStripButton("Tải lên");
            btnUpload.Image = SystemIcons.Application.ToBitmap();
            btnUpload.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            btnUpload.Click += BtnUpload_Click;
            
            ToolStripButton btnDownload = new ToolStripButton("Tải xuống");
            btnDownload.Image = SystemIcons.Shield.ToBitmap();
            btnDownload.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            btnDownload.Click += BtnDownload_Click;
            
            ToolStripButton btnCreateDir = new ToolStripButton("Tạo thư mục");
            btnCreateDir.Image = SystemIcons.Application.ToBitmap(); // Thay thế SystemIcons.Folder
            btnCreateDir.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            btnCreateDir.Click += BtnCreateDir_Click;
            
            ToolStripButton btnDelete = new ToolStripButton("Xóa");
            btnDelete.Image = SystemIcons.Error.ToBitmap();
            btnDelete.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            btnDelete.Click += BtnDelete_Click;
            
            ToolStripButton btnRefresh = new ToolStripButton("Làm mới");
            btnRefresh.Image = SystemIcons.Information.ToBitmap();
            btnRefresh.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            btnRefresh.Click += BtnRefresh_Click;
            
            ToolStripButton btnLogout = new ToolStripButton("Đăng xuất");
            btnLogout.Image = SystemIcons.WinLogo.ToBitmap();
            btnLogout.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            btnLogout.Click += BtnLogout_Click;

            toolStrip.Items.Add(btnUpload);
            toolStrip.Items.Add(btnDownload);
            toolStrip.Items.Add(btnCreateDir);
            toolStrip.Items.Add(btnDelete);
            toolStrip.Items.Add(btnRefresh);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(btnLogout);

            // Path bar
            Panel pathPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = Color.WhiteSmoke,
                Padding = new Padding(5)
            };

            Label lblPath = new Label
            {
                Text = "Đường dẫn:",
                AutoSize = true,
                Location = new Point(10, 10),
                Font = new Font("Segoe UI", 9)
            };

            TextBox txtPath = new TextBox
            {
                Name = "txtPath",
                ReadOnly = true,
                Location = new Point(80, 8),
                Width = pathPanel.Width - 90,
                Font = new Font("Segoe UI", 9),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            pathPanel.Controls.Add(lblPath);
            pathPanel.Controls.Add(txtPath);

            // ListView hiển thị file và thư mục
            ListView listView = new ListView
            {
                Name = "listViewFiles",
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                GridLines = true,
                SmallImageList = _imageList,
                Font = new Font("Segoe UI", 9)
            };
            listView.Columns.Add("Tên", 350);
            listView.Columns.Add("Loại", 150);
            listView.DoubleClick += ListView_DoubleClick;

            // Status bar
            StatusStrip statusStrip = new StatusStrip
            {
                BackColor = Color.WhiteSmoke,
                SizingGrip = false
            };
            ToolStripStatusLabel lblStatus = new ToolStripStatusLabel("Sẵn sàng")
            {
                Name = "lblStatus",
                Font = new Font("Segoe UI", 9)
            };
            statusStrip.Items.Add(lblStatus);

            filePanel.Controls.Add(listView);
            filePanel.Controls.Add(pathPanel);
            filePanel.Controls.Add(toolStrip);
            filePanel.Controls.Add(statusStrip);

            this.Controls.Add(loginPanel);
            this.Controls.Add(filePanel);
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _client.Disconnect();
        }

        private void BtnRegister_Click(object? sender, EventArgs e)
        {
            RegisterForm registerForm = new RegisterForm(_client);
            registerForm.ShowDialog();
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            LoginForm loginForm = new LoginForm(_client);
            if (loginForm.ShowDialog() == DialogResult.OK)
            {
                _username = loginForm.Username;
                this.Text = $"Ứng dụng lưu trữ file - {_username}";
                Panel? loginPanel = this.Controls["loginPanel"] as Panel;
                Panel? filePanel = this.Controls["filePanel"] as Panel;
                
                if (loginPanel != null && filePanel != null)
                {
                    loginPanel.Visible = false;
                    filePanel.Visible = true;
                    RefreshFileList();
                }
            }
        }

        private async void BtnUpload_Click(object? sender, EventArgs e)
        {
            using OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string localPath = openFileDialog.FileName;
                string fileName = Path.GetFileName(localPath);
                string remotePath = Path.Combine(_currentDirectory, fileName).Replace("\\", "/");

                UpdateStatus($"Đang tải lên {fileName}...");
                string response = await _client.UploadFileAsync(localPath, remotePath);
                DisplayResponse(response);
                RefreshFileList();
            }
        }

        private async void BtnDownload_Click(object? sender, EventArgs e)
        {
            ListView? listView = GetListView();
            if (listView == null || listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một file để tải xuống", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ListViewItem selectedItem = listView.SelectedItems[0];
            if (selectedItem.SubItems[1].Text != "File")
            {
                MessageBox.Show("Chỉ có thể tải xuống file", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string fileName = selectedItem.Text;
            string remotePath = Path.Combine(_currentDirectory, fileName).Replace("\\", "/");

            using SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                FileName = fileName
            };

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string localPath = saveFileDialog.FileName;
                UpdateStatus($"Đang tải xuống {fileName}...");
                string response = await _client.DownloadFileAsync(remotePath, localPath);
                DisplayResponse(response);
            }
        }

        private async void BtnCreateDir_Click(object? sender, EventArgs e)
        {
            string? dirName = PromptForInput("Nhập tên thư mục mới:", "Tạo thư mục");
            if (string.IsNullOrEmpty(dirName))
                return;

            string path = Path.Combine(_currentDirectory, dirName).Replace("\\", "/");
            UpdateStatus($"Đang tạo thư mục {dirName}...");
            string response = await _client.CreateDirectoryAsync(path);
            DisplayResponse(response);
            RefreshFileList();
        }

        private async void BtnDelete_Click(object? sender, EventArgs e)
        {
            ListView? listView = GetListView();
            if (listView == null || listView.SelectedItems.Count == 0)
            {
                MessageBox.Show("Vui lòng chọn một mục để xóa", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string itemName = listView.SelectedItems[0].Text;
            string itemType = listView.SelectedItems[0].SubItems[1].Text;
            
            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa {itemType.ToLower()} '{itemName}'?", "Xác nhận", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                string path = Path.Combine(_currentDirectory, itemName).Replace("\\", "/");
                UpdateStatus($"Đang xóa {itemName}...");
                string response = await _client.DeleteAsync(path);
                DisplayResponse(response);
                RefreshFileList();
            }
        }

        private void BtnRefresh_Click(object? sender, EventArgs e)
        {
            RefreshFileList();
        }

        private async void BtnLogout_Click(object? sender, EventArgs e)
        {
            UpdateStatus("Đang đăng xuất...");
            string response = await _client.LogoutAsync();
            DisplayResponse(response);

            _username = "";
            _currentDirectory = "";
            this.Text = "Ứng dụng lưu trữ file";

            Panel? loginPanel = this.Controls["loginPanel"] as Panel;
            Panel? filePanel = this.Controls["filePanel"] as Panel;
            
            if (loginPanel != null && filePanel != null)
            {
                loginPanel.Visible = true;
                filePanel.Visible = false;
            }
        }

        private async void ListView_DoubleClick(object? sender, EventArgs e)
        {
            ListView? listView = sender as ListView;
            if (listView == null || listView.SelectedItems.Count == 0)
                return;

            ListViewItem selectedItem = listView.SelectedItems[0];
            if (selectedItem.SubItems[1].Text == "Thư mục")
            {
                string dirName = selectedItem.Text;
                if (dirName == "..")
                {
                    // Đi lên thư mục cha
                    if (!string.IsNullOrEmpty(_currentDirectory))
                    {
                        int lastSlash = _currentDirectory.LastIndexOf('/');
                        if (lastSlash >= 0)
                        {
                            _currentDirectory = _currentDirectory.Substring(0, lastSlash);
                        }
                        else
                        {
                            _currentDirectory = "";
                        }
                        RefreshFileList();
                    }
                }
                else
                {
                    // Đi vào thư mục con
                    string newPath = string.IsNullOrEmpty(_currentDirectory) ? 
                        dirName : Path.Combine(_currentDirectory, dirName).Replace("\\", "/");
                    
                    var (success, _) = await _client.ListDirectoryAsync(newPath);
                    if (success)
                    {
                        _currentDirectory = newPath;
                        RefreshFileList();
                    }
                    else
                    {
                        MessageBox.Show("Không thể mở thư mục", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void RefreshFileList()
        {
            ListView? listView = GetListView();
            TextBox? txtPath = GetPathTextBox();
            
            if (listView == null || txtPath == null)
                return;

            listView.Items.Clear();
            UpdateStatus("Đang tải danh sách file và thư mục...");

            // Hiển thị đường dẫn hiện tại
            txtPath.Text = string.IsNullOrEmpty(_currentDirectory) ? "/" : _currentDirectory;

            // Thêm mục đi lên thư mục cha (trừ khi đang ở thư mục gốc)
            if (!string.IsNullOrEmpty(_currentDirectory))
            {
                ListViewItem parentItem = new ListViewItem("..")
                {
                    ImageIndex = 0
                };
                parentItem.SubItems.Add("Thư mục");
                listView.Items.Add(parentItem);
            }

            var (success, items) = await _client.ListDirectoryAsync(_currentDirectory);
            if (success)
            {
                foreach (string item in items)
                {
                    if (item.StartsWith("D:"))
                    {
                        ListViewItem dirItem = new ListViewItem(item.Substring(2))
                        {
                            ImageIndex = 0
                        };
                        dirItem.SubItems.Add("Thư mục");
                        listView.Items.Add(dirItem);
                    }
                    else if (item.StartsWith("F:"))
                    {
                        ListViewItem fileItem = new ListViewItem(item.Substring(2))
                        {
                            ImageIndex = 1
                        };
                        fileItem.SubItems.Add("File");
                        listView.Items.Add(fileItem);
                    }
                }
                UpdateStatus("Sẵn sàng");
            }
            else
            {
                UpdateStatus("Không thể tải danh sách file và thư mục");
            }
        }

        private ListView? GetListView()
        {
            Panel? filePanel = this.Controls["filePanel"] as Panel;
            return filePanel?.Controls["listViewFiles"] as ListView;
        }

        private TextBox? GetPathTextBox()
        {
            Panel? filePanel = this.Controls["filePanel"] as Panel;
            Panel? pathPanel = filePanel?.Controls.OfType<Panel>().FirstOrDefault();
            return pathPanel?.Controls["txtPath"] as TextBox;
        }

        private void UpdateStatus(string message)
        {
            Panel? filePanel = this.Controls["filePanel"] as Panel;
            StatusStrip? statusStrip = filePanel?.Controls.OfType<StatusStrip>().FirstOrDefault();
            ToolStripStatusLabel? lblStatus = statusStrip?.Items["lblStatus"] as ToolStripStatusLabel;
            
            if (lblStatus != null)
            {
                lblStatus.Text = message;
            }
        }

        private void DisplayResponse(string response)
        {
            try
            {
                using System.Text.Json.JsonDocument doc = System.Text.Json.JsonDocument.Parse(response);
                System.Text.Json.JsonElement root = doc.RootElement;
                
                if (root.TryGetProperty("status", out System.Text.Json.JsonElement statusElement))
                {
                    string status = statusElement.GetString() ?? "";
                    
                    if (root.TryGetProperty("message", out System.Text.Json.JsonElement messageElement))
                    {
                        string message = messageElement.GetString() ?? "";
                        
                        if (status == "success")
                        {
                            UpdateStatus(message);
                        }
                        else
                        {
                            MessageBox.Show(message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            UpdateStatus("Sẵn sàng");
                        }
                    }
                    else
                    {
                        UpdateStatus(status);
                    }
                }
                else
                {
                    MessageBox.Show($"Phản hồi không hợp lệ: {response}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus("Sẵn sàng");
                }
            }
            catch
            {
                MessageBox.Show($"Phản hồi không hợp lệ: {response}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus("Sẵn sàng");
            }
        }

        private string? PromptForInput(string prompt, string title)
        {
            Form promptForm = new Form()
            {
                Width = 300,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = title,
                StartPosition = FormStartPosition.CenterParent
            };

            Label textLabel = new Label() { Left = 20, Top = 20, Text = prompt, AutoSize = true };
            TextBox textBox = new TextBox() { Left = 20, Top = 50, Width = 240 };
            Button confirmButton = new Button() { Text = "OK", Left = 120, Width = 80, Top = 80, DialogResult = DialogResult.OK };
            confirmButton.Click += (sender, e) => { promptForm.Close(); };

            promptForm.Controls.Add(textLabel);
            promptForm.Controls.Add(textBox);
            promptForm.Controls.Add(confirmButton);
            promptForm.AcceptButton = confirmButton;

            return promptForm.ShowDialog() == DialogResult.OK ? textBox.Text : null;
        }

        // Thêm phương thức xử lý sự kiện cho nút Back
        private void BtnBack_Click(object? sender, EventArgs e)
        {
            // Đi lên thư mục cha
            if (!string.IsNullOrEmpty(_currentDirectory))
            {
                int lastSlash = _currentDirectory.LastIndexOf('/');
                if (lastSlash >= 0)
                {
                    _currentDirectory = _currentDirectory.Substring(0, lastSlash);
                }
                else
                {
                    _currentDirectory = "";
                }
                RefreshFileList();
            }
        }
    }
}