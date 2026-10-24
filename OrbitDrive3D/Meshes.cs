using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace OrbitDrive3D;

// Small procedural mesh toolkit. All models are original; no asset downloads.
internal static class Meshes
{
    private static readonly Dictionary<string, Material> Materials = new();
    public static Material Mat(string hex, bool glow = false)
    {
        string key = hex + glow;
        if (Materials.TryGetValue(key, out var cached)) return cached;
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        brush.Freeze();
        var group = new MaterialGroup();
        group.Children.Add(new DiffuseMaterial(brush));
        if (glow) group.Children.Add(new EmissiveMaterial(brush));
        else group.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromRgb(150, 170, 190)), 45));
        group.Freeze(); Materials[key] = group; return group;
    }

    public static GeometryModel3D Box(double x, double y, double z, double w, double h, double d, string color, bool glow = false)
    {
        double a = x - w / 2, b = x + w / 2, c = y - h / 2, e = y + h / 2, f = z - d / 2, g = z + d / 2;
        var mesh = new MeshGeometry3D();
        Quad(mesh, new(a,c,g), new(b,c,g), new(b,e,g), new(a,e,g));
        Quad(mesh, new(b,c,f), new(a,c,f), new(a,e,f), new(b,e,f));
        Quad(mesh, new(a,e,g), new(b,e,g), new(b,e,f), new(a,e,f));
        Quad(mesh, new(a,c,f), new(b,c,f), new(b,c,g), new(a,c,g));
        Quad(mesh, new(b,c,g), new(b,c,f), new(b,e,f), new(b,e,g));
        Quad(mesh, new(a,c,f), new(a,c,g), new(a,e,g), new(a,e,f));
        return Model(mesh, color, glow);
    }

    public static void Quad(MeshGeometry3D m, Point3D a, Point3D b, Point3D c, Point3D d)
    {
        int i = m.Positions.Count;
        m.Positions.Add(a); m.Positions.Add(b); m.Positions.Add(c); m.Positions.Add(d);
        foreach (int n in new[] {0,1,2,0,2,3}) m.TriangleIndices.Add(i+n);
    }

    public static GeometryModel3D Model(MeshGeometry3D mesh, string color, bool glow = false)
    {
        mesh.Freeze();
        var material = Mat(color, glow);
        var model = new GeometryModel3D(mesh, material) { BackMaterial = material };
        model.Freeze(); return model;
    }

    public static GeometryModel3D Sphere(double x, double y, double z, double radius, string color, bool glow = false, int segments = 12)
    {
        var mesh = new MeshGeometry3D();
        int rows = segments / 2;
        Point3D P(int r, int s)
        {
            double lat = Math.PI * r / rows, lon = Math.PI * 2 * s / segments;
            return new(x + radius*Math.Sin(lat)*Math.Cos(lon), y+radius*Math.Cos(lat), z+radius*Math.Sin(lat)*Math.Sin(lon));
        }
        for(int r=0;r<rows;r++) for(int s=0;s<segments;s++)
            Quad(mesh,P(r,s),P(r+1,s),P(r+1,s+1),P(r,s+1));
        return Model(mesh,color,glow);
    }

    public static GeometryModel3D Ring(double radius, double thickness, string color)
    {
        var mesh = new MeshGeometry3D();
        for(int i=0;i<36;i++)
        {
            double a=i*Math.PI/18, b=(i+1)*Math.PI/18;
            Quad(mesh,new(Math.Cos(a)*radius,Math.Sin(a)*radius,0),new(Math.Cos(b)*radius,Math.Sin(b)*radius,0),
                new(Math.Cos(b)*(radius-thickness),Math.Sin(b)*(radius-thickness),0),new(Math.Cos(a)*(radius-thickness),Math.Sin(a)*(radius-thickness),0));
        }
        return Model(mesh,color,true);
    }

    public static Model3DGroup Car()
    {
        var car = new Model3DGroup();
        car.Children.Add(Box(0,0.63,0,2.12,0.6,4.25,"#AEBBC8"));
        car.Children.Add(Box(0,0.36,0,1.98,0.18,4.1,"#111B2B"));
        // Angular canopy, with a long sloping windshield and pickup-style rear deck.
        var roof = new MeshGeometry3D();
        Quad(roof,new(-1,0.93,1.65),new(1,0.93,1.65),new(0.82,1.58,0.15),new(-0.82,1.58,0.15));
        Quad(roof,new(-0.82,1.58,0.15),new(0.82,1.58,0.15),new(0.85,1.48,-0.5),new(-0.85,1.48,-0.5));
        car.Children.Add(Model(roof,"#8196AA"));
        var glass = new MeshGeometry3D();
        Quad(glass,new(-0.85,1.48,-0.5),new(0.85,1.48,-0.5),new(1,0.94,-1.86),new(-1,0.94,-1.86));
        Quad(glass,new(-1,0.94,-1.86),new(-0.85,1.48,-0.5),new(-0.82,1.58,0.15),new(-1,0.94,1.4));
        Quad(glass,new(1,0.94,1.4),new(0.82,1.58,0.15),new(0.85,1.48,-0.5),new(1,0.94,-1.86));
        car.Children.Add(Model(glass,"#0A2338"));
        car.Children.Add(Box(0,0.96,1.1,1.65,0.035,0.8,"#233143"));
        car.Children.Add(Box(0,0.85,-2.14,1.95,0.075,0.04,"#D8FFFF",true));
        car.Children.Add(Box(0,0.85,2.14,1.96,0.075,0.04,"#FF5575",true));
        for(int side=-1;side<=1;side+=2)
        {
            car.Children.Add(Box(side*1.07,0.47,0,0.04,0.045,3.9,"#66F5EB",true));
            foreach(double z in new[]{-1.34,1.32})
            {
                car.Children.Add(Wheel(side*1.04,0.44,z));
                car.Children.Add(Box(side*0.72,0.27,z,0.44,0.08,0.65,"#52E6FF",true));
            }
        }
        return car;
    }

    private static Model3DGroup Wheel(double x,double y,double z)
    {
        var group=new Model3DGroup();
        var mesh=new MeshGeometry3D();
        for(int i=0;i<12;i++)
        {
            double a=i*Math.PI/6,b=(i+1)*Math.PI/6;
            Quad(mesh,new(x-0.16,y+Math.Sin(a)*0.44,z+Math.Cos(a)*0.44),new(x+0.16,y+Math.Sin(a)*0.44,z+Math.Cos(a)*0.44),
                new(x+0.16,y+Math.Sin(b)*0.44,z+Math.Cos(b)*0.44),new(x-0.16,y+Math.Sin(b)*0.44,z+Math.Cos(b)*0.44));
            Quad(mesh,new(x+0.17,y,z),new(x+0.17,y+Math.Sin(a)*0.44,z+Math.Cos(a)*0.44),new(x+0.17,y+Math.Sin(b)*0.44,z+Math.Cos(b)*0.44),new(x+0.17,y,z));
            Quad(mesh,new(x-0.17,y,z),new(x-0.17,y+Math.Sin(b)*0.44,z+Math.Cos(b)*0.44),new(x-0.17,y+Math.Sin(a)*0.44,z+Math.Cos(a)*0.44),new(x-0.17,y,z));
        }
        group.Children.Add(Model(mesh,"#101722"));
        group.Children.Add(Box(x+Math.Sign(x)*0.18,y,z,0.02,0.37,0.37,"#7D98AB"));
        return group;
    }
}
