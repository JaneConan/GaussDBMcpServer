using HuaweiCloud.GaussDB;
using Microsoft.Extensions.Logging;

namespace GaussDBMcpServer
{
    // 数据库服务（直接从系统环境变量读取配置）
    public class GaussDBService
    {// 环境变量名称与Python版本保持一致
        private const string EnvHost = "GAUSSDB_HOST";
        private const string EnvPort = "GAUSSDB_PORT";
        private const string EnvUser = "GAUSSDB_USER";
        private const string EnvPassword = "GAUSSDB_PASSWORD";
        private const string EnvDatabase = "GAUSSDB_DATABASE";

        // 数据库连接参数
        public string Host { get; }
        public int Port { get; }
        public string User { get; }
        public string Password { get; }
        public string Database { get; }

        private readonly ILogger<GaussDBService> _logger;

        public GaussDBService(ILogger<GaussDBService> logger)
        {
            _logger = logger;

            // 从系统环境变量读取配置，提供默认值
            Host = Environment.GetEnvironmentVariable(EnvHost) ?? "127.0.0.1";
            User = Environment.GetEnvironmentVariable(EnvUser) ?? "root";
            Password = Environment.GetEnvironmentVariable(EnvPassword) ?? "password";
            Database = Environment.GetEnvironmentVariable(EnvDatabase) ?? "postgres";

            // 处理端口号（需要转换为int）
            if (!int.TryParse(Environment.GetEnvironmentVariable(EnvPort), out var port))
            {
                _logger.LogWarning($"环境变量 {EnvPort} 未设置或格式错误，使用默认端口 8000");
                Port = 8000;
            }
            else
            {
                Port = port;
            }

            _logger.LogInformation($"Initialized GaussDB service with: " +
                $"Host={Host}, Port={Port}, Database={Database}, User={User}");
        }

        // 获取数据库连接字符串
        public string GetConnectionString(string? database = null)
        {
            return $"Host={Host};Port={Port};Username={User};Password={Password};Database={database ?? Database};" +
                   "Pooling=true;MaxPoolSize=20;MinPoolSize=5;Timeout=10;CommandTimeout=10;";
        }

        // 获取数据库连接
        public async Task<GaussDBConnection> GetConnectionAsync(string? database = null, CancellationToken cancellationToken = default)
        {
            var connection = new GaussDBConnection(GetConnectionString(database));
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        /// <summary>
        /// 测试数据库连接
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>连接测试结果</returns>
        public async Task<ConnectionTestResult> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            var result = new ConnectionTestResult
            {
                Host = Host,
                Port = Port,
                Database = Database,
                User = User,
                Timestamp = DateTime.Now
            };

            try
            {
                // 尝试连接数据库，设置较短的超时时间
                using var connection = new GaussDBConnection(GetConnectionString());
                await connection.OpenAsync(cancellationToken);

                // 连接成功，执行简单查询验证
                await using var command = new GaussDBCommand("SELECT 1", connection);
                command.CommandTimeout = 10; // 10秒超时

                var queryResult = await command.ExecuteScalarAsync(cancellationToken);

                result.IsSuccess = true;
                result.Message = "Database connection successful";
                result.ServerVersion = connection.PostgreSqlVersion.ToString();
                result.ConnectionString = GetConnectionString("***"); // 隐藏实际数据库名
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.Message = $"Connection failed: {ex.Message}";
                result.ErrorDetails = ex.ToString();
                result.ConnectionString = GetConnectionString("***");
            }

            return result;
        }
    }

    /// <summary>
    /// 数据库连接测试结果模型
    /// </summary>
    public class ConnectionTestResult
    {
        /// <summary>
        /// 是否连接成功
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 主机地址
        /// </summary>
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// 端口号
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// 数据库名称
        /// </summary>
        public string Database { get; set; } = string.Empty;

        /// <summary>
        /// 用户名
        /// </summary>
        public string User { get; set; } = string.Empty;

        /// <summary>
        /// 测试时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 消息
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 数据库服务器版本
        /// </summary>
        public string ServerVersion { get; set; } = string.Empty;

        /// <summary>
        /// 错误详情（仅失败时提供）
        /// </summary>
        public string ErrorDetails { get; set; } = string.Empty;

        /// <summary>
        /// 连接字符串（隐藏敏感信息）
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// 转换为字符串输出
        /// </summary>
        public override string ToString()
        {
            var successStr = IsSuccess ? "SUCCESS" : "FAILED";
            var details = IsSuccess
                ? $"Server Version: {ServerVersion}"
                : $"Error: {ErrorDetails}";

            return $"[{successStr}] Database Connection Test - {Timestamp:yyyy-MM-dd HH:mm:ss}\n" +
                   $"Host: {Host}:{Port}\n" +
                   $"User: {User}\n" +
                   $"Database: {Database}\n" +
                   $"Connection String: {ConnectionString}\n" +
                   $"Message: {Message}\n" +
                   $"Details: {details}";
        }
    }
}
