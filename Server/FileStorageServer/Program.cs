using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace FileStorageServer
{
    // Lớp đại diện cho người dùng
    public class User
    {
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string UserDirectory { get; set; }
        public List<string> Permissions { get; set; } = new List<string>() { "read", "write", "delete", "create" };
    }

    // Lớp xử lý các yêu cầu từ client
    public class RequestHandler
    {
        private readonly string _usersFilePath = "users.json";
        private readonly string _storageBasePath = "storage";
        private List<User> _users = new List<User>();

        public RequestHandler()
        {
            // Tạo thư mục lưu trữ nếu chưa tồn tại
            if (!Directory.Exists(_storageBasePath))
            {
                Directory.CreateDirectory(_storageBasePath);
            }

            // Tải danh sách người dùng từ file nếu tồn tại
            if (File.Exists(_usersFilePath))
            {
                try
                {
                    string json = File.ReadAllText(_usersFilePath);
                    _users = JsonSerializer.Deserialize<List<User>>(json) ?? new List<User>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Lỗi khi tải danh sách người dùng: {ex.Message}");
                    _users = new List<User>();
                }
            }
        }

        // Lưu danh sách người dùng vào file
        private void SaveUsers()
        {
            try
            {
                string json = JsonSerializer.Serialize(_users, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_usersFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi lưu danh sách người dùng: {ex.Message}");
            }
        }

        // Băm mật khẩu
        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }

        // Đăng ký tài khoản mới
        public bool Register(string username, string password)
        {
            if (_users.Any(u => u.Username == username))
            {
                return false; // Tên người dùng đã tồn tại
            }

            string userDirectory = Path.Combine(_storageBasePath, username);
            Directory.CreateDirectory(userDirectory);

            User newUser = new User
            {
                Username = username,
                PasswordHash = HashPassword(password),
                UserDirectory = userDirectory
            };

            _users.Add(newUser);
            SaveUsers();
            return true;
        }

        // Đăng nhập
        public User? Login(string username, string password)
        {
            User? user = _users.FirstOrDefault(u => u.Username == username);
            if (user != null && user.PasswordHash == HashPassword(password))
            {
                return user;
            }
            return null;
        }

        // Tạo thư mục
        public bool CreateDirectory(User user, string directoryPath)
        {
            if (!user.Permissions.Contains("create"))
            {
                return false; // Không có quyền tạo
            }

            string fullPath = Path.Combine(user.UserDirectory, directoryPath);
            try
            {
                // Đảm bảo đường dẫn nằm trong thư mục của người dùng
                if (!IsPathInUserDirectory(user, fullPath))
                {
                    return false;
                }

                Directory.CreateDirectory(fullPath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Tải file lên
        public bool UploadFile(User user, string filePath, byte[] fileData)
        {
            if (!user.Permissions.Contains("write"))
            {
                return false; // Không có quyền ghi
            }

            string fullPath = Path.Combine(user.UserDirectory, filePath);
            try
            {
                // Đảm bảo đường dẫn nằm trong thư mục của người dùng
                if (!IsPathInUserDirectory(user, fullPath))
                {
                    return false;
                }

                // Tạo thư mục cha nếu chưa tồn tại
                string? directory = Path.GetDirectoryName(fullPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(fullPath, fileData);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Tải file xuống
        public byte[]? DownloadFile(User user, string filePath)
        {
            if (!user.Permissions.Contains("read"))
            {
                return null; // Không có quyền đọc
            }

            string fullPath = Path.Combine(user.UserDirectory, filePath);
            try
            {
                // Đảm bảo đường dẫn nằm trong thư mục của người dùng
                if (!IsPathInUserDirectory(user, fullPath) || !File.Exists(fullPath))
                {
                    return null;
                }

                return File.ReadAllBytes(fullPath);
            }
            catch
            {
                return null;
            }
        }

        // Xóa file hoặc thư mục
        public bool Delete(User user, string path)
        {
            if (!user.Permissions.Contains("delete"))
            {
                return false; // Không có quyền xóa
            }

            string fullPath = Path.Combine(user.UserDirectory, path);
            try
            {
                // Đảm bảo đường dẫn nằm trong thư mục của người dùng
                if (!IsPathInUserDirectory(user, fullPath))
                {
                    return false;
                }

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    return true;
                }
                else if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        // Liệt kê các file và thư mục
        public List<string> ListDirectory(User user, string directoryPath)
        {
            if (!user.Permissions.Contains("read"))
            {
                return new List<string>(); // Không có quyền đọc
            }

            string fullPath = Path.Combine(user.UserDirectory, directoryPath);
            try
            {
                // Đảm bảo đường dẫn nằm trong thư mục của người dùng
                if (!IsPathInUserDirectory(user, fullPath) || !Directory.Exists(fullPath))
                {
                    return new List<string>();
                }

                List<string> result = new List<string>();
                foreach (string dir in Directory.GetDirectories(fullPath))
                {
                    result.Add($"D:{Path.GetFileName(dir)}");
                }
                foreach (string file in Directory.GetFiles(fullPath))
                {
                    result.Add($"F:{Path.GetFileName(file)}");
                }
                return result;
            }
            catch
            {
                return new List<string>();
            }
        }

        // Kiểm tra xem đường dẫn có nằm trong thư mục của người dùng không
        private bool IsPathInUserDirectory(User user, string path)
        {
            string normalizedPath = Path.GetFullPath(path);
            string normalizedUserDir = Path.GetFullPath(user.UserDirectory);
            return normalizedPath.StartsWith(normalizedUserDir);
        }
    }

    // Lớp xử lý giao tiếp với client
    public class ClientHandler
    {
        private readonly TcpClient _client;
        private readonly RequestHandler _requestHandler;
        private User? _currentUser = null;
        // Tăng kích thước buffer lên đáng kể
        private const int BufferSize = 1024 * 1024; // 1MB buffer

        public ClientHandler(TcpClient client, RequestHandler requestHandler)
        {
            _client = client;
            _requestHandler = requestHandler;
        }

        public async Task HandleClientAsync()
        {
            try
            {
                using NetworkStream stream = _client.GetStream();
                // Tăng kích thước buffer
                byte[] buffer = new byte[BufferSize];
                int bytesRead;
                
                // Tăng thời gian chờ để xử lý file lớn
                _client.ReceiveTimeout = 300000; // 5 phút
                _client.SendTimeout = 300000; // 5 phút

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string response = ProcessRequest(request);
                    byte[] responseData = Encoding.UTF8.GetBytes(response);
                    await stream.WriteAsync(responseData, 0, responseData.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi xử lý client: {ex.Message}");
            }
            finally
            {
                _client.Close();
            }
        }

        private string ProcessRequest(string requestJson)
        {
            try
            {
                Dictionary<string, string>? request = JsonSerializer.Deserialize<Dictionary<string, string>>(requestJson);
                if (request == null || !request.ContainsKey("command"))
                {
                    return CreateErrorResponse("Yêu cầu không hợp lệ");
                }

                string command = request["command"];
                switch (command)
                {
                    case "register":
                        return HandleRegister(request);
                    case "login":
                        return HandleLogin(request);
                    case "create_directory":
                        return HandleCreateDirectory(request);
                    case "upload_file":
                        return HandleUploadFile(request);
                    case "download_file":
                        return HandleDownloadFile(request);
                    case "delete":
                        return HandleDelete(request);
                    case "list_directory":
                        return HandleListDirectory(request);
                    case "logout":
                        _currentUser = null;
                        return CreateSuccessResponse("Đăng xuất thành công");
                    default:
                        return CreateErrorResponse("Lệnh không được hỗ trợ");
                }
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Lỗi xử lý yêu cầu: {ex.Message}");
            }
        }

        private string HandleRegister(Dictionary<string, string> request)
        {
            if (!request.ContainsKey("username") || !request.ContainsKey("password"))
            {
                return CreateErrorResponse("Thiếu thông tin đăng ký");
            }

            string username = request["username"];
            string password = request["password"];

            bool success = _requestHandler.Register(username, password);
            if (success)
            {
                return CreateSuccessResponse("Đăng ký thành công");
            }
            else
            {
                return CreateErrorResponse("Tên người dùng đã tồn tại");
            }
        }

        private string HandleLogin(Dictionary<string, string> request)
        {
            if (!request.ContainsKey("username") || !request.ContainsKey("password"))
            {
                return CreateErrorResponse("Thiếu thông tin đăng nhập");
            }

            string username = request["username"];
            string password = request["password"];

            User? user = _requestHandler.Login(username, password);
            if (user != null)
            {
                _currentUser = user;
                return CreateSuccessResponse("Đăng nhập thành công");
            }
            else
            {
                return CreateErrorResponse("Tên người dùng hoặc mật khẩu không đúng");
            }
        }

        private string HandleCreateDirectory(Dictionary<string, string> request)
        {
            if (_currentUser == null)
            {
                return CreateErrorResponse("Chưa đăng nhập");
            }

            if (!request.ContainsKey("path"))
            {
                return CreateErrorResponse("Thiếu đường dẫn thư mục");
            }

            string path = request["path"];
            bool success = _requestHandler.CreateDirectory(_currentUser, path);
            if (success)
            {
                return CreateSuccessResponse("Tạo thư mục thành công");
            }
            else
            {
                return CreateErrorResponse("Không thể tạo thư mục");
            }
        }

        private string HandleUploadFile(Dictionary<string, string> request)
        {
            if (_currentUser == null)
            {
                return CreateErrorResponse("Chưa đăng nhập");
            }

            if (!request.ContainsKey("path") || !request.ContainsKey("data"))
            {
                return CreateErrorResponse("Thiếu thông tin file");
            }

            string path = request["path"];
            try
            {
                // Thêm xử lý ngoại lệ khi chuyển đổi dữ liệu Base64
                byte[] fileData = Convert.FromBase64String(request["data"]);
                
                // Kiểm tra kích thước file (tùy chọn - có thể đặt giới hạn cao hơn)
                // Ví dụ: giới hạn 100MB
                long maxFileSize = 100 * 1024 * 1024;
                if (fileData.Length > maxFileSize)
                {
                    return CreateErrorResponse($"Kích thước file vượt quá giới hạn cho phép ({maxFileSize / (1024 * 1024)}MB)");
                }

                bool success = _requestHandler.UploadFile(_currentUser, path, fileData);
                if (success)
                {
                    return CreateSuccessResponse("Tải file lên thành công");
                }
                else
                {
                    return CreateErrorResponse("Không thể tải file lên");
                }
            }
            catch (FormatException ex)
            {
                return CreateErrorResponse($"Lỗi xử lý dữ liệu file: {ex.Message}");
            }
            catch (Exception ex)
            {
                return CreateErrorResponse($"Lỗi không xác định: {ex.Message}");
            }
        }

        private string HandleDownloadFile(Dictionary<string, string> request)
        {
            if (_currentUser == null)
            {
                return CreateErrorResponse("Chưa đăng nhập");
            }

            if (!request.ContainsKey("path"))
            {
                return CreateErrorResponse("Thiếu đường dẫn file");
            }

            string path = request["path"];
            byte[]? fileData = _requestHandler.DownloadFile(_currentUser, path);

            if (fileData != null)
            {
                Dictionary<string, string> response = new Dictionary<string, string>
                {
                    { "status", "success" },
                    { "data", Convert.ToBase64String(fileData) }
                };
                return JsonSerializer.Serialize(response);
            }
            else
            {
                return CreateErrorResponse("Không thể tải file xuống");
            }
        }

        private string HandleDelete(Dictionary<string, string> request)
        {
            if (_currentUser == null)
            {
                return CreateErrorResponse("Chưa đăng nhập");
            }

            if (!request.ContainsKey("path"))
            {
                return CreateErrorResponse("Thiếu đường dẫn");
            }

            string path = request["path"];
            bool success = _requestHandler.Delete(_currentUser, path);

            if (success)
            {
                return CreateSuccessResponse("Xóa thành công");
            }
            else
            {
                return CreateErrorResponse("Không thể xóa");
            }
        }

        private string HandleListDirectory(Dictionary<string, string> request)
        {
            if (_currentUser == null)
            {
                return CreateErrorResponse("Chưa đăng nhập");
            }

            string path = request.ContainsKey("path") ? request["path"] : "";
            List<string> items = _requestHandler.ListDirectory(_currentUser, path);

            Dictionary<string, object> response = new Dictionary<string, object>
            {
                { "status", "success" },
                { "items", items }
            };

            return JsonSerializer.Serialize(response);
        }

        private string CreateSuccessResponse(string message)
        {
            Dictionary<string, string> response = new Dictionary<string, string>
            {
                { "status", "success" },
                { "message", message }
            };
            return JsonSerializer.Serialize(response);
        }

        private string CreateErrorResponse(string message)
        {
            Dictionary<string, string> response = new Dictionary<string, string>
            {
                { "status", "error" },
                { "message", message }
            };
            return JsonSerializer.Serialize(response);
        }
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                TcpListener server = new TcpListener(IPAddress.Any, 8888);
                server.Start();
                Console.WriteLine("Server đã khởi động. Đang lắng nghe kết nối...");

                RequestHandler requestHandler = new RequestHandler();

                while (true)
                {
                    TcpClient client = await server.AcceptTcpClientAsync();
                    // Cấu hình TcpClient để xử lý dữ liệu lớn
                    client.ReceiveBufferSize = 1024 * 1024; // 1MB
                    client.SendBufferSize = 1024 * 1024; // 1MB
                    Console.WriteLine("Client đã kết nối");

                    ClientHandler clientHandler = new ClientHandler(client, requestHandler);
                    _ = Task.Run(() => clientHandler.HandleClientAsync());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi server: {ex.Message}");
            }
        }
    }
}
