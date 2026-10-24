using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace OrbitDrive3D;

public sealed class GameWindow : Window
{
    private readonly GameState game = new();
    private readonly HashSet<Key> keys = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private double previousTime;
    private readonly Grid root = new();
    private readonly Viewport3D viewport = new();
    private readonly PerspectiveCamera camera = new() { FieldOfView = 64, NearPlaneDistance = 0.1, FarPlaneDistance = 1500 };
    private readonly Model3DGroup world = new();
    private readonly Model3DGroup obstacleModels = new();
    private readonly Dictionary<int, Model3DGroup> objectModels = new();
    private readonly List<(TranslateTransform3D Transform, double BaseZ)> scenery = new();
    private readonly TranslateTransform3D carPosition = new();
    private readonly AxisAngleRotation3D carRoll = new(new Vector3D(0,0,1),0);
    private readonly AxisAngleRotation3D carPitch = new(new Vector3D(1,0,0),0);
    private readonly ScaleTransform3D flameScale = new(1,1,1);
    private readonly SolidColorBrush skyBottom = new(Color.FromRgb(45,85,111));
    private readonly StarField stars = new();
    private readonly TextBlock score = Label("00000",30,"#F0F8FF",true);
    private readonly TextBlock speed = Label("0",38,"#FFFFFF",true);
    private readonly TextBlock altitude = Label("0 m",26,"#FFFFFF",true);
    private readonly TextBlock zone = Label("GROUND",13,"#78F2DF",true);
    private readonly TextBlock mission = Label("01  LAUNCH  /  02  REACH ORBIT  /  03  LAND",12,"#ACBDCF");
    private readonly TextBlock tip = Label("PRESS ENTER TO START",13,"#D5E7F3");
    private readonly TextBlock bestLabel = Label("PERSONAL BEST  0",11,"#A6B8CC");
    private readonly ProgressBar energyBar = new() { Minimum = 0, Maximum = 100, Value = 100, Height = 6, Foreground = Brush("#78F2DF"), Background = Brush("#26364A"), BorderThickness = new Thickness(0) };
    private readonly ProgressBar distanceBar = new() { Minimum = 0, Maximum = GameState.MissionDistance, Height = 3, Foreground = Brush("#78F2DF"), Background = Brush("#182C40"), BorderThickness = new Thickness(0) };
    private readonly Border overlay = new();
    private readonly TextBlock overlayEyebrow = Label("A NEW KIND OF ROAD TRIP",11,"#78F2DF",true);
    private readonly TextBlock overlayTitle = Label("ORBIT\nDRIVE",68,"#F2F8FF",true);
    private readonly TextBlock overlayBody = Label("The road is only the beginning.\nTake your electric rover from the runway\nto orbit, then bring it home.",16,"#B9CCDD");
    private readonly Button action = new();
    private RunStatus shownStatus = (RunStatus)(-1);
    private int best;
    private bool closeCamera;
    private bool reducedDetail;
    private readonly string saveFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"OrbitDrive3D","best.txt");

    public GameWindow()
    {
        Title = "ORBIT DRIVE 3D | C# Flying Car";
        Width = 1280; Height = 800; MinWidth = 960; MinHeight = 650;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Brush("#050C1A"); FontFamily = new FontFamily("Segoe UI");
        Content = root;
        ReadBest(); BuildScene(); BuildInterface();
        PreviewKeyDown += OnKeyDown;
        PreviewKeyUp += (_,e) => { keys.Remove(e.Key); e.Handled = true; };
        Deactivated += (_,_) => { keys.Clear(); if(game.Status==RunStatus.Running) game.TogglePause(); };
        Loaded += (_,_) => { Focus(); previousTime = clock.Elapsed.TotalSeconds; CompositionTarget.Rendering += OnFrame; };
        Closed += (_,_) => { CompositionTarget.Rendering -= OnFrame; SaveBest(); };
    }

    private void BuildScene()
    {
        var gradient = new LinearGradientBrush();
        gradient.StartPoint = new Point(0,0); gradient.EndPoint = new Point(0,1);
        gradient.GradientStops.Add(new GradientStop(Color.FromRgb(3,8,23),0));
        gradient.GradientStops.Add(new GradientStop(Color.FromRgb(11,26,52),0.5));
        gradient.GradientStops.Add(new GradientStop(skyBottom.Color,1));
        root.Background = gradient;
        root.Children.Add(stars);
        viewport.Camera = camera; viewport.IsHitTestVisible = false; root.Children.Add(viewport);
        world.Children.Add(new AmbientLight(Color.FromRgb(135,150,175)));
        world.Children.Add(new DirectionalLight(Color.FromRgb(225,240,255),new Vector3D(-0.4,-1,-0.5)));
        world.Children.Add(new DirectionalLight(Color.FromRgb(90,170,190),new Vector3D(0.8,-0.2,1)));
        world.Children.Add(Meshes.Box(0,-0.7,-300,1800,0.5,1800,"#0B1C2A"));
        world.Children.Add(Meshes.Sphere(130,150,-650,62,"#315574",false,32));
        world.Children.Add(Meshes.Sphere(114,162,-596,12,"#527886",false,16));
        world.Children.Add(Meshes.Sphere(141,140,-592,16,"#24465C",false,16));

        // Recycle scenery around the player to avoid precision loss and growing memory.
        var rng = new Random(42);
        for(int i=0;i<28;i++)
        {
            var tile = new Model3DGroup();
            tile.Children.Add(Meshes.Box(0,-0.06,0,20,0.1,30,"#142334"));
            foreach(double x in new[]{-10.1,10.1}) tile.Children.Add(Meshes.Box(x,0.02,0,0.13,0.06,30,"#53C4CE",true));
            foreach(double x in new[]{-3.1,3.1}) tile.Children.Add(Meshes.Box(x,0.01,0,0.08,0.04,8,"#73919E"));
            for(int side=-1;side<=1;side+=2)
            {
                tile.Children.Add(Meshes.Box(side*11,0.35,0,0.35,0.7,0.8,"#79FFE6",true));
                double h=3+rng.NextDouble()*24;
                double x=side*(24+rng.NextDouble()*95);
                tile.Children.Add(Meshes.Box(x,h/2,-3,6+rng.Next(8),h,7+rng.Next(12),i%2==0?"#142C40":"#1A3447"));
                tile.Children.Add(Meshes.Box(x,h+0.02,-3,2,0.08,3,"#527C8B"));
                tile.Children.Add(Meshes.Box(x+side*3,h*0.7,1,0.18,h*0.35,0.25,"#547B8C",true));
            }
            double baseZ=30-i*30;
            var move=new TranslateTransform3D(0,0,baseZ);
            tile.Transform=move; scenery.Add((move,baseZ)); world.Children.Add(tile);
        }
        // Distant orbital guide rails give altitude and forward motion visual context.
        for(int i=0;i<14;i++)
        {
            var beacon=new Model3DGroup();
            foreach(double x in new[]{-13.5,13.5})
            {
                beacon.Children.Add(Meshes.Box(x,74,0,0.12,10,0.12,"#20536A",true));
                beacon.Children.Add(Meshes.Box(x,79,0,1.4,0.15,0.15,"#65D8DD",true));
            }
            double baseZ=30-i*60;
            var move=new TranslateTransform3D(0,0,baseZ); beacon.Transform=move;
            scenery.Add((move,baseZ)); world.Children.Add(beacon);
        }
        world.Children.Add(obstacleModels);
        var car = Meshes.Car();
        var pose = new Transform3DGroup();
        pose.Children.Add(new RotateTransform3D(carPitch));
        pose.Children.Add(new RotateTransform3D(carRoll)); pose.Children.Add(carPosition); car.Transform=pose;
        world.Children.Add(car);
        var flames=new Model3DGroup();
        foreach(double x in new[]{-0.72,0.72})
        {
            flames.Children.Add(Meshes.Box(x,0.2,2.9,0.28,0.16,1.6,"#65F8FF",true));
            flames.Children.Add(Meshes.Box(x,0.2,3.6,0.14,0.09,1.3,"#397ECD",true));
        }
        var flamePose=new Transform3DGroup(); flamePose.Children.Add(flameScale); flamePose.Children.Add(carPosition); flames.Transform=flamePose;
        world.Children.Add(flames);
        viewport.Children.Add(new ModelVisual3D { Content=world });
    }

    private void BuildInterface()
    {
        var hud = new Grid { Margin=new Thickness(30,24,30,24), IsHitTestVisible=false };
        root.Children.Add(hud);
        var top = new Grid { VerticalAlignment=VerticalAlignment.Top };
        top.ColumnDefinitions.Add(new ColumnDefinition()); top.ColumnDefinitions.Add(new ColumnDefinition { Width=GridLength.Auto });
        var brand=new StackPanel(); brand.Children.Add(Label("O R B I T  /  D R I V E",18,"#F0F8FF",true));
        brand.Children.Add(Label("ELECTRIC FLIGHT EXPERIMENT   /   01",10,"#83A0B8")); top.Children.Add(brand);
        var scores=new StackPanel { HorizontalAlignment=HorizontalAlignment.Right };
        scores.Children.Add(Label("DISTANCE + ENERGY",10,"#93ABBE")); scores.Children.Add(score); scores.Children.Add(bestLabel);
        Grid.SetColumn(scores,1); top.Children.Add(scores); hud.Children.Add(top);

        var missionPanel=new StackPanel { VerticalAlignment=VerticalAlignment.Top, HorizontalAlignment=HorizontalAlignment.Center, Margin=new Thickness(0,86,0,0), Width=460 };
        mission.TextAlignment=TextAlignment.Center; missionPanel.Children.Add(mission);
        distanceBar.Margin=new Thickness(0,12,0,0); missionPanel.Children.Add(distanceBar); hud.Children.Add(missionPanel);

        var bottom=new Grid { VerticalAlignment=VerticalAlignment.Bottom };
        bottom.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(190)});
        bottom.ColumnDefinitions.Add(new ColumnDefinition()); bottom.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(190)});
        var speedPanel=new StackPanel(); speedPanel.Children.Add(Label("VELOCITY",10,"#8EA9BC"));
        var numberRow=new StackPanel {Orientation=Orientation.Horizontal}; numberRow.Children.Add(speed);
        numberRow.Children.Add(new TextBlock {Text=" km/h",Foreground=Brush("#83A0B8"),VerticalAlignment=VerticalAlignment.Bottom,Margin=new Thickness(0,0,0,8)});
        speedPanel.Children.Add(numberRow); speedPanel.Children.Add(Label("BOOST CAPACITOR",10,"#A6B8C9"));
        energyBar.Margin=new Thickness(0,8,15,0); speedPanel.Children.Add(energyBar); bottom.Children.Add(speedPanel);
        var middle=new StackPanel {VerticalAlignment=VerticalAlignment.Bottom,HorizontalAlignment=HorizontalAlignment.Center};
        tip.TextAlignment=TextAlignment.Center; middle.Children.Add(tip);
        middle.Children.Add(new TextBlock {Text="A / D  STEER     F  FLY / LAND     SPACE  JUMP",FontSize=10,Foreground=Brush("#839FB6"),Margin=new Thickness(0,10,0,0),TextAlignment=TextAlignment.Center});
        Grid.SetColumn(middle,1); bottom.Children.Add(middle);
        var altPanel=new StackPanel {HorizontalAlignment=HorizontalAlignment.Right};
        zone.HorizontalAlignment=HorizontalAlignment.Right; altPanel.Children.Add(zone);
        altitude.HorizontalAlignment=HorizontalAlignment.Right; altPanel.Children.Add(altitude);
        altPanel.Children.Add(Label("E / Q  ALTITUDE",10,"#A6B8C9"));
        altPanel.Children.Add(Label("SHIFT  BOOST    P  PAUSE",10,"#839FB6"));
        Grid.SetColumn(altPanel,2); bottom.Children.Add(altPanel); hud.Children.Add(bottom);

        overlay.Background=Brush("#E8081424"); overlay.CornerRadius=new CornerRadius(18);
        overlay.BorderBrush=Brush("#344C62"); overlay.BorderThickness=new Thickness(1);
        overlay.HorizontalAlignment=HorizontalAlignment.Left; overlay.VerticalAlignment=VerticalAlignment.Center;
        overlay.Margin=new Thickness(48,65,0,60); overlay.Padding=new Thickness(32,26,32,26); overlay.Width=445;
        var content=new StackPanel(); content.Children.Add(overlayEyebrow);
        overlayTitle.LineHeight=66; overlayTitle.Margin=new Thickness(0,14,0,12); content.Children.Add(overlayTitle);
        overlayBody.TextWrapping=TextWrapping.Wrap; overlayBody.LineHeight=24; content.Children.Add(overlayBody);
        var rule=new Border {Height=1,Background=Brush("#34485C"),Margin=new Thickness(0,18,0,15)}; content.Children.Add(rule);
        content.Children.Add(Label("YOUR MISSION",10,"#79EFE0",true));
        content.Children.Add(new TextBlock {Text="Reach 65 m • Return to the road • Travel 4.5 km\nCollect cyan energy cores. Avoid orange hazards.",FontSize=12,Foreground=Brush("#B8CCDC"),Margin=new Thickness(0,7,0,17),LineHeight=20});
        action.Content="START FLIGHT    →"; action.Height=48; action.Background=Brush("#80F2DE");
        action.Foreground=Brush("#071822"); action.FontSize=13; action.FontWeight=FontWeights.Bold;
        action.BorderThickness=new Thickness(0); action.Cursor=Cursors.Hand; action.Focusable=false;
        action.Click+=(_,_)=>StartOrResume(); content.Children.Add(action);
        content.Children.Add(new TextBlock {Text="ENTER  START / RESUME    •    F11  FULLSCREEN\nC  CAMERA    •    L  DETAIL    •    ESC  PAUSE",FontSize=10,Foreground=Brush("#7793A9"),TextAlignment=TextAlignment.Center,Margin=new Thickness(0,14,0,0),LineHeight=17});
        overlay.Child=new ScrollViewer { Content=content, VerticalScrollBarVisibility=ScrollBarVisibility.Auto, HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled };
        root.Children.Add(overlay);
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        var key=e.Key==Key.System?e.SystemKey:e.Key;
        keys.Add(key); e.Handled=true;
        if(e.IsRepeat) return;
        switch(key)
        {
            case Key.Enter: StartOrResume(); break;
            case Key.F: game.ToggleFlight(); break;
            case Key.Space: game.Hop(); break;
            case Key.P: case Key.Escape: game.TogglePause(); break;
            case Key.R: Restart(); break;
            case Key.C: closeCamera=!closeCamera; break;
            case Key.L: reducedDetail=!reducedDetail; break;
            case Key.F11:
                if(WindowStyle==WindowStyle.None) { WindowStyle=WindowStyle.SingleBorderWindow; WindowState=WindowState.Normal; }
                else { WindowStyle=WindowStyle.None; WindowState=WindowState.Maximized; }
                break;
        }
    }

    private void StartOrResume()
    {
        if(game.Status==RunStatus.Paused) game.TogglePause();
        else if(game.Status!=RunStatus.Running) Restart();
        Focus();
    }

    private void Restart()
    {
        SaveBest(); keys.Clear(); objectModels.Clear(); obstacleModels.Children.Clear();
        game.Start(); previousTime=clock.Elapsed.TotalSeconds; Focus();
    }

    private bool Down(params Key[] choices)=>choices.Any(keys.Contains);

    private void OnFrame(object? sender, EventArgs e)
    {
        double now=clock.Elapsed.TotalSeconds;
        double dt=Math.Clamp(now-previousTime,0,0.05); previousTime=now;
        double steer=(Down(Key.D,Key.Right)?1:0)-(Down(Key.A,Key.Left)?1:0);
        double climb=(Down(Key.E,Key.Up)?1:0)-(Down(Key.Q,Key.Down)?1:0);
        game.Update(dt,new PilotInput(steer,climb,Down(Key.LeftShift,Key.RightShift)));
        DrawScene(steer,now,dt); UpdateHud();
    }

    private void DrawScene(double steering,double now,double dt)
    {
        bool running=game.Status==RunStatus.Running;
        double visualTime=game.Status==RunStatus.Ready?now:game.Time;
        double height=game.Altitude;
        double hover=height>0.1?Math.Sin(visualTime*3)*0.07:0;
        carPosition.OffsetX=game.X; carPosition.OffsetY=height+hover;
        double response=1-Math.Exp(-dt*9);
        carRoll.Angle+=( (running?-steering*(height>1?12:4):0)-carRoll.Angle)*response;
        carPitch.Angle+=(Math.Clamp((game.TargetAltitude-height)*0.5,-7,7)-carPitch.Angle)*response;
        flameScale.ScaleZ=game.IsBoosting?2.6:height>1?1.1:0.2;
        flameScale.ScaleY=game.IsBoosting?1.8:height>1?1:0.2;
        double preview=game.Status==RunStatus.Ready?now*8:game.Distance;
        foreach(var item in scenery)
            item.Transform.OffsetZ=Wrap(item.BaseZ+preview+780,840)-780;

        foreach(var item in game.Objects)
        {
            double relative=item.Z-game.Distance;
            if(item.Collected||relative < -20||relative> (reducedDetail?380:760)) continue;
            if(!objectModels.TryGetValue(item.Id,out var model))
            {
                model=MakeObject(item); objectModels.Add(item.Id,model); obstacleModels.Children.Add(model);
            }
            var t=(TranslateTransform3D)model.Transform;
            t.OffsetZ=-relative; t.OffsetY=item.Y+(item.Kind==ObjectKind.Energy?Math.Sin(visualTime*3+item.Id)*0.22:0);
        }
        var visibleIds=game.Objects.Where(o=>!o.Collected && o.Z-game.Distance>=-20 && o.Z-game.Distance<=(reducedDetail?380:760)).Select(o=>o.Id).ToHashSet();
        foreach(int id in objectModels.Keys.Where(id=>!visibleIds.Contains(id)).ToArray())
        { obstacleModels.Children.Remove(objectModels[id]); objectModels.Remove(id); }

        double camZ=closeCamera?7.8:12.5, camY=height+(closeCamera?3.7:5.1);
        double camX=game.X*0.68;
        if(game.Status==RunStatus.Ready) { camX=5.8; camY=3.3; camZ=9.5; }
        camera.Position=new Point3D(camX,camY,camZ);
        var target=new Point3D(game.X*0.9,height+1.4,game.Status==RunStatus.Ready?0:-16);
        camera.LookDirection=target-camera.Position;
        camera.UpDirection=new Vector3D(0,1,0);
        camera.FieldOfView=game.IsBoosting?70:64;
        double orbit=Math.Clamp(height/70,0,1);
        var gradient=(LinearGradientBrush)root.Background;
        gradient.GradientStops[2].Color=Color.FromRgb((byte)(45-39*orbit),(byte)(85-73*orbit),(byte)(111-82*orbit));
        stars.Opacity=0.45+0.55*orbit;
    }

    private static Model3DGroup MakeObject(WorldObject item)
    {
        var group=new Model3DGroup();
        if(item.Kind==ObjectKind.Barrier)
        {
            group.Children.Add(Meshes.Box(0,0,0,2.6,2.6,2.6,"#60453B"));
            group.Children.Add(Meshes.Box(0,0.35,1.32,2.5,0.28,0.04,"#FFAD70",true));
            group.Children.Add(Meshes.Box(0,-0.35,1.32,2.5,0.28,0.04,"#FFAD70",true));
            group.Children.Add(Meshes.Box(0,1.34,0,2.7,0.06,2.7,"#F5B477",true));
        }
        else if(item.Kind==ObjectKind.Asteroid)
        {
            group.Children.Add(Meshes.Sphere(0,0,0,item.Radius,"#786574",false,10));
            group.Children.Add(Meshes.Box(0,0,item.Radius*0.85,item.Radius*1.2,0.13,0.2,"#FFA066",true));
        }
        else
        {
            group.Children.Add(Meshes.Sphere(0,0,0,0.55,"#80FFE3",true,8));
            group.Children.Add(Meshes.Ring(1.3,0.08,"#70F9DE"));
        }
        group.Transform=new TranslateTransform3D(item.X,item.Y,0); return group;
    }

    private void UpdateHud()
    {
        score.Text=game.Score.ToString("D5",CultureInfo.InvariantCulture);
        speed.Text=game.Status==RunStatus.Ready?"0":((int)(game.Speed*3.6)).ToString(CultureInfo.InvariantCulture);
        altitude.Text=$"{game.Altitude:0} m"; zone.Text=game.Zone;
        energyBar.Value=game.Battery; distanceBar.Value=Math.Min(game.Distance,GameState.MissionDistance);
        mission.Text=$"{(game.VisitedSpace?"✓":"○")}  REACH ORBIT     {(game.ReturnedToGround?"✓":"○")}  LAND     {game.Distance/1000:0.0} / 4.5 KM";
        if(game.Status==RunStatus.Running)
        {
            if(game.Distance<140) tip.Text="F  TO LIFT OFF  ·  SPACE TO JUMP";
            else if(game.Altitude<2) tip.Text=game.VisitedSpace?"BACK ON EARTH  ·  KEEP DRIVING":"F  TO FLY  ·  DODGE THE BARRIERS";
            else if(game.Altitude<65) tip.Text=game.TargetAltitude<1?"LANDING  ·  CHECK THE ROAD BELOW":"CLIMBING  ·  E / Q TO ADJUST HEIGHT";
            else tip.Text="ORBIT REACHED  ·  F TO RETURN TO EARTH";
            if(game.IsBoosting) tip.Text="BOOST ACTIVE  /  CAPACITOR DRAINING";
        }
        if(game.Status==shownStatus) return;
        shownStatus=game.Status;
        overlay.Visibility=game.Status==RunStatus.Running?Visibility.Collapsed:Visibility.Visible;
        if(game.Status==RunStatus.Ready) return;
        if(game.Status==RunStatus.Paused)
        {
            overlayEyebrow.Text="FLIGHT ON HOLD"; overlayTitle.Text="TAKE A\nBREATHER.";
            overlayTitle.FontSize=45; overlayTitle.LineHeight=48;
            overlayBody.Text="Your run is paused.\nPress Enter or P to continue.\nR starts a new run."; action.Content="RESUME FLIGHT    →";
        }
        else
        {
            SaveBest();
            bool won=game.Status==RunStatus.Complete;
            overlayEyebrow.Text=won?"GROUND → ORBIT → HOME":"COLLISION DETECTED";
            overlayTitle.Text=won?"WELCOME\nHOME.":"FLIGHT\nENDED.";
            overlayTitle.FontSize=56; overlayTitle.LineHeight=58;
            overlayBody.Text=$"Score  {game.Score:N0}    •    Energy cores  {game.EnergyCells}\nDistance  {game.Distance/1000:0.2} km\n"+(won?"Mission complete. Ready for another flight?":"Steer, jump or fly above the orange hazards.");
            action.Content="TRY ANOTHER FLIGHT    →";
            tip.Text=won?"MISSION COMPLETE":"R  TO RESTART";
        }
    }

    private void ReadBest()
    {
        try { if(File.Exists(saveFile)) int.TryParse(File.ReadAllText(saveFile),out best); }
        catch(IOException) { } catch(UnauthorizedAccessException) { }
        bestLabel.Text=$"PERSONAL BEST  {best:N0}";
    }
    private void SaveBest()
    {
        if(game.Score<=best) return;
        best=game.Score; bestLabel.Text=$"PERSONAL BEST  {best:N0}";
        try { Directory.CreateDirectory(System.IO.Path.GetDirectoryName(saveFile)!); File.WriteAllText(saveFile,best.ToString(CultureInfo.InvariantCulture)); }
        catch(IOException) { } catch(UnauthorizedAccessException) { }
    }
    private static double Wrap(double x,double span)=>(x%span+span)%span;
    private static SolidColorBrush Brush(string hex)=>new((Color)ColorConverter.ConvertFromString(hex));
    private static TextBlock Label(string text,double size,string color,bool bold=false)=>new()
    { Text=text,FontSize=size,Foreground=Brush(color),FontWeight=bold?FontWeights.SemiBold:FontWeights.Normal };
}

internal sealed class StarField : FrameworkElement
{
    private readonly (double X,double Y,double Size)[] points;
    public StarField()
    {
        IsHitTestVisible=false;
        var random=new Random(9);
        points=Enumerable.Range(0,240).Select(_=>(random.NextDouble(),random.NextDouble()*0.85,0.4+random.NextDouble()*1.1)).ToArray();
        SizeChanged+=(_,_)=>InvalidateVisual();
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        foreach(var p in points) dc.DrawEllipse(Brushes.LightSteelBlue,null,new Point(p.X*ActualWidth,p.Y*ActualHeight),p.Size,p.Size);
    }
}
