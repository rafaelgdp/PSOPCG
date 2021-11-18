using Godot;

public class Global : Node
{

	public static bool IsDebug = true;
	public static float Gravity = 1000F;
	public static float Friction = 1000F;
	public static int Population = 30;
	public static int TilePixelWidth = 64;
	public static float PlayerMaxXSpeed = 500F;
	public static float PlayerJumpForce = -Global.Gravity * 0.59F;
	private static int renderWidth = 50;
	public static int RenderWidth {
		get { return renderWidth; }
		set {
			// TODO: Adjust render width with window size.
			renderWidth = 60;
		}
	}
	private static int geneticWidth = 30;
	public static int GeneticWidth {
		get { return geneticWidth; }
		set {
			geneticWidth = value;
		}
	}

	public static int MaxIterations = 10;
	public static float MutationRate = 0.05F;
	public static float ElitismRate = 0.05F;
	public static bool IsRightGenBusy = true;
	public static bool IsLeftGenBusy = true;

	public static MapGeneratorTest MainScene;
	public static Player Player;
	public static int MappedPlayerX {
		get {
			if (Player != null && MainScene != null && MainScene.tilemap != null) {
				return (int) MainScene.tilemap.WorldToMap(Player.GlobalPosition).x;
			}
			return 0;
		}
	}

	public override void _Input(InputEvent @event) {
		if (@event.IsActionPressed("quit")) {
			GetTree().Quit();
		}
	}

	public static System.Random random = new System.Random();

}
