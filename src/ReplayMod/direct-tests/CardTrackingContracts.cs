using ReplayMod;

internal static class CardTrackingContracts
{
    private sealed class Card;
    private sealed class Pile
    {
        internal readonly List<Card> Cards=[];
        internal event Action? Changed;
        internal int Listeners=>Changed?.GetInvocationList().Length??0;
        internal void Add(Card card){Cards.Add(card);Changed?.Invoke();}
    }
    private sealed class Database(Pile[] piles)
    {
        internal readonly Pile[] Piles=piles;
        internal uint Next;
        internal readonly Dictionary<Card,uint> Ids=[];
        internal readonly Dictionary<uint,Card> Cards=[];
        internal readonly List<(Pile Pile,Action Action)> Subscriptions=[];
        private void Changed(Pile pile)
        {
            foreach(var card in pile.Cards)if(!Ids.ContainsKey(card)){Ids.Add(card,Next);Cards.Add(Next++,card);}
        }
        internal int Attach()=>PileSubscriptionRepair.AttachMissing(Piles,Subscriptions.Select(s=>s.Pile),pile=>
        {
            Action action=()=>Changed(pile);Subscriptions.Add((pile,action));pile.Changed+=action;
        });
        internal void Start(){foreach(var pile in Piles)Changed(pile);Attach();}
        internal void End(){foreach(var s in Subscriptions)s.Pile.Changed-=s.Action;Subscriptions.Clear();}
    }
    private static IdentityMemento Capture(Database db)=>new([db],o=>o is Database or Pile or Card,
        f=>!typeof(Delegate).IsAssignableFrom(f.FieldType)&&f.Name!=nameof(Database.Subscriptions));

    internal static void Run(string output)
    {
        var passed=new List<string>();
        void Need(bool ok,string why){if(!ok)throw new InvalidDataException(why);}
        void Test(string name,Action run){run();passed.Add(name);}
        (Database Db,Card Original,IdentityMemento Saved) Setup()
        {
            var piles=new[]{new Pile(),new Pile()};var original=new Card();piles[0].Add(original);
            var db=new Database(piles);db.Start();return(db,original,Capture(db));
        }
        Test("regression-model-restoring-id-maps-after-victory-does-not-restore-native-listeners",()=>
        {
            var (db,original,saved)=Setup();db.Piles[0].Add(new Card());db.End();saved.Restore();var missing=new Card();db.Piles[0].Add(missing);
            Need(db.Ids[original]==0&&db.Next==1&&!db.Ids.ContainsKey(missing)&&db.Piles.All(p=>p.Listeners==0),"missing-listener-regression-not-reproduced");
        });
        Test("reattach-missing-piles-keeps-restored-ids-and-counter-and-registers-future-cards",()=>
        {
            var (db,original,saved)=Setup();db.Piles[0].Add(new Card());db.End();saved.Restore();
            Need(db.Attach()==2&&db.Next==1&&db.Ids[original]==0&&db.Cards[0]==original,"repair-reset-existing-identities");
            var a=new Card();var b=new Card();db.Piles[0].Add(a);db.Piles[1].Add(b);
            Need(db.Ids[a]==1&&db.Ids[b]==2&&db.Next==3&&db.Cards[1]==a&&db.Cards[2]==b,"future-cards-not-registered-in-order");
        });
        Test("repeat-repair-and-duplicate-source-piles-do-not-add-duplicate-listeners",()=>
        {
            var (db,_,saved)=Setup();db.End();saved.Restore();Need(db.Attach()==2&&db.Attach()==0,"repair-not-idempotent");
            var added=PileSubscriptionRepair.AttachMissing(db.Piles.Concat(db.Piles),db.Subscriptions.Select(s=>s.Pile),_=>throw new Exception("duplicate-attach"));
            Need(added==0&&db.Piles.All(p=>p.Listeners==1)&&db.Subscriptions.Count==2,"duplicate-listeners-created");
            db.End();Need(db.Piles.All(p=>p.Listeners==0)&&db.Subscriptions.Count==0,"new-handles-not-cleaned-up");
        });
        Test("repeated-victory-and-restore-cycles-rebind-current-handles-without-id-drift",()=>
        {
            var (db,original,saved)=Setup();
            for(var i=0;i<3;i++)
            {
                db.End();saved.Restore();Need(db.Attach()==2&&db.Next==1&&db.Ids[original]==0,"cycle-reset-original-card");
                var card=new Card();db.Piles[0].Add(card);Need(db.Ids[card]==1&&db.Next==2&&db.Piles.All(p=>p.Listeners==1),"cycle-card-id-or-listener-drift");
            }
            db.End();Need(db.Piles.All(p=>p.Listeners==0),"cycle-cleanup-left-listeners");
        });
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllBytes(output,Codec.Bytes(new{Result="PASS_PILE_SUBSCRIPTION_REPAIR_AND_IDENTITY_MEMENTO_MODEL_ONLY",Tests=passed,
            ProductionAttachMissingUsed=true,NativeGameObjectsExecuted=false,NativeVictoryRestoreGenerationStillPending=true}));
        Console.WriteLine("PASS_CARD_TRACKING_MODEL_NOT_NATIVE_RUNTIME");
    }
}
