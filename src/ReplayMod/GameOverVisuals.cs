using Godot;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.GameOverScreen;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;

namespace ReplayMod;

// Bounded observations at native death/overlay boundaries. Never move or repair a model.
internal static class GameOverVisuals
{
    internal static void Capture(string phase)
    {
        try
        {
            var run=RunManager.Instance.DebugOnlyGetState();if(run is null)return;
            var screen=NOverlayStack.Instance?.Peek() as NGameOverScreen;
            var container=screen is null?null:RunPatches.Field(screen,"_creatureContainer") as Node;
            var node=NCombatRoom.Instance?.GetCreatureNode(run.Players[0].Creature);
            var visuals=node?.Visuals;var body=node?.Body;
            object? State(CanvasItem? item)=>item is null||!GodotObject.IsInstanceValid(item)?null:new
            {
                Type=item.GetType().Name,Id=item.GetInstanceId(),Parent=item.GetParent()?.GetType().Name,
                item.Visible,VisibleInTree=item.IsVisibleInTree(),InsideTree=item.IsInsideTree(),Queued=item.IsQueuedForDeletion(),
                Alpha=item.Modulate.A,SelfAlpha=item.SelfModulate.A,
                X=item.GetGlobalTransform().Origin.X,Y=item.GetGlobalTransform().Origin.Y,
                ScaleX=item.GetGlobalTransform().Scale.X,ScaleY=item.GetGlobalTransform().Scale.Y,
                CanvasX=item.GetGlobalTransformWithCanvas().Origin.X,CanvasY=item.GetGlobalTransformWithCanvas().Origin.Y,
                item.ZIndex,item.ZAsRelative
            };
            Runtime.Log("native-death-model-state",new{Phase=phase,Playback=PlayerLoop.Active,
                PlayerHp=run.Players[0].Creature.CurrentHp,NativeOverlay=screen is not null,
                PlayerNodePresent=node is not null,VisualInGameOver=container is not null&&visuals is not null&&container.IsAncestorOf(visuals),
                Creature=State(node),Visuals=State(visuals),Body=State(body),MutationPerformed=false});
        }
        catch(Exception ex){Runtime.Log("native-death-model-observation-unavailable",new{Phase=phase,Reason=ex.GetType().Name,MutationPerformed=false});}
    }
    internal static async Task After(Task native,RunState owner,string phase)
    {
        try
        {
            await native;
            if(ReferenceEquals(owner,RunManager.Instance.DebugOnlyGetState()))Capture(phase);
        }
        catch(OperationCanceledException){}
        catch(Exception ex){Runtime.Log("native-death-model-observation-task-failed",new{Phase=phase,Reason=ex.GetType().Name,MutationPerformed=false});}
    }
}
