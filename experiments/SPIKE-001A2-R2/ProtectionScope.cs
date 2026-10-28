using Godot;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using System.Security.Cryptography;

namespace Spike001A2R2;

internal static class ProtectionScope
{
    internal static ProtectedState Capture(SaveManager manager)
    {
        if (manager.CurrentProfileId != 1 || !UserDataPathProvider.IsRunningModded)
            throw new InvalidOperationException("active profile must be modded profile 1");
        var token = UserDataPathProvider.GetProfileDir(1).Replace('\\', '/');
        if (token != "modded/profile1") throw new InvalidOperationException("profile1 token mismatch: " + token);
        var profileRoot = Full(ProjectSettings.GlobalizePath(UserDataPathProvider.GetProfileScopedBasePath(1, null, null)));
        RejectReparse(profileRoot);
        var dirs = Directory.EnumerateDirectories(profileRoot, "*", SearchOption.AllDirectories)
            .Select(x => Rel(profileRoot, x)).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var names = Directory.EnumerateFiles(profileRoot, "*", SearchOption.AllDirectories)
            .Select(x => Rel(profileRoot, x)).OrderBy(x => x, StringComparer.Ordinal).ToList();
        var files = names.Select(x => Identity(Path.Combine(profileRoot, x.Replace('/', Path.DirectorySeparatorChar)))).ToList();
        var accountRoot = Full(ProjectSettings.GlobalizePath(UserDataPathProvider.GetAccountScopedBasePath(null)));
        files.Add(Identity(Path.Combine(accountRoot, "profile.save")));
        files.Add(Identity(Full(ProjectSettings.GlobalizePath(UserDataPathProvider.GetAccountScopedBasePath(null)
            .PathJoin(SettingsSaveManager.settingsSaveFileName)))));
        return new(profileRoot, files.OrderBy(x => x.Path, StringComparer.OrdinalIgnoreCase).ToList(), new(dirs, names));
    }

    internal static FileIdentity CurrentRunIdentity()
    {
        var logical = UserDataPathProvider.GetProfileScopedPath(1, "saves/" + RunSaveManager.runSaveFileName);
        return Identity(Full(ProjectSettings.GlobalizePath(logical)));
    }

    internal static string ReadStableText(FileIdentity expected)
    {
        if (!expected.Exists) throw new FileNotFoundException("current_run.save is missing", expected.Path);
        var text = File.ReadAllText(expected.Path);
        var after = Identity(expected.Path);
        if (after.Length != expected.Length || after.Sha256 != expected.Sha256 || after.LastWriteUtc != expected.LastWriteUtc)
            throw new IOException("checkpoint changed while being read");
        return text;
    }

    private static FileIdentity Identity(string path)
    {
        path = Full(path); RejectReparse(Path.GetDirectoryName(path)!);
        if (!File.Exists(path)) return new(path, false, null, null, null);
        var info = new FileInfo(path);
        if ((info.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("reparse file rejected: " + path);
        var length = info.Length; var mtime = info.LastWriteTimeUtc;
        using var stream = new FileStream(path, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read);
        var hash = Convert.ToHexString(SHA256.HashData(stream)); info.Refresh();
        if (length != info.Length || mtime != info.LastWriteTimeUtc) throw new IOException("file changed during capture: " + path);
        return new(path, true, length, hash, mtime.ToString("O"));
    }
    private static string Rel(string root, string path)
    {
        var value = Path.GetRelativePath(root, path).Replace('\\', '/');
        if (Path.IsPathRooted(value) || value.Split('/').Any(x => x is "" or "." or "..")) throw new IOException("path escaped profile root");
        return value;
    }
    private static void RejectReparse(string path)
    {
        var full = Full(path); var root = Path.GetPathRoot(full)!; var current = root;
        foreach (var part in full[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        { current = Path.Combine(current, part); if (Directory.Exists(current) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) throw new IOException("reparse path rejected"); }
    }
    private static string Full(string path) => Path.GetFullPath(path).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
}
