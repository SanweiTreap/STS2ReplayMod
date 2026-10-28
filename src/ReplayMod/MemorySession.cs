using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Managers;
using MegaCrit.Sts2.Core.Saves.Test;

namespace ReplayMod;

internal sealed class MemorySession
{
    internal SaveManager Original {get;}
    internal SaveManager Isolated {get;private set;}
    private readonly string _nativePath,_progressPath,_beforeProgress;
    private readonly string? _runHash,_backupHash,_progressHash;
    private bool _installed;
    internal MemorySession()
    {
        Original=SaveManager.Instance;
        _nativePath=ProjectSettings.GlobalizePath(RunSaveManager.GetRunSavePath(Original.CurrentProfileId,RunSaveManager.runSaveFileName));
        _runHash=FileHash(_nativePath);_backupHash=FileHash(_nativePath+".backup");_beforeProgress=Progress(Original);
        _progressPath=ProjectSettings.GlobalizePath(ProgressSaveManager.GetProgressPathForProfile(Original.CurrentProfileId));_progressHash=FileHash(_progressPath);
        Isolated=CreateIsolated();
    }
    private SaveManager CreateIsolated()
    {
        var profileManager=AccessTools.Field(typeof(SaveManager),"_profileSaveManager").GetValue(Original)!;
        var profile=(ProfileSave)AccessTools.Property(profileManager.GetType(),"Profile").GetValue(profileManager)!;
        var memory=new MockGodotFileIo("user://ReplayMod/memory-only"){getCurrentTime=()=>DateTimeOffset.UtcNow};
        memory.WriteFile(ProfileSaveManager.GetProfileSavePath(null),JsonSerializationUtility.ToJson(profile));
        memory.WriteFile(SettingsSaveManager.settingsSaveFileName,JsonSerializationUtility.ToJson(Original.SettingsSave));
        memory.WriteFile(PrefsSaveManager.GetPrefsPath(Original.CurrentProfileId,null),JsonSerializationUtility.ToJson(Original.PrefsSave));
        memory.WriteFile(ProgressSaveManager.GetProgressPathForProfile(Original.CurrentProfileId,null),_beforeProgress);
        var isolated=new SaveManager(memory,memory,true);isolated.InitProfileId(null);
        Codec.Require(isolated.InitSettingsData().Success&&isolated.InitPrefsData().Success&&isolated.InitProgressData().Success,"memory-context-init");
        Codec.Require(isolated.CurrentProfileId==Original.CurrentProfileId&&!ReferenceEquals(Original.SettingsSave,isolated.SettingsSave)&&
            !ReferenceEquals(Original.PrefsSave,isolated.PrefsSave)&&!ReferenceEquals(Original.Progress,isolated.Progress),"memory-context-sharing");
        return isolated;
    }
    private static string Progress(SaveManager manager)
    {
        var progress=manager.Progress.ToSerializable();progress.SchemaVersion=manager.GetLatestSchemaVersion<SerializableProgress>();
        return JsonSerializationUtility.ToJson(progress);
    }
    private static string? FileHash(string p)=>File.Exists(p)?Codec.Hash(File.ReadAllBytes(p)):null;
    internal void Install(){Codec.Require(!_installed&&ReferenceEquals(SaveManager.Instance,Original),"save-context-owner");SaveManager.MockInstanceForTesting(Isolated);_installed=true;}
    internal async Task Drain(){if(Isolated.CurrentRunSaveTask is {} task)await task;}
    // Called only after the old run and its saves have drained. The original manager stays detached.
    internal void Restart()
    {
        Codec.Require(_installed&&ReferenceEquals(SaveManager.Instance,Isolated),"save-context-owner");
        var fresh=CreateIsolated();SaveManager.MockInstanceForTesting(fresh);Isolated=fresh;
    }
    internal bool Restore()
    {
        if(_installed){SaveManager.ClearInstanceForTesting();_installed=false;}
        Codec.Require(ReferenceEquals(SaveManager.Instance,Original),"save-manager-not-restored");
        return FileHash(_nativePath)==_runHash&&FileHash(_nativePath+".backup")==_backupHash&&FileHash(_progressPath)==_progressHash&&Progress(Original)==_beforeProgress;
    }
}
