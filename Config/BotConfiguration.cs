using System.Text.Json;

namespace ForwardBot_LagrangeV1.Config;

public class BotConfiguration
{
    /// <summary>
    /// Bot账号配置
    /// </summary>
    public BotAccountConfig Account { get; set; } = new();

    /// <summary>
    /// 消息发送配置
    /// </summary>
    public MessageConfig Message { get; set; } = new();

    /// <summary>
    /// 消息转发配置
    /// </summary>
    public ForwardConfig Forward { get; set; } = new();

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
/// 消息配置
/// </summary>
public class MessageConfig
{
    /// <summary>
    /// 是否启用消息发送
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 消息发送间隔（毫秒）
    /// </summary>
    public int SendInterval { get; set; } = 1000;
}

/// <summary>
/// 消息转发配置
/// </summary>
public class ForwardConfig
{
    /// <summary>
    /// 是否启用消息转发功能
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 转发规则列表
    /// </summary>
    public List<ForwardRule> Rules { get; set; } = new();
}

/// <summary>
/// 转发规则
/// </summary>
public class ForwardRule
{
    /// <summary>
    /// 规则名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 是否启用此规则
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 源群号列表（监听这些群的消息）
    /// </summary>
    public List<uint> SourceGroups { get; set; } = new();

    /// <summary>
    /// 目标群号列表（转发到这些群）
    /// </summary>
    public List<uint> TargetGroups { get; set; } = new();

    /// <summary>
    /// 消息前缀过滤（以此开头的消息会被转发）
    /// </summary>
    public List<string> MessagePrefixes { get; set; } = new();

    /// <summary>
    /// 关键词过滤（包含这些关键词的消息会被转发）
    /// </summary>
    public List<string> Keywords { get; set; } = new();

    /// <summary>
    /// 是否转发整条消息（包含关键词时）
    /// </summary>
    public bool ForwardFullMessage { get; set; } = true;

    /// <summary>
    /// 转发消息时是否保留原格式
    /// </summary>
    public bool PreserveFormat { get; set; } = true;

    /// <summary>
    /// 转发消息前缀模板
    /// </summary>
    public string ForwardPrefix { get; set; } = "[转发来自群 {sourceGroup}] ";
}