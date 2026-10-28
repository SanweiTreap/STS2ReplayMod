using System.Runtime.CompilerServices;
// Homomorphic POCO types in this TEST assembly; sts2/Godot are not referenced or loaded.
namespace DummyFixture
{
    public static class World
    {
        public static Task Next=Task.CompletedTask;public static int OriginalCalls,Hp=77,Energy=3;public static bool Ready,Bootstrap;
        public static Exception? SyncFault;
        [MethodImpl(MethodImplOptions.NoInlining)]public static void Body(){OriginalCalls++;if(SyncFault is not null)throw SyncFault;}
        public static string Values=>Hp+":"+Energy+":A,B";
    }
}
namespace MegaCrit.Sts2.Core.Nodes.Screens.MainMenu{public class NMainMenu{[MethodImpl(MethodImplOptions.NoInlining)]public void _Ready()=>DummyFixture.World.Body();}}
namespace MegaCrit.Sts2.Core.Saves{public class SerializableRun{}}
namespace MegaCrit.Sts2.Core.Map{public struct MapCoord{public int row,col;}}
namespace MegaCrit.Sts2.Core.Runs
{
    public class RunState{}
    public class RunManager
    {
        [MethodImpl(MethodImplOptions.NoInlining)]public void SetUpNewSingleplayer(RunState state,bool shouldSave,DateTimeOffset? dailyTime=null)=>DummyFixture.World.Body();
        [MethodImpl(MethodImplOptions.NoInlining)]public Task SetUpSavedSingleplayer(RunState state,MegaCrit.Sts2.Core.Saves.SerializableRun save){DummyFixture.World.Body();return DummyFixture.World.Next;}
        [MethodImpl(MethodImplOptions.NoInlining)]public Task EnterMapCoord(MegaCrit.Sts2.Core.Map.MapCoord coord){DummyFixture.World.Body();return DummyFixture.World.Next;}
    }
}
namespace MegaCrit.Sts2.Core.GameActions
{
    public class GameAction
    {
        [MethodImpl(MethodImplOptions.NoInlining)]public Task Execute(){DummyFixture.World.Body();return DummyFixture.World.Next;}
        [MethodImpl(MethodImplOptions.NoInlining)]public void Cancel()=>DummyFixture.World.Body();
    }
    public sealed class PlayCardAction:GameAction{[MethodImpl(MethodImplOptions.NoInlining)]private Task ExecuteAction(){DummyFixture.World.Body();return DummyFixture.World.Next;}}
    public sealed class EndPlayerTurnAction:GameAction{[MethodImpl(MethodImplOptions.NoInlining)]private Task ExecuteAction(){DummyFixture.World.Body();return DummyFixture.World.Next;}}
    public sealed class MoveToMapCoordAction:GameAction{[MethodImpl(MethodImplOptions.NoInlining)]private Task ExecuteAction(){DummyFixture.World.Body();return DummyFixture.World.Next;}}
}
namespace MegaCrit.Sts2.Core.GameActions.Multiplayer
{
    public class PlayerChoiceContext{}
    public class ActionQueueSynchronizer{[MethodImpl(MethodImplOptions.NoInlining)]public void RequestEnqueue(MegaCrit.Sts2.Core.GameActions.GameAction action)=>DummyFixture.World.Body();}
}
namespace MegaCrit.Sts2.Core.Entities.Creatures{public class Creature{}}
namespace MegaCrit.Sts2.Core.Entities.Players{public class Player{}}
namespace MegaCrit.Sts2.Core.Entities.Cards{public struct ResourceInfo{public int EnergySpent;}}
namespace MegaCrit.Sts2.Core.Models
{
    public class CardModel
    {
        [MethodImpl(MethodImplOptions.NoInlining)]public Task OnPlayWrapper(MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext choiceContext,
            MegaCrit.Sts2.Core.Entities.Creatures.Creature target,bool isAutoPlay,MegaCrit.Sts2.Core.Entities.Cards.ResourceInfo resources,bool skipCardPileVisuals=false)
        {DummyFixture.World.Body();return DummyFixture.World.Next;}
    }
}
namespace MegaCrit.Sts2.Core.Combat
{
    internal sealed class CombatTurnState{}
    public class CombatManager
    {
        [MethodImpl(MethodImplOptions.NoInlining)]public void SetReadyToEndTurn(MegaCrit.Sts2.Core.Entities.Players.Player player,bool canBackOut,Func<Task>? actionDuringEnemyTurn=null)
        {DummyFixture.World.Body();DummyFixture.World.Ready=true;}
        [MethodImpl(MethodImplOptions.NoInlining)]private Task StartTurn(CombatTurnState turnState,Func<Task>? actionDuringEnemyTurn=null){DummyFixture.World.Body();return DummyFixture.World.Next;}
    }
}
