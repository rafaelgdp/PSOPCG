using Godot;
using System.Collections.Generic;

public class GeneratedTileMap : TileMap
{
    [Export]
    private int cellSize = 64;
    MapGenerator mapGenerator = new MapGenerator();
    Dictionary<char, int> tileFromCode = new Dictionary<char, int>() { 
        {'B', -1},
        {'G', 0}
    };
    private int chunkSize = 20;
    private int leftChunkLimit = 0;
    public override void _Ready()
    {
        UpdateWorld();
    }

    private void UpdateWorld() {
        for (int i = leftChunkLimit; i < chunkSize; i++) {
            for(int j = 0; j > -mapGenerator.Height; j--) { // Height is inverted
                char cellCode = mapGenerator.GetGlobalCell(i, j);
                SetCell(i, j, tileFromCode[cellCode]);
            }
        }
    }

}
