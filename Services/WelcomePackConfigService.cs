using System;
using System.IO;
using System.Text;
using System.Text.Json;
using ScumFreeBot.Models;

namespace ScumFreeBot.Services;

public sealed class WelcomePackConfigService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public WelcomePackConfigService()
    {
        EnsureDefaultFileExists();
    }

    public WelcomePackConfig Load()
    {
        EnsureDefaultFileExists();

        var path = AppPaths.WelcomePackItemsFile;

        try
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            var config = JsonSerializer.Deserialize<WelcomePackConfig>(json);
            return config ?? new WelcomePackConfig();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WP LOAD ERROR: {ex}");
            return new WelcomePackConfig();
        }
    }

    private void EnsureDefaultFileExists()
    {
        if (File.Exists(AppPaths.WelcomePackItemsFile))
            return;

        var defaultConfig = new WelcomePackConfig
        {
            Items = new()
            {
                "#spawnitem BP_MRE",
                "#spawnitem BP_WaterBottle",
                "#spawnitem BP_Bandage"
            }
        };

        var json = JsonSerializer.Serialize(defaultConfig, _jsonOptions);
        File.WriteAllText(AppPaths.WelcomePackItemsFile, json, Encoding.UTF8);
    }
}