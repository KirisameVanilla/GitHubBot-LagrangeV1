using Lagrange.Core;
using Lagrange.Core.Common.Interface.Api;
using Lagrange.Core.Event.EventArg;
using Lagrange.Core.Message;
using ForwardBot_LagrangeV1.Config;

namespace ForwardBot_LagrangeV1.Services
{
    /// <summary>
    /// 消息转发服务
    /// </summary>
    public class MessageForwardService
    {
        private readonly BotContext _botContext;
        private readonly ForwardConfig _config;
        private readonly Dictionary<string, int> _forwardCounters = new();

        public MessageForwardService(BotContext botContext, ForwardConfig config)
        {
            _botContext = botContext;
            _config = config;
        }

        /// <summary>
        /// 处理群消息
        /// </summary>
        /// <param name="e">群消息事件参数</param>
        public async Task HandleGroupMessage(GroupMessageEvent e)
        {
            if (!_config.Enabled)
                return;

            var sourceGroupId = e.Chain.GroupUin ?? 0;
            var messageText = ExtractMessageText(e.Chain);
            var senderUin = e.Chain.FriendUin;

            if (string.IsNullOrWhiteSpace(messageText) || sourceGroupId == 0)
                return;

            Console.WriteLine($"[转发检测] 收到群 {sourceGroupId} 的消息: {messageText.Substring(0, Math.Min(50, messageText.Length))}...");

            // 遍历所有转发规则
            foreach (var rule in _config.Rules.Where(r => r.Enabled))
            {
                await ProcessForwardRule(e.Chain, rule, sourceGroupId, senderUin, messageText);
            }
        }

        /// <summary>
        /// 处理转发规则
        /// </summary>
        private async Task ProcessForwardRule(MessageChain originalChain, ForwardRule rule, uint sourceGroupId, uint senderUin, string messageText)
        {
            // 检查是否来自监听的源群
            if (!rule.SourceGroups.Contains(sourceGroupId))
                return;

            bool shouldForward = false;
            string forwardReason = "";

            // 检查消息前缀
            if (rule.MessagePrefixes.Any(prefix => messageText.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                shouldForward = true;
                forwardReason = $"匹配前缀规则";
                Console.WriteLine($"[转发] 消息匹配前缀规则: {rule.Name}");
            }

            // 检查关键词
            if (rule.Keywords.Any(keyword => messageText.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                shouldForward = true;
                forwardReason = $"匹配关键词规则";
                Console.WriteLine($"[转发] 消息匹配关键词规则: {rule.Name}");
            }

            // 如果符合转发条件，执行转发
            if (shouldForward)
            {
                await ForwardMessage(originalChain, rule, sourceGroupId, senderUin, forwardReason);
            }
        }

        /// <summary>
        /// 转发消息到目标群
        /// </summary>
        private async Task ForwardMessage(MessageChain originalChain, ForwardRule rule, uint sourceGroupId, uint senderUin, string reason)
        {
            var successCount = 0;
            var failCount = 0;

            try
            {
                foreach (var targetGroupId in rule.TargetGroups)
                {
                    // 避免转发到源群（死循环）
                    if (targetGroupId == sourceGroupId)
                    {
                        Console.WriteLine($"[转发跳过] 跳过转发到源群 {targetGroupId}");
                        continue;
                    }

                    var builder = MessageBuilder.Group(targetGroupId);

                    // 添加转发前缀
                    if (!string.IsNullOrEmpty(rule.ForwardPrefix))
                    {
                        var prefix = rule.ForwardPrefix
                            .Replace("{sourceGroup}", sourceGroupId.ToString())
                            .Replace("{senderUin}", senderUin.ToString());
                        builder.Text(prefix);
                    }

                    // 根据配置决定是否转发完整消息
                    if (rule.PreserveFormat && rule.ForwardFullMessage)
                    {
                        // 保留原始格式，复制所有消息元素
                        foreach (var element in originalChain)
                        {
                            builder.Add(element);
                        }
                    }
                    else
                    {
                        // 只转发文本内容
                        var messageText = ExtractMessageText(originalChain);
                        builder.Text(messageText);
                    }

                    // 构建并发送消息
                    var forwardChain = builder.Build();
                    var result = await _botContext.SendMessage(forwardChain);
                    
                    if (result.Result == 0)
                    {
                        successCount++;
                        Console.WriteLine($"[转发成功] 已将消息从群 {sourceGroupId} 转发到群 {targetGroupId} (规则: {rule.Name})");
                    }
                    else
                    {
                        failCount++;
                        Console.WriteLine($"[转发失败] 从群 {sourceGroupId} 到群 {targetGroupId} 失败，错误码: {result.Result}");
                    }

                    // 避免发送过快
                    await Task.Delay(500);
                }

                if (successCount > 0 || failCount > 0)
                {
                    Console.WriteLine($"[转发汇总] 规则 {rule.Name}: 成功 {successCount} 次, 失败 {failCount} 次");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[转发异常] 规则 {rule.Name} 发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 从消息链中提取文本内容
        /// </summary>
        private static string ExtractMessageText(MessageChain chain)
        {
            return string.Join("", chain.Select(e => e.ToPreviewText()));
        }

        /// <summary>
        /// 获取转发统计信息
        /// </summary>
        public string GetForwardStatistics()
        {
            var enabledRules = _config.Rules.Count(r => r.Enabled);
            var totalSourceGroups = _config.Rules.Where(r => r.Enabled).SelectMany(r => r.SourceGroups).Distinct().Count();
            var totalTargetGroups = _config.Rules.Where(r => r.Enabled).SelectMany(r => r.TargetGroups).Distinct().Count();

            return $"📊 转发服务状态: {(_config.Enabled ? "✅启用" : "❌禁用")}\n" +
                   $"📋 活跃规则数: {enabledRules}\n" +
                   $"👂 监听群数: {totalSourceGroups}\n" +
                   $"📤 目标群数: {totalTargetGroups}";
        }

        /// <summary>
        /// 获取规则详细信息
        /// </summary>
        public string GetRuleDetails()
        {
            if (!_config.Rules.Any())
                return "❌ 没有配置转发规则";

            var details = "📋 转发规则详情:\n\n";
            
            for (int i = 0; i < _config.Rules.Count; i++)
            {
                var rule = _config.Rules[i];
                var status = rule.Enabled ? "✅" : "❌";
                
                details += $"{i + 1}. {status} {rule.Name}\n";
                details += $"   监听群: {string.Join(", ", rule.SourceGroups)}\n";
                details += $"   目标群: {string.Join(", ", rule.TargetGroups)}\n";
                
                if (rule.MessagePrefixes.Any())
                    details += $"   前缀过滤: {string.Join(", ", rule.MessagePrefixes)}\n";
                
                if (rule.Keywords.Any())
                    details += $"   关键词过滤: {string.Join(", ", rule.Keywords)}\n";
                
                details += $"   转发完整消息: {(rule.ForwardFullMessage ? "是" : "否")}\n";
                details += $"   保持原格式: {(rule.PreserveFormat ? "是" : "否")}\n\n";
            }

            return details;
        }
    }
}