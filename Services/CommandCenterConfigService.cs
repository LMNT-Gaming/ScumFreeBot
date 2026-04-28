using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ScumFreeBot.Models;

namespace ScumFreeBot.Services;

public sealed class CommandCenterConfigService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public CommandCenterConfig Load()
    {
        EnsureDefaultFiles();

        try
        {
            var json = File.ReadAllText(AppPaths.CommandCenterConfigFile);
            return JsonSerializer.Deserialize<CommandCenterConfig>(json, _jsonOptions) ?? new CommandCenterConfig();
        }
        catch
        {
            return new CommandCenterConfig();
        }
    }

    public void Save(CommandCenterConfig config)
    {
        EnsureDefaultFiles();
        var json = JsonSerializer.Serialize(config, _jsonOptions);
        File.WriteAllText(AppPaths.CommandCenterConfigFile, json);
    }

    private void EnsureDefaultFiles()
    {
        Directory.CreateDirectory(AppPaths.DataDirectory);

        if (!File.Exists(AppPaths.CommandCenterConfigFile))
        {
            var config = new CommandCenterConfig
            {
                Commands = new List<CommandRule>
                {
                    new()
                    {
                        Enabled = true,
                        Trigger = "!welcomepack",
                        ScriptFile = "welcomepack.sfb",
                        RunMode = "UniquePerPlayer",
                        CooldownHours = 0,
                        Zone = string.Empty,
                        DenyMessage = "{player} du hast diesen Befehl bereits benutzt."
                    }
                }
            };

            var json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(AppPaths.CommandCenterConfigFile, json);
        }

        var welcomePackScript = Path.Combine(AppPaths.DataDirectory, "welcomepack.sfb");
        if (!File.Exists(welcomePackScript))
        {
            File.WriteAllText(welcomePackScript, string.Join(Environment.NewLine, new[]
            {
                "# Ablaufscript fuer !welcomepack",
                "# Platzhalter: {player}, {steamId}, {arg1}, {arg2}, ...",
                "say {player} dein Welcomepack ist auf dem Weg, bitte bleib stehen!",
                "wait 1s",
                "#teleportto {player}",
                "wait 30s",
                "#spawnitem BP_Weapon_98k_Kar98 1 Location {player}",
                "wait 500ms",
                "#spawnitem BP_Weapon_Magazine_Kar98 2 Location {player}",
                "wait 500ms",
                "say {player} dein Welcomepack wurde zugestellt. Viel Erfolg!"
            }));
        }
    }
}
