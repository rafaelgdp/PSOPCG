using Godot;
using System;

public class ClockItem : Area2D
{
    private float amount = 10F;
    public int GeneMatrixX = 0;
    public int GeneMatrixY = 0;

    public void Init(int x, int y) {
        this.GeneMatrixX = x;
        this.GeneMatrixY = y;
    }

    public void _on_ClockItem_body_entered(PhysicsBody body) {
        if (body.IsInGroup("player")) {
            Global.MainScene.TimeLeft += amount;
            CPUParticles2D p = GetNode("CPUParticles2D") as CPUParticles2D;
            CollisionShape2D c = GetNode("CollisionShape2D") as CollisionShape2D;
            p.Emitting = true;
            c.Disabled = true;
            Tween t = new Tween();
            Sprite s = GetNode("Sprite") as Sprite;
            t.InterpolateProperty(s, "modulate", s.Modulate, Colors.Transparent, 1F);
            AddChild(t);
            t.Connect("tween_all_completed", this, nameof(destroy));
            t.Start();
        }
    }

    private void destroy() {
        QueueFree();
    }
}
