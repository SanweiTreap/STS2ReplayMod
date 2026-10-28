using Godot;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace SpikeInv007A;

internal sealed record ProtectionRoots(string ProfileRoot, string AccountSelector, string MachineSettings);

internal static class ProtectionScope
{
    internal static (ProtectionRoots Roots, ProtectionSnapshot Snapshot) CaptureAndBackup(SaveManager manager)
    {
        var (roots, snapshot) = CaptureCurrent(manager);
        var backupRoot = Path.Combine(ProbeConstants.EvidenceRoot, "restricted-recovery", "r3-" + DateTimeOffset.UtcNow.ToString("yyyyMMddTHHmmss.fffffffZ"));
        Directory.CreateDirectory(backupRoot);
        RejectReparseChain(backupRoot);
        var backups = new List<BackupEntry>();
        foreach (var file in snapshot.Files.Where(x => x.Exists))
        {
            var source = PhysicalPath(roots, file);
            var destination = Full(Path.Combine(backupRoot, file.Scope.ToString(), file.RelativeName.Replace('/', Path.DirectorySeparatorChar)));
            RequireStrictChild(destination, backupRoot);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            using (var input = OpenVerified(source, file.Scope == ProtectedScope.ActiveProfile ? roots.ProfileRoot : source, file.Scope != ProtectedScope.ActiveProfile))
            {
                var sourceHash = Hash(input); input.Position = 0;
                if (!string.Equals(file.Sha256, sourceHash, StringComparison.Ordinal)) throw new IOException("protected source changed during backup");
                using var output = new FileStream(destination, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None);
                input.CopyTo(output); output.Flush(true);
            }
            using var verify = OpenVerified(destination, backupRoot, false);
            var backupHash = Hash(verify);
            if (!string.Equals(file.Sha256, backupHash, StringComparison.Ordinal)) throw new IOException("recovery backup verification failed");
            backups.Add(new BackupEntry(file.Scope, file.RelativeName, file.Length!.Value, file.Sha256!, backupHash));
        }
        return (roots, snapshot with { Backups = backups });
    }

    internal static (ProtectionRoots Roots, ProtectionSnapshot Snapshot) CaptureCurrent(SaveManager manager)
    {
        if (manager.CurrentProfileId != 1 || !UserDataPathProvider.IsRunningModded)
            throw new InvalidOperationException("active profile must be modded profile 1");
        var token = UserDataPathProvider.GetProfileDir(manager.CurrentProfileId).Replace('\\', '/');
        if (!string.Equals(token, "modded/profile1", StringComparison.Ordinal))
            throw new InvalidOperationException("active profile token mismatch");

        var accountBase = Full(ProjectSettings.GlobalizePath(UserDataPathProvider.GetAccountScopedBasePath(null)));
        var profileRoot = Full(ProjectSettings.GlobalizePath(UserDataPathProvider.GetProfileScopedBasePath(1, null, null)));
        var selector = Full(Path.Combine(accountBase, "profile.save"));
        var settings = Full(ProjectSettings.GlobalizePath(UserDataPathProvider.GetAccountScopedBasePath(null).PathJoin(SettingsSaveManager.settingsSaveFileName)));
        RequireStrictChild(profileRoot, accountBase);
        RejectReparseChain(profileRoot);
        RejectReparseChain(accountBase);
        RejectReparseChain(Path.GetDirectoryName(settings)!);

        var roots = new ProtectionRoots(profileRoot, selector, settings);
        var snapshot = Capture(roots, includeBackups: false);
        if (snapshot.ProfileInventory.Files.Count == 0) throw new InvalidOperationException("profile1 inventory is empty");
        return (roots, snapshot);
    }

    internal static RecoveryBaseline CaptureRecoveryBaseline(string recoveryRoot)
    {
        var root = Full(recoveryRoot);
        RejectReparseChain(root);
        var activeRoot = Full(Path.Combine(root, nameof(ProtectedScope.ActiveProfile)));
        var selector = Full(Path.Combine(root, nameof(ProtectedScope.AccountSelector), "modded", "profile.save"));
        var settings = Full(Path.Combine(root, nameof(ProtectedScope.MachineSettings), "settings.save"));
        RequireStrictChild(activeRoot, root);
        RejectReparseChain(activeRoot);
        var (directories, names) = EnumerateProfileTree(activeRoot);
        var files = names.Select(name => Identity(ProtectedScope.ActiveProfile, name,
            Path.Combine(activeRoot, name.Replace('/', Path.DirectorySeparatorChar)), activeRoot)).ToList();
        files.Add(Identity(ProtectedScope.AccountSelector, "modded/profile.save", selector));
        files.Add(Identity(ProtectedScope.MachineSettings, "settings.save", settings));
        var allPhysical = EnumerateAllFiles(root)
            .Select(path => new { Path = path, Relative = Relative(root, path) })
            .OrderBy(x => x.Relative, StringComparer.Ordinal).ToList();
        var aggregate = new StringBuilder();
        foreach (var item in allPhysical)
        {
            var info = new FileInfo(item.Path);
            using var stream = OpenVerified(item.Path, root, false);
            aggregate.Append(item.Relative).Append('\t').Append(info.Length).Append('\t').Append(Hash(stream)).Append('\n');
        }
        var digest = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(aggregate.ToString())));
        var snapshot = new ProtectionSnapshot(files.OrderBy(x => x.Scope).ThenBy(x => x.RelativeName, StringComparer.Ordinal).ToList(),
            new ProfileInventory(directories, names), Array.Empty<BackupEntry>());
        return new RecoveryBaseline(snapshot, allPhysical.Count, digest);
    }

    internal static (bool FileSetEqual, bool ContentEqual, bool InventoryEqual) ComparePrior(
        ProtectionSnapshot recovery, ProtectionSnapshot current)
    {
        var left = recovery.Files.ToDictionary(x => (x.Scope, x.RelativeName));
        var right = current.Files.ToDictionary(x => (x.Scope, x.RelativeName));
        var fileSetEqual = left.Count == right.Count && left.Keys.All(right.ContainsKey);
        var contentEqual = fileSetEqual && left.All(pair =>
        {
            var value = right[pair.Key];
            return pair.Value.Exists == value.Exists && pair.Value.Length == value.Length
                && string.Equals(pair.Value.Sha256, value.Sha256, StringComparison.Ordinal);
        });
        var inventoryEqual = recovery.ProfileInventory.Directories.SequenceEqual(current.ProfileInventory.Directories, StringComparer.Ordinal)
            && recovery.ProfileInventory.Files.SequenceEqual(current.ProfileInventory.Files, StringComparer.Ordinal);
        return (fileSetEqual, contentEqual, inventoryEqual);
    }

    internal static ProtectionSnapshot Capture(ProtectionRoots roots, bool includeBackups = false)
    {
        RejectReparseChain(roots.ProfileRoot);
        var (dirs, names) = EnumerateProfileTree(roots.ProfileRoot);
        var files = names.Select(name => Identity(ProtectedScope.ActiveProfile, name, Path.Combine(roots.ProfileRoot, name.Replace('/', Path.DirectorySeparatorChar)), roots.ProfileRoot)).ToList();
        if (files.Any(x => !x.Exists)) throw new IOException("profile inventory changed during capture");
        files.Add(Identity(ProtectedScope.AccountSelector, "modded/profile.save", roots.AccountSelector));
        files.Add(Identity(ProtectedScope.MachineSettings, "settings.save", roots.MachineSettings));
        return new ProtectionSnapshot(files.OrderBy(x => x.Scope).ThenBy(x => x.RelativeName, StringComparer.Ordinal).ToList(), new ProfileInventory(dirs, names), Array.Empty<BackupEntry>());
    }

    internal static bool Equivalent(ProtectionSnapshot before, ProtectionSnapshot after)
    {
        if (!before.ProfileInventory.Directories.SequenceEqual(after.ProfileInventory.Directories, StringComparer.Ordinal) || !before.ProfileInventory.Files.SequenceEqual(after.ProfileInventory.Files, StringComparer.Ordinal)) return false;
        var left = before.Files.ToDictionary(x => (x.Scope, x.RelativeName));
        var right = after.Files.ToDictionary(x => (x.Scope, x.RelativeName));
        return left.Count == right.Count && left.All(pair => right.TryGetValue(pair.Key, out var value) && pair.Value.Exists == value.Exists && pair.Value.Length == value.Length && pair.Value.Sha256 == value.Sha256 && pair.Value.LastWriteUtc == value.LastWriteUtc);
    }

    private static ProtectedFile Identity(ProtectedScope scope, string name, string path, string? profileRoot = null)
    {
        RejectReparseChain(Path.GetDirectoryName(path)!);
        if (!File.Exists(path)) return new ProtectedFile(scope, name, false, null, null, null);
        var info = new FileInfo(path);
        if ((info.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("reparse-point file rejected");
        var lengthBefore = info.Length; var mtimeBefore = info.LastWriteTimeUtc;
        var authorizedRoot = scope == ProtectedScope.ActiveProfile ? profileRoot ?? throw new IOException("profile root missing") : path;
        using var stream = OpenVerified(path, authorizedRoot, scope != ProtectedScope.ActiveProfile);
        var hash = Hash(stream);
        info.Refresh();
        if (lengthBefore != info.Length || mtimeBefore != info.LastWriteTimeUtc) throw new IOException("protected file changed during identity capture");
        return new ProtectedFile(scope, name, true, info.Length, hash, info.LastWriteTimeUtc.ToString("O"));
    }

    private static string PhysicalPath(ProtectionRoots roots, ProtectedFile file) => file.Scope switch
    {
        ProtectedScope.ActiveProfile => Full(Path.Combine(roots.ProfileRoot, file.RelativeName.Replace('/', Path.DirectorySeparatorChar))),
        ProtectedScope.AccountSelector => roots.AccountSelector,
        ProtectedScope.MachineSettings => roots.MachineSettings,
        _ => throw new ArgumentOutOfRangeException()
    };
    private static (List<string> Directories, List<string> Files) EnumerateProfileTree(string root)
    {
        var directories = new List<string>(); var files = new List<string>();
        var pending = new Stack<string>(); pending.Push(root);
        while (pending.Count != 0)
        {
            var current = pending.Pop();
            foreach (var entry in Directory.EnumerateFileSystemEntries(current, "*", SearchOption.TopDirectoryOnly))
            {
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("reparse-point entry rejected");
                if ((attributes & FileAttributes.Directory) != 0) { directories.Add(Relative(root, entry)); pending.Push(entry); }
                else files.Add(Relative(root, entry));
            }
        }
        directories.Sort(StringComparer.Ordinal); files.Sort(StringComparer.Ordinal);
        return (directories, files);
    }
    private static IReadOnlyList<string> EnumerateAllFiles(string root)
    {
        var files = new List<string>();
        var pending = new Stack<string>(); pending.Push(root);
        while (pending.Count != 0)
        {
            var current = pending.Pop();
            foreach (var entry in Directory.EnumerateFileSystemEntries(current, "*", SearchOption.TopDirectoryOnly))
            {
                var attributes = File.GetAttributes(entry);
                if ((attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("reparse-point entry rejected");
                if ((attributes & FileAttributes.Directory) != 0) pending.Push(entry);
                else files.Add(entry);
            }
        }
        return files;
    }
    private static string Relative(string root, string path)
    {
        var value = Path.GetRelativePath(root, path).Replace('\\', '/');
        if (Path.IsPathRooted(value) || value.Split('/').Any(x => x is "" or "." or "..")) throw new IOException("profile inventory escaped root");
        return value;
    }
    private static void RejectReparseChain(string path)
    {
        var full = Full(path); var current = new DirectoryInfo(Path.GetPathRoot(full)!);
        foreach (var part in full[Path.GetPathRoot(full)!.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            current = new DirectoryInfo(Path.Combine(current.FullName, part));
            if (current.Exists && (current.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("reparse-point path rejected");
        }
    }
    private static FileStream OpenVerified(string path, string authorized, bool exact)
    {
        var stream = new FileStream(path, FileMode.Open, System.IO.FileAccess.Read, FileShare.Read);
        try
        {
            var resolved = FinalPath(stream.SafeFileHandle);
            if (exact)
            {
                if (!string.Equals(resolved, Full(authorized), StringComparison.OrdinalIgnoreCase)) throw new IOException("final handle escaped exact file");
            }
            else RequireStrictChild(resolved, authorized);
            return stream;
        }
        catch { stream.Dispose(); throw; }
    }
    private static string Hash(FileStream stream) { stream.Position = 0; return Convert.ToHexString(SHA256.HashData(stream)); }
    private static string FinalPath(SafeFileHandle handle)
    {
        var buffer = new StringBuilder(1024);
        var length = GetFinalPathNameByHandle(handle, buffer, (uint)buffer.Capacity, 0);
        if (length == 0 || length >= buffer.Capacity) throw new IOException("could not resolve final file handle");
        var value = buffer.ToString();
        if (value.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase)) value = @"\\" + value[8..];
        else if (value.StartsWith(@"\\?\", StringComparison.OrdinalIgnoreCase)) value = value[4..];
        return Full(value);
    }
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandle(SafeFileHandle hFile, StringBuilder lpszFilePath, uint cchFilePath, uint dwFlags);
    private static void RequireStrictChild(string candidate, string root)
    {
        var prefix = Full(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!Full(candidate).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new IOException("path containment failed");
    }
    private static string Full(string path) => Path.GetFullPath(path).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
}
