using System;
using System.Threading;
using System.Threading.Tasks;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.DTOs;
using System.Linq;

namespace PNGTuber_GPTv2.Consumers.Command
{
    public class CommandStep : IPipelineStep
    {
        private readonly ICacheService _cache;
        private readonly INicknameRepository _nicknames;
        private readonly ILogger _logger;

        public CommandStep(ICacheService cache, INicknameRepository nicknames, ILogger logger)
        {
            _cache = cache;
            _nicknames = nicknames;
            _logger = logger;
        }

        public async Task ExecuteAsync(string contextId, CancellationToken ct)
        {
            var key = $"req_{contextId}";
            var context = _cache.Get<RequestContext>(key);
            if (context == null) return;

            if (context.EventType == "Command" && !string.IsNullOrEmpty(context.CommandId))
            {
                await HandleNativeCommand(context, ct);
            }
            else if (context.EventType == "Chat" && !string.IsNullOrEmpty(context.CleanedMessage))
            {
                if (context.CleanedMessage.StartsWith("!")) await HandleTextCommand(context, ct);
            }

            _cache.Set(key, context, TimeSpan.FromMinutes(10));
        }

        private async Task HandleNativeCommand(RequestContext ctx, CancellationToken ct)
        {

        }

        private async Task HandleTextCommand(RequestContext context, CancellationToken ct)
        {
            var parts = context.CleanedMessage.Split(' ');
            var command = parts[0].ToLowerInvariant();
            var args = string.Join(" ", parts.Skip(1)).Trim();

            if (command == "!setnick") await HandleSetNick(context, args, ct);
            else if (command == "!removenick") await HandleRemoveNick(context, ct);
        }

        private async Task HandleSetNick(RequestContext ctx, string newNick, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(newNick)) return;
            if (newNick.Length > 30) return;

            await _nicknames.SetNicknameAsync(ctx.User.Id, newNick, ct);
            ctx.GeneratedResponse = $"Nickname set to {newNick}!";
            _logger.Info($"User {ctx.User.DisplayName} set nickname to {newNick}");
        }

        private async Task HandleRemoveNick(RequestContext ctx, CancellationToken ct)
        {
            await _nicknames.SetNicknameAsync(ctx.User.Id, null, ct);
            ctx.GeneratedResponse = "Nickname removed.";
            _logger.Info($"User {ctx.User.DisplayName} removed nickname.");
        }
    }
}
