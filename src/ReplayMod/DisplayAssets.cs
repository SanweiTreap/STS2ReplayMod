using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.Pooling;
using System.Collections;

namespace ReplayMod;

// One watching session owns these display-only models/nodes. They never enter combat piles or the net DB.
internal static class DisplayAssets
{
    internal sealed class Asset(CardModel source,CardModel frozen)
    {
        internal readonly CardModel Source=source,Model=frozen;
        internal readonly List<View> Views=[];
    }
    internal sealed record View(NCard Card,Label Index);
    private static readonly DisplayVariantCache<CardModel,Asset> Models=new();
    private static readonly HashSet<CardModel> FrozenModels=new(ReferenceEqualityComparer.Instance);
    private static readonly Dictionary<PotionModel,NPotion> Potions=new(ReferenceEqualityComparer.Instance);
    private static readonly Dictionary<int,TextureRect> EmptySlots=[];
    private static Control? _root;
    private static readonly PrivateDisplayOwnership<NCard> PrivateCards=new();
    private static readonly PrivateDisplayOwnership<NCard> RetiringCards=new();
    internal static bool IsPrivate(NCard card)=>PrivateCards.Contains(card);
    internal static bool IsCacheRoot(Node node)=>ReferenceEquals(node,_root);
    internal static int ModelsCreated {get;private set;}
    internal static int ViewsCreated {get;private set;}
    internal static int LeasesReused {get;private set;}
    internal static int Generation {get;private set;}
    internal static bool CanWarm(int generation)=>generation==Generation&&PlayerLoop.Active&&!PlayerLoop.TearingDown&&
        NCombatRoom.Instance is {} room&&GodotObject.IsInstanceValid(room)&&!room.IsQueuedForDeletion();
    private static Control Root
    {
        get
        {
            if(_root is null||!GodotObject.IsInstanceValid(_root))
            {
                _root=new(){Name="ReplayDisplayCache",Visible=false,ProcessMode=Node.ProcessModeEnum.Disabled,MouseFilter=Control.MouseFilterEnum.Ignore};
                NCombatRoom.Instance!.Ui.AddChild(_root);
            }
            return _root;
        }
    }
    internal static Asset Get(CardModel source,Action beforeCopy)
    {
        var fingerprint=LiveState.Fingerprint(LiveState.CardDetails(source));
        return Models.Get(source,fingerprint,()=>{
        beforeCopy();var clone=(CardModel)AccessTools.Method(typeof(object),"MemberwiseClone").Invoke(source,null)!;
        foreach(var field in IdentityMemento.Fields(clone.GetType()).Where(f=>typeof(Delegate).IsAssignableFrom(f.FieldType)))field.SetValue(clone,null);
        AccessTools.Method(clone.GetType(),"DeepCloneFields").Invoke(clone,null);AccessTools.Method(clone.GetType(),"AfterCloned").Invoke(clone,null);
        var asset=new Asset(source,clone);FrozenModels.Add(clone);ModelsCreated++;return asset;});
    }
    internal static void Prime(IEnumerable<CardModel> cards)
    {
        string? before=null;var count=ModelsCreated;var started=System.Diagnostics.Stopwatch.GetTimestamp();
        foreach(var card in cards.Distinct(ReferenceEqualityComparer.Instance).Cast<CardModel>())Get(card,()=>before??=LiveState.ChoiceProjection());
        if(before is not null)Codec.Require(before==LiveState.ChoiceProjection(),"display-prime-mutated-business-or-rng");
        Runtime.Log("display-models-primed",new{Created=ModelsCreated-count,Milliseconds=System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds,ViewsInstantiated=false});
    }
    private static bool Valid(View view)=>GodotObject.IsInstanceValid(view.Card)&&!view.Card.IsQueuedForDeletion()&&!RetiringCards.Contains(view.Card);
    internal static async Task Warm(IEnumerable<Asset> assets)
    {
        var generation=Generation;
        var slots=new Dictionary<Asset,int>();
        foreach(var asset in assets)
        {
            if(!CanWarm(generation))return;
            var slot=slots.GetValueOrDefault(asset);slots[asset]=slot+1;
            if(slot<asset.Views.Count&&Valid(asset.Views[slot]))continue;
            // These views never borrow the game's shared pool. Native exhaustion can free them
            // through NodePool.Free later; the narrowly scoped ownership hook sends that to disposal.
            var card=PreloadManager.Cache.GetScene("res://scenes/cards/card.tscn").Instantiate<NCard>();
            PrivateCards.Mark(card);card.OnInstantiated();card.OnReturnedFromPool();card.Model=asset.Model;card.Visibility=ModelVisibility.Visible;
            Root.AddChild(card);card.Visible=false;
            // Explicitly isolate the mutable shader even if the packed scene happens to share it.
            var highlight=card.CardHighlight;var material=(ShaderMaterial)highlight.Material.Duplicate();
            highlight.Material=material;AccessTools.Field(typeof(NCardHighlight),"_shaderMaterial").SetValue(highlight,material);
            card.MouseFilter=Control.MouseFilterEnum.Ignore;
            var index=slot<asset.Views.Count?asset.Views[slot].Index:new Label{Visible=false,MouseFilter=Control.MouseFilterEnum.Ignore};
            if(index.GetParent() is null)Root.AddChild(index);
            var view=new View(card,index);if(slot==asset.Views.Count)asset.Views.Add(view);else asset.Views[slot]=view;
            ViewsCreated++;await Runtime.NextFrame(); // Spread first-use construction, rather than a whole-hand burst.
        }
    }
    internal static View Lease(Asset asset,int slot,Control parent)
    {
        Codec.Require(slot<asset.Views.Count&&Valid(asset.Views[slot]),"display-cache-not-prepared");
        var view=asset.Views[slot];
        if(!ReferenceEquals(view.Card.Model,asset.Model))view.Card.Model=asset.Model;
        view.Card.Reparent(parent);view.Index.Reparent(parent);LeasesReused++;return view;
    }
    internal static async Task WarmPotions(IEnumerable<PotionModel> models,IEnumerable<int> slots)
    {
        var generation=Generation;
        foreach(var model in models.Distinct())
        {
            if(!CanWarm(generation))return;
            if(Potions.TryGetValue(model,out var existing)&&GodotObject.IsInstanceValid(existing))continue;
            NPotion node;
            node=NPotion.Create(model)??throw new InvalidDataException("display-cache-potion-create");Root.AddChild(node);node.Visible=false;
            node.MouseFilter=Control.MouseFilterEnum.Ignore;Potions[model]=node;await Runtime.NextFrame();
        }
        foreach(var slot in slots.Distinct())
        {
            if(!CanWarm(generation))return;
            if(EmptySlots.ContainsKey(slot))continue;
            var node=new TextureRect{Visible=false,MouseFilter=Control.MouseFilterEnum.Ignore};Root.AddChild(node);EmptySlots[slot]=node;
        }
    }
    internal static NPotion LeasePotion(PotionModel model,Control parent)
    {
        var node=Potions[model];node.SetAnchorsPreset(Control.LayoutPreset.TopLeft);node.Reparent(parent);return node;
    }
    internal static TextureRect LeaseEmpty(int slot,Control parent)
    {
        var node=EmptySlots[slot];node.Reparent(parent);return node;
    }
    internal static void Return(Control node)
    {
        if(GodotObject.IsInstanceValid(node)&&!node.IsQueuedForDeletion()){node.Visible=false;node.Reparent(Root);}
    }
    internal static void Return(View view,bool consumed)
    {
        if(GodotObject.IsInstanceValid(view.Index)&&!view.Index.IsQueuedForDeletion()){view.Index.Visible=false;view.Index.Reparent(Root);}
        if(!GodotObject.IsInstanceValid(view.Card)||view.Card.IsQueuedForDeletion())return;
        if(consumed){view.Card.GetParent()?.RemoveChild(view.Card);view.Card.QueueFree();return;}
        view.Card.PlayPileTween?.Kill();view.Card.PlayPileTween=null;view.Card.Visible=false;view.Card.Reparent(Root);
    }
    internal static void ContinueConsumed(View view)
    {
        // A delayed native exhaust still owns this card. Keep its private-disposal identity,
        // but make it unavailable to new display leases before the delayed callback frees it.
        RetiringCards.Mark(view.Card);Return(view.Index);
    }
    internal static void Clear()
    {
        Generation++;
        if(_root is not null&&GodotObject.IsInstanceValid(_root)&&!_root.IsQueuedForDeletion()){_root.GetParent()?.RemoveChild(_root);_root.QueueFree();}
        _root=null;Models.Clear();FrozenModels.Clear();Potions.Clear();EmptySlots.Clear();ModelsCreated=0;ViewsCreated=0;LeasesReused=0;
    }
    internal static void Install(Harmony harmony)
    {
        harmony.Patch(AccessTools.DeclaredMethod(typeof(NodePool),"Free",[typeof(IPoolable)]),
            prefix:new HarmonyMethod(typeof(DisplayAssets),nameof(FreePrivate)));
        harmony.Patch(AccessTools.DeclaredMethod(typeof(NCard),"Create",[typeof(CardModel),typeof(ModelVisibility)]),
            prefix:new HarmonyMethod(typeof(DisplayAssets),nameof(CreateFrozen)));
    }
    private static bool CreateFrozen(CardModel __0,ModelVisibility __1,ref NCard? __result)
    {
        if(!FrozenModels.Contains(__0))return true;
        var card=PreloadManager.Cache.GetScene("res://scenes/cards/card.tscn").Instantiate<NCard>();
        PrivateCards.Mark(card);card.OnInstantiated();card.OnReturnedFromPool();card.Model=__0;card.Visibility=__1;
        __result=card;return false;
    }
    private static bool FreePrivate(IPoolable __0)
    {
        if(__0 is not NCard card||!PrivateCards.Contains(card))return true;
        if(GodotObject.IsInstanceValid(card)&&!card.IsQueuedForDeletion()){card.GetParent()?.RemoveChild(card);card.QueueFreeSafelyNoPool();}
        Runtime.Log("display-private-disposal",new{NativePoolUntouched=true,DeferredNativeCallbackHandled=true});return false;
    }
    internal static void VerifyNativePool()
    {
        var pools=(IDictionary)AccessTools.Field(typeof(NodePool),"_pools").GetValue(null)!;
        if(pools[typeof(NCard)] is not NodePool<NCard> pool)return;
        var free=pool.DebugFreeObjects;
        Codec.Require(free.All(c=>GodotObject.IsInstanceValid(c)&&!c.IsQueuedForDeletion()&&!PrivateCards.Contains(c)),"native-card-pool-invalid-free-entry");
        Runtime.Log("display-native-pool-verified",new{FreeCards=free.Count,DisposedEntries=0,PrivateDisplayEntries=0});
    }
}
