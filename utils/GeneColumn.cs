using System;
using Godot;

public class GeneColumn {
    int groundHeight = 0;
    public int GroundHeight {
        get { return groundHeight; }
        set { groundHeight = Mathf.Clamp(value, 0, 12); }
        }
    public bool HasSpike = false;
    public bool HasClock = false;

    public int GlobalX = 0;
    public int GlobalY = 0;

    public GeneColumn() : this(0) {}
    public GeneColumn(int groundHeight) {
        this.groundHeight = groundHeight;
    }

    public GeneColumn(int gX, int gY, int groundHeight) {
        
    }

    public bool ClockPlaced = false;

    internal char CellAtY(int y) {
        int localY = Mathf.Abs(y);
        if (localY < GroundHeight) return 'G';
        if (localY == GroundHeight && HasSpike) return 'S';
        if  ((localY == GroundHeight && HasClock) ||
            (localY == GroundHeight + 1 && HasSpike && HasClock))
                return ClockPlaced ? 'c' : 'C';
        return 'B';
    }

    public void Clone(GeneColumn gc) {
        this.GroundHeight = gc.GroundHeight;
        this.HasSpike = gc.HasSpike;
        this.HasClock = gc.HasClock;
        this.ClockPlaced = gc.ClockPlaced;
    }
}