using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileStorageClient
{
    // Lớp xử lý kết nối với server
    public class ServerConnection
    {
        private readonly string _serverIp;
        private readonly int _serverPort;
        private TcpClient? _client;
        private NetworkStream? _stream;

        public ServerConnection(string serverIp, int serverPort)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;
        }

        // Kết nối đến server
        public async Task<bool> ConnectAsync()
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(_serverIp, _serverPort);
                _stream = _client.GetStream();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi kết nối: {ex.Message}");
                return false;
            }
        }

        // Đóng kết nối
        public void Disconnect()
        {
            _stream?.Close();
            _client?.Close();
        }

        // Gửi yêu cầu đến server và nhận phản hồi
        public async Task<string> SendRequestAsync(Dictionary<string, string> request)
        {
            if (_stream == null)
            {
                throw new InvalidOperationException("Chưa kết nối đến server");
            }

            try
            {
                string requestJson = JsonSerializer.Serialize(request);
                byte[] requestData = Encoding.UTF8.GetBytes(requestJson);
                await _stream.WriteAsync(requestData, 0, requestData.Length);

                byte[] buffer = new byte[4096];
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                return Encoding.UTF8.GetString(buffer, 0, bytesRead);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi gửi yêu cầu: {ex.Message}");
                return JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "status", "error" },
                    { "message", $"Lỗi gửi yêu cầu: {ex.Message}" }
                });
            }
        }
    }

    // Lớp xử lý các chức năng của client
    public class FileStorageClient
    {
        private readonly ServerConnection _connection;
        private bool _isLoggedIn = false;

        public FileStorageClient(string serverIp, int serverPort)
        {
            _connection = new ServerConnection(serverIp, serverPort);
        }

        // Kết nối đến server
        public async Task<bool> ConnectAsync()
        {
            return await _connection.ConnectAsync();
        }

        // Đăng ký tài khoản mới
        public async Task<string> RegisterAsync(string username, string password)
        {
            Dictionary<string, string> request = new Dictionary<string, string>
            {
                { "command", "register" },
                { "username", username },
                { "password", password }
            };

            string response = await _connection.SendRequestAsync(request);
            return response;
        }

        // Đăng nhập
        public async Task<string> LoginAsync(string username, string password)
        {
            Dictionary<string, string> request = new Dictionary<string, string>
            {
                { "command", "login" },
                { "username", username },
                { "password", password }
            };

            string response = await _connection.SendRequestAsync(request);
            Dictionary<string, string>? responseObj = JsonSerializer.Deserialize<Dictionary<string, string>>(response);
            
            if (responseObj != null && responseObj.ContainsKey("status") && responseObj["status"] == "success")
            {
                _isLoggedIn = true;
            }
            
            return response;
        }

        // Đăng xuất
        public async Task<string> LogoutAsync()
        {
            Dictionary<string, string> request = new Dictionary<string, string>
            {
                { "command", "logout" }
            };

            string response = await _connection.SendRequestAsync(request);
            Dictionary<string, string>? responseObj = JsonSerializer.Deserialize<Dictionary<string, string>>(response);
            
            if (responseObj != null && responseObj.ContainsKey("status") && responseObj["status"] == "success")
            {
                _isLoggedIn = false;
            }
            
            return response;
        }

        // Tạo thư mục mới
        public async Task<string> CreateDirectoryAsync(string path)
        {
            if (!_isLoggedIn)
            {
                return JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "status", "error" },
                    { "message", "Chưa đăng nhập" }
                });
            }

            Dictionary<string, string> request = new Dictionary<string, string>
            {
                { "command", "create_directory" },
                { "path", path }
            };

            return await _connection.SendRequestAsync(request);
        }

        // Tải file lên server
        public async Task<string> UploadFileAsync(string localFilePath, string remotePath)
        {
            if (!_isLoggedIn)
            {
                return JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "status", "error" },
                    { "message", "Chưa đăng nhập" }
                });
            }

            try
            {
                if (!File.Exists(localFilePath))
                {
                    return JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        { "status", "error" },
                        { "message", "File không tồn tại" }
                    });
                }

                byte[] fileData = File.ReadAllBytes(localFilePath);
                string base64Data = Convert.ToBase64String(fileData);

                Dictionary<string, string> request = new Dictionary<string, string>
                {
                    { "command", "upload_file" },
                    { "path", remotePath },
                    { "data", base64Data }
                };

                return await _connection.SendRequestAsync(request);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "status", "error" },
                    { "message", $"Lỗi tải file lên: {ex.Message}" }
                });
            }
        }

        // Tải file từ server
        public async Task<string> DownloadFileAsync(string remotePath, string localFilePath)
        {
            if (!_isLoggedIn)
            {
                return JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "status", "error" },
                    { "message", "Chưa đăng nhập" }
                });
            }

            try
            {
                Dictionary<string, string> request = new Dictionary<string, string>
                {
                    { "command", "download_file" },
                    { "path", remotePath }
                };

                string response = await _connection.SendRequestAsync(request);
                Dictionary<string, string>? responseObj = JsonSerializer.Deserialize<Dictionary<string, string>>(response);

                if (responseObj != null && responseObj.ContainsKey("status") && responseObj["status"] == "success" && responseObj.ContainsKey("data"))
                {
                    byte[] fileData = Convert.FromBase64String(responseObj["data"]);
                    
                    // Tạo thư mục cha nếu chưa tồn tại
                    string? directory = Path.GetDirectoryName(localFilePath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                    
                    File.WriteAllBytes(localFilePath, fileData);
                    return JsonSerializer.Serialize(new Dictionary<string, string>
                    {
                        { "status", "success" },
                        { "message", "Tải file xuống thành công" }
                    });
                }
                
                return response;
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "status", "error" },
                    { "message", $"Lỗi tải file xuống: {ex.Message}" }
                });
            }
        }

        // Xóa file hoặc thư mục
        public async Task<string> DeleteAsync(string path)
        {
            if (!_isLoggedIn)
            {
                return JsonSerializer.Serialize(new Dictionary<string, string>
                {
                    { "status", "error" },
                    { "message", "Chưa đăng nhập" }
                });
            }

            Dictionary<string, string> request = new Dictionary<string, string>
            {
                { "command", "delete" },
                { "path", path }
            };

            return await _connection.SendRequestAsync(request);
        }

        // Liệt kê nội dung thư mục
        public async Task<(bool success, List<string> items)> ListDirectoryAsync(string path)
        {
            if (!_isLoggedIn)
            {
                return (false, new List<string>());
            }

            Dictionary<string, string> request = new Dictionary<string, string>
            {
                { "command", "list_directory" },
                { "path", path }
            };

            string response = await _connection.SendRequestAsync(request);
            try
            {
                using JsonDocument doc = JsonDocument.Parse(response);
                JsonElement root = doc.RootElement;

                if (root.TryGetProperty("status", out JsonElement statusElement) && 
                    statusElement.GetString() == "success" &&
                    root.TryGetProperty("items", out JsonElement itemsElement))
                {
                    List<string> items = new List<string>();
                    foreach (JsonElement item in itemsElement.EnumerateArray())
                    {
                        items.Add(item.GetString() ?? string.Empty);
                    }
                    return (true, items);
                }
                
                return (false, new List<string>());
            }
            catch
            {
                return (false, new List<string>());
            }
        }

        // Đóng kết nối
        public void Disconnect()
        {
            _connection.Disconnect();
        }
    }

    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
        // Xóa phương thức Main thứ hai này
        // static async Task Main(string[] args)
        // {
        //     Console.OutputEncoding = Encoding.UTF8;
        //     Console.InputEncoding = Encoding.UTF8;
        //     
        //     Console.WriteLine("===== ỨNG DỤNG LƯU TRỮ FILE CLIENT =====");
        //     
        //     string serverIp = "127.0.0.1";
        //     int serverPort = 8888;
        //     
        //     FileStorageClient client = new FileStorageClient(serverIp, serverPort);
        //     
        //     Console.WriteLine("Đang kết nối đến server...");
        //     bool connected = await client.ConnectAsync();
        //     if (!connected)
        //     {
        //         Console.WriteLine("Không thể kết nối đến server. Ứng dụng sẽ thoát.");
        //         return;
        //     }
        //     
        //     Console.WriteLine("Đã kết nối đến server!");
        //     
        //     bool running = true;
        //     while (running)
        //     {
        //         Console.WriteLine("\n===== MENU =====");
        //         Console.WriteLine("1. Đăng ký tài khoản");
        //         Console.WriteLine("2. Đăng nhập");
        //         Console.WriteLine("3. Tạo thư mục");
        //         Console.WriteLine("4. Tải file lên");
        //         Console.WriteLine("5. Tải file xuống");
        //         Console.WriteLine("6. Xóa file hoặc thư mục");
        //         Console.WriteLine("7. Liệt kê nội dung thư mục");
        //         Console.WriteLine("8. Đăng xuất");
        //         Console.WriteLine("9. Thoát");
        //         Console.Write("Chọn chức năng: ");
        //         
        //         if (!int.TryParse(Console.ReadLine(), out int choice))
        //         {
        //             Console.WriteLine("Lựa chọn không hợp lệ!");
        //             continue;
        //         }
        //         
        //         switch (choice)
        //         {
        //             case 1: // Đăng ký
        //                 await RegisterAsync(client);
        //                 break;
        //             case 2: // Đăng nhập
        //                 await LoginAsync(client);
        //                 break;
        //             case 3: // Tạo thư mục
        //                 await CreateDirectoryAsync(client);
        //                 break;
        //             case 4: // Tải file lên
        //                 await UploadFileAsync(client);
        //                 break;
        //             case 5: // Tải file xuống
        //                 await DownloadFileAsync(client);
        //                 break;
        //             case 6: // Xóa file hoặc thư mục
        //                 await DeleteAsync(client);
        //                 break;
        //             case 7: // Liệt kê nội dung thư mục
        //                 await ListDirectoryAsync(client);
        //                 break;
        //             case 8: // Đăng xuất
        //                 await LogoutAsync(client);
        //                 break;
        //             case 9: // Thoát
        //                 running = false;
        //                 break;
        //             default:
        //                 Console.WriteLine("Lựa chọn không hợp lệ!");
        //                 break;
        //         }
        //     }
        //     
        //     client.Disconnect();
        //     Console.WriteLine("Đã đóng kết nối. Ứng dụng sẽ thoát.");
        // }
        // Giữ lại các phương thức hỗ trợ khác
        // Xử lý đăng ký
        static async Task RegisterAsync(FileStorageClient client)
        {
            Console.Write("Nhập tên người dùng: ");
            string username = Console.ReadLine() ?? string.Empty;
            
            Console.Write("Nhập mật khẩu: ");
            string password = Console.ReadLine() ?? string.Empty;
            
            string response = await client.RegisterAsync(username, password);
            DisplayResponse(response);
        }
        
        // Xử lý đăng nhập
        static async Task LoginAsync(FileStorageClient client)
        {
            Console.Write("Nhập tên người dùng: ");
            string username = Console.ReadLine() ?? string.Empty;
            
            Console.Write("Nhập mật khẩu: ");
            string password = Console.ReadLine() ?? string.Empty;
            
            string response = await client.LoginAsync(username, password);
            DisplayResponse(response);
        }
        
        // Xử lý tạo thư mục
        static async Task CreateDirectoryAsync(FileStorageClient client)
        {
            Console.Write("Nhập đường dẫn thư mục cần tạo: ");
            string path = Console.ReadLine() ?? string.Empty;
            
            string response = await client.CreateDirectoryAsync(path);
            DisplayResponse(response);
        }
        
        // Xử lý tải file lên
        static async Task UploadFileAsync(FileStorageClient client)
        {
            Console.Write("Nhập đường dẫn file trên máy tính: ");
            string localPath = Console.ReadLine() ?? string.Empty;
            
            Console.Write("Nhập đường dẫn file trên server: ");
            string remotePath = Console.ReadLine() ?? string.Empty;
            
            string response = await client.UploadFileAsync(localPath, remotePath);
            DisplayResponse(response);
        }
        
        // Xử lý tải file xuống
        static async Task DownloadFileAsync(FileStorageClient client)
        {
            Console.Write("Nhập đường dẫn file trên server: ");
            string remotePath = Console.ReadLine() ?? string.Empty;
            
            Console.Write("Nhập đường dẫn lưu file trên máy tính: ");
            string localPath = Console.ReadLine() ?? string.Empty;
            
            string response = await client.DownloadFileAsync(remotePath, localPath);
            DisplayResponse(response);
        }
        
        // Xử lý xóa file hoặc thư mục
        static async Task DeleteAsync(FileStorageClient client)
        {
            Console.Write("Nhập đường dẫn file hoặc thư mục cần xóa: ");
            string path = Console.ReadLine() ?? string.Empty;
            
            string response = await client.DeleteAsync(path);
            DisplayResponse(response);
        }
        
        // Xử lý liệt kê nội dung thư mục
        static async Task ListDirectoryAsync(FileStorageClient client)
        {
            Console.Write("Nhập đường dẫn thư mục cần liệt kê (để trống cho thư mục gốc): ");
            string path = Console.ReadLine() ?? string.Empty;
            
            var (success, items) = await client.ListDirectoryAsync(path);
            
            if (success)
            {
                Console.WriteLine("\nDanh sách file và thư mục:");
                if (items.Count == 0)
                {
                    Console.WriteLine("(Thư mục trống)");
                }
                else
                {
                    foreach (string item in items)
                    {
                        if (item.StartsWith("D:"))
                        {
                            Console.WriteLine($"[Thư mục] {item.Substring(2)}");
                        }
                        else if (item.StartsWith("F:"))
                        {
                            Console.WriteLine($"[File] {item.Substring(2)}");
                        }
                        else
                        {
                            Console.WriteLine(item);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Không thể liệt kê nội dung thư mục.");
            }
        }
        
        // Xử lý đăng xuất
        static async Task LogoutAsync(FileStorageClient client)
        {
            string response = await client.LogoutAsync();
            DisplayResponse(response);
        }
        
        // Hiển thị phản hồi từ server
        static void DisplayResponse(string response)
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(response);
                JsonElement root = doc.RootElement;
                
                if (root.TryGetProperty("status", out JsonElement statusElement))
                {
                    string status = statusElement.GetString() ?? "";
                    
                    if (root.TryGetProperty("message", out JsonElement messageElement))
                    {
                        string message = messageElement.GetString() ?? "";
                        
                        if (status == "success")
                        {
                            Console.WriteLine($"\n[Thành công] {message}");
                        }
                        else
                        {
                            Console.WriteLine($"\n[Lỗi] {message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"\n[{status.ToUpper()}]");
                    }
                }
                else
                {
                    Console.WriteLine($"\nPhản hồi không hợp lệ: {response}");
                }
            }
            catch
            {
                Console.WriteLine($"\nPhản hồi không hợp lệ: {response}");
            }
        }
    }
}