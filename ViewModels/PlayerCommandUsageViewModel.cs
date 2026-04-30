using System;
using System.Globalization;

namespace ScumFreeBot.ViewModels;

public sealed class PlayerCommandUsageViewModel : ObservableObject
{
    private bool _isSelected;

    public string PlayerKey { get; init; } = string.Empty;
    public string PlayerName { get; init; } = string.Empty;
    public string Trigger { get; init; } = string.Empty;
    public int UseCount { get; init; }
    public DateTime? LastUsedAtUtc { get; init; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public string DisplayPlayerName =>
        string.IsNullOrWhiteSpace(PlayerName)
            ? PlayerKey
            : PlayerName;

    public string LastUsedText =>
        LastUsedAtUtc is null
            ? "-"
            : DateTime.SpecifyKind(LastUsedAtUtc.Value, DateTimeKind.Utc)
                .ToLocalTime()
                .ToString("dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture);
}