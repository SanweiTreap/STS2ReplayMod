namespace Spike001A4;

internal sealed class FloorContract
{
    internal FloorContract(int liveFloor, int saveFloor)
    {
        if (liveFloor < 0 || saveFloor < 0)
            throw new InvalidDataException("arm floor facts must be present and non-negative");
        if (liveFloor != saveFloor)
            throw new InvalidDataException("arm live TotalFloor and predecessor save FloorReached differ");
        ArmPredecessorFloor = liveFloor;
        ExpectedPlayableFloor = checked(liveFloor + 1);
        CheckpointFloor = CheckpointFloorBinding.Unbound();
    }

    internal FloorContract(TargetIdentity target, CheckpointFloorBinding checkpointFloor)
    {
        if (target.ArmPredecessorFloor < 0 || target.ExpectedPlayableFloor < 0
            || target.ExpectedPlayableFloor != checked(target.ArmPredecessorFloor + 1))
            throw new InvalidDataException("serialized arm floor contract is invalid");
        ArmPredecessorFloor = target.ArmPredecessorFloor;
        ExpectedPlayableFloor = target.ExpectedPlayableFloor;
        CheckpointFloor = checkpointFloor;
        ValidateBindingShape(checkpointFloor);
    }

    internal int ArmPredecessorFloor { get; }
    internal int ExpectedPlayableFloor { get; }
    internal CheckpointFloorBinding CheckpointFloor { get; private set; }

    internal void BindCheckpoint(int capturedFloor)
    {
        if (CheckpointFloor.State != BindingState.Unbound || CheckpointFloor.Value is not null)
            throw new InvalidOperationException("checkpoint floor is already bound");
        if (capturedFloor < 0 || capturedFloor != ArmPredecessorFloor)
            throw new InvalidDataException("checkpoint FloorReached does not equal ArmPredecessorFloor");
        CheckpointFloor = CheckpointFloorBinding.Bound(capturedFloor);
    }

    internal void ValidatePreRoom(int saveFloor, int restoredTotalFloor)
    {
        var checkpoint = RequireBound();
        if (saveFloor != checkpoint || restoredTotalFloor != checkpoint)
            throw new InvalidDataException("B pre-room floor facts do not equal sealed checkpoint floor");
    }

    internal void ValidatePlayable(int totalFloor)
    {
        _ = RequireBound();
        if (totalFloor != ExpectedPlayableFloor)
            throw new InvalidDataException("playable TotalFloor does not equal ExpectedPlayableFloor");
    }

    private int RequireBound()
    {
        ValidateBindingShape(CheckpointFloor);
        if (CheckpointFloor.State != BindingState.Bound || CheckpointFloor.Value is null)
            throw new InvalidOperationException("checkpoint floor is unbound");
        return CheckpointFloor.Value.Value;
    }

    private static void ValidateBindingShape(CheckpointFloorBinding binding)
    {
        if ((binding.State == BindingState.Unbound && binding.Value is not null)
            || (binding.State == BindingState.Bound && (binding.Value is null || binding.Value < 0)))
            throw new InvalidDataException("checkpoint floor binding shape is invalid");
    }
}
