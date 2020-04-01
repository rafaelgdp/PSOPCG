using Godot;

public class MapGeneratorTest : Node2D
{
    Player player;
    GeneratedTileMap tilemap;
    RichTextLabel debugLabel;

    int generationToleranceOffset = 10;
    int renderToleranceOffset = 10;

    public override void _Ready() {
        player = GetNode<Player>("Player");
        tilemap = GetNode<GeneratedTileMap>("GeneratedTileMap");
        debugLabel = GetNode<RichTextLabel>("DebugUI/DebugLabel");
        generationToleranceOffset = Global.GeneticWidth / 4; // 25% offset tolerance 
        renderToleranceOffset = Global.RenderWidth / 4; // 25% offset tolerance 
    }

    float lastDebugTime = 0F;
    public override void _PhysicsProcess(float delta) {

        CheckWorldUpdate();
        lastDebugTime += delta;
        if (lastDebugTime > 1F) {
            lastDebugTime = 0F;
            debugLabel.BbcodeText = tilemap.mMapGenerator.ToRichTextString();
        }

    }

    private void CheckWorldUpdate() {
        var mappedPlayerPosX = (int) tilemap.WorldToMap(player.GlobalPosition).x;
        
        if (mappedPlayerPosX < (tilemap.mMapGenerator.LeftmostGlobalX + generationToleranceOffset) ||
            mappedPlayerPosX > (tilemap.mMapGenerator.RightmostGlobalX - generationToleranceOffset)) {
            // TODO: Possibly make signals for these
            tilemap.mMapGenerator.RegenerateChunkCenteredAt(mappedPlayerPosX);
        }

        if (mappedPlayerPosX < (tilemap.LeftmostGlobalX + renderToleranceOffset) ||
            mappedPlayerPosX > (tilemap.RightmostGlobalX - renderToleranceOffset)) {
            // TODO: Possibly make signals for these
            tilemap.UpdateWorldAroundX(mappedPlayerPosX);
        }

    }

    public override void _Input(InputEvent e) {
        if (e.IsActionPressed("restart")) {
            GetTree().ReloadCurrentScene();
        }
    }

    private void OnPlayerExitedScreen() {
        GetTree().ReloadCurrentScene();
    }
}
