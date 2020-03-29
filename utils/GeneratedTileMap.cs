using Godot;
using System.Collections.Generic;

public class GeneratedTileMap : TileMap
{
    [Export]
    private int cellSize = 64;
    public MapGenerator mMapGenerator = new MapGenerator(10, 12, 0);

    Dictionary<char, int> tileFromCode = new Dictionary<char, int>() { 
        {'B', -1},
        {'G', 0},
        {'N', 1}
    };

    public int RenderChunkWidth { get { return renderChunkWidth; } }
    public int LeftmostGlobalX { get { return leftChunkLimit; }}
    public int RightmostGlobalX { get { return rightChunkLimit; }}
    private int renderChunkWidth = 10;
    private int leftChunkLimit = 0;
    private int rightChunkLimit { get { return leftChunkLimit + renderChunkWidth; } }
    public override void _Ready()
    {
        UpdateWorldAroundX(0);
    }

    public void UpdateWorldAroundX(int x) {
        
        var newLeftChunkLimit = x - (int) (renderChunkWidth / 2);
        trimCells(newLeftChunkLimit);
        leftChunkLimit = newLeftChunkLimit;
        for (int i = leftChunkLimit; i < rightChunkLimit; i++) {
            for(int j = 0; j > -mMapGenerator.Height; j--) { // Height is inverted
                char cellCode = mMapGenerator.GetGlobalCell(i, j);
                SetCell(i, j, tileFromCode[cellCode]);
            }
        }
    }

    private void trimCells(int newLeftChunkLimit) {

        var currentCells = GetUsedCells();
        var newRightChunkLimit = newLeftChunkLimit + renderChunkWidth;
        if (newLeftChunkLimit < leftChunkLimit) {
            for (int i = newRightChunkLimit + 1; i <= rightChunkLimit; i++) {
                for(int j = 0; j > -mMapGenerator.Height; j--) { // Height is inverted
                    SetCell(i, j, tileFromCode['B']); // Set outer cells to blank
                }
            }
        } else {
            for (int i = leftChunkLimit; i < newLeftChunkLimit; i++) {
                for(int j = 0; j > -mMapGenerator.Height; j--) { // Height is inverted
                    SetCell(i, j, tileFromCode['B']); // Set outer cells to blank
                }
            }
        }
    }

}
