using OrbitDrive3D;

int passed = 0;
void Check(string name, Action test)
{
    test(); passed++; Console.WriteLine("PASS  " + name);
}
void Assert(bool condition, string message)
{
    if(!condition) throw new Exception(message);
}
GameState NewGame() { var g=new GameState(); g.Start(); return g; }
void SafeStep(GameState g,int count,PilotInput input)
{
    for(int i=0;i<count;i++) { g.Objects.Clear(); g.Update(1.0/60,input); }
}
var idle = new PilotInput(0,0,false);

Check("Ready state does not advance",()=>{
    var g=new GameState(); g.Update(1,idle); Assert(g.Distance==0,"Ready moved");
});
Check("Forward movement and dt cap",()=>{
    var g=NewGame(); g.Update(8,idle); Assert(g.Distance>0&&g.Distance<3,"Large frame not capped");
});
Check("Pause freezes simulation and resume restores it",()=>{
    var g=NewGame(); g.TogglePause(); g.Update(0.05,new(1,1,true));
    Assert(g.Distance==0&&g.X==0&&g.Battery==100,"Pause leaked movement");
    g.TogglePause(); g.Update(0.05,idle); Assert(g.Distance>0,"Resume failed");
});
Check("Steering stays inside road bounds",()=>{
    var g=NewGame(); SafeStep(g,200,new(1,0,false)); Assert(g.X==8,"Right limit");
    SafeStep(g,200,new(-1,0,false)); Assert(g.X== -8,"Left limit");
});
Check("Flight reaches orbit and toggled landing reaches zero",()=>{
    var g=NewGame(); g.ToggleFlight(); SafeStep(g,400,idle);
    Assert(g.VisitedSpace&&Math.Abs(g.Altitude-74)<0.01,"No orbit");
    g.ToggleFlight(); SafeStep(g,400,idle);
    Assert(g.ReturnedToGround&&g.Altitude==0,"No landing");
});
Check("Manual altitude clamps at ceiling and floor",()=>{
    var g=NewGame(); SafeStep(g,700,new(0,1,false)); Assert(g.Altitude==90,"Ceiling failed");
    SafeStep(g,700,new(0,-1,false)); Assert(g.Altitude==0,"Floor failed");
});
Check("Jump rises then settles on road",()=>{
    var g=NewGame(); g.Hop(); SafeStep(g,45,idle); Assert(g.Altitude>6,"No jump");
    SafeStep(g,70,idle); Assert(g.Altitude==0,"Jump did not finish");
});
Check("Boost consumes battery; release recharges it",()=>{
    var g=NewGame(); SafeStep(g,120,new(0,0,true));
    double battery=g.Battery; Assert(battery<60&&g.Speed>55,"No boost drain");
    SafeStep(g,60,idle); Assert(g.Battery>battery,"No recharge");
});
Check("Direct barrier collision ends run",()=>{
    var g=NewGame(); g.Objects.Clear();
    g.Objects.Add(new WorldObject{Id=999,Kind=ObjectKind.Barrier,X=0,Y=1.3,Z=4,Radius=1.3});
    g.Update(0.05,idle); Assert(g.Status==RunStatus.Crashed,"Collision missed");
});
Check("Different-lane obstacle is safe",()=>{
    var g=NewGame(); g.Objects.Clear();
    g.Objects.Add(new WorldObject{Id=999,Kind=ObjectKind.Barrier,X=6,Y=1.3,Z=4,Radius=1.3});
    for(int i=0;i<20;i++)g.Update(0.05,idle);
    Assert(g.Status==RunStatus.Running,"False collision");
});
Check("Energy pickup scores only once",()=>{
    var g=NewGame(); g.Objects.Clear();
    g.Objects.Add(new WorldObject{Id=999,Kind=ObjectKind.Energy,X=0,Y=1,Z=3,Radius=0.65});
    for(int i=0;i<10;i++)g.Update(0.016,idle);
    Assert(g.EnergyCells==1&&g.Score>=250,"Pickup duplicate or missing");
});
Check("Restart clears progress and restores seeded course",()=>{
    var g=NewGame(); var first=g.Objects.Select(o=>(o.X,o.Y,o.Z,o.Kind)).ToArray();
    SafeStep(g,30,new(1,1,true)); g.Start();
    Assert(g.Distance==0&&g.X==0&&g.Altitude==0&&g.Battery==100&&g.EnergyCells==0,"Reset failed");
    Assert(first.SequenceEqual(g.Objects.Select(o=>(o.X,o.Y,o.Z,o.Kind))),"Course not reproducible");
});
Check("Travel distance alone cannot complete mission",()=>{
    var g=NewGame(); SafeStep(g,7000,idle);
    Assert(g.Distance>=GameState.MissionDistance&&g.Status==RunStatus.Running,"Mission skipped flight");
});
Check("Orbit, landing and distance complete mission",()=>{
    var g=NewGame(); g.ToggleFlight(); SafeStep(g,350,idle); g.ToggleFlight(); SafeStep(g,350,idle);
    SafeStep(g,7000,idle); Assert(g.Status==RunStatus.Complete,"Mission not completed");
    double distance=g.Distance;g.Update(0.05,idle);Assert(distance==g.Distance,"Complete state moved");
});
Console.WriteLine($"\n{passed} gameplay checks passed.");
