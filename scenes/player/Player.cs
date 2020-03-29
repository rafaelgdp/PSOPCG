using Godot;

public class Player : KinematicBody2D
{
    [Signal]
    public delegate void PlayerReady(KinematicBody2D me);

    [Signal]
    public delegate void PlayerExitedScreen();

    [Export]
    float maxSpeed = 500F;

    [Export]
    float minSpeed = 10F;

    [Export]
    float jumpForce = -Global.Gravity * 0.59F;

    [Export]
    float acceleration = 2000F;

    [Export]
    Vector2 defaultSnapVector = Vector2.Down * 4f;
    Vector2 snapVector = Vector2.Zero;
    Vector2 motion = Vector2.Zero;
    
    public Vector2 Speed { 
            get => motion;
        }

    // Child Nodes
    Node2D flipNodes;

    public override void _Ready()
    {
        flipNodes = GetNode<Node2D>("FlipNodes");
        EmitSignal("PlayerReady", this);
    }

    public override void _PhysicsProcess(float delta) {
        
        HandleMotion(delta);
        motion = MoveAndSlideWithSnap(motion, snapVector, Vector2.Up, false, 4, Mathf.Deg2Rad(46), false);

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
        motion.x = Mathf.Clamp(motion.x, -maxSpeed, maxSpeed);
        motion.y = Mathf.Clamp(motion.y, -Mathf.Abs(jumpForce), Mathf.Abs(jumpForce));
        
        HandleAnimation(frameMotion.x);

        if (IsOnFloor()) {
            snapVector = Vector2.Zero;
            if (Input.IsActionJustPressed("jump")) {
                GD.Print("Here?");
                motion.y = jumpForce;
            }
        } else {
            snapVector = Vector2.Down * 1F;
        }

        if (Mathf.Abs(horizontalMotion) < 0.2) { // no horizontal movement pressed
            // Apply friction
            motion.x += -Mathf.Sign(motion.x) * Global.Friction * delta;
            if (Mathf.Abs(motion.x) < minSpeed)
                motion.x = 0F;
        }

    }

    private void OnExitedScreen() {
        EmitSignal("PlayerExitedScreen");
    }
    float GetHorizontalMotion() {
        return Input.GetActionStrength("ui_right") - Input.GetActionStrength("ui_left");
    } 
}
