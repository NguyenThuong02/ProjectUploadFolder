using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Windows.Forms;

namespace FileStorageClient
{
    public partial class LoginForm : Form
    {
        private readonly FileStorageClient _client;
        public string Username { get; private set; } = "";

        public LoginForm(FileStorageClient client)
        {
            InitializeComponent();
            _client = client;
        }

        private void InitializeComponent()
        {
            this.Text = "Đăng nhập - Ứng dụng lưu trữ file";
            this.Size = new System.Drawing.Size(400, 250);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Tạo các control
            Label lblUsername = new Label
            {
                Text = "Tên đăng nhập:",
                Location = new System.Drawing.Point(50, 30),
                Size = new System.Drawing.Size(100, 20)
            };

            TextBox txtUsername = new TextBox
            {
                Location = new System.Drawing.Point(150, 30),
                Size = new System.Drawing.Size(200, 20),
                Name = "txtUsername"
            };

            Label lblPassword = new Label
            {
                Text = "Mật khẩu:",
                Location = new System.Drawing.Point(50, 70),
                Size = new System.Drawing.Size(100, 20)
            };

            TextBox txtPassword = new TextBox
            {
                Location = new System.Drawing.Point(150, 70),
                Size = new System.Drawing.Size(200, 20),
                Name = "txtPassword",
                PasswordChar = '*'
            };

            Button btnLogin = new Button
            {
                Text = "Đăng nhập",
                Location = new System.Drawing.Point(150, 110),
                Size = new System.Drawing.Size(100, 30)
            };
            btnLogin.Click += BtnLogin_Click;

            Button btnCancel = new Button
            {
                Text = "Hủy",
                Location = new System.Drawing.Point(260, 110),
                Size = new System.Drawing.Size(100, 30),
                DialogResult = DialogResult.Cancel
            };

            // Thêm các control vào form
            this.Controls.Add(lblUsername);
            this.Controls.Add(txtUsername);
            this.Controls.Add(lblPassword);
            this.Controls.Add(txtPassword);
            this.Controls.Add(btnLogin);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnLogin;
            this.CancelButton = btnCancel;
        }

        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            if (this.Controls["txtUsername"] is TextBox txtUsername && 
                this.Controls["txtPassword"] is TextBox txtPassword)
            {
                string username = txtUsername.Text;
                string password = txtPassword.Text;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    MessageBox.Show("Vui lòng nhập đầy đủ thông tin", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string response = await _client.LoginAsync(username, password);
                try
                {
                    var responseObj = JsonSerializer.Deserialize<Dictionary<string, string>>(response);
                    if (responseObj != null && responseObj.ContainsKey("status") && responseObj["status"] == "success")
                    {
                        Username = username;
                        this.DialogResult = DialogResult.OK;
                        this.Close();
                    }
                    else
                    {
                        string message = responseObj != null && responseObj.ContainsKey("message") ? responseObj["message"] : "Đăng nhập thất bại";
                        MessageBox.Show(message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch
                {
                    MessageBox.Show("Có lỗi xảy ra khi xử lý phản hồi từ server", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}