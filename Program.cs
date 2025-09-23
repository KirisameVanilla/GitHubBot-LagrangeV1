using Lagrange.Core;
using Lagrange.Core.Common;
using Lagrange.Core.Common.Interface;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using GitHubBot_LagrangeV1.Config;
using GitHubBot_LagrangeV1.Services;
using System.Text.Json;
using Lagrange.Core.Common.Interface.Api;

namespace GitHubBot_LagrangeV1;

class Program
{
    private static BotContext? _bot;
    private static BotConfiguration? _config;
    private static GitHubService? _gitHubService;
    private static readonly Dictionary<string, DateTime> _lastCheckTimes = new();
    private static Timer? _monitorTimer;
    private static readonly Dictionary<string, HashSet<string>> _seenItems = new();

    static async Task Main(string[] args)
    {
        Console.WriteLine("🤖 GitHub监听QQ Bot - 基于Lagrange.Core");
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

            // 初始化GitHub服务
            var httpClient = new HttpClient();
            _gitHubService = new GitHubService(httpClient, _config.GitHub.Token);

            // 初始化Bot
            await InitializeBot();

            // 启动监听
            if (_config.Monitor.SendStartupMessage && _bot != null)
            {
                await SendStartupMessages();
            }

            StartGitHubMonitor();

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
            _monitorTimer?.Dispose();
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

        if (_bot == null)
        {
            _bot = BotFactory.Create(botConfig, deviceInfo, keystore);
        }

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

        Console.WriteLine($"[群消息] {groupUin}({memberUin}): {message}");

        // 处理简单命令
        if (message.StartsWith("/github"))
        {
            await HandleGitHubCommand(bot, groupUin, null, message);
        }
    }

    static async void OnFriendMessageReceived(BotContext bot, FriendMessageEvent friendMessageEvent)
    {
        var message = friendMessageEvent.Chain.ToPreviewText();
        var friendUin = friendMessageEvent.Chain.FriendUin;

        Console.WriteLine($"[好友消息] {friendUin}: {message}");

        // 处理简单命令
        if (message.StartsWith("/github"))
        {
            await HandleGitHubCommand(bot, null, friendUin, message);
        }
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

    static async Task HandleGitHubCommand(BotContext bot, uint? groupUin, uint? friendUin, string command)
    {
        try
        {
            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return;

            var subCommand = parts[1].ToLower();
            var response = subCommand switch
            {
                "status" => "GitHub Bot运行中",
                "repos" => GetMonitoredReposStatus(),
                _ => "❓ 未知命令。可用命令: /github status, /github repos"
            };

            // 发送回复
            var chain = MessageBuilder.Group(groupUin ?? 0).Text(response).Build();
            if (groupUin.HasValue)
            {
                await bot.SendMessage(MessageBuilder.Group(groupUin.Value).Text(response).Build());
            }
            else if (friendUin.HasValue)
            {
                await bot.SendMessage(MessageBuilder.Friend(friendUin.Value).Text(response).Build());
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理GitHub命令失败: {ex.Message}");
        }
    }

    static string GetMonitoredReposStatus()
    {
        if (_config?.GitHub.Repositories.Count == 0)
        {
            return "❌ 当前没有监听任何仓库";
        }

        var status = "📊 监听的仓库:\n";
        foreach (var repo in _config!.GitHub.Repositories)
        {
            var lastCheck = _lastCheckTimes.GetValueOrDefault($"{repo.Owner}/{repo.Name}", DateTime.MinValue);
            var lastCheckStr = lastCheck == DateTime.MinValue ? "从未检查" : lastCheck.ToString("MM-dd HH:mm");
            status += $"• {repo.DisplayName}({repo.Owner}/{repo.Name}) - 上次检查: {lastCheckStr}\n";
        }

        return status.TrimEnd('\n');
    }

    static void StartGitHubMonitor()
    {
        if (_config?.GitHub.Repositories.Count == 0)
        {
            Console.WriteLine("⚠️ 没有配置要监听的GitHub仓库");
            return;
        }

        var interval = TimeSpan.FromSeconds(_config.Monitor.Interval);
        _monitorTimer = new Timer(async _ => await CheckGitHubUpdates(), null, TimeSpan.Zero, interval);
        
        Console.WriteLine($"🔄 GitHub监听已启动，检查间隔: {_config.Monitor.Interval}秒");
    }

    static async Task CheckGitHubUpdates()
    {
        if (_gitHubService == null || _bot == null || _config == null) return;

        foreach (var repo in _config.GitHub.Repositories)
        {
            try
            {
                var repoKey = $"{repo.Owner}/{repo.Name}";
                var isFirstCheck = !_seenItems.ContainsKey(repoKey);
                
                if (isFirstCheck)
                {
                    _seenItems[repoKey] = new HashSet<string>();
                }

                if (_config.Monitor.VerboseLogging)
                {
                    Console.WriteLine($"🔍 检查仓库: {repoKey}");
                }

                // 检查commits
                if (repo.WatchEvents.Contains("commits"))
                {
                    await CheckNewCommits(repo, isFirstCheck);
                }

                // 检查issues
                if (repo.WatchEvents.Contains("issues"))
                {
                    await CheckNewIssues(repo, isFirstCheck);
                }

                // 检查releases
                if (repo.WatchEvents.Contains("releases"))
                {
                    await CheckNewReleases(repo, isFirstCheck);
                }

                _lastCheckTimes[repoKey] = DateTime.Now;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 检查仓库 {repo.Owner}/{repo.Name} 失败: {ex.Message}");
            }
        }
    }

    static async Task CheckNewCommits(Config.GitHubRepository repo, bool isFirstCheck)
    {
        var commits = await _gitHubService!.GetLatestCommitsAsync(repo.Owner, repo.Name, _config!.Monitor.MaxItemsPerCheck);
        var repoKey = $"{repo.Owner}/{repo.Name}";
        var seenCommits = _seenItems[repoKey];

        foreach (var commit in commits)
        {
            if (seenCommits.Contains(commit.Sha)) continue;

            seenCommits.Add(commit.Sha);
            
            if (isFirstCheck) continue; // 首次检查不发送消息

            var message = _config.Message.Templates.NewCommit
                .Replace("{repo}", repo.DisplayName)
                .Replace("{author}", commit.Author.Login)
                .Replace("{message}", commit.Commit.Message.Split('\n')[0]) // 只取第一行
                .Replace("{url}", commit.HtmlUrl);

            await SendNotification(repo, message);
        }
    }

    static async Task CheckNewIssues(Config.GitHubRepository repo, bool isFirstCheck)
    {
        var issues = await _gitHubService!.GetLatestIssuesAsync(repo.Owner, repo.Name, _config!.Monitor.MaxItemsPerCheck);
        var repoKey = $"{repo.Owner}/{repo.Name}";
        var seenIssues = _seenItems[repoKey];

        foreach (var issue in issues)
        {
            var issueId = $"issue-{issue.Number}";
            if (seenIssues.Contains(issueId)) continue;

            seenIssues.Add(issueId);
            
            if (isFirstCheck) continue; // 首次检查不发送消息

            var message = _config.Message.Templates.NewIssue
                .Replace("{repo}", repo.DisplayName)
                .Replace("{author}", issue.User.Login)
                .Replace("{title}", issue.Title)
                .Replace("{url}", issue.HtmlUrl);

            await SendNotification(repo, message);
        }
    }

    static async Task CheckNewReleases(Config.GitHubRepository repo, bool isFirstCheck)
    {
        var releases = await _gitHubService!.GetLatestReleasesAsync(repo.Owner, repo.Name, _config!.Monitor.MaxItemsPerCheck);
        var repoKey = $"{repo.Owner}/{repo.Name}";
        var seenReleases = _seenItems[repoKey];

        foreach (var release in releases)
        {
            var releaseId = $"release-{release.TagName}";
            if (seenReleases.Contains(releaseId)) continue;

            seenReleases.Add(releaseId);
            
            if (isFirstCheck) continue; // 首次检查不发送消息

            var message = _config.Message.Templates.NewRelease
                .Replace("{repo}", repo.DisplayName)
                .Replace("{version}", release.TagName)
                .Replace("{title}", release.Name)
                .Replace("{url}", release.HtmlUrl);

            await SendNotification(repo, message);
        }
    }

    static async Task SendNotification(Config.GitHubRepository repo, string message)
    {
        if (!_config!.Message.Enabled || _bot == null) return;

        try
        {
            // 发送到目标群
            foreach (var groupUin in repo.TargetGroups)
            {
                var chain = MessageBuilder.Group(groupUin).Text(message).Build();
                await _bot.SendMessage(chain);
                await Task.Delay(_config.Message.SendInterval);
            }

            // 发送到目标好友
            foreach (var friendUin in repo.TargetFriends)
            {
                var chain = MessageBuilder.Friend(friendUin).Text(message).Build();
                await _bot.SendMessage(chain);
                await Task.Delay(_config.Message.SendInterval);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 发送通知失败: {ex.Message}");
        }
    }

    static async Task SendStartupMessages()
    {
        if (_bot == null || _config == null) return;

        var message = _config.Monitor.StartupMessage;
        
        // 收集所有目标
        var allGroups = _config.GitHub.Repositories.SelectMany(r => r.TargetGroups).Distinct();
        var allFriends = _config.GitHub.Repositories.SelectMany(r => r.TargetFriends).Distinct();

        try
        {
            foreach (var groupUin in allGroups)
            {
                var chain = MessageBuilder.Group(groupUin).Text(message).Build();
                await _bot.SendMessage(chain);
                await Task.Delay(_config.Message.SendInterval);
            }

            foreach (var friendUin in allFriends)
            {
                var chain = MessageBuilder.Friend(friendUin).Text(message).Build();
                await _bot.SendMessage(chain);
                await Task.Delay(_config.Message.SendInterval);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 发送启动消息失败: {ex.Message}");
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
                Console.WriteLine($"监听仓库数: {_config?.GitHub.Repositories.Count ?? 0}");
                Console.WriteLine($"上次检查: {_lastCheckTimes.Values.DefaultIfEmpty(DateTime.MinValue).Max():yyyy-MM-dd HH:mm:ss}");
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
            GitHub = new GitHubConfig
            {
                Token = "", // 可选：GitHub Personal Access Token
                Repositories = new List<Config.GitHubRepository>
                {
                    new()
                    {
                        Owner = "LagrangeDev",
                        Name = "Lagrange.Core",
                        DisplayName = "Lagrange.Core",
                        WatchEvents = new List<string> { "commits", "issues", "releases" },
                        TargetGroups = new List<uint> { 123456789 }, // 替换为你的群号
                        TargetFriends = new List<uint>() // 可选：好友QQ号
                    }
                }
            }
        };

        await defaultConfig.SaveToFileAsync();
    }
}
