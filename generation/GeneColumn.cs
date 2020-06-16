using System;
using Godot;

public class GeneColumn {
    private static long uid = 0;
    private long myuid = uid++;
    int groundHeight = 0;
    public int GroundHeight {
        get { return groundHeight + (HasSpike ? 1 : 0); }
        set { groundHeight = Mathf.Clamp(value, 0, 12); }
        }
    public bool HasSpike = false;
    public bool HasClock = false;
    private float clockExtraTime = 5F;
    public float ClockExtraTime {
        set { value = Mathf.Clamp(value, 2F, 10F); }
        get { 
                if (HasClock) {
                    return clockExtraTime;
                } else {
                    return 0F;
                }
            }
    }

    public bool IsMutable = true;

    public int GlobalX = 0;

    public GeneColumn Next = null;
    public GeneColumn Previous = null;

    private static Random random = new Random();

    public GeneColumn(GeneColumn toBeCopied) {
        this.Clone(toBeCopied);
    }

    public GeneColumn(GeneColumn toBeCopied, GeneColumn previous, GeneColumn next) : this(toBeCopied) {
        this.Previous = previous;
        this.Next = next;
    }

    public GeneColumn(GeneColumn left, GeneColumn right, int globalX) : this(
        left, right, globalX,
        random.Next(0, 4), // GroundHeight
        random.Next(0, 2) == 0 ? false : true, // HasSpike
        random.Next(0, 2) == 0 ? false : true, // HasClock
        true // is mutable
    ) {}

    public GeneColumn(GeneColumn left, GeneColumn right, int globalX, int groundHeight, bool hasSpike, bool hasClock, bool isMutable) {
        this.Previous = left;
        this.Next = right;
        this.GlobalX = globalX;
        this.GroundHeight = groundHeight;
        this.IsMutable = isMutable;
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
        this.ClockExtraTime = gc.ClockExtraTime;
    }

    public GeneColumn SeekNthColumn(int n) {
        if (n == 0) return this;
        int totalSteps = Mathf.Abs(n);
        GeneColumn iterator = this;
        if (n < 0) {
            for (int i = 0; i < totalSteps; i++) {
                iterator = iterator.Previous;
                if (iterator == null) {
                    return null;
                }
            }
            return iterator;
        } else {
            for (int i = 0; i < totalSteps; i++) {
                iterator = iterator.Next;
                if (iterator == null) {
                    return null;
                }
            }
            return iterator;
        }
    }

    public override string ToString() {
        String r = $"gX: {GlobalX}, uid: {myuid}, GroundHeight: {GroundHeight}, HasSpike: {HasSpike}, HasClock: {HasClock}, ClockExtraTime: {ClockExtraTime}.";
        return r;
    }
}