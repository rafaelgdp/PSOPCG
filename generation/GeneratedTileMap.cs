using Godot;
using System.Collections.Generic;

public class GeneratedTileMap : TileMap
{
    [Export]
    private int cellSize = 64;
    public GAMapGenerator mMapGenerator = new GAMapGenerator(
        Global.GeneticWidth, // referenceChunkSize
        Global.GeneticWidth, // generationChunkSize
        12,                  // maxHeight
        6,                   // baseXOrigin
        Global.Population,   // populationSize
        Global.MutationRate, // mutationRate
        4);                  // numberOfInitialChunks

    public static Dictionary<char, int> TileDictionary = new Dictionary<char, int>() { 
        {'B', -1},
        {'G', 0},
        {'N', 1},
        {'S', 2},
        {'C', -1},
        {'c', -1}
    };

    // The clock is a special tile.
    // 'C' denotes an unplaced clock.
    // 'c' denotes a placed clock.

    public int RenderChunkWidth { get { return renderChunkWidth; } }
    public int LeftmostGlobalX { get { return leftChunkLimit; }}
    public int RightmostGlobalX { get { return rightChunkLimit; }}
    private int renderChunkWidth = Global.RenderWidth;
    private int leftChunkLimit = 0;
    private int rightChunkLimit { get { return leftChunkLimit + renderChunkWidth; } }
    private HashSet<int> placedXLabels = new HashSet<int>();

    private PackedScene clockScene;
    private PackedScene cellXLabelScene;
    public override void _Ready() {
        clockScene = (PackedScene) GD.Load("res://scenes/powerups/ClockItem.tscn");
        cellXLabelScene = (PackedScene) GD.Load("res://scenes/tilemap/CellXLabel.tscn");
        UpdateWorldAroundX(10);
    }

    public void UpdateWorldAroundX(int x) {
        
        var newLeftChunkLimit = x - (int) (renderChunkWidth / 2);
        trimCells(newLeftChunkLimit);
        leftChunkLimit = newLeftChunkLimit;
        for (int i = leftChunkLimit; i < rightChunkLimit; i++) {
            for(int j = 0; j > -mMapGenerator.MaxHeight; j--) { // Height is inverted
                char cellCode = mMapGenerator.GetGlobalCell(i, j);
                if (cellCode == 'C') {
                    ClockItem clock = (ClockItem) clockScene.Instance();
                    clock.GlobalPosition = MapToWorld(new Vector2(i, j)) + (new Vector2(32F, 32F));
                    AddChild(clock);
                    mMapGenerator.GetGlobalColumn(i).ClockPlaced = true;
                    break; // Not necessary to preceed
                } else {
                    SetCell(i, j, tileFromCode(cellCode));
                }
            }
            if (i % 10 == 0 && !placedXLabels.Contains(i)) {
                // Place a Global X Label
                CellXLabel xlabel = (CellXLabel) cellXLabelScene.Instance();
                xlabel.GlobalX = i;
                xlabel.GlobalPosition = MapToWorld(new Vector2(i, -9)) + (new Vector2(32F, 32F));
                AddChild(xlabel);
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

        int startIndex;
        int endIndex;
        if (newLeftChunkLimit < leftChunkLimit) {
            startIndex = newRightChunkLimit + 1;
            endIndex = rightChunkLimit;
        } else {
            startIndex = leftChunkLimit;
            endIndex = newLeftChunkLimit - 1;
        }
        for (int i = startIndex + 1; i <= endIndex; i++) {
            for(int j = 0; j > -mMapGenerator.MaxHeight; j--) { // Height is inverted
                SetCell(i, j, TileDictionary['B']); // Set outer cells to blank
            }
        }
    }

}
