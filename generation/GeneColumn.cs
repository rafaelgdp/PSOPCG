using System;
using Godot;

public class GeneColumn {
    int groundHeight = 0;
    public int GroundHeight {
        get { return groundHeight + (HasSpike ? 1 : 0); }
        set { groundHeight = Mathf.Clamp(value, 0, 12); }
        }
    public bool HasSpike = false;
    public bool HasClock = false;

    public int GlobalX = 0;

    public GeneColumn RightColumn = null;
    public GeneColumn LeftColumn = null;

    public GeneColumn(int gX) : this(gX, 0) {}

    public GeneColumn(int gX, int groundHeight) {
        this.GlobalX = gX;
        this.groundHeight = groundHeight;
    }

    public GeneColumn(GeneColumn toBeCopied) {
        this.Clone(toBeCopied);
    }

    public GeneColumn(GeneColumn left, GeneColumn right, int gX, int groundHeight = 0) {
        this.LeftColumn = left;
        this.RightColumn = right;
        this.GlobalX = gX;
        this.groundHeight = groundHeight;
    }

    public bool ClockPlaced = false;

    internal char CellAtY(int y) {
        int localY = Mathf.Abs(y);
        if (localY < GroundHeight) return 'G';
        if (HasSpike && localY == GroundHeight) return 'S';
        if  (HasClock &&
            ((localY == GroundHeight && !HasSpike) ||
            (localY == GroundHeight + 1 && HasSpike)))
                return ClockPlaced ? 'c' : 'C';
        return 'B';
    }

    public void Clone(GeneColumn gc) {
        this.GroundHeight = gc.GroundHeight;
        this.HasSpike = gc.HasSpike;
        this.HasClock = gc.HasClock;
        this.ClockPlaced = gc.ClockPlaced;
        this.GlobalX = gc.GlobalX;
        this.LeftColumn = gc.LeftColumn;
        this.RightColumn = gc.RightColumn;
    }

    public GeneColumn SeekNthColumn(int n) {
        if (n == 0) return this;
        int totalSteps = Mathf.Abs(n);
        GeneColumn iterator = this;
        if (n < 0) {
            for (int i = 0; i < totalSteps; i++) {
                iterator = iterator.LeftColumn;
                if (iterator == null) {
                    return null;
                }
            }
            return iterator;
        } else {
            for (int i = 0; i < totalSteps; i++) {
                iterator = iterator.RightColumn;
                if (iterator == null) {
                    return null;
                }
            }
            return iterator;
        }
    }

}

    public override string ToString() {
        String r = $"GroundHeight: {GroundHeight}, HasSpike: {HasSpike}, HasClock: {HasClock}.";
        return r;
    }
}