using Godot;
using System.Collections.Generic;

public class GeneratedTileMap : TileMap
{
    [Export]
    private int cellSize = 64;
    public GAMapGenerator mMapGenerator = new GAMapGenerator(Global.GeneticWidth, 12, 6, Global.Population, Global.MutationRate);
    // public MapGenerator mMapGenerator = new MapGenerator(100, 12, 10);

    public static Dictionary<char, int> TileDictionary = new Dictionary<char, int>() { 
        {'B', -1},
        {'G', 0},
        {'N', 1},
        {'S', 2},
        {'C', -1},
        {'P', -1}
    };

    // The clock is a special tile.
    // 'C' denotes an unplaced clock.
    // 'P' denotes a placed clock.

    public int RenderChunkWidth { get { return renderChunkWidth; } }
    public int LeftmostGlobalX { get { return leftChunkLimit; }}
    public int RightmostGlobalX { get { return rightChunkLimit; }}
    private int renderChunkWidth = Global.RenderWidth;
    private int leftChunkLimit = 0;
    private int rightChunkLimit { get { return leftChunkLimit + renderChunkWidth; } }

    private PackedScene clockScene;
    public override void _Ready() {
        clockScene = (PackedScene) GD.Load("res://scenes/powerups/ClockItem.tscn");
        UpdateWorldAroundX(10);
    }

    public void UpdateWorldAroundX(int x) {
        
        var newLeftChunkLimit = x - (int) (renderChunkWidth / 2);
        trimCells(newLeftChunkLimit);
        leftChunkLimit = newLeftChunkLimit;
        for (int i = leftChunkLimit; i < rightChunkLimit; i++) {
            for(int j = 0; j > -mMapGenerator.Height; j--) { // Height is inverted
                char cellCode = mMapGenerator.GetGlobalCell(i, j);
                if (cellCode == 'C') {
                    ClockItem clock = (ClockItem) clockScene.Instance();
                    clock.GlobalPosition = MapToWorld(new Vector2(i, j)) + (new Vector2(32F, 32F));
                    AddChild(clock);
                    mMapGenerator.GetGlobalColumn(i).ClockPlaced = true;
                } else {
                    SetCell(i, j, tileFromCode(cellCode));
                }
            }
        }
    }

    int tileFromCode(char code) {
        if (TileDictionary.ContainsKey(code)) return TileDictionary[code];
        return TileDictionary['B'];
    }

    private void trimCells(int newLeftChunkLimit) {

        var currentCells = GetUsedCells();
        var newRightChunkLimit = newLeftChunkLimit + renderChunkWidth;
        if (newLeftChunkLimit < leftChunkLimit) {
            for (int i = newRightChunkLimit + 1; i <= rightChunkLimit; i++) {
                for(int j = 0; j > -mMapGenerator.Height; j--) { // Height is inverted
                    SetCell(i, j, TileDictionary['B']); // Set outer cells to blank
                }
            }
        } else {
            for (int i = leftChunkLimit; i < newLeftChunkLimit; i++) {
                for(int j = 0; j > -mMapGenerator.Height; j--) { // Height is inverted
                    SetCell(i, j, TileDictionary['B']); // Set outer cells to blank
                }
            }
        }
    }

}
