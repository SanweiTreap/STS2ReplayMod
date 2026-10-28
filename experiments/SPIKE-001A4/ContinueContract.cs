namespace Spike001A4;

internal sealed record ContinueFacts(bool PreFinishedRoomNull, int SaveFloor, int RestoredTotalFloor,
    string SaveSeed, string RestoredSeed, string SaveCharacter, string RestoredCharacter,
    int SaveAct, int RestoredAct, int SaveRow, int SaveCol, int RestoredRow, int RestoredCol);

internal static class ContinueContract
{
    internal static void Validate(TargetIdentity target, CheckpointFloorBinding checkpointFloor, ContinueFacts facts)
    {
        if (!facts.PreFinishedRoomNull) throw new InvalidDataException("B save PreFinishedRoom is not null");
        var floors = new FloorContract(target, checkpointFloor);
        floors.ValidatePreRoom(facts.SaveFloor, facts.RestoredTotalFloor);
        SemanticProjectionBuilder.RequireText(facts.SaveSeed, "B save seed");
        SemanticProjectionBuilder.RequireText(facts.RestoredSeed, "B restored seed");
        SemanticProjectionBuilder.RequireText(facts.SaveCharacter, "B save character");
        SemanticProjectionBuilder.RequireText(facts.RestoredCharacter, "B restored character");
        if (facts.SaveSeed != target.Seed || facts.RestoredSeed != target.Seed
            || facts.SaveCharacter != target.Character || facts.RestoredCharacter != target.Character
            || facts.SaveAct != target.ActIndex || facts.RestoredAct != target.ActIndex
            || facts.SaveRow != target.MapRow || facts.SaveCol != target.MapCol
            || facts.RestoredRow != target.MapRow || facts.RestoredCol != target.MapCol)
            throw new InvalidDataException("normal Continue save/restored identity does not match sealed target");
    }
}
