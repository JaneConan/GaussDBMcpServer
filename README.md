# GaussDB MCP Server

一个基于 Model Context Protocol (MCP) 的 GaussDB 数据库操作服务器，使用 C# 和 .NET 10.0 开发。该服务器提供了完整的数据库操作工具集，可以通过 AI 助手（如 Copilot Chat）直接操作 GaussDB 数据库。

## 功能特性

- ✅ **数据库连接测试** - 验证数据库连接配置是否有效
- ✅ **数据库管理** - 创建数据库
- ✅ **表管理** - 创建、删除表，获取建表语句
- ✅ **数据操作** - 支持插入、查询、更新、删除数据
- ✅ **安全防护** - 使用参数化查询，防止 SQL 注入
- ✅ **连接池管理** - 内置连接池，提高性能
- ✅ **多平台支持** - 支持 Windows、macOS、Linux 等多个平台

## 环境要求

- .NET 10.0 SDK 或更高版本（用于开发）
- GaussDB 数据库实例（用于运行）
- VS Code 或 Visual Studio（用于配置 MCP 服务器）

## 安装和配置

### 1. 环境变量配置

在运行 MCP 服务器之前，需要设置以下环境变量来配置数据库连接：

| 环境变量 | 说明 | 默认值 | 必需 |
|---------|------|--------|------|
| `GAUSSDB_HOST` | 数据库主机地址 | `127.0.0.1` | 否 |
| `GAUSSDB_PORT` | 数据库端口号 | `8000` | 否 |
| `GAUSSDB_USER` | 数据库用户名 | `root` | 否 |
| `GAUSSDB_PASSWORD` | 数据库密码 | `password` | 否 |
| `GAUSSDB_DATABASE` | 默认数据库名称 | `postgres` | 否 |

**示例（Linux/macOS）：**
```bash
export GAUSSDB_HOST=your-db-host
export GAUSSDB_PORT=8000
export GAUSSDB_USER=your-username
export GAUSSDB_PASSWORD=your-password
export GAUSSDB_DATABASE=your-database
```

**示例（Windows PowerShell）：**
```powershell
$env:GAUSSDB_HOST="your-db-host"
$env:GAUSSDB_PORT="8000"
$env:GAUSSDB_USER="your-username"
$env:GAUSSDB_PASSWORD="your-password"
$env:GAUSSDB_DATABASE="your-database"
```

### 2. 从 NuGet 安装（推荐）

如果已发布到 NuGet.org，可以通过以下方式配置：

**VS Code：** 创建 `<工作区目录>/.vscode/mcp.json` 文件

**Visual Studio：** 创建 `<解决方案目录>\.mcp.json` 文件

配置内容：
```json
{
  "servers": {
    "GaussDBMcpServer": {
      "type": "stdio",
      "command": "dnx",
      "args": [
        "GaussDBMcpServer",
        "--version",
        "0.1.0-beta",
        "--yes"
      ],
      "env": {
        "GAUSSDB_HOST": "your-db-host",
        "GAUSSDB_PORT": "8000",
        "GAUSSDB_USER": "your-username",
        "GAUSSDB_PASSWORD": "your-password",
        "GAUSSDB_DATABASE": "your-database"
      }
    }
  }
}
```

### 3. 本地开发模式

如果要从源代码直接运行，可以配置 IDE 使用 `dotnet run`：

```json
{
  "servers": {
    "GaussDBMcpServer": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "<项目目录路径>"
      ],
      "env": {
        "GAUSSDB_HOST": "your-db-host",
        "GAUSSDB_PORT": "8000",
        "GAUSSDB_USER": "your-username",
        "GAUSSDB_PASSWORD": "your-password",
        "GAUSSDB_DATABASE": "your-database"
      }
    }
  }
}
```

## 可用工具

### 1. TestDatabaseConnection

测试数据库连接可用性，验证当前配置的数据库连接参数是否有效。

**返回结果：** JSON 格式的连接测试结果，包含连接状态、服务器版本等信息。

### 2. CreateDatabase

创建新数据库。

**参数：**
- `dbName` (string, 必需): 数据库名称，应符合 GaussDB 命名规范

**示例：** 创建名为 `my_database` 的数据库

### 3. CreateTable

创建数据表。

**参数：**
- `databaseName` (string, 必需): 数据库名称
- `tableName` (string, 必需): 表名称
- `schema` (string, 必需): 表结构定义，例如 `"id INT PRIMARY KEY, name VARCHAR(255)"`
- `schemaName` (string, 可选): 数据库模式，默认为 `"public"`

**示例：** 在 `my_db` 数据库中创建 `users` 表
```
databaseName: "my_db"
tableName: "users"
schema: "id SERIAL PRIMARY KEY, name VARCHAR(100) NOT NULL, email VARCHAR(255)"
schemaName: "public"
```

### 4. DropTable

删除数据表。

**参数：**
- `databaseName` (string, 必需): 数据库名称
- `tableName` (string, 必需): 表名称
- `schemaName` (string, 可选): 数据库模式，默认为 `"public"`

### 5. GetCreateTableSql

获取指定表的建表 SQL 语句。

**参数：**
- `databaseName` (string, 必需): 数据库名称
- `tableName` (string, 必需): 表名称

**返回：** 完整的 CREATE TABLE SQL 语句

### 6. Insert

向表中插入数据。

**参数：**
- `databaseName` (string, 必需): 数据库名称
- `tableName` (string, 必需): 表名称
- `data` (Dictionary<string, object>, 必需): 要插入的数据字典，例如 `{"id": 1, "name": "John"}`
- `schemaName` (string, 可选): 数据库模式，默认为 `"public"`

**示例：**
```
databaseName: "my_db"
tableName: "users"
data: {"name": "John Doe", "email": "john@example.com"}
```

### 7. Select

查询表中的数据。

**参数：**
- `databaseName` (string, 必需): 数据库名称
- `tableName` (string, 必需): 表名称
- `condition` (Dictionary<string, object>, 可选): 查询条件字典，例如 `{"id": 1}`。如果不提供，则查询所有数据
- `schemaName` (string, 可选): 数据库模式，默认为 `"public"`

**返回：** JSON 格式的查询结果数组

**示例：**
```
databaseName: "my_db"
tableName: "users"
condition: {"id": 1}
```

### 8. Update

更新表中的数据。

**参数：**
- `databaseName` (string, 必需): 数据库名称
- `tableName` (string, 必需): 表名称
- `data` (Dictionary<string, object>, 必需): 要更新的数据字典，例如 `{"name": "Jane"}`
- `condition` (Dictionary<string, object>, 必需): 更新条件字典，例如 `{"id": 1}`
- `schemaName` (string, 可选): 数据库模式，默认为 `"public"`

**示例：**
```
databaseName: "my_db"
tableName: "users"
data: {"name": "Jane Doe"}
condition: {"id": 1}
```

### 9. Delete

删除表中的数据。

**参数：**
- `databaseName` (string, 必需): 数据库名称
- `tableName` (string, 必需): 表名称
- `condition` (Dictionary<string, object>, 可选): 删除条件字典，例如 `{"id": 1}`。如果不提供，将删除表中所有数据（请谨慎使用）
- `schemaName` (string, 可选): 数据库模式，默认为 `"public"`

## 使用示例

配置完成后，你可以在 Copilot Chat 中使用自然语言与数据库交互：

- "测试一下数据库连接"
- "在 my_db 数据库中创建一个名为 users 的表，包含 id、name 和 email 字段"
- "查询 users 表中 id 为 1 的记录"
- "向 users 表插入一条新记录，name 是 John，email 是 john@example.com"
- "更新 users 表中 id 为 1 的记录，将 name 改为 Jane"
- "删除 users 表中 id 为 1 的记录"

## 本地开发

### 构建项目

```bash
dotnet build
```

### 运行项目

```bash
dotnet run
```

### 打包 NuGet 包

```bash
dotnet pack -c Release
```

打包后的 `.nupkg` 文件位于 `bin/Release` 目录。

## 发布到 NuGet.org

1. 确保已更新 `.csproj` 文件中的包元数据，特别是 `<PackageId>` 和 `<PackageVersion>`
2. 更新 `.mcp/server.json` 以声明 MCP 服务器的输入配置
3. 运行 `dotnet pack -c Release` 创建 NuGet 包
4. 使用以下命令发布到 NuGet.org：
   ```bash
   dotnet nuget push bin/Release/*.nupkg --api-key <your-api-key> --source https://api.nuget.org/v3/index.json
   ```

## 技术栈

- **.NET 10.0** - 开发框架
- **ModelContextProtocol** - MCP C# SDK
- **HuaweiCloud.Driver.GaussDB** - GaussDB 数据库驱动
- **Microsoft.Extensions.Hosting** - 依赖注入和日志

## 安全注意事项

- ✅ 所有 SQL 查询都使用参数化查询，防止 SQL 注入攻击
- ✅ 数据库标识符（表名、列名等）都经过转义处理
- ✅ 连接字符串中的敏感信息在日志中会被隐藏
- ⚠️ 请妥善保管数据库密码，不要将其提交到版本控制系统

## 支持的平台

该 MCP 服务器支持以下平台：

- `win-x64` - Windows 64 位
- `win-arm64` - Windows ARM64
- `osx-arm64` - macOS ARM64 (Apple Silicon)
- `linux-x64` - Linux 64 位
- `linux-arm64` - Linux ARM64
- `linux-musl-x64` - Linux musl 64 位

如需支持更多平台，请在项目的 `<RuntimeIdentifiers />` 元素中添加相应的运行时标识符。

## 更多信息

- [Model Context Protocol 官方文档](https://modelcontextprotocol.io/)
- [MCP 协议规范](https://spec.modelcontextprotocol.io/)
- [MCP GitHub 组织](https://github.com/modelcontextprotocol)
- [在 VS Code 中使用 MCP 服务器](https://code.visualstudio.com/docs/copilot/chat/mcp-servers)
- [在 Visual Studio 中使用 MCP 服务器](https://learn.microsoft.com/visualstudio/ide/mcp-servers)
- [.NET MCP 服务器开发指南](https://aka.ms/nuget/mcp/guide)

## 许可证

请查看 [LICENSE](../LICENSE) 文件了解许可证信息。

## 贡献

欢迎提交 Issue 和 Pull Request！

## 版本历史

- **0.1.0-beta** - 初始版本
  - 支持基本的数据库操作（创建、查询、更新、删除）
  - 支持表管理（创建、删除、获取建表语句）
  - 支持数据库连接测试
  - 支持数据库创建
