using System.Threading.Tasks;

namespace ScumFreeBot.Services;

public sealed class WelcomePackService
{
    private readonly CommandSenderService _commandSenderService;
    private readonly PlayerStateStore _playerStateStore;
    private readonly WelcomePackConfigService _configService;

    public WelcomePackService(
        CommandSenderService commandSenderService,
        PlayerStateStore playerStateStore,
        WelcomePackConfigService configService)
    {
        _commandSenderService = commandSenderService;
        _playerStateStore = playerStateStore;
        _configService = configService;
    }

    public async Task HandleAsync(string autoHotkeyPath, string scriptPath, string steamId, string playerName)
    {
        if (_playerStateStore.HasReceivedWelcomePack(steamId))
        {
            await _commandSenderService.SendCommandAsync(
                autoHotkeyPath,
                scriptPath,
                $"{playerName} du hast dein Welcomepack bereits erhalten.");
            return;
        }

        await _commandSenderService.SendCommandAsync(
            autoHotkeyPath,
            scriptPath,
            $"{playerName} dein Welcomepack ist auf dem Weg, bitte bleib 30 - Sekunden stehen!");

        await Task.Delay(200);

        await _commandSenderService.SendCommandAsync(
            autoHotkeyPath,
            scriptPath,
            $"#teleportto {playerName}");

        await Task.Delay(30000);

        var config = _configService.Load();

        foreach (var itemCommand in config.Items)
        {
            await _commandSenderService.SendCommandAsync(
                autoHotkeyPath,
                scriptPath,
                itemCommand);

            await Task.Delay(150);
        }

        _playerStateStore.MarkWelcomePackReceived(steamId, playerName);

        await _commandSenderService.SendCommandAsync(
            autoHotkeyPath,
            scriptPath,
            $"{playerName} dein Welcomepack wurde zugestellt. Viel Erfolg!");
    }
}