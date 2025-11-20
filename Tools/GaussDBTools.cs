using GaussDBMcpServer;
using HuaweiCloud.GaussDB;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Data;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// MCP tools for GaussDB.
/// </summary>
[McpServerToolType]
internal class GaussDBTools
{
    /// <summary>
    /// 测试数据库连接
    /// </summary>
    /// <param name="service">数据库服务</param>
    /// <param name="logger">日志工具</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>连接测试结果，包含详细的连接信息和状态</returns>
    [McpServerTool]
    [Description("测试数据库连接可用性，验证当前配置的数据库连接参数是否有效")]
    public static async Task<string> TestDatabaseConnection(
        GaussDBService service,
        ILogger<GaussDBTools> logger,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting database connection test...");

        var result = await service.TestConnectionAsync(cancellationToken);

        // 记录日志
        if (result.IsSuccess)
        {
            logger.LogInformation(result.ToString());
        }
        else
        {
            logger.LogError(result.ToString());
        }

        // 返回JSON格式的结果，便于解析
        return JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        });
    }

    /// <summary>
    /// 创建数据库（非事务方式）
    /// </summary>
    /// <param name="dbName">数据库名称，例如 "my_database"。应符合 GaussDB 命名规范</param>
    /// <param name="service">数据库服务</param>
    /// <param name="logger">日志工具</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>确认消息</returns>
    [McpServerTool]
    [Description("创建数据库（非事务方式）")]
    public static async Task<string> CreateDatabase(
        [Description("数据库名称，例如 \"my_database\"。应符合 GaussDB 命名规范（如不以数字开头，不能包含特殊字符等）")]
        string dbName,
        GaussDBService service,
        ILogger<GaussDBTools> logger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 连接到默认数据库创建新数据库
            using var connection = await service.GetConnectionAsync("postgres", cancellationToken);
            await using var command = new GaussDBCommand($"CREATE DATABASE {EscapeIdentifier(dbName)}", connection);
            await command.ExecuteNonQueryAsync(cancellationToken);

            var message = $"Successfully created database: {dbName}";
            logger.LogInformation(message);
            return message;
        }
        catch (Exception ex)
        {
            var message = $"Failed to create database {dbName}: {ex.Message}";
            logger.LogError(ex, message);
            throw new McpException(message);
        }
    }

    /// <summary>
    /// 创建表
    /// </summary>
    /// <param name="databaseName">数据库名称，例如 "my_db"</param>
    /// <param name="schemaName">数据库模式（可选，默认public），例如 "public"</param>
    /// <param name="tableName">表名称，例如 "my_table"</param>
    /// <param name="schema">表结构定义，例如 "id INT PRIMARY KEY, name VARCHAR(255)"</param>
    /// <param name="service">数据库服务</param>
    /// <param name="logger">日志工具</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>确认消息</returns>
    [McpServerTool]
    [Description("创建表")]
    public static async Task<string> CreateTable(
        GaussDBService service,
        ILogger<GaussDBTools> logger,
        [Description("数据库名称，例如 \"my_db\"。必须是有效的 GaussDB 数据库名称")]
        string databaseName,
        [Description("表名称，例如 \"my_table\"。必须是有效的 GaussDB 表名称，遵循命名规则")]
        string tableName,
        [Description("表结构定义，例如 'id INT PRIMARY KEY, name VARCHAR(255)'。必须是一个合法的 SQL 语句")]
        string schema,
        [Description("数据库模式（可选，默认public），例如 \"public\"")]
        string schemaName = "public",
        CancellationToken cancellationToken = default)
    {
        // 严格参数校验
        var validationErrors = new List<string>();
        if (string.IsNullOrWhiteSpace(databaseName))
            validationErrors.Add("Database name cannot be empty.");
        if (string.IsNullOrWhiteSpace(tableName))
            validationErrors.Add("Table name cannot be empty.");
        if (string.IsNullOrWhiteSpace(schema))
            validationErrors.Add("Table schema definition cannot be empty.");
        if (string.IsNullOrWhiteSpace(schemaName))
            schemaName = "public"; // 兜底默认模式

        if (validationErrors.Any())
        {
            var errorMsg = $"Cannot create table: {string.Join(" ", validationErrors)}";
            logger.LogError(errorMsg);
            throw new McpException(errorMsg);
        }

        try
        {
            using var connection = await service.GetConnectionAsync(databaseName, cancellationToken: cancellationToken);
            await using var command = new GaussDBCommand(
                $"CREATE TABLE IF NOT EXISTS {EscapeIdentifier(schemaName)}.{EscapeIdentifier(tableName)} ({schema})",
                connection);

            var affectedRows = await command.ExecuteNonQueryAsync(cancellationToken);
            var successMsg = $"{affectedRows} Successfully created table: {databaseName}.{schemaName}.{tableName}";
            logger.LogInformation(successMsg);

            return successMsg;
        }
        catch (Exception ex)
        {
            var message = $"Failed to create table {tableName}: {ex.Message}";
            logger.LogError(ex, message);
            throw new McpException(message);
        }
    }

    /// <summary>
    /// 删除表
    /// </summary>
    /// <param name="databaseName">数据库名称，例如 "my_db"</param>
    /// <param name="schemaName">数据库模式（可选，默认public），例如 "public"</param>
    /// <param name="tableName">表名称，例如 "my_table"</param>
    /// <param name="service">数据库服务</param>
    /// <param name="logger">日志工具</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>确认消息</returns>
    [McpServerTool]
    [Description("删除表")]
    public static async Task<string> DropTable(
        GaussDBService service,
        ILogger<GaussDBTools> logger,
        [Description("数据库名称，例如 \"my_db\"。必须是有效的 GaussDB 数据库名称")]
        string databaseName,
        [Description("表名称，例如 \"my_table\"。必须是有效的 GaussDB 表名称")]
        string tableName,
        [Description("数据库模式（可选，默认public），例如 \"public\"")]
        string schemaName = "public",
        CancellationToken cancellationToken = default)
    {
        // 严格参数校验
        var validationErrors = new List<string>();
        if (string.IsNullOrWhiteSpace(databaseName))
            validationErrors.Add("Database name cannot be empty.");
        if (string.IsNullOrWhiteSpace(tableName))
            validationErrors.Add("Table name cannot be empty.");
        if (string.IsNullOrWhiteSpace(schemaName))
            schemaName = "public"; // 兜底默认模式

        if (validationErrors.Any())
        {
            var errorMsg = $"Cannot drop table: {string.Join(" ", validationErrors)}";
            logger.LogError(errorMsg);
            throw new McpException(errorMsg);
        }

        try
        {
            using var connection = await service.GetConnectionAsync(databaseName, cancellationToken: cancellationToken);
            await using var command = new GaussDBCommand(
                $"DROP TABLE IF EXISTS {EscapeIdentifier(schemaName)}.{EscapeIdentifier(tableName)}",
                connection);

            await command.ExecuteNonQueryAsync(cancellationToken);

            var message = $"Successfully dropped table: {tableName}";
            logger.LogInformation(message);
            return message;
        }
        catch (Exception ex)
        {
            var message = $"Failed to drop table {tableName}: {ex.Message}";
            logger.LogError(ex, message);
            throw new McpException(message);
        }
    }

    /// <summary>
    /// 获取建表语句
    /// </summary>
    /// <param name="databaseName">数据库名称，例如 "my_db"</param>
    /// <param name="tableName">表名称，例如 "my_table"</param>
    /// <param name="service">数据库服务</param>
    /// <param name="logger">日志工具</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>建表语句</returns>
    [McpServerTool]
    [Description("获取建表语句")]
    public static async Task<string> GetCreateTableSql(
        [Description("数据库名称，例如 \"my_db\"。必须是有效的 GaussDB 数据库名称")]
        string databaseName,
        [Description("表名称，例如 \"my_table\"。必须是有效的 GaussDB 表名称")]
        string tableName,
        GaussDBService service,
        ILogger<GaussDBTools> logger,
        CancellationToken cancellationToken = default)
    {
        var query = """
            SELECT 
                'CREATE TABLE ' || relname || E'\n(\n' ||
                string_agg(
                    '    ' || column_name || ' ' || data_type ||
                    CASE WHEN is_nullable = 'NO' THEN ' NOT NULL' ELSE '' END,
                    E',\n'
                ) || E'\n);' AS create_table_sql
            FROM (
                SELECT 
                    c.relname,
                    a.attname AS column_name,
                    pg_catalog.format_type(a.atttypid, a.atttypmod) AS data_type,
                    col.is_nullable
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                JOIN pg_attribute a ON a.attrelid = c.oid
                JOIN information_schema.columns col 
                    ON col.table_name = c.relname AND col.column_name = a.attname
                WHERE c.relkind = 'r'
                    AND a.attnum > 0
                    AND NOT a.attisdropped
                    AND c.relname = @tableName
                ORDER BY a.attnum
            ) AS table_info
            GROUP BY relname
        """;

        try
        {
            using var connection = await service.GetConnectionAsync(databaseName, cancellationToken: cancellationToken);
            await using var command = new GaussDBCommand(query, connection);
            command.Parameters.AddWithValue("@tableName", tableName);

            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (result is string createSql)
            {
                logger.LogInformation($"Successfully generated create SQL for {tableName}");
                return createSql;
            }
            else
            {
                var message = $"Table '{tableName}' not found.";
                logger.LogError(message);
                throw new McpException(message);
            }
        }
        catch (McpException)
        {
            throw;
        }
        catch (Exception ex)
        {
            var message = $"Failed to get create table SQL for '{tableName}': {ex.Message}";
            logger.LogError(ex, message);
            throw new McpException(message);
        }
    }

    /// <summary>
    /// 插入数据
    /// </summary>
    /// <param name="databaseName">数据库名称，例如 "my_db"</param>
    /// <param name="schemaName">数据库模式（可选，默认public），例如 "public"</param>
    /// <param name="tableName">表名称，例如 "my_table"</param>
    /// <param name="data">要插入的数据字典，例如 {"id": 1, "name": "John"}</param>
    /// <param name="service">数据库服务</param>
    /// <param name="logger">日志工具</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>确认消息</returns>
    [McpServerTool]
    [Description("插入数据")]
    public static async Task<string> Insert(
        GaussDBService service,
        ILogger<GaussDBTools> logger,
        [Description("数据库名称，例如 \"my_db\"。必须是有效的 GaussDB 数据库名称")]
        string databaseName,
        [Description("表名称，例如 \"my_table\"。必须是有效的 GaussDB 表名称")]
        string tableName,
        [Description("要插入的数据字典，例如 {\"id\": 1, \"name\": \"John\"}")]
        Dictionary<string, object> data,
        [Description("数据库模式（可选，默认public），例如 \"public\"")]
        string schemaName = "public",
        CancellationToken cancellationToken = default)
    {
        // 严格参数校验
        var validationErrors = new List<string>();
        if (string.IsNullOrWhiteSpace(databaseName))
            validationErrors.Add("Database name cannot be empty.");
        if (string.IsNullOrWhiteSpace(tableName))
            validationErrors.Add("Table name cannot be empty.");
        if (string.IsNullOrWhiteSpace(schemaName))
            schemaName = "public"; // 兜底默认模式
            
        if (validationErrors.Any())
        {
            var errorMsg = $"Cannot drop table: {string.Join(" ", validationErrors)}";
            logger.LogError(errorMsg);
            throw new McpException(errorMsg);
        }

        if (data == null || data.Count == 0)
        {
            var message = "Insert data cannot be empty";
            logger.LogError(message);
            throw new McpException(message);
        }

        try
        {
            using var connection = await service.GetConnectionAsync(databaseName, cancellationToken: cancellationToken);

            var columns = string.Join(", ", data.Keys.Select(EscapeIdentifier));
            var placeholders = string.Join(", ", data.Keys.Select(k => $"@{k}"));

            await using var command = new GaussDBCommand(
                $"INSERT INTO {EscapeIdentifier(schemaName)}.{EscapeIdentifier(tableName)} ({columns}) VALUES ({placeholders})",
                connection);

            foreach (var (key, rawValue) in data)
            {
                var value = NormalizeValue(rawValue);

                var p = command.CreateParameter();
                p.ParameterName = $"@{key}";
                p.Value = value;

                // 显式设置 DbType，避免被推成 Text
                try
                {
                    p.DbType = GetDbType(value);
                }
                catch { }

                command.Parameters.Add(p);
            }

            await command.ExecuteNonQueryAsync(cancellationToken);

            var dataJson = JsonSerializer.Serialize(data);
            var message = $"Successfully inserted data into {tableName}: {dataJson}";
            logger.LogInformation(message);
            return message;
        }
        catch (Exception ex)
        {
            var message = $"Failed to insert data into {tableName}: {ex.Message}";
            logger.LogError(ex, message);
            throw new McpException(message);
        }
    }
    private static object? NormalizeValue(object? value)
    {
        if (value is null)
            return DBNull.Value;

        // 必须处理 JsonElement
        if (value is JsonElement je)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.Number:
                    if (je.TryGetInt32(out var i32)) return i32;
                    if (je.TryGetInt64(out var i64)) return i64;
                    if (je.TryGetDouble(out var d)) return d;
                    return je.GetRawText();

                case JsonValueKind.String:
                    return je.GetString();

                case JsonValueKind.True:
                case JsonValueKind.False:
                    return je.GetBoolean();

                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    // 对象或数组 → 保存成 JSON 文本
                    return je.GetRawText();

                case JsonValueKind.Null:
                    return DBNull.Value;

                default:
                    return je.ToString();
            }
        }

        // 如果是 JsonNode / JsonValue 也处理一下（你的 MCP server 会遇到）
        if (value is JsonNode jn)
        {
            return NormalizeValue(jn.AsValue().GetValue<JsonElement>());
        }

        return value;
    }


    private static DbType GetDbType(object? value)
    {
        if (value == null || value == DBNull.Value) return DbType.Object;

        return value switch
        {
            int => DbType.Int32,
            long => DbType.Int64,
            float => DbType.Single,
            double => DbType.Double,
            decimal => DbType.Decimal,
            bool => DbType.Boolean,
            string => DbType.String,
            DateTime => DbType.DateTime,
            Guid => DbType.Guid,
            byte[] => DbType.Binary,
            _ => DbType.String
        };
    }

    /// <summary>
    /// 查询数据
    /// </summary>
    /// <param name="databaseName">数据库名称，例如 "my_db"</param>
    /// <param name="schemaName">数据库模式（可选，默认public），例如 "public"</param>
    /// <param name="tableName">表名称，例如 "my_table"</param>
    /// <param name="condition">查询条件字典（可选），例如 {"id": 1}</param>
    /// <param name="service">数据库服务</param>
    /// <param name="logger">日志工具</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>查询结果列表</returns>
    [McpServerTool]
    [Description("查询数据")]
    public static async Task<string> Select(
        [Description("数据库名称，例如 \"my_db\"。必须是有效的 GaussDB 数据库名称")]
        string databaseName,
        [Description("表名称，例如 \"my_table\"。必须是有效的 GaussDB 表名称")]
        string tableName,
        [Description("查询条件字典（可选），查询表中所有数据时不需要条件，例如 {\"id\": 1}")]
        Dictionary<string, object>? condition = null,
        [Description("数据库模式（可选，默认public），例如 \"public\"")]
        string schemaName = "public",
        GaussDBService service = default!,
        ILogger<GaussDBTools> logger = default!,
        CancellationToken cancellationToken = default)
    {
        // 严格参数校验
        var validationErrors = new List<string>();
        if (string.IsNullOrWhiteSpace(databaseName))
            validationErrors.Add("Database name cannot be empty.");
        if (string.IsNullOrWhiteSpace(tableName))
            validationErrors.Add("Table name cannot be empty.");
        if (string.IsNullOrWhiteSpace(schemaName))
            schemaName = "public"; // 兜底默认模式

        if (validationErrors.Any())
        {
            var errorMsg = $"Cannot drop table: {string.Join(" ", validationErrors)}";
            logger.LogError(errorMsg);
            throw new McpException(errorMsg);
        }

        try
        {
            using var connection = await service.GetConnectionAsync(databaseName, cancellationToken: cancellationToken);
            var query = $"SELECT * FROM {EscapeIdentifier(schemaName)}.{EscapeIdentifier(tableName)}";
            var paramIndex = 0;

            if (condition != null && condition.Count > 0)
            {
                var whereClauses = new List<string>();
                foreach (var key in condition.Keys)
                {
                    whereClauses.Add($"{EscapeIdentifier(key)} = @param{paramIndex}");
                    paramIndex++;
                }
                query += $" WHERE {string.Join(" AND ", whereClauses)}";
            }

            await using var command = new GaussDBCommand(query, connection);

            if (condition != null && condition.Count > 0)
            {
                paramIndex = 0;
                foreach (var value in condition.Values)
                {
                    command.Parameters.AddWithValue($"@param{paramIndex}", value ?? DBNull.Value);
                    paramIndex++;
                }
            }

            var results = new List<Dictionary<string, object>>();
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = await reader.IsDBNullAsync(i, cancellationToken)
                        ? null
                        : reader.GetValue(i);

                    // 处理日期时间类型，与Python的isoformat保持一致
                    if (value is DateTime dt)
                    {
                        row[reader.GetName(i)] = dt.ToString("o");
                    }
                    else if (value is DateOnly date)
                    {
                        row[reader.GetName(i)] = date.ToString("yyyy-MM-dd");
                    }
                    else
                    {
                        row[reader.GetName(i)] = value ?? "";
                    }
                }
                results.Add(row);
            }

            logger.LogInformation($"Successfully selected {results.Count} rows from {tableName}");
            return JsonSerializer.Serialize(results);
        }
        catch (Exception ex)
        {
            var message = $"Failed to select from {tableName}: {ex.Message}";
            logger.LogError(ex, message);
            throw new McpException(message);
        }
    }

    /// <summary>
    /// 更新数据
    /// </summary>
    /// <param name="databaseName">数据库名称，例如 "my_db"</param>
    /// <param name="schemaName">数据库模式（可选，默认public），例如 "public"</param>
    /// <param name="tableName">表名称，例如 "my_table"</param>
    /// <param name="data">要更新的数据字典，例如 {"name": "John"}</param>
    /// <param name="condition">更新条件字典（可选），例如 {"id": 1}</param>
    /// <param name="service">数据库服务</param>
    /// <param name="logger">日志工具</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>确认消息</returns>
    [McpServerTool]
    [Description("更新数据")]
    public static async Task<string> Update(
        GaussDBService service,
        ILogger<GaussDBTools> logger,
        [Description("数据库名称，例如 \"my_db\"。必须是有效的 GaussDB 数据库名称")]
        string databaseName,
        [Description("表名称，例如 \"my_table\"。必须是有效的 GaussDB 表名称")]
        string tableName,
        [Description("要更新的数据字典，例如 {\"name\": \"John\"}")]
        Dictionary<string, object> data,
        [Description("更新条件字典（可选），例如 {\"id\": 1}")]
        Dictionary<string, object>? condition,
        [Description("数据库模式（可选，默认public），例如 \"public\"")]
        string schemaName = "public",
        CancellationToken cancellationToken = default)
    {
        // 严格参数校验
        var validationErrors = new List<string>();
        if (string.IsNullOrWhiteSpace(databaseName))
            validationErrors.Add("Database name cannot be empty.");
        if (string.IsNullOrWhiteSpace(tableName))
            validationErrors.Add("Table name cannot be empty.");
        if (string.IsNullOrWhiteSpace(schemaName))
            schemaName = "public"; // 兜底默认模式

        if (validationErrors.Any())
        {
            var errorMsg = $"Cannot drop table: {string.Join(" ", validationErrors)}";
            logger.LogError(errorMsg);
            throw new McpException(errorMsg);
        }

        if (data == null || data.Count == 0)
        {
            var message = "Update data cannot be empty";
            logger.LogError(message);
            throw new McpException(message);
        }

        if (condition == null || condition.Count == 0)
        {
            var message = "Update condition cannot be empty";
            logger.LogError(message);
            throw new McpException(message);
        }

        try
        {
            using var connection = await service.GetConnectionAsync(databaseName, cancellationToken: cancellationToken);

            var setClauses = new List<string>();
            var paramIndex = 0;

            // 构建SET子句
            foreach (var key in data.Keys)
            {
                setClauses.Add($"{EscapeIdentifier(key)} = @set{paramIndex}");
                paramIndex++;
            }

            // 构建WHERE子句
            var whereClauses = new List<string>();
            foreach (var key in condition.Keys)
            {
                whereClauses.Add($"{EscapeIdentifier(key)} = @where{paramIndex - data.Count}");
                paramIndex++;
            }

            var query = $"UPDATE {EscapeIdentifier(schemaName)}.{EscapeIdentifier(tableName)} SET {string.Join(", ", setClauses)} WHERE {string.Join(" AND ", whereClauses)}";
            await using var command = new GaussDBCommand(query, connection);

            // 添加参数
            paramIndex = 0;
            foreach (var value in data.Values)
            {
                command.Parameters.AddWithValue($"@set{paramIndex}", value ?? DBNull.Value);
                paramIndex++;
            }

            paramIndex = 0;
            foreach (var value in condition.Values)
            {
                command.Parameters.AddWithValue($"@where{paramIndex}", value ?? DBNull.Value);
                paramIndex++;
            }

            await command.ExecuteNonQueryAsync(cancellationToken);

            var dataJson = JsonSerializer.Serialize(data);
            var message = $"Successfully updated {tableName} with data: {dataJson}";
            logger.LogInformation(message);
            return message;
        }
        catch (Exception ex)
        {
            var message = $"Failed to update {tableName}: {ex.Message}";
            logger.LogError(ex, message);
            throw new McpException(message);
        }
    }

    /// <summary>
    /// 删除数据
    /// </summary>
    /// <param name="databaseName">数据库名称，例如 "my_db"</param>
    /// <param name="schemaName">数据库模式（可选，默认public），例如 "public"</param>
    /// <param name="tableName">表名称，例如 "my_table"</param>
    /// <param name="condition">删除条件字典（可选），例如 {"id": 1}</param>
    /// <param name="service">数据库服务</param>
    /// <param name="logger">日志工具</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>确认消息</returns>
    [McpServerTool]
    [Description("删除数据")]
    public static async Task<string> Delete(
        GaussDBService service,
        ILogger<GaussDBTools> logger,
        [Description("数据库名称，例如 \"my_db\"。必须是有效的 GaussDB 数据库名称")]
        string databaseName,
        [Description("表名称，例如 \"my_table\"。必须是有效的 GaussDB 表名称")]
        string tableName,
        [Description("删除条件字典（可选），删除表中所有数据时不需要条件，例如 {\"id\": 1}")]
        Dictionary<string, object>? condition,
        [Description("数据库模式（可选，默认public），例如 \"public\"")]
        string schemaName = "public",
        CancellationToken cancellationToken = default)
    {
        // 严格参数校验
        var validationErrors = new List<string>();
        if (string.IsNullOrWhiteSpace(databaseName))
            validationErrors.Add("Database name cannot be empty.");
        if (string.IsNullOrWhiteSpace(tableName))
            validationErrors.Add("Table name cannot be empty.");
        if (string.IsNullOrWhiteSpace(schemaName))
            schemaName = "public"; // 兜底默认模式

        if (validationErrors.Any())
        {
            var errorMsg = $"Cannot drop table: {string.Join(" ", validationErrors)}";
            logger.LogError(errorMsg);
            throw new McpException(errorMsg);
        }

        try
        {
            using var connection = await service.GetConnectionAsync(databaseName, cancellationToken: cancellationToken);

            var query = $"DELETE FROM {EscapeIdentifier(schemaName)}.{EscapeIdentifier(tableName)}";
            var paramIndex = 0;

            if (condition != null && condition.Count > 0)
            {
                var whereClauses = new List<string>();
                foreach (var key in condition.Keys)
                {
                    whereClauses.Add($"{EscapeIdentifier(key)} = @param{paramIndex}");
                    paramIndex++;
                }
                query += $" WHERE {string.Join(" AND ", whereClauses)}";
            }

            await using var command = new GaussDBCommand(query, connection);

            if (condition != null && condition.Count > 0)
            {
                paramIndex = 0;
                foreach (var value in condition.Values)
                {
                    command.Parameters.AddWithValue($"@param{paramIndex}", value ?? DBNull.Value);
                    paramIndex++;
                }
            }

            await command.ExecuteNonQueryAsync(cancellationToken);

            var conditionJson = condition != null ? JsonSerializer.Serialize(condition) : "None";
            var message = $"Successfully deleted from {tableName} with condition: {conditionJson}";
            logger.LogInformation(message);
            return message;
        }
        catch (Exception ex)
        {
            var message = $"Failed to delete from {tableName}: {ex.Message}";
            logger.LogError(ex, message);
            throw new McpException(message);
        }
    }

    /// <summary>
    /// 转义SQL标识符，防止SQL注入
    /// </summary>
    private static string EscapeIdentifier(string identifier)
    {
        // 使用双引号转义标识符，兼容PostgreSQL/GaussDB
        return $"\"{identifier.Replace("\"", "\"\"")}\"";
    }
}
