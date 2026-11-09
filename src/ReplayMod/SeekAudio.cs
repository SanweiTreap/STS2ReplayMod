using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Audio;
using MegaCrit.Sts2.Core.Saves;

namespace ReplayMod;

internal static class SeekAudio
{
    private static bool _active,_settingVolume,_godotMuted;
    private static float _volume;

    internal static void Install(Harmony h)=>h.Patch(
        AccessTools.DeclaredMethod(typeof(NAudioManager),nameof(NAudioManager.SetMasterVol)),
        prefix:new HarmonyMethod(typeof(SeekAudio),nameof(KeepMuted)));

    private static void KeepMuted(ref float __0)
    {
        if(!_active||_settingVolume)return;
        _volume=__0;__0=0;
    }

    internal static void Begin(bool room)
    {
        if(_active)return;
        _active=true;_volume=SaveManager.Instance.SettingsSave.VolumeMaster;
        _godotMuted=AudioServer.IsBusMute(0);
        AudioServer.SetBusMute(0,true);
        SetVolume(0);
        if(room)NAudioManager.Instance?.StopAllLoops();
        Runtime.Log("seek-audio-held",new{Room=room});
    }

    internal static void End()
    {
        if(!_active)return;
        _active=false;
        try{SetVolume(_volume);}
        finally{AudioServer.SetBusMute(0,_godotMuted);}
        Runtime.Log("seek-audio-released",new{MasterVolumeRestored=true});
    }

    private static void SetVolume(float volume)
    {
        _settingVolume=true;
        try{NAudioManager.Instance?.SetMasterVol(volume);}
        finally{_settingVolume=false;}
    }
}
