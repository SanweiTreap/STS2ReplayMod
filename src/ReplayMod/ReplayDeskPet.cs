using Godot;

namespace ReplayMod;

internal sealed class ReplayDeskPet
{
    private static readonly Vector2 Pocket=new(0,-45);
    private static readonly Vector2[] Wheel=[new(-118,-183),new(-59,-205),new(0,-214),new(59,-205),new(118,-183)];
    private readonly Control _root=new(){Name="ReplayDeskPetOverlay",MouseFilter=Control.MouseFilterEnum.Ignore};
    private readonly Control _pet=new(){Name="ReplayDeskPet",MouseFilter=Control.MouseFilterEnum.Ignore};
    private readonly TextureButton _body=new(){Name="PetDragAndHead",IgnoreTextureSize=true,StretchMode=TextureButton.StretchModeEnum.Scale,
        FocusMode=Control.FocusModeEnum.None,MouseDefaultCursorShape=Control.CursorShape.Drag};
    private readonly TextureRect _left,_right,_star;
    private readonly Orb[] _orbs;
    private readonly Art _art;
    private Tween? _motion,_starTween;
    private bool _combat,_expanded,_landed,_moving,_anchored,_held,_dragging,_pressedHead;
    private Vector2 _press,_dragOrigin;

    internal ReplayDeskPet(CanvasLayer layer)
    {
        _art=Art.Load();
        _left=Decoration(_art.Left.Texture);_right=Decoration(_art.Right.Texture);_star=Decoration(_art.Star.Texture);
        _root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _root.AddChild(_pet);_pet.AddChild(_body);
        _orbs=[
            new(_pet,_art.Orbs[0],"|<","上一回合","previous-turn","","e7e962"),
            new(_pet,_art.Orbs[1],"<<","撤回整项","back","2","98eaff"),
            new(_pet,_art.Orbs[2],">","精细前进","fine","6\n[color=#7de6ed]6[/color]","d59bff"),
            new(_pet,_art.Orbs[3],">>","完成整项操作","step","4","f3efff"),
            new(_pet,_art.Orbs[4],">|","下一回合","next-turn","","83ffe2")
        ];
        foreach(var orb in _orbs)orb.Invoked+=Play;
        _pet.AddChild(_left);_pet.AddChild(_right);_pet.AddChild(_star);
        Place(_left,new(-22,-40),new(27,25));Place(_right,new(22,-40),new(27,25));_star.Hide();
        _body.GuiInput+=BodyInput;
        _root.Resized+=Layout;
        SetPose(_art.Grip,146);SetHands(true);
        for(var i=0;i<_orbs.Length;i++)_orbs[i].Node.Position=i==2?Pocket:Wheel[i];
        Dark.Node.Show();layer.AddChild(_root);Layout();
    }

    private Orb Dark=>_orbs[2];
    internal bool Visible {set=>_root.Visible=value;}

    internal void Sync(bool combat,bool temporary,bool fine,bool whole,bool back)
    {
        _combat=combat;
        if(!combat&&_expanded)CloseWheel();
        _orbs[0].Availability(combat&&NativeSeek.CanJumpTurn(next:false),temporary,_landed);
        Dark.Availability(fine,temporary,!_moving||_landed);
        _orbs[1].Availability(back,temporary,_landed);
        _orbs[3].Availability(whole,temporary,_landed);
        _orbs[4].Availability(combat&&NativeSeek.CanJumpTurn(next:true),temporary,_landed);
    }

    private void Play(string command)
    {
        Runtime.Log("desk-pet-control",new{Command=command,Combat=_combat,Expanded=_expanded});
        Runtime.Guard(()=>PlayerLoop.Control(command));
    }

    private void BodyInput(InputEvent input)
    {
        var pointer=_root.GetLocalMousePosition();
        if(input is InputEventMouseButton {ButtonIndex:MouseButton.Left} button)
        {
            if(button.Pressed)
            {
                _held=true;_dragging=false;_pressedHead=OverHead();
                _press=pointer;_dragOrigin=_pet.Position;
            }
            else if(_held)
            {
                _held=false;
                if(!_dragging&&(pointer-_press).Length()<6*_pet.Scale.X&&_pressedHead&&OverHead()&&_combat&&!_moving)
                {
                    Shine(_pet.GetLocalMousePosition());
                    if(_expanded)CloseWheel();else OpenWheel();
                }
                if(_dragging)Runtime.Log("desk-pet-dragged",new{X=_pet.Position.X,Y=_pet.Position.Y,Expanded=_expanded});
                _dragging=false;
            }
            return;
        }
        if(input is not InputEventMouseMotion motion||!_held)return;
        if((motion.ButtonMask&MouseButtonMask.Left)==0){_held=false;_dragging=false;return;}
        var delta=pointer-_press;
        if(!_dragging&&delta.Length()<6*_pet.Scale.X)return;
        _dragging=true;_pet.Position=Clamp(_dragOrigin+delta);
    }

    private bool OverHead()
    {
        var local=_body.GetLocalMousePosition();
        var head=new Rect2(0,0,_body.Size.X,_body.Size.Y*(_expanded?.57f:.63f));
        return head.HasPoint(local);
    }

    private void Layout()
    {
        var size=_root.Size;
        if(size.X<=0||size.Y<=0)return;
        var scale=Math.Min(Math.Clamp(size.Y/1080f,.64f,1.22f),Math.Min(size.X/314,size.Y/260));
        _pet.Scale=Vector2.One*scale;
        if(!_anchored){_pet.Position=new(size.X*.85f,size.Y*.71f);_anchored=true;}
        _pet.Position=Clamp(_pet.Position);
    }

    private Vector2 Clamp(Vector2 position)
    {
        var s=_pet.Scale.X;
        var side=153*s;
        return new(Math.Clamp(position.X,side,_root.Size.X-side),Math.Clamp(position.Y,244*s,_root.Size.Y-8*s));
    }

    private void OpenWheel()
    {
        _expanded=true;_moving=true;_landed=false;
        SetHands(false);SetPose(_art.Flinch,145);
        var motion=_motion=_pet.CreateTween().SetParallel();
        Fly(motion,Pocket,Wheel[2],.32f);
        motion.TweenCallback(Callable.From(()=>SetPose(_art.Fallen,122,14))).SetDelay(.16);
        motion.TweenCallback(Callable.From(()=>_landed=true)).SetDelay(.37);
        motion.TweenCallback(Callable.From(()=>SetPose(_art.Rising,130,7))).SetDelay(.54);
        motion.TweenCallback(Callable.From(()=>{SetPose(_art.Standing,146);_moving=false;})).SetDelay(.77);
        for(var i=0;i<_orbs.Length;i++)
        {
            if(i==2)continue;
            var orb=_orbs[i].Node;orb.Position=Wheel[i]+new Vector2(0,-10);orb.Modulate=Colors.Transparent;orb.Show();
            motion.TweenProperty(orb,"modulate:a",1f,.16).SetDelay(.16);
            motion.TweenProperty(orb,"position",Wheel[i],.19).SetDelay(.16).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        }
        Runtime.Log("desk-pet-wheel",new{Expanded=true,Combat=_combat});
    }

    private void CloseWheel()
    {
        _expanded=false;_landed=false;_moving=true;
        _motion?.Kill();
        var motion=_motion=_pet.CreateTween().SetParallel();
        Fly(motion,Dark.Node.Position,Pocket,.27f);
        for(var i=0;i<_orbs.Length;i++)
        {
            if(i==2)continue;
            var orb=_orbs[i].Node;
            motion.TweenProperty(orb,"modulate:a",0f,.14);
            motion.TweenCallback(Callable.From(orb.Hide)).SetDelay(.14);
        }
        motion.TweenCallback(Callable.From(()=>SetPose(_art.Flinch,145))).SetDelay(.11);
        motion.TweenCallback(Callable.From(()=>{SetPose(_art.Grip,146);SetHands(true);_moving=false;})).SetDelay(.27);
        Runtime.Log("desk-pet-wheel",new{Expanded=false,Combat=_combat});
    }

    private void Fly(Tween tween,Vector2 from,Vector2 to,float duration)
    {
        tween.TweenMethod(Callable.From<float>(t=>
            Dark.Node.Position=from.Lerp(to,t)+new Vector2(61*Mathf.Sin(t*Mathf.Pi),0)),0f,1f,duration)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
    }

    private void Shine(Vector2 point)
    {
        _starTween?.Kill();Place(_star,point,new(29,29));_star.PivotOffset=_star.Size/2;
        _star.Modulate=Colors.White;_star.Scale=Vector2.One*.68f;_star.Show();
        var tween=_starTween=_star.CreateTween().SetParallel();
        tween.TweenProperty(_star,"scale",Vector2.One*1.10f,.21).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_star,"modulate:a",0f,.19).SetDelay(.09);
        tween.TweenCallback(Callable.From(_star.Hide)).SetDelay(.28);
    }

    private void SetHands(bool visible){_left.Visible=visible;_right.Visible=visible;}
    private void SetPose(Sprite pose,float height,float x=0)
    {
        var size=new Vector2(height*pose.Texture.GetWidth()/pose.Texture.GetHeight(),height);
        _body.TextureNormal=pose.Texture;_body.TextureClickMask=pose.Mask;
        Place(_body,new Vector2(x,-height/2),size);
    }

    private static TextureRect Decoration(Texture2D texture)=>new(){Texture=texture,MouseFilter=Control.MouseFilterEnum.Ignore,
        ExpandMode=TextureRect.ExpandModeEnum.IgnoreSize,StretchMode=TextureRect.StretchModeEnum.Scale};
    private static void Place(Control control,Vector2 center,Vector2 size){control.Size=size;control.Position=center-size/2;}

    private sealed class Orb
    {
        private const float Diameter=49;
        private readonly TextureButton _button;
        private readonly Panel _slot,_ring;
        private readonly Label _glyph;
        private readonly RichTextLabel _amount;
        private readonly Line2D _spark;
        private readonly Node2D _shards=new(){Visible=false};
        private readonly string? _command;
        private Tween? _feedback;
        private bool _filled,_available,_ready,_hover;
        internal Control Node{get;}=new(){MouseFilter=Control.MouseFilterEnum.Ignore,Visible=false};
        internal event Action<string>? Invoked;

        internal Orb(Control parent,Sprite art,string glyph,string description,string? command,string amount,string tint)
        {
            _command=command;_filled=command is not null;
            var color=new Color(tint);
            parent.AddChild(Node);
            _slot=Circle(Diameter,new Color(.08f,.10f,.15f,.85f),color with{A=.33f},2);
            _ring=Circle(Diameter+6,Colors.Transparent,color with{A=.72f},1);
            _button=new TextureButton{TextureNormal=art.Texture,TextureDisabled=art.Texture,TextureClickMask=art.Mask,IgnoreTextureSize=true,
                StretchMode=TextureButton.StretchModeEnum.Scale,FocusMode=Control.FocusModeEnum.None,TooltipText=description,
                MouseDefaultCursorShape=Control.CursorShape.PointingHand,Disabled=true};
            _glyph=new Label{Text=glyph,HorizontalAlignment=HorizontalAlignment.Center,VerticalAlignment=VerticalAlignment.Center,
                MouseFilter=Control.MouseFilterEnum.Ignore};
            _glyph.AddThemeFontSizeOverride("font_size",22);_glyph.AddThemeConstantOverride("outline_size",5);
            _glyph.AddThemeColorOverride("font_outline_color",new Color(.10f,.06f,.12f));
            _amount=new RichTextLabel{BbcodeEnabled=true,Text=amount,ScrollActive=false,MouseFilter=Control.MouseFilterEnum.Ignore,
                Position=new(13,amount.Contains('\n')?5:17),Size=new(22,39),Visible=false};
            _amount.AddThemeFontSizeOverride("normal_font_size",13);_amount.AddThemeConstantOverride("outline_size",4);
            _amount.AddThemeColorOverride("font_outline_color",new Color(.08f,.05f,.10f));
            _spark=new Line2D{Width=1.8f,DefaultColor=color,Antialiased=true,Visible=false,
                Points=command switch
                {
                    "back"=>[new(-25,-12),new(-32,-16),new(-27,-4),new(-36,-7)],
                    "fine"=>[new(-21,-16),new(-27,-8),new(-29,2),new(-27,10)],
                    _=>[new(13,-20),new(23,-28),new(19,-16),new(28,-22)]
                }};
            Node.AddChild(_slot);Node.AddChild(_button);Node.AddChild(_ring);Node.AddChild(_glyph);Node.AddChild(_amount);Node.AddChild(_spark);
            Node.AddChild(_shards);
            _shards.AddChild(new Polygon2D{Color=color,Polygon=[new(-13,-5),new(-20,-8),new(-16,0)]});
            _shards.AddChild(new Polygon2D{Color=color,Polygon=[new(8,-13),new(14,-20),new(15,-11)]});
            _shards.AddChild(new Polygon2D{Color=color,Polygon=[new(9,13),new(18,13),new(14,20)]});
            Place(_button,Vector2.Zero,new(Diameter,Diameter));_button.PivotOffset=_button.Size/2;
            Place(_glyph,Vector2.Zero,new(Diameter,Diameter));ApplyVisual();
            _button.MouseEntered+=()=>{_hover=true;ApplyHover();};_button.MouseExited+=()=>{_hover=false;ApplyHover();};
            _button.ButtonDown+=()=>{if(_command=="step"&&_available&&_ready)NativeDisplayTrace.MarkStepPointerDown();};
            _button.Pressed+=()=>
            {
                if(_command is null||!_available||!_ready)return;
                Feedback();Invoked?.Invoke(_command);
            };
        }

        internal void Availability(bool available,bool temporary,bool ready)
        {
            _available=available;_ready=ready;_button.Disabled=!available||!ready;
            // A newly permitted action must be reachable even during a live operation.
            // Only a settled loss of permission empties an already visible socket.
            if(_filled!=available&&(available||!temporary))
            {
                _filled=available;ApplyVisual();
                if(!temporary&&Node.IsVisibleInTree())ChangeFill();
            }
            ApplyHover();
        }

        private void ApplyVisual()
        {
            _slot.Visible=!_filled;_button.Visible=_filled;
            _glyph.Modulate=_filled?Colors.White:new Color(.76f,.77f,.84f,.65f);
        }

        private void ApplyHover()
        {
            _ring.Visible=_filled&&_hover&&_ready;
            _amount.Visible=_ring.Visible&&_amount.Text.Length>0;
        }

        private void Feedback()
        {
            ResetFeedback();
            _button.Scale=Vector2.One*1.11f;_button.SelfModulate=new Color(1.22f,1.22f,1.22f);
            _spark.Modulate=Colors.White;_spark.Show();
            var tween=_feedback=Node.CreateTween().SetParallel();
            tween.TweenProperty(_button,"scale",Vector2.One,.18).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(_button,"self_modulate",Colors.White,.18);
            tween.TweenProperty(_spark,"modulate:a",0f,.21);
            tween.TweenCallback(Callable.From(_spark.Hide)).SetDelay(.21);
        }

        private void ChangeFill()
        {
            ResetFeedback();
            var tween=_feedback=Node.CreateTween().SetParallel();
            if(_filled)
            {
                _button.Scale=Vector2.One*.82f;
                tween.TweenProperty(_button,"scale",Vector2.One,.17).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            }
            else
            {
                _shards.Scale=Vector2.One;_shards.Modulate=Colors.White;_shards.Show();
                tween.TweenProperty(_shards,"scale",Vector2.One*1.55f,.19);
                tween.TweenProperty(_shards,"modulate:a",0f,.19);
                tween.TweenCallback(Callable.From(_shards.Hide)).SetDelay(.19);
            }
        }

        private void ResetFeedback()
        {
            _feedback?.Kill();_spark.Hide();_shards.Hide();
            _button.Scale=Vector2.One;_button.SelfModulate=Colors.White;
        }

        private static Panel Circle(float diameter,Color fill,Color border,int thickness)
        {
            var style=new StyleBoxFlat{BgColor=fill,BorderColor=border};
            style.SetBorderWidthAll(thickness);style.SetCornerRadiusAll((int)(diameter/2));
            var panel=new Panel{MouseFilter=Control.MouseFilterEnum.Ignore};
            panel.AddThemeStyleboxOverride("panel",style);
            Place(panel,Vector2.Zero,new(diameter,diameter));
            return panel;
        }
    }

    private sealed record Sprite(ImageTexture Texture,Bitmap Mask);
    private sealed record Art(Sprite Grip,Sprite Left,Sprite Right,Sprite Flinch,Sprite Fallen,Sprite Rising,Sprite Standing,Sprite Star,Sprite[] Orbs)
    {
        private static Art? _cached;
        internal static Art Load()=>_cached??=Read();
        private static Art Read()
        {
            using var poses=Sheet("poses");using var orbs=Sheet("orbs");using var grip=Sheet("grip");
            return new(Cut(grip,134,21,685,713),Cut(grip,1030,295,1197,451),Cut(grip,1699,295,1866,451),
                Cut(poses,418,26,662,351),Cut(poses,759,76,1055,351),Cut(poses,1120,68,1401,345),
                Cut(poses,67,372,329,711),Cut(poses,770,775,1038,1029),
                [Cut(orbs,42,178,419,558),Cut(orbs,458,149,859,565),Cut(orbs,892,175,1272,565),
                 Cut(orbs,1310,178,1688,564),Cut(orbs,1724,165,2130,569)]);
        }
        private static Image Sheet(string name)
        {
            using var stream=typeof(ReplayDeskPet).Assembly.GetManifestResourceStream($"ReplayMod.UI001.{name}.png")
                ??throw new InvalidDataException($"desk-pet-sheet-missing-{name}");
            var bytes=new byte[checked((int)stream.Length)];stream.ReadExactly(bytes);
            var image=new Image();
            Codec.Require(image.LoadPngFromBuffer(bytes)==Error.Ok,$"desk-pet-sheet-invalid-{name}");
            return image;
        }
        private static Sprite Cut(Image sheet,int left,int top,int right,int bottom)
        {
            using var image=sheet.GetRegion(new Rect2I(left,top,right-left,bottom-top));
            var mask=new Bitmap();mask.CreateFromImageAlpha(image,.16f);
            return new(ImageTexture.CreateFromImage(image),mask);
        }
    }
}
