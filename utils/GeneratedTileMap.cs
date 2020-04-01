using Godot;
using System.Collections.Generic;

public class GeneratedTileMap : TileMap
{
    [Export]
    private int cellSize = 64;
    public GAMapGenerator mMapGenerator = new GAMapGenerator(Global.GeneticWidth, 12, 6, Global.Population, Global.MutationRate);
    // public MapGenerator mMapGenerator = new MapGenerator(100, 12, 10);

    Dictionary<char, int> tileDictionary = new Dictionary<char, int>() { 
        {'B', -1},
        {'G', 0},
        {'N', 1}
    };

    public int RenderChunkWidth { get { return renderChunkWidth; } }
    public int LeftmostGlobalX { get { return leftChunkLimit; }}
    public int RightmostGlobalX { get { return rightChunkLimit; }}
    private int renderChunkWidth = Global.RenderWidth;
    private int leftChunkLimit = 0;
    private int rightChunkLimit { get { return leftChunkLimit + renderChunkWidth; } }
    public override void _Ready() {
        UpdateWorldAroundX(10);
    }

    public void UpdateWorldAroundX(int x) {
        
        var newLeftChunkLimit = x - (int) (renderChunkWidth / 2);
        trimCells(newLeftChunkLimit);
        leftChunkLimit = newLeftChunkLimit;
        for (int i = leftChunkLimit; i < rightChunkLimit; i++) {
            for(int j = 0; j > -mMapGenerator.Height; j--) { // Height is inverted
                char cellCode = mMapGenerator.GetGlobalCell(i, j);
                SetCell(i, j, tileFromCode(cellCode));
            }
        }
    }

    int tileFromCode(char code) {
        if (tileDictionary.ContainsKey(code)) return tileDictionary[code];
        return tileDictionary['B'];
    }

    private void trimCells(int newLeftChunkLimit) {

        var currentCells = GetUsedCells();
        var newRightChunkLimit = newLeftChunkLimit + renderChunkWidth;
        if (newLeftChunkLimit < leftChunkLimit) {
            for (int i = newRightChunkLimit + 1; i <= rightChunkLimit; i++) {
                for(int j = 0; j > -mMapGenerator.Height; j--) { // Height is inverted
                    SetCell(i, j, tileDictionary['B']); // Set outer cells to blank
                }
            }
        } else {
            for (int i = leftChunkLimit; i < newLeftChunkLimit; i++) {
                for(int j = 0; j > -mMapGenerator.Height; j--) { // Height is inverted
                    SetCell(i, j, tileDictionary['B']); // Set outer cells to blank
                }
            }
        }
    }

}
