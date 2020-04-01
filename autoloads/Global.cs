using Godot;
using System;

public class Global : Node
{
    public static float Gravity = 1000F;
    public static float Friction = 650F;
    public static int Population = 30;
    private static int renderWidth = 20;
    public static int RenderWidth {
        get { return renderWidth; }
        set {
            geneticWidth = value * 3;
            renderWidth = value;
        }
    }
    private static int geneticWidth = 60;
    public static int GeneticWidth {
        get { return geneticWidth; }
        set {
            geneticWidth = value;
            renderWidth = value / 3;
        }
    }

    public static int MaxIterations = 10;
    public static float MutationRate = 0.05F;

    public override void _Input(InputEvent @event) {
        if (@event.IsActionPressed("quit")) {
            GetTree().Quit();
        }
    }

}
