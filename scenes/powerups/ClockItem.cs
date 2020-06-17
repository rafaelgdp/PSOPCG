using Godot;
using System;

public class ClockItem : Area2D
{
    public float ExtraTime = 10F;
    public int GeneMatrixX = 0;
    public int GeneMatrixY = 0;
    public GeneColumn SourceGeneColumn = null;
    private bool picked = false;
    private CPUParticles2D particles;
    private CollisionShape2D collision;
    private Sprite sprite;
    private AnimationPlayer animation;
    private Label extraTimeLabel;
    private Timer destroyCheckTimer;
    public void Init(int x, int y) {
        this.GeneMatrixX = x;
        this.GeneMatrixY = y;
    }

    public override void _Ready() {
        particles = GetNode("CPUParticles2D") as CPUParticles2D;
        collision = GetNode("CollisionShape2D") as CollisionShape2D;
        sprite = GetNode("Sprite") as Sprite;
        extraTimeLabel = GetNode("ExtraTimeLabel") as Label;
        destroyCheckTimer = GetNode("DestroyCheckTimer") as Timer;
        destroyCheckTimer.Stop();
        animation = GetNode("AnimationPlayer") as AnimationPlayer;
        animation.Play("waiting_pick");
    }

    private void onClockItemBodyEntered(PhysicsBody body) {
        if (body.IsInGroup("player")) {
            picked = true;
            Global.MainScene.TimeLeft += ExtraTime;
            particles.Emitting = true;
            collision.SetDeferred("disabled", true);
            Tween t = new Tween();
            t.InterpolateProperty(sprite, "modulate", sprite.Modulate, Colors.Transparent, 1F);
            extraTimeLabel.Text = $"+{ExtraTime:0.0}";
            extraTimeLabel.Visible = true;
            t.InterpolateProperty(extraTimeLabel, "rect_position", extraTimeLabel.RectPosition, extraTimeLabel.RectPosition + (Vector2.Up * 40F), 2F);
            AddChild(t);
            t.Connect("tween_all_completed", this, nameof(destroy));
            t.Start();
        }
    }

    private void onVisibilityNotifier2DScreenExited() {
        destroyCheckTimer.Start();
    }

    private void onVisibilityNotifier2DScreenEntered() {
        destroyCheckTimer.Stop();
    }

    float destroyDistance = 1000F;
    private void onDestroyCheckTimerTimeout() {
        if (GlobalPosition.DistanceTo(Global.Player.GlobalPosition) > destroyDistance)
            destroy();
    }

    private void destroy() {
        if (SourceGeneColumn != null && !picked) {
            SourceGeneColumn.ClockPlaced = false;
        }
        QueueFree();
    }
}
