using System;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using ScumFreeBot.Models;

namespace ScumFreeBot.Services;

public sealed class UpdateService
{
    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(6)
    };

    public async Task<UpdateInfo?> GetLatestAsync(string latestUrl)
    {
        var json = await _httpClient.GetStringAsync(latestUrl);

        return JsonSerializer.Deserialize<UpdateInfo>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }

    public static Version GetCurrentVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var cleanVersion = informationalVersion.Split('+')[0].Trim();

            if (Version.TryParse(cleanVersion, out var version))
            {
                return version;
            }
        }

        return assembly.GetName().Version ?? new Version(0, 0, 0, 0);
    }

    public static string GetCurrentVersionText()
    {
        return GetCurrentVersion().ToString();
    }

    public static bool IsNewer(string? latestVersionText)
    {
        if (string.IsNullOrWhiteSpace(latestVersionText))
        {
            return false;
        }

        if (!Version.TryParse(latestVersionText.Trim(), out var latestVersion))
        {
            return false;
        }

        return latestVersion > GetCurrentVersion();
    }
}