# GitHub监听QQ Bot - 基于Lagrange.Core

这是一个基于Lagrange.Core框架开发的QQ Bot，用于监听GitHub仓库的动态并推送到指定的QQ群或好友。

## 功能特性

- 🚀 监听GitHub仓库的新提交(commits)
- 🐛 监听GitHub仓库的新Issue
- 🎉 监听GitHub仓库的新版本发布(releases)
- 📱 支持多仓库、多群组、多好友推送
- ⚙️ 灵活的配置选项和消息模板
- 🔄 自动重连和错误恢复
- 💬 简单的命令交互

## 环境要求

- .NET 8.0 或更高版本
- Windows/Linux/macOS

## 快速开始

### 1. 配置Bot账号

1. 将 `config.example.json` 复制为 `config.json`
2. 编辑 `config.json`：
   ```json
   {
     "Account": {
       "Uin": 1234567890,  // 你的Bot QQ号
       "Password": "",     // 留空使用二维码登录
       "Protocol": "Linux",
       "AutoReconnect": true
     }
   }
   ```

### 2. 配置GitHub监听

在 `config.json` 中配置要监听的仓库：

```json
{
  "GitHub": {
    "Token": "ghp_xxxxxxxxxxxx",  // 可选：GitHub Personal Access Token
    "Repositories": [
      {
        "Owner": "microsoft",           // 仓库所有者
        "Name": "vscode",              // 仓库名称
        "DisplayName": "VS Code",      // 显示名称
        "WatchEvents": ["commits", "issues", "releases"],
        "TargetGroups": [123456789],   // 目标QQ群号
        "TargetFriends": []           // 目标QQ好友号
      }
    ]
  }
}
```

### 3. 运行Bot

```bash
dotnet run
```

首次运行时如果没有设置密码，Bot会生成二维码文件 `qrcode.png`，请扫码登录。

## 配置说明

### Account 配置

- `Uin`: Bot的QQ号
- `Password`: QQ密码（建议留空使用二维码登录）
- `Protocol`: 协议类型，可选值：`Linux`、`MacOs`、`Windows`
- `AutoReconnect`: 是否自动重连

### GitHub 配置

- `Token`: GitHub Personal Access Token（可选，用于提高API调用限制）
- `Repositories`: 要监听的仓库列表
  - `Owner`: 仓库所有者
  - `Name`: 仓库名称
  - `DisplayName`: 在消息中显示的名称
  - `WatchEvents`: 监听的事件类型（`commits`、`issues`、`releases`）
  - `TargetGroups`: 接收通知的QQ群号列表
  - `TargetFriends`: 接收通知的QQ好友号列表

### Message 配置

- `Enabled`: 是否启用消息发送
- `Templates`: 消息模板配置
- `SendInterval`: 消息发送间隔（毫秒）

### Monitor 配置

- `Interval`: 监听间隔（秒，建议不少于300秒以避免API限制）
- `SendStartupMessage`: 是否发送启动消息
- `StartupMessage`: 启动消息内容
- `MaxItemsPerCheck`: 每次检查获取的最大条目数
- `VerboseLogging`: 是否启用详细日志

## 消息模板

支持以下占位符：

### 提交消息模板 (NewCommit)
- `{repo}`: 仓库显示名称
- `{author}`: 提交作者
- `{message}`: 提交消息
- `{url}`: 提交链接

### Issue消息模板 (NewIssue)
- `{repo}`: 仓库显示名称
- `{author}`: Issue创建者
- `{title}`: Issue标题
- `{url}`: Issue链接

### Release消息模板 (NewRelease)
- `{repo}`: 仓库显示名称
- `{version}`: 版本标签
- `{title}`: Release标题
- `{url}`: Release链接

## Bot命令

在QQ群或私聊中可以使用以下命令：

- `/github status` - 查看Bot状态
- `/github repos` - 查看监听的仓库状态

## 文件说明

运行后会生成以下文件：

- `config.json` - 配置文件
- `device.json` - 设备信息（自动生成，请勿删除）
- `keystore.json` - 密钥库（自动生成，请勿删除）
- `qrcode.png` - 登录二维码（仅在扫码登录时生成）

## 注意事项

1. **GitHub API限制**: 
   - 未认证用户：每小时60次请求
   - 认证用户：每小时5000次请求
   - 建议配置GitHub Token以获得更高的API限制

2. **监听间隔**:
   - 建议设置至少5分钟（300秒）的监听间隔
   - 过于频繁的请求可能导致API限制

3. **首次运行**:
   - 首次检查时不会发送通知，避免历史消息轰炸
   - Bot会记住已经处理过的项目，只推送新的动态

4. **安全性**:
   - 请妥善保管 `device.json` 和 `keystore.json` 文件
   - 建议定期备份这些文件

## 故障排除

### 常见问题

1. **类型冲突错误**
   ```
   dotnet clean
   dotnet restore
   dotnet build
   ```

2. **登录失败**
   - 检查QQ号和密码是否正确
   - 尝试使用二维码登录
   - 确认网络连接正常

3. **GitHub API失败**
   - 检查网络连接
   - 验证GitHub Token是否有效
   - 检查仓库名称是否正确

4. **消息发送失败**
   - 确认Bot已成功加入目标群聊
   - 检查群号是否正确
   - 确认Bot有发言权限

## 开发说明

项目结构：
```
GitHubBot-LagrangeV1/
├── Program.cs              # 主程序
├── Config/
│   └── BotConfiguration.cs # 配置类
├── Services/
│   └── GitHubService.cs    # GitHub API服务
├── Lagrange.Core/          # Lagrange.Core子模块
└── config.example.json     # 配置模板
```

## 许可证

本项目采用与Lagrange.Core相同的许可证。

## 贡献

欢迎提交Issue和Pull Request！