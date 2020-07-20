using Godot;

public class Player : KinematicBody2D
{
    [Signal]
    public delegate void PlayerReady(Player me);

    [Signal]
    public delegate void PlayerExitedScreen();

    [Export]
    public float MaxSpeed = Global.PlayerMaxXSpeed;

    [Export]
    float minSpeed = 10F;

    [Export]
    float jumpForce = Global.PlayerJumpForce;

    [Export]
    float acceleration = 2000F;

    [Export]
    Vector2 defaultSnapVector = Vector2.Down * 4f;
    Vector2 snapVector = Vector2.Zero;
    Vector2 motion = Vector2.Zero;
    float hurtForceIntensity = 500F;

    private bool isFalling { get { return motion.y > 0; } }
    
    public Vector2 Speed { 
            get => motion;
        }

    // Debug
    [Export]
    public bool FallingEnabled = true;

    // Child Nodes
    Node2D flipNodes;
    AnimationPlayer hurtAnimation;
    Timer hurtTimer;
    PackedScene hurtLabelScene;

    public override void _Ready()
    {
        Global.Player = this;
        flipNodes = GetNode<Node2D>("FlipNodes");
        hurtAnimation = GetNode<AnimationPlayer>("HurtAnimationPlayer");
        hurtTimer = GetNode<Timer>("HurtCooldownTimer");
        hurtLabelScene = GD.Load("res://scenes/player/HurtLabel.tscn") as PackedScene;
        EmitSignal("PlayerReady", this);
    }

    public override void _PhysicsProcess(float delta) {
        
        HandleMotion(delta);
        motion = MoveAndSlideWithSnap(motion, snapVector, Vector2.Up, false, 4, Mathf.Deg2Rad(46), false);
        HandleCollision();

    }
    bool canBeHit = true;
    private void HandleCollision() {
        if (canBeHit) {
            for (int i = 0; i < GetSlideCount(); i++) {
                var collision = GetSlideCollision(i);
                GeneratedTileMap collider = collision.Collider as GeneratedTileMap;
                if (collider != null) {
                    var cell = collider.WorldToMap(collision.Position - collision.Normal);
                    var tileId = collider.GetCellv(cell);
                    if (tileId == GeneratedTileMap.TileDictionary['S']) {
                        // Collided with a Spike here!
                        var reactionForce = collision.Normal + (Vector2.Up / 5F);
                        reactionForce = reactionForce.Normalized();
                        motion += reactionForce * hurtForceIntensity;
                        canBeHit = false;
                        hurtAnimation.Play("hurt");

                        // Decreased time animation
                        float amount = (float) GD.RandRange(2.0F, 5.0F);
                        Global.MainScene.TimeLeft -= amount;
                        Label hurtLabel = hurtLabelScene.Instance() as Label;
                        Tween t = new Tween();
                        Global.MainScene.AddChild(t);
                        hurtLabel.Visible = true;
                        hurtLabel.RectPosition = GlobalPosition;
                        t.InterpolateProperty(hurtLabel, "rect_position", hurtLabel.RectPosition, hurtLabel.RectPosition + (Vector2.Up * 40F), 0.9F);
                        t.Connect("tween_all_completed", hurtLabel, nameof(hurtLabel.QueueFree));
                        t.Start();

                        // Start timer that resets to "hurtable" state
                        hurtTimer.Start();
                        break;
                    }
                }
            }
        }
    }

    private void onHurtCooldownTimerTimeout() {
        canBeHit = true;
        hurtAnimation.Play("normal");
    }

    private Vector2 scaleLeft = new Vector2(-1, 1);
    private Vector2 scaleRight = new Vector2(1, 1);
    private void HandleAnimation(float frameMotionX) {
        if (frameMotionX < 0F) {
            flipNodes.Scale = scaleLeft;
        } else if (frameMotionX > 0F) {
            flipNodes.Scale = scaleRight;
        }
    }

    private void HandleMotion(float delta) {
        var frameMotion = Vector2.Zero;
        var horizontalMotion = GetHorizontalMotion();
        frameMotion.x += horizontalMotion * acceleration * delta;
        frameMotion.y += Global.Gravity * delta;
        
        motion += frameMotion;
        motion.x = Mathf.Clamp(motion.x, -MaxSpeed, MaxSpeed);
        motion.y = Mathf.Clamp(motion.y, -Mathf.Abs(jumpForce), Mathf.Abs(jumpForce));
        
        HandleAnimation(frameMotion.x);

        if (IsOnFloor()) {
            snapVector = Vector2.Zero;
            if (Input.IsActionJustPressed("jump")) {
                motion.y = jumpForce;
            }
        } else {
            snapVector = Vector2.Down * 1F;
        }

        if (Input.IsActionJustReleased("jump") && !isFalling) {
            motion.y /= 4;
        }

        if (Mathf.Abs(horizontalMotion) < 0.2) { // no horizontal movement pressed
            // Apply friction
            motion.x += -Mathf.Sign(motion.x) * Global.Friction * delta;
            if (Mathf.Abs(motion.x) < minSpeed)
                motion.x = 0F;
        }

        if (FallingEnabled == false) {
            motion.y = (Input.GetActionStrength("ui_down") - Input.GetActionStrength("ui_up")) * MaxSpeed;
        }

    }

    private void OnExitedScreen() {
        EmitSignal("PlayerExitedScreen");
    }
    float GetHorizontalMotion() {
        return Input.GetActionStrength("ui_right") - Input.GetActionStrength("ui_left");
    } 
}
