using System;
using System.IO;
using System.Text.Json;
using ScumFreeBot.Models;

namespace ScumFreeBot.Services;

public sealed class PlayerStateStore
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public PlayerStateDb Load()
    {
        if (!File.Exists(AppPaths.PlayerStateFile))
            return new PlayerStateDb();

        try
        {
            var json = File.ReadAllText(AppPaths.PlayerStateFile);
            return JsonSerializer.Deserialize<PlayerStateDb>(json, _jsonOptions) ?? new PlayerStateDb();
        }
        catch
        {
            return new PlayerStateDb();
        }
    }

    public void Save(PlayerStateDb db)
    {
        var json = JsonSerializer.Serialize(db, _jsonOptions);
        File.WriteAllText(AppPaths.PlayerStateFile, json);
    }

    public bool HasReceivedWelcomePack(string steamId)
    {
        if (string.IsNullOrWhiteSpace(steamId))
            return false;

        var db = Load();
        return db.Players.TryGetValue(steamId, out var state) && state.WelcomePackReceived;
    }

    public void MarkWelcomePackReceived(string steamId, string playerName)
    {
        if (string.IsNullOrWhiteSpace(steamId))
            return;

        var db = Load();

        if (!db.Players.TryGetValue(steamId, out var state))
        {
            state = new PlayerState();
            db.Players[steamId] = state;
        }

        state.PlayerName = playerName;
        state.WelcomePackReceived = true;
        state.WelcomePackReceivedAtUtc = DateTime.UtcNow;

        Save(db);
    }

    public void ResetWelcomePack(string steamId)
    {
        if (string.IsNullOrWhiteSpace(steamId))
            return;

        var db = Load();

        if (db.Players.TryGetValue(steamId, out var state))
        {
            state.WelcomePackReceived = false;
            state.WelcomePackReceivedAtUtc = null;
            Save(db);
        }
    }

    public void ResetAllWelcomePacks()
    {
        var db = Load();

        foreach (var entry in db.Players.Values)
        {
            entry.WelcomePackReceived = false;
            entry.WelcomePackReceivedAtUtc = null;
        }

        Save(db);
    }
    public PlayerCommandState? GetCommandState(string steamId, string trigger)
    {
        if (string.IsNullOrWhiteSpace(steamId) || string.IsNullOrWhiteSpace(trigger))
            return null;

        var playerKey = steamId.Trim();
        var triggerKey = trigger.Trim().ToLowerInvariant();

        var db = Load();

        if (!db.Players.TryGetValue(playerKey, out var state))
            return null;

        state.CommandStates ??= new Dictionary<string, PlayerCommandState>(StringComparer.OrdinalIgnoreCase);

        return state.CommandStates.TryGetValue(triggerKey, out var commandState)
            ? commandState
            : null;
    }

    public void MarkCommandUsed(string steamId, string playerName, string trigger)
    {
        if (string.IsNullOrWhiteSpace(steamId) || string.IsNullOrWhiteSpace(trigger))
            return;

        var playerKey = steamId.Trim();
        var triggerKey = trigger.Trim().ToLowerInvariant();

        var db = Load();

        if (!db.Players.TryGetValue(playerKey, out var state))
        {
            state = new PlayerState();
            db.Players[playerKey] = state;
        }

        state.PlayerName = playerName;
        state.CommandStates ??= new Dictionary<string, PlayerCommandState>(StringComparer.OrdinalIgnoreCase);

        if (!state.CommandStates.TryGetValue(triggerKey, out var commandState))
        {
            commandState = new PlayerCommandState();
            state.CommandStates[triggerKey] = commandState;
        }

        commandState.UseCount++;
        commandState.LastUsedAtUtc = DateTime.UtcNow;

        Save(db);
    }
}
