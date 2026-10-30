namespace ReplayMod;

internal static class NativePlaybackSupport
{
    internal static string? Reason(Clip clip)
    {
        if(clip.Format is <4 or >10)return "此格式尚未接入新原生播放器；STATE冷恢复请使用独立工具入口";
        if(clip.Character is not ("CHARACTER.IRONCLAD" or "CHARACTER.SILENT" or "CHARACTER.REGENT")&&
            !(clip.Format>=8&&clip.Character is "CHARACTER.DEFECT" or "CHARACTER.NECROBINDER"))return "新原生播放器暂仅支持铁甲／猎人／储君及格式8的故障机器人／亡灵契约师录制范围";
        if((clip.Choices??[]).Any(c=>!c.Automated&&c.Kind is not ("FromHand" or "FromHandForUpgrade" or "FromCombatPile" or
            "FromChooseACardScreen" or "FromSimpleGrid" or "FromSimpleGridForRewards" or "FromChooseABundleScreen")))
            return "此录像包含尚未接入新原生播放器的选择类型";
        return null;
    }
}
