using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Elementalist.Infrastructure.Config;
using Microsoft.Extensions.Options;

namespace Elementalist;

public class BotStartupService : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly IOptions<BotTokenSettings> _options;
    private readonly InteractionService _interactionService;
    private readonly Serilog.ILogger _logger;

    // Unfortunately Discord.Net needs the IServiceProvider to make interaction service calls, instead using DI properly
    private readonly IServiceProvider _serviceProvider;

    public BotStartupService(DiscordSocketClient client, IOptions<BotTokenSettings> options, InteractionService interactionService, Serilog.ILogger logger, IServiceProvider serviceProvider)
    {
        _client = client;
        _options = options;
        _interactionService = interactionService;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (string.IsNullOrEmpty(_options.Value.Token))
            {
                throw new ApplicationException("The BOT_TOKEN environment variable must be initialized.");
            }

            _client.Log += LogAsync;
            _interactionService.Log += LogAsync;
            _client.Ready += clientReady;
            _client.InteractionCreated += _client_InteractionCreated;

            await _client.LoginAsync(_options.Value.TokenType, _options.Value.Token);

            await _client.StartAsync();
            await _client.SetCustomStatusAsync("Looking for a one foot ruler...");
            _logger.Information("Discord client started");

            //Block the thread so that the client stays logged in, at least until the user requests the service to restart/stop
            await Task.Delay(-1, stoppingToken);
        }
        catch (Exception ex) when (ex is not TaskCanceledException)
        {
            _logger.Fatal(ex, "A fatal exception occurred.");
        }
        finally
        {
            await _client.StopAsync();
        }
    }

    private async Task LogAsync(LogMessage message)
    {
        switch (message.Severity)
        {
            case LogSeverity.Critical:
                _logger.Fatal(message.Exception, message.ToString());
                break;

            case LogSeverity.Error:
                _logger.Error(message.Exception, message.ToString());
                break;

            case LogSeverity.Warning:
                _logger.Warning(message.ToString());
                break;

            case LogSeverity.Info:
                _logger.Information(message.ToString());
                break;

            case LogSeverity.Verbose:
                _logger.Debug(message.ToString());
                break;

            case LogSeverity.Debug:
                _logger.Debug(message.ToString());
                break;

            default:
                break;
        }
    }

    private async Task _client_InteractionCreated(SocketInteraction arg)
    {
        var ctx = new SocketInteractionContext(_client, arg);
        var component = arg as SocketMessageComponent;

        _logger.Information("{user} is executing discord {type} id {Id}. Interaction {customId}", arg.User.Username, arg.Type, arg.Id, component?.Data.CustomId);

        try
        {
            await _interactionService.ExecuteCommandAsync(ctx, _serviceProvider);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Discord {Type} interaction {Id} failed.", arg.Type, arg.Id);

            if (!ctx.Interaction.HasResponded)
            {
                await ctx.Interaction.RespondAsync($"An error occoured: {ex.Message}", ephemeral: true);
            }
        }
    }

    private async Task clientReady()
    {
        try
        {
            await _interactionService.RegisterCommandsGloballyAsync();

            _client.Ready -= clientReady;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "An error occourred while registering commands");
        }
    }
}
