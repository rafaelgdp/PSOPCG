using Godot;
using System;

public class Global : Node
{
    public static float Gravity = 1000F;
    public static float Friction = 650F;

    public override void _Input(InputEvent @event) {
        if (@event.IsActionPressed("quit")) {
            GetTree().Quit();
        }
    }

}
