using Godot;
using System.Collections.Generic;

public class Global : Node
{
	public static Dictionary<string, double> Parameters = new Dictionary<string, double>() {
		{ "Population", 30 },
		{ "MaxIterations", 10 },
		{ "ParticleWidth", 30 },
		{ "ParticleReferenceWidth", 10 },
		{ "W", 0.01 },
		{ "Cmin", 0.05 },
		{ "Cmax", 0.35 },
		{ "Rmin", 0.0 },
		{ "Rmax", 0.2 }
	};

	public static bool IsDebug = false;
	public static float Gravity = 1000F;
	public static float Friction = 1000F;
	public static int Population {
		get {
			return (int)Parameters["Population"];
		}

		set {
			Parameters["Population"] = value;
		}
	}
	public static int TilePixelWidth = 64;
	public static float PlayerMaxXSpeed = 500F;
	public static float PlayerJumpForce = -Gravity * 0.59F;
	public static int RenderWidth  = 60;
	public static float InitialTimeLeft = 25F;
	public static int ParticleWidth {
		get { return (int) Parameters["ParticleWidth"]; }
		set {
			Parameters["ParticleWidth"] = value;
		}
	}
	public static int MaxIterations {
		get { return (int) Parameters["MaxIterations"]; }
		set {
			Parameters["MaxIterations"] = value;
		}
	}
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
