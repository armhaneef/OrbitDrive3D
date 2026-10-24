namespace OrbitDrive3D;

public enum RunStatus { Ready, Running, Paused, Crashed, Complete }
public enum ObjectKind { Barrier, Asteroid, Energy }

public sealed class WorldObject
{
    public int Id { get; init; }
    public ObjectKind Kind { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public double Z { get; init; } // Absolute distance along the route.
    public double Radius { get; init; }
    public bool Collected { get; set; }
}

public readonly record struct PilotInput(double Steering, double Climb, bool Boost);

// Pure gameplay logic: no WPF dependency, so rules can be tested separately.
public sealed class GameState
{
    public const double MissionDistance = 4500;
    public const double MaxAltitude = 90;
    public List<WorldObject> Objects { get; } = new();
    public RunStatus Status { get; private set; } = RunStatus.Ready;
    public double X { get; private set; }
    public double Altitude { get; private set; }
    public double TargetAltitude { get; private set; }
    public double Distance { get; private set; }
    public double Speed { get; private set; } = 38;
    public double Battery { get; private set; } = 100;
    public double Time { get; private set; }
    public int EnergyCells { get; private set; }
    public bool VisitedSpace { get; private set; }
    public bool ReturnedToGround { get; private set; }
    public bool IsBoosting { get; private set; }
    public int Score => (int)Distance + EnergyCells * 250;
    public string Zone => Altitude >= 65 ? "ORBIT" : Altitude > 3 ? "SKY" : "GROUND";
    private Random random = new(76);
    private int nextId;
    private double nextSpawn;
    private double hopTime;

    public void Start()
    {
        X = Altitude = TargetAltitude = Distance = Time = 0;
        Speed = 38; Battery = 100; EnergyCells = 0;
        VisitedSpace = ReturnedToGround = IsBoosting = false;
        hopTime = 0; nextId = 0; nextSpawn = 170;
        random = new Random(76); Objects.Clear();
        Status = RunStatus.Running;
        Populate();
    }

    public void ToggleFlight()
    {
        if (Status != RunStatus.Running) return;
        TargetAltitude = TargetAltitude > 0 ? 0 : 74;
        hopTime = 0;
    }

    public void Hop()
    {
        if (Status == RunStatus.Running && Altitude < 0.15 && TargetAltitude == 0)
            hopTime = 1.7;
    }

    public void TogglePause()
    {
        if (Status == RunStatus.Running) { Status = RunStatus.Paused; IsBoosting = false; }
        else if (Status == RunStatus.Paused) Status = RunStatus.Running;
    }

    public void Update(double dt, PilotInput input)
    {
        if (Status != RunStatus.Running || dt <= 0) return;
        dt = Math.Min(dt, 0.05); // Avoid large jumps after a stalled frame.
        double oldDistance = Distance, oldX = X, oldAltitude = Altitude;
        Time += dt;
        X = Math.Clamp(X + Math.Clamp(input.Steering, -1, 1) * 11 * dt, -8, 8);
        if (input.Climb != 0)
        {
            TargetAltitude = Math.Clamp(TargetAltitude + input.Climb * 23 * dt, 0, MaxAltitude);
            hopTime = 0;
        }
        if (hopTime > 0)
        {
            hopTime = Math.Max(0, hopTime - dt);
            Altitude = Math.Sin((1.7 - hopTime) / 1.7 * Math.PI) * 7;
            if (hopTime == 0) Altitude = 0;
        }
        else Altitude = MoveTowards(Altitude, TargetAltitude, 16 * dt);

        IsBoosting = input.Boost && Battery > 2;
        Battery = Math.Clamp(Battery + (IsBoosting ? -24 : 10) * dt, 0, 100);
        double targetSpeed = (38 + Math.Min(Distance / 500, 12)) * (IsBoosting ? 1.65 : 1);
        Speed = MoveTowards(Speed, targetSpeed, 45 * dt);
        Distance += Speed * dt;
        if (Altitude >= 65) VisitedSpace = true;
        if (VisitedSpace && Altitude <= 0.1 && TargetAltitude == 0) ReturnedToGround = true;

        foreach (var o in Objects)
        {
            if (o.Collected) continue;
            // Swept longitudinal interval prevents tunnelling through a thin object.
            if (o.Z < oldDistance - o.Radius - 2 || o.Z > Distance + o.Radius + 2) continue;
            double t = Distance > oldDistance ? Math.Clamp((o.Z - oldDistance) / (Distance - oldDistance), 0, 1) : 1;
            double px = oldX + (X - oldX) * t;
            double py = oldAltitude + (Altitude - oldAltitude) * t + 0.9;
            double dx = Math.Abs(o.X - px), dy = Math.Abs(o.Y - py);
            bool hit = o.Kind == ObjectKind.Energy
                ? dx * dx + dy * dy < 3.5 * 3.5
                : dx < o.Radius + 0.85 && dy < o.Radius + 0.7;
            if (!hit) continue;
            if (o.Kind == ObjectKind.Energy)
            { o.Collected = true; EnergyCells++; Battery = Math.Min(100, Battery + 22); }
            else { Status = RunStatus.Crashed; IsBoosting = false; return; }
        }
        Objects.RemoveAll(o => o.Z < Distance - 25);
        Populate();
        if (Distance >= MissionDistance && VisitedSpace && ReturnedToGround)
        { Status = RunStatus.Complete; IsBoosting = false; }
    }

    private void Populate()
    {
        while (nextSpawn < Distance + 850)
        {
            // One ground lane is blocked; the other two remain available.
            double lane = nextId == 0 ? 0 : (random.Next(3) - 1) * 6;
            Add(ObjectKind.Barrier, lane, 1.3, nextSpawn, 1.3);
            Add(ObjectKind.Energy, lane == 0 ? -6 : 0, 1.5, nextSpawn + 35, 0.65);
            // Sparse sky and orbital hazards can be avoided laterally or vertically.
            Add(ObjectKind.Asteroid, (random.Next(3) - 1) * 6, 24 + random.Next(20), nextSpawn + 20, 2.2);
            double orbitLane = (random.Next(3) - 1) * 6;
            Add(ObjectKind.Asteroid, orbitLane, 73, nextSpawn + 55, 2.7);
            Add(ObjectKind.Energy, orbitLane == 0 ? 6 : 0, 74.9, nextSpawn + 78, 0.65);
            nextSpawn += 150;
        }
    }

    private void Add(ObjectKind kind, double x, double y, double z, double radius) =>
        Objects.Add(new WorldObject { Id = nextId++, Kind = kind, X = x, Y = y, Z = z, Radius = radius });

    private static double MoveTowards(double value, double goal, double step) =>
        value < goal ? Math.Min(value + step, goal) : Math.Max(value - step, goal);
}
