using System.Windows;

namespace OrbitDrive3D;

internal static class Program
{
    [STAThread]
    public static void Main()
    {
        var app = new Application();
        app.Run(new GameWindow());
    }
}
