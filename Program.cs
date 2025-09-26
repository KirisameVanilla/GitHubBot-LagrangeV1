using Lagrange.Core;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using System.Text.Json;
using Lagrange.Core.Common.Interface.Api;
using ForwardBot_LagrangeV1.Services;
using ForwardBot_LagrangeV1.Config;

namespace ForwardBot_LagrangeV1;

class Program
{
    private static BotContext? _bot;
    private static BotConfiguration? _config;
    private static MessageForwardService? _forwardService;

    static async Task Main(string[] args)
    {
        Console.WriteLine("正在初始化...\n");

        try
        {
            // 加载配置
            _config = await BotConfiguration.LoadFromFileAsync();
            if (_config.Account.Uin == 0)
            {
                Console.WriteLine("请在config.json中配置Bot账号信息！");
                await CreateDefaultConfig();
                Console.WriteLine("已创建默认配置文件，请编辑后重新启动。");
                return;
            }


            // 初始化Bot
            await InitializeBot();

            // 初始化转发服务
            if (_bot != null)
            {
                _forwardService = new MessageForwardService(_bot, _config.Forward);
            }

            Console.WriteLine("✅ Bot初始化完成！");
            Console.WriteLine("按 'q' 退出，按任意其他键查看状态\n");

            // 保持程序运行
            await KeepAlive();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 程序启动失败: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
        finally
        {
            _bot?.Dispose();
        }
    }

    static async Task InitializeBot()
    {
        var botConfig = new BotConfig
        {
            Protocol = _config!.Account.Protocol switch
            {
                "MacOs" => Protocols.MacOs,
                "Windows" => Protocols.Windows,
                "Linux" => Protocols.Linux,
                _ => Protocols.Linux
            },
            AutoReconnect = _config.Account.AutoReconnect,
            UseIPv6Network = false,
            GetOptimumServer = true
        };

        // 创建或加载设备信息和密钥库
        BotDeviceInfo deviceInfo;
        BotKeystore keystore;

        if (File.Exists("device.json") && File.Exists("keystore.json"))
        {
            // 从文件加载
            var deviceJson = await File.ReadAllTextAsync("device.json");
            var keystoreJson = await File.ReadAllTextAsync("keystore.json");
            
            deviceInfo = JsonSerializer.Deserialize<BotDeviceInfo>(deviceJson) ?? BotDeviceInfo.GenerateInfo();
            keystore = JsonSerializer.Deserialize<BotKeystore>(keystoreJson) ?? new BotKeystore();
        }
        else
        {
            // 创建新的设备信息
            _bot = BotFactory.Create(botConfig, _config.Account.Uin, _config.Account.Password, out deviceInfo);
            keystore = _bot.UpdateKeystore();
            
            // 保存设备信息和密钥库
            await SaveDeviceInfo(deviceInfo);
            await SaveKeystore(keystore);
        }

        _bot ??= BotFactory.Create(botConfig, deviceInfo, keystore);

        // 设置事件处理器
        _bot.Invoker.OnBotLogEvent += OnBotLogEvent;
        _bot.Invoker.OnBotOnlineEvent += OnBotOnlineEvent;
        _bot.Invoker.OnBotOfflineEvent += OnBotOfflineEvent;
        _bot.Invoker.OnGroupMessageReceived += OnGroupMessageReceived;
        _bot.Invoker.OnFriendMessageReceived += OnFriendMessageReceived;
        _bot.Invoker.OnBotCaptchaEvent += OnBotCaptchaEvent;

        // 登录
        if (string.IsNullOrEmpty(_config.Account.Password))
        {
            Console.WriteLine("正在生成二维码登录...");
            var qrCode = await _bot.FetchQrCode();
            if (qrCode != null)
            {
                await File.WriteAllBytesAsync("qrcode.png", qrCode.Value.QrCode);
                Console.WriteLine("二维码已保存为 qrcode.png，请扫码登录");
                
                await _bot.LoginByQrCode();
            }
        }
        else
        {
            Console.WriteLine("正在使用密码登录...");
            await _bot.LoginByPassword();
        }
    }

    static void OnBotLogEvent(BotContext bot, BotLogEvent logEvent)
    {
        var color = logEvent.Level switch
        {
            LogLevel.Verbose => ConsoleColor.Gray,
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Information => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Exception => ConsoleColor.Red,
            LogLevel.Fatal => ConsoleColor.DarkRed,
            _ => ConsoleColor.White
        };

        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{logEvent.Level}] {logEvent.EventMessage}");
        Console.ForegroundColor = originalColor;
    }

    static async void OnBotOnlineEvent(BotContext bot, BotOnlineEvent onlineEvent)
    {
        Console.WriteLine($"✅ Bot上线: {bot.BotName}({bot.BotUin})");
        
        // 保存密钥库
        await SaveKeystore(bot.UpdateKeystore());
    }

    static void OnBotOfflineEvent(BotContext bot, BotOfflineEvent offlineEvent)
    {
        Console.WriteLine($"❌ Bot离线: {offlineEvent.EventMessage}");
    }

    static async void OnGroupMessageReceived(BotContext bot, GroupMessageEvent groupMessageEvent)
    {
        var message = groupMessageEvent.Chain.ToPreviewText();
        var groupUin = groupMessageEvent.Chain.GroupUin!.Value;
        var memberUin = groupMessageEvent.Chain.FriendUin;

        // 防止自循环递归
        if (memberUin == _bot?.BotUin)
        {
            Console.WriteLine("发出了一条信息");
            return;
        }

        Console.WriteLine($"[群消息] {groupUin}({memberUin}): {message}");

        // 处理消息转发
        if (_forwardService != null)
        {
            await _forwardService.HandleGroupMessage(groupMessageEvent);
        }

        // 处理简单命令
        if (message.StartsWith("/forward"))
        {
            await HandleForwardCommand(bot, groupUin, message);
        }
    }

    static void OnFriendMessageReceived(BotContext bot, FriendMessageEvent friendMessageEvent)
    {
        var message = friendMessageEvent.Chain.ToPreviewText();
        var friendUin = friendMessageEvent.Chain.FriendUin;

        Console.WriteLine($"[好友消息] {friendUin}: {message}");

        // 处理简单命令
    }

    static void OnBotCaptchaEvent(BotContext bot, BotCaptchaEvent captchaEvent)
    {
        Console.WriteLine($"需要验证码: {captchaEvent.ToString()}");
        Console.Write("请输入验证码: ");
        var captcha = Console.ReadLine();
        Console.Write("请输入随机字符串: ");
        var randStr = Console.ReadLine();

        if (!string.IsNullOrEmpty(captcha) && !string.IsNullOrEmpty(randStr))
        {
            bot.SubmitCaptcha(captcha, randStr);
        }
    }

    static async Task KeepAlive()
    {
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.KeyChar == 'q' || key.KeyChar == 'Q')
            {
                Console.WriteLine("\n正在退出...");
                break;
            }
            else
            {
                Console.WriteLine($"\n📊 状态信息 - {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine($"Bot状态: {(_bot != null ? "在线" : "离线")}");
                Console.WriteLine("按 'q' 退出，按任意键查看状态\n");
            }

            await Task.Delay(100);
        }
    }

    static async Task SaveDeviceInfo(BotDeviceInfo deviceInfo)
    {
        var json = JsonSerializer.Serialize(deviceInfo, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync("device.json", json);
    }

    static async Task SaveKeystore(BotKeystore keystore)
    {
        var json = JsonSerializer.Serialize(keystore, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync("keystore.json", json);
    }

    static async Task HandleForwardCommand(BotContext bot, uint groupUin, string command)
    {
        try
        {
            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return;

            var subCommand = parts[1].ToLower();
            string response = subCommand switch
            {
                "status" => _forwardService?.GetForwardStatistics() ?? "转发服务未初始化",
                "rules" => _forwardService?.GetRuleDetails() ?? "转发服务未初始化",
                "help" => "📋 转发Bot命令帮助:\n" +
                         "/forward status - 查看转发状态统计\n" +
                         "/forward rules - 查看转发规则详情\n" +
                         "/forward help - 显示帮助信息",
                _ => "❓ 未知命令。使用 /forward help 查看可用命令"
            };

            // 发送回复
            await bot.SendMessage(MessageBuilder.Group(groupUin).Text(response).Build());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理转发命令失败: {ex.Message}");
        }
    }

    static async Task CreateDefaultConfig()
    {
        var defaultConfig = new BotConfiguration
        {
            Account = new BotAccountConfig
            {
                Uin = 0, // 请填入你的Bot QQ号
                Password = "", // 密码留空使用二维码登录
                Protocol = "Linux",
                AutoReconnect = true
            },
            Forward = new ForwardConfig
            {
                Enabled = true,
                Rules = new List<ForwardRule>
                {
                    new()
                    {
                        Name = "注释消息转发",
                        Enabled = true,
                        SourceGroups = new List<uint> { 123456789 }, // 替换为要监听的群号
                        TargetGroups = new List<uint> { 987654321 }, // 替换为转发目标群号
                        MessagePrefixes = new List<string> { "注：" },
                        Keywords = new List<string>(),
                        ForwardFullMessage = true,
                        PreserveFormat = true,
                        ForwardPrefix = "[转发来自群 {sourceGroup}] "
                    },
                    new()
                    {
                        Name = "关键词消息转发",
                        Enabled = true,
                        SourceGroups = new List<uint> { 123456789 }, // 替换为要监听的群号
                        TargetGroups = new List<uint> { 987654321 }, // 替换为转发目标群号
                        MessagePrefixes = new List<string>(),
                        Keywords = new List<string> { "重要", "紧急", "通知" },
                        ForwardFullMessage = true,
                        PreserveFormat = true,
                        ForwardPrefix = "[关键词转发来自群 {sourceGroup}] "
                    }
                }
            }
        };

        await defaultConfig.SaveToFileAsync();
    }
}
