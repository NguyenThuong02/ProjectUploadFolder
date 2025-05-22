using System;
using System.Windows.Forms;

namespace FileStorageClient
{
    public partial class RegisterForm : Form
    {
        private readonly FileStorageClient _client;

        public RegisterForm(FileStorageClient client)
        {
            InitializeComponent();
            _client = client;
        }

        private void InitializeComponent()
        {
            this.Text = "Đăng ký - Ứng dụng lưu trữ file";
            this.Size = new System.Drawing.Size(400, 300);
            this.StartPosition = FormStartPosition.CenterScreen;

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

            Label lblConfirmPassword = new Label
            {
                Text = "Xác nhận mật khẩu:",
                Location = new System.Drawing.Point(50, 110),
                Size = new System.Drawing.Size(100, 20)
            };

            TextBox txtConfirmPassword = new TextBox
            {
                Location = new System.Drawing.Point(150, 110),
                Size = new System.Drawing.Size(200, 20),
                Name = "txtConfirmPassword",
                PasswordChar = '*'
            };

            Button btnRegister = new Button
            {
                Text = "Đăng ký",
                Location = new System.Drawing.Point(150, 150),
                Size = new System.Drawing.Size(100, 30)
            };
            btnRegister.Click += BtnRegister_Click;

            Button btnCancel = new Button
            {
                Text = "Hủy",
                Location = new System.Drawing.Point(260, 150),
                Size = new System.Drawing.Size(100, 30)
            };
            btnCancel.Click += (s, e) => this.Close();

            // Thêm các control vào form
            this.Controls.Add(lblUsername);
            this.Controls.Add(txtUsername);
            this.Controls.Add(lblPassword);
            this.Controls.Add(txtPassword);
            this.Controls.Add(lblConfirmPassword);
            this.Controls.Add(txtConfirmPassword);
            this.Controls.Add(btnRegister);
            this.Controls.Add(btnCancel);
        }

        private async void BtnRegister_Click(object? sender, EventArgs e)
        {
            if (this.Controls["txtUsername"] is TextBox txtUsername && 
                this.Controls["txtPassword"] is TextBox txtPassword && 
                this.Controls["txtConfirmPassword"] is TextBox txtConfirmPassword)
            {
                string username = txtUsername.Text;
                string password = txtPassword.Text;
                string confirmPassword = txtConfirmPassword.Text;

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(confirmPassword))
                {
                    MessageBox.Show("Vui lòng nhập đầy đủ thông tin", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (password != confirmPassword)
                {
                    MessageBox.Show("Mật khẩu xác nhận không khớp", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string response = await _client.RegisterAsync(username, password);
                try
                {
                    var responseObj = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(response);
                    if (responseObj != null && responseObj.ContainsKey("status") && responseObj["status"] == "success")
                    {
                        MessageBox.Show("Đăng ký thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        this.Close();
                    }
                    else
                    {
                        string message = responseObj != null && responseObj.ContainsKey("message") ? responseObj["message"] : "Đăng ký thất bại";
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