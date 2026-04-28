using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ScumFreeBot.Services;

public sealed class CommandSenderService
{
    public async Task<CommandSendResult> SendCommandAsync(string autoHotkeyPath, string scriptPath, string command)
    {
        if (string.IsNullOrWhiteSpace(autoHotkeyPath) || !File.Exists(autoHotkeyPath))
        {
            return CommandSendResult.Fail("AutoHotkey EXE wurde nicht gefunden.");
        }

        if (string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
        {
            return CommandSendResult.Fail("AHK-Script wurde nicht gefunden.");
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            return CommandSendResult.Fail("Kein Befehl eingegeben.");
        }

        var psi = new ProcessStartInfo
        {
            FileName = autoHotkeyPath,
            Arguments = $"\"{scriptPath}\" cmd \"{command}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        try
        {
            using var process = new Process { StartInfo = psi };

            var stdOut = new StringBuilder();
            var stdErr = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    stdOut.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    stdErr.AppendLine(e.Data);
                }
            };

            if (!process.Start())
            {
                return CommandSendResult.Fail("AutoHotkey konnte nicht gestartet werden.");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (process.ExitCode == 0)
            {
                return CommandSendResult.Ok("Befehl erfolgreich an AutoHotkey übergeben.");
            }

            var errorText = stdErr.Length > 0
                ? stdErr.ToString().Trim()
                : stdOut.Length > 0
                    ? stdOut.ToString().Trim()
                    : $"AHK wurde mit ExitCode {process.ExitCode} beendet.";

            return CommandSendResult.Fail(errorText);
        }
        catch (Exception ex)
        {
            return CommandSendResult.Fail(ex.Message);
        }
    }
}

public sealed class CommandSendResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;

    public static CommandSendResult Ok(string message) => new()
    {
        Success = true,
        Message = message
    };

    public static CommandSendResult Fail(string message) => new()
    {
        Success = false,
        Message = message
    };
}