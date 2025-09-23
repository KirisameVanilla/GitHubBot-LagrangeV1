using System.Text.Json;

namespace GitHubBot_LagrangeV1.Config;

public class BotConfiguration
{
    /// <summary>
    /// Bot账号配置
    /// </summary>
    public BotAccountConfig Account { get; set; } = new();

    /// <summary>
    /// GitHub相关配置
    /// </summary>
    public GitHubConfig GitHub { get; set; } = new();

    /// <summary>
    /// 消息发送配置
    /// </summary>
    public MessageConfig Message { get; set; } = new();

    /// <summary>
    /// 监听配置
    /// </summary>
    public MonitorConfig Monitor { get; set; } = new();

    /// <summary>
    /// 从JSON文件加载配置
    /// </summary>
    public static async Task<BotConfiguration> LoadFromFileAsync(string filePath = "config.json")
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"配置文件 {filePath} 不存在，使用默认配置");
                return new BotConfiguration();
            }

            var json = await File.ReadAllTextAsync(filePath);
            var config = JsonSerializer.Deserialize<BotConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });

            return config ?? new BotConfiguration();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载配置文件失败: {ex.Message}，使用默认配置");
            return new BotConfiguration();
        }
    }

    /// <summary>
    /// 保存配置到JSON文件
    /// </summary>
    public async Task SaveToFileAsync(string filePath = "config.json")
    {
        try
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
            await File.WriteAllTextAsync(filePath, json);
            Console.WriteLine($"配置已保存到 {filePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"保存配置文件失败: {ex.Message}");
        }
    }
}

/// <summary>
/// Bot账号配置
/// </summary>
public class BotAccountConfig
{
    /// <summary>
    /// Bot QQ号
    /// </summary>
    public uint Uin { get; set; } = 0;

    /// <summary>
    /// Bot密码（可选，建议使用扫码登录）
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 协议类型
    /// </summary>
    public string Protocol { get; set; } = "Linux";

    /// <summary>
    /// 是否自动重连
    /// </summary>
    public bool AutoReconnect { get; set; } = true;
}

/// <summary>
/// GitHub配置
/// </summary>
public class GitHubConfig
{
    /// <summary>
    /// GitHub API Token（可选，用于提高API调用限制）
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// 要监听的仓库列表
    /// </summary>
    public List<GitHubRepository> Repositories { get; set; } = new();
}

/// <summary>
/// GitHub仓库配置
/// </summary>
public class GitHubRepository
{
    /// <summary>
    /// 仓库所有者
    /// </summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>
    /// 仓库名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称（用于消息中显示）
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 监听的事件类型
    /// </summary>
    public List<string> WatchEvents { get; set; } = new() { "commits", "issues", "releases" };

    /// <summary>
    /// 要发送消息的群号列表
    /// </summary>
    public List<uint> TargetGroups { get; set; } = new();

    /// <summary>
    /// 要发送消息的好友QQ号列表
    /// </summary>
    public List<uint> TargetFriends { get; set; } = new();
}

/// <summary>
/// 消息配置
/// </summary>
public class MessageConfig
{
    /// <summary>
    /// 是否启用消息发送
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 消息模板配置
    /// </summary>
    public MessageTemplates Templates { get; set; } = new();

    /// <summary>
    /// 消息发送间隔（毫秒）
    /// </summary>
    public int SendInterval { get; set; } = 1000;
}

/// <summary>
/// 消息模板
/// </summary>
public class MessageTemplates
{
    /// <summary>
    /// 新提交消息模板
    /// </summary>
    public string NewCommit { get; set; } = "🚀 [{repo}] 新提交\n👤 作者: {author}\n📝 消息: {message}\n🔗 链接: {url}";

    /// <summary>
    /// 新Issue消息模板
    /// </summary>
    public string NewIssue { get; set; } = "🐛 [{repo}] 新Issue\n👤 创建者: {author}\n📋 标题: {title}\n🔗 链接: {url}";

    /// <summary>
    /// 新Release消息模板
    /// </summary>
    public string NewRelease { get; set; } = "🎉 [{repo}] 新版本发布\n🏷️ 版本: {version}\n📋 标题: {title}\n🔗 链接: {url}";

    /// <summary>
    /// Issue状态更新消息模板
    /// </summary>
    public string IssueUpdate { get; set; } = "📝 [{repo}] Issue更新\n👤 更新者: {author}\n📋 标题: {title}\n🔄 状态: {state}\n🔗 链接: {url}";
}

/// <summary>
/// 监听配置
/// </summary>
public class MonitorConfig
{
    /// <summary>
    /// 监听间隔（秒）
    /// </summary>
    public int Interval { get; set; } = 60; // 1分钟

    /// <summary>
    /// 是否在启动时发送测试消息
    /// </summary>
    public bool SendStartupMessage { get; set; } = true;

    /// <summary>
    /// 启动消息内容
    /// </summary>
    public string StartupMessage { get; set; } = "GitHub监听Bot已启动！";

    /// <summary>
    /// 每次检查获取的最大条目数
    /// </summary>
    public int MaxItemsPerCheck { get; set; } = 5;

    /// <summary>
    /// 是否启用详细日志
    /// </summary>
    public bool VerboseLogging { get; set; } = false;
}