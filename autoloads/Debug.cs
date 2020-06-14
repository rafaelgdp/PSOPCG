using Godot;

public class Debug : Node {

    public static File debug;

    public override void _Ready() {
        initializeDebugFile();
    }

    private static void initializeDebugFile() {
        debug = new File();
        debug.Open("debug.log", File.ModeFlags.Write);
    }

    public static void Log(string s) {
        if (debug == null) initializeDebugFile();
        debug.StoreString(s + "\n");
    }

}