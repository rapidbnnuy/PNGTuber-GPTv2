using System;
using System.Threading;
using System.Threading.Tasks;
using PNGTuber_GPTv2.Core.Interfaces;
using PNGTuber_GPTv2.Domain.DTOs;
using PNGTuber_GPTv2.Core;
using System.Linq;

namespace PNGTuber_GPTv2.Consumers.Command
{
    public class CommandProcessingService : IConsumer
    {
        private readonly ICacheService _cache;
        private readonly INicknameRepository _nicks;
        private readonly IKnowledgeRepository _knowledge;
        private readonly ILogger _logger;
        private readonly IProcessingChannel<string> _inChannel;
        private readonly IProcessingChannel<string> _outChannel;

        public CommandProcessingService(
            ICacheService cache, 
            INicknameRepository nicks, 
            IKnowledgeRepository knowledge, 
            ILogger logger,
            IProcessingChannel<string> inChannel,
            IProcessingChannel<string> outChannel)
        {
            _cache = cache;
            _nicks = nicks;
            _knowledge = knowledge;
            _logger = logger;
            _inChannel = inChannel;
            _outChannel = outChannel;
        }

        public Task StartAsync(CancellationToken ct)
        {
            return Task.Run(async () => await RunLoopAsync(ct), ct);
        }

        private async Task RunLoopAsync(CancellationToken ct)
        {
            _logger.Info("[CommandService] Started.");
            try 
            {
                while (await _inChannel.WaitToReadAsync(ct))
                {
                    while (_inChannel.TryRead(out var contextId))
                    {
                        await ProcessAsync(contextId, ct);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { _logger.Error($"[CommandService] Crash: {ex.Message}"); }
        }

        private async Task ProcessAsync(string contextId, CancellationToken ct)
        {
            var key = $"req_{contextId}";
            var context = _cache.Get<RequestContext>(key);
            if (context == null) return;

            try
            {
                if (context.EventType == "Command" && !string.IsNullOrEmpty(context.CommandId))
                {
                    await HandleNativeCommand(context, ct);
                }
                else if (context.EventType == "Chat" && !string.IsNullOrWhiteSpace(context.CleanedMessage))
                {
                    if (context.CleanedMessage.StartsWith("!")) await HandleTextCommand(context, ct);
                }

                _cache.Set(key, context, TimeSpan.FromMinutes(10));
                _outChannel.TryWrite(contextId);
            }
            catch (Exception ex)
            {
                _logger.Error($"[CommandService] Failed: {ex.Message}");
            }
        }

        private async Task HandleNativeCommand(RequestContext ctx, CancellationToken ct)
        {
            var args = ExtractArgs(ctx.CleanedMessage);

            if (await HandleIdentityCommands(ctx.CommandId, ctx, args, ct)) return;
            if (await HandleKnowledgeCommands(ctx.CommandId, ctx, args, ct)) return;
            if (HandleSystemCommands(ctx.CommandId, ctx)) return;

            _logger.Warn($"[Command] Unhandled GUID: {ctx.CommandId}");
        }

        private async Task<bool> HandleIdentityCommands(string id, RequestContext ctx, string args, CancellationToken ct)
        {
            switch (id)
            {
                case CommandRegistry.SetNick:
                    await HandleSetNick(ctx, args, ct);
                    return true;
                case CommandRegistry.RemoveNick:
                    await HandleRemoveNick(ctx, ct);
                    return true;
                case CommandRegistry.CurrentNick:
                    var nick = ctx.User.Nickname ?? "None";
                    var pronouns = ctx.Pronouns.Display ?? "They/Them";
                    ctx.GeneratedResponse = $"Identity: {ctx.User.DisplayName} ({pronouns}) aka '{nick}'";
                    return true;
            }
            return false;
        }

        private async Task<bool> HandleKnowledgeCommands(string id, RequestContext ctx, string args, CancellationToken ct)
        {
            switch (id)
            {
                case CommandRegistry.RememberThis:
                    await HandleTeach(ctx, args, ct);
                    return true;
                case CommandRegistry.ForgetThis:
                    await HandleForget(ctx, args, ct);
                    return true;
            }
            return false;
        }

        private bool HandleSystemCommands(string id, RequestContext ctx)
        {
            switch (id)
            {
                case CommandRegistry.Help:
                    ctx.GeneratedResponse = "Commands: !setnick, !removenick, !teach <fact>, !forget <fact>, !help, !version, !setpronouns, !sayplay";
                    return true;
                case CommandRegistry.Version:
                    ctx.GeneratedResponse = "PNGTuber-GPTv2 v2.0.0";
                    return true;
                case CommandRegistry.SetPronouns:
                    ctx.GeneratedResponse = "To set your pronouns, go to https://pr.alejo.io/";
                    return true;
                case CommandRegistry.SayPlay:
                    ctx.GeneratedResponse = "!play";
                    return true;
            }
            return false;
        }

        private async Task HandleTextCommand(RequestContext context, CancellationToken ct)
        {
            var parts = context.CleanedMessage.Split(' ');
            var command = parts[0].ToLowerInvariant();
            var args = string.Join(" ", parts.Skip(1)).Trim();

            if (command == "!setnick") await HandleSetNick(context, args, ct);
            else if (command == "!removenick") await HandleRemoveNick(context, ct);
            else if (command == "!modteach" || command == "!rememberthis" || command == "!teach") await HandleTeach(context, args, ct);
            else if (command == "!modforget" || command == "!forgetthis" || command == "!forget") await HandleForget(context, args, ct);
            else if (command == "!help") HandleSystemCommands(CommandRegistry.Help, context);
            else if (command == "!version") HandleSystemCommands(CommandRegistry.Version, context);
            else if (command == "!setpronouns") HandleSystemCommands(CommandRegistry.SetPronouns, context);
            else if (command == "!sayplay") HandleSystemCommands(CommandRegistry.SayPlay, context);
        }

        private string ExtractArgs(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return "";
            var parts = message.Split(new[] { ' ' }, 2);
            return parts.Length > 1 ? parts[1].Trim() : "";
        }

        private async Task HandleSetNick(RequestContext ctx, string newNick, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(newNick)) return;
            if (newNick.Length > 30) return;

            await _nicks.SetNicknameAsync(ctx.User.Id, newNick, ct);
            ctx.GeneratedResponse = $"Nickname set to {newNick}!";
            _logger.Info($"User {ctx.User.DisplayName} set nickname to {newNick}");
        }

        private async Task HandleRemoveNick(RequestContext ctx, CancellationToken ct)
        {
            await _nicks.SetNicknameAsync(ctx.User.Id, null, ct);
            ctx.GeneratedResponse = "Nickname removed.";
            _logger.Info($"User {ctx.User.DisplayName} removed nickname.");
        }

        private async Task HandleTeach(RequestContext ctx, string args, CancellationToken ct)
        {
            var parts = args.Split(new[] { ' ' }, 2);
            if (parts.Length == 2)
            {
                var key = parts[0];
                var content = parts[1];
                await _knowledge.AddFactAsync(key, content, ctx.User.Id, ct);
                _logger.Info($"[Command] User {ctx.User.DisplayName} taught: {key}");
                ctx.GeneratedResponse = $"Memorized: {key}";
            }
        }

        private async Task HandleForget(RequestContext ctx, string args, CancellationToken ct)
        {
            if (!string.IsNullOrWhiteSpace(args))
            {
                await _knowledge.RemoveFactAsync(args, ct);
                _logger.Info($"[Command] User {ctx.User.DisplayName} forgot: {args}");
                ctx.GeneratedResponse = $"Forgot: {args}";
            }
        }
    }
}
