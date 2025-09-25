# QQ消息转发Bot - 基于Lagrange.Core

这是一个基于Lagrange.Core框架开发的QQ Bot，用于监听指定QQ群的消息并根据配置规则转发到目标群。

## ✨ 主要功能

- 🔄 多群消息转发
- 📝 前缀过滤转发（如"注："开头的消息）
- 🔍 关键词过滤转发
- ⚙️ 灵活的转发规则配置
- 📊 转发统计功能
- 🚀 支持多种消息格式（文本、图片、表情等）

## 🚀 快速开始

### 1. 环境要求

- .NET 8.0 或更高版本
- Windows/Linux/macOS

### 2. 配置Bot

编辑 `config.json` 文件：

```json
{
  "Account": {
    "Uin": 0,  // 你的Bot QQ号
    "Password": "",  // 密码留空使用二维码登录
    "Protocol": "Linux",
    "AutoReconnect": true
  },
  "Message": {
    "Enabled": true,
    "SendInterval": 1000
  },
  "Forward": {
    "Enabled": true,
    "Rules": [
      {
        "Name": "注释消息转发",
        "Enabled": true,
        "SourceGroups": [123456789],  // 监听的源群号
        "TargetGroups": [987654321],  // 转发的目标群号
        "MessagePrefixes": ["注："],   // 以此开头的消息会被转发
        "Keywords": [],
        "ForwardFullMessage": true,
        "PreserveFormat": true,
        "ForwardPrefix": "[转发来自群 {sourceGroup}] "
      },
      {
        "Name": "关键词消息转发",
        "Enabled": true,
        "SourceGroups": [123456789],
        "TargetGroups": [987654321],
        "MessagePrefixes": [],
        "Keywords": ["重要", "紧急", "通知"],  // 包含这些关键词的消息会被转发
        "ForwardFullMessage": true,
        "PreserveFormat": true,
        "ForwardPrefix": "[关键词转发来自群 {sourceGroup}] "
      }
    ]
  }
}
```

### 3. 启动Bot

```bash
dotnet run
```

首次运行需要扫码登录。

## 📝 配置说明

### Bot账号配置

- `Uin`: Bot的QQ号
- `Password`: 密码（留空使用扫码登录，推荐）
- `Protocol`: 登录协议（Linux/Windows/macOS）
- `AutoReconnect`: 是否自动重连

### 消息配置

- `Enabled`: 是否启用消息发送
- `SendInterval`: 消息发送间隔（毫秒）

### 转发配置

每个转发规则包含：

- `Name`: 规则名称
- `Enabled`: 是否启用此规则
- `SourceGroups`: 源群列表（监听这些群的消息）
- `TargetGroups`: 目标群列表（转发到这些群）
- `MessagePrefixes`: 消息前缀过滤（以此开头的消息会被转发）
- `Keywords`: 关键词过滤（包含这些关键词的消息会被转发）
- `ForwardFullMessage`: 是否转发完整消息
- `PreserveFormat`: 是否保留原始格式
- `ForwardPrefix`: 转发消息的前缀模板

## 🎮 Bot命令

在群聊中发送以下命令：

- `/forward status` - 查看转发状态统计
- `/forward help` - 显示帮助信息

## 🔧 高级功能

### 转发前缀变量

在 `ForwardPrefix` 中可以使用以下变量：

- `{sourceGroup}` - 源群号
- `{senderUin}` - 发送者QQ号
- `{ruleName}` - 规则名称
- `{reason}` - 转发原因

### 消息过滤

支持两种过滤方式：

1. **前缀过滤**: 消息必须以指定前缀开头
2. **关键词过滤**: 消息包含指定关键词

可以同时配置多个前缀和关键词，满足任一条件即会转发。

## ⚠️ 注意事项

1. **避免循环转发**: 
   - 不要将目标群设置为源群
   - 注意多个规则间的交叉转发

2. **发送频率限制**:
   - 默认消息间隔为1000毫秒
   - 避免被腾讯风控

3. **权限要求**:
   - Bot需要在源群和目标群中
   - 需要相应的发言权限

## 🛠️ 开发

### 项目结构

```
ForwardBot-LagrangeV1/
├── Program.cs              # 主程序入口
├── Config/
│   └── BotConfiguration.cs # 配置管理
└── Services/
    └── MessageForwardService.cs # 转发服务
```

### 构建项目

```bash
# 克隆项目
git clone <repository-url>
cd ForwardBot-LagrangeV1

# 构建
dotnet build

# 运行
dotnet run
```

## 📄 许可证

本项目基于 MIT 许可证开源。

## 🤝 贡献

欢迎提交 Issue 和 Pull Request！

## 📞 支持

如有问题，请通过 GitHub Issues 联系我们。