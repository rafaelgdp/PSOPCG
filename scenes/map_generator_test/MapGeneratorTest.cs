using Godot;

public class MapGeneratorTest : Node2D
{
    Player player;
    GeneratedTileMap tilemap;

    int generationToleranceOffset = 10;
    int renderToleranceOffset = 10;

    public override void _Ready() {
        player = GetNode<Player>("Player");
        tilemap = GetNode<GeneratedTileMap>("GeneratedTileMap");
        generationToleranceOffset = tilemap.mMapGenerator.Width / 4; // 25% offset tolerance 
        renderToleranceOffset = tilemap.RenderChunkWidth / 4; // 25% offset tolerance 
    }

    public override void _PhysicsProcess(float delta) {

        CheckWorldUpdate();

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

    private void OnPlayerExitedScreen() {
        GetTree().ReloadCurrentScene();
    }
}
