using Godot;
using System.Collections.Generic;

public class GeneratedTileMap : TileMap
{
    [Export]
    private int cellSize = 64;
    MapGenerator mapGenerator = new MapGenerator(10, 12, 0);
    Dictionary<char, int> tileFromCode = new Dictionary<char, int>() { 
        {'B', -1},
        {'G', 0},
        {'N', 1}
    };
    private int rightChunkLimit = 8;
    private int leftChunkLimit = -8;
    public override void _Ready()
    {
        GD.Print(TileSet.GetTilesIds());
        UpdateWorld();
        GD.Print(TileSet.GetTilesIds());
    }

    private void UpdateWorld() {
        var times = 0;
        for (int i = leftChunkLimit; i < rightChunkLimit; i++) {
            for(int j = 0; j > -mapGenerator.Height; j--) { // Height is inverted
                char cellCode = mapGenerator.GetGlobalCell(i, j);
                SetCell(i, j, tileFromCode[cellCode]);
                times++;
            }
        }
        GD.Print($"Done this {times} times.");
    }

}
