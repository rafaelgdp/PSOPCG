using Godot;

public class Global : Node
{

    public static bool IsDebug = true;
    public static float Gravity = 1000F;
    public static float Friction = 1000F;
    public static int Population = 30;
    public static int TilePixelWidth = 64;
    public static float PlayerMaxSpeed = 500F;
    private static int renderWidth = 20;
    public static int RenderWidth {
        get { return renderWidth; }
        set {
            geneticWidth = value * 2;
            renderWidth = value;
        }
    }
    private static int geneticWidth = 10;
    public static int GeneticWidth {
        get { return geneticWidth; }
        set {
            geneticWidth = value;
            renderWidth = value / 2;
        }
    }

    public static int MaxIterations = 10;
    public static float MutationRate = 0.05F;

    public static MapGeneratorTest MainScene;
    public static Player Player;

    public override void _Input(InputEvent @event) {
        if (@event.IsActionPressed("quit")) {
            GetTree().Quit();
        }
    }

}
