using Microsoft.Win32;
using Nightcrew.Core;

namespace Nightcrew.Platform;

/// <summary>The restore record's second copy under HKCU\Software\Nightcrew\Restore (format: <see cref="RestoreBackupFormat"/>).</summary>
public sealed class RegistryRestoreBackup(string subKey = RegistryRestoreBackup.DefaultSubKey) : IRestoreBackup
{
    public const string DefaultSubKey = @"Software\Nightcrew\Restore";
    private const string AppKey = @"Software\Nightcrew";

    public void Save(RestoreRecord record)
    {
        using var key = Registry.CurrentUser.CreateSubKey(subKey) ?? throw new InvalidOperationException($"Couldn't open HKCU\\{subKey}");
        foreach (var entry in RestoreBackupFormat.ToValues(record))
            key.SetValue(entry.Key, entry.Value, entry.Value is int ? RegistryValueKind.DWord : RegistryValueKind.String);
        if (record.Scheme is null) key.DeleteValue(nameof(RestoreRecord.Scheme), throwOnMissingValue: false);
    }

    public RestoreRecord? Load()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(subKey, writable: false);
            return key is null ? null : RestoreBackupFormat.FromValues(name => key.GetValue(name));
        }
        catch (Exception)
        {
            return null; // read every tick through HasRecord: an unreadable copy counts as none
        }
    }

    public void Clear()
    {
        try { Registry.CurrentUser.DeleteSubKeyTree(subKey, throwOnMissingSubKey: false); }
        catch (Exception) { } // a copy left behind is ignored once the settings are the user's own
    }

    /// <summary>Removes HKCU\Software\Nightcrew and everything under it, for --uninstall. Never throws.</summary>
    public static void RemoveAll()
    {
        try { Registry.CurrentUser.DeleteSubKeyTree(AppKey, throwOnMissingSubKey: false); }
        catch (Exception) { }
    }
}
