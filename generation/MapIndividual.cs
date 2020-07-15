using System;
using Godot;

public class MapIndividual {

    public GeneColumn Head;
    public GeneColumn Tail;
    public GeneColumn GenerationHead;
    public GeneColumn GenerationTail;
    public bool IsLeftGeneration;
    public int Width;
    bool isFitnessUpdated = false;
    int cachedFitness = 0;
    public int Fitness { 
        get {
            if (!isFitnessUpdated) {
                cachedFitness = GetFitness();
            }
            return cachedFitness;
        }
    }

    public bool IsPlayable {
        get {
            // TODO: implement algorithm that determines playability
            return true;
        }
    }

    public MapIndividual(GeneColumn head) {
        this.Head = head;
        var iterator = head;
        if (head.IsMutable) {
            this.GenerationHead = head;
            this.IsLeftGeneration = true;
            while (iterator.Next != null && iterator.Next.IsMutable) { iterator = iterator.Next; }
            this.GenerationTail = iterator;
        } else {
            this.IsLeftGeneration = false;
            while (!iterator.IsMutable) { iterator = iterator.Next; }
            this.GenerationHead = iterator;
            while (iterator.Next != null) { iterator = iterator.Next; }
            this.GenerationTail = iterator;
        }
        while(iterator.Next != null) { iterator = iterator.Next; }
        this.Tail = iterator;
        this.Width = this.Tail.GlobalX - this.Head.GlobalX + 1;
    }

    public MapIndividual(GeneColumn head, GeneColumn generationHead, GeneColumn generationTail, bool isLeftGeneration) {
        
        this.Head = head;
        this.GenerationHead = generationHead;
        this.GenerationTail = generationTail;
        this.IsLeftGeneration = isLeftGeneration;
        
        GeneColumn iterator;
        if (generationTail != null) {
            iterator = GenerationTail;
        } else {
            iterator = Head;
        }
        while(iterator.Next != null) { iterator = iterator.Next; }
        this.Tail = iterator;
        this.Width = this.Tail.GlobalX - this.Head.GlobalX + 1;

    }

    public int GetFitness() {

        int fitness = 10000;

        fitness += getGroundFitness();
        fitness += getSpikeFitness();
        fitness += getClockFitness();
        fitness += getSafeJumpFitness();
        
        cachedFitness = fitness;
        isFitnessUpdated = true;

        return fitness;
    }

    static int maxSpikeDistance = 4;
    static int spikePenalty = 20;
    private int getSpikeFitness() {
        int spikeFitness = 0;
        int spikeLength = 0;
        for (GeneColumn x = this.Head; x != null; x = x.Next) {
            if (x.HasSpike) {
                x = x.Next;
                spikeLength = 1;
                while(x != null && x.HasSpike) {
                    x = x.Next;
                    spikeLength++;
                }
                // Compute fitness penalty
                if (spikeLength > maxSpikeDistance) {
                    spikeFitness -= spikeLength * spikePenalty;
                }
            }
            if (x == null) break;
        }
        return spikeFitness;
    }

    int jumpLimit = 2;
    int maxJumpableHole = 4;

    private int getSafeJumpFitness() {
        int safeJumpFitness = 0;
        GeneColumn currentGC = this.Head;
        while(currentGC != null) {
            
        }

        
        return safeJumpFitness;
    }

    private int getGroundFitness() {
        int groundFitness = 0;
        int holeLength = 0;
        for (GeneColumn x = this.Head.Next; x.Next != null; x = x.Next) {
            if (x.Previous.ObstacleHeight == 0) {
                holeLength++;
                if (holeLength > maxJumpableHole) {
                    groundFitness -= 100;
                }
            } else {
                holeLength = 0;
            }
            var ghDiff = Mathf.Abs(x.Previous.ObstacleHeight - x.ObstacleHeight);
            if (ghDiff > jumpLimit) {
                groundFitness -= 100 * ghDiff;
            } else {
                // The closer they are, the better
                groundFitness += 10 - (2 * ghDiff);
            }
        }
        return groundFitness;
    }

    private int getClockFitness() {
        float idealExtraTime = ((Width) * Global.TilePixelWidth / Global.PlayerMaxSpeed) * 15.0F;
        float extraTime = 0F;

        for (GeneColumn x = this.Head; x != null; x = x.Next) {
            if (x.HasClock) {
                extraTime += x.ClockExtraTime;
            }
        }

        int clockFitness = (int) ((5 * idealExtraTime) - Mathf.Abs(idealExtraTime - extraTime));
        return clockFitness;
    }

    internal MapIndividual CrossoverWith(MapIndividual parent2) {
        Random random = new Random();
        int mutableXRange = this.GenerationTail.GlobalX - this.GenerationHead.GlobalX + 1;
        int leftCrossLimit = (mutableXRange / 3);
        int rightCrossLimit = mutableXRange - (mutableXRange / 3);

        MapIndividual leftParent = random.Next(1) == 0 ? this : parent2;
        MapIndividual rightParent = leftParent == this ? parent2 : this;

        int maxTries = rightCrossLimit - leftCrossLimit + 1;
        int tries = 0;
        int originalCrossoverXPoint = random.Next(leftCrossLimit, rightCrossLimit);
        int crossoverXPoint = originalCrossoverXPoint;
        
        GeneColumn leftParentCrossPoint = leftParent.GenerationHead.SeekNthColumn(crossoverXPoint);
        GeneColumn rightParentCrossPoint = rightParent.GenerationHead.SeekNthColumn(crossoverXPoint);

        int lowestDiff = Mathf.Abs(leftParentCrossPoint.GroundHeight - rightParentCrossPoint.GroundHeight);
        // Make sure gene combination creates a valid individual
        int crossoverJump = 1;
        while (tries < maxTries && !IsCrossOverXValid(leftParentCrossPoint, rightParentCrossPoint)) {
            leftParentCrossPoint = leftParentCrossPoint.SeekNthColumn(crossoverJump);
            rightParentCrossPoint = rightParentCrossPoint.SeekNthColumn(crossoverJump);
            if (leftParentCrossPoint.GlobalX - leftParent.GenerationHead.GlobalX > rightCrossLimit) {
                leftParentCrossPoint = leftParent.GenerationHead.SeekNthColumn(leftCrossLimit);
                rightParentCrossPoint = rightParent.GenerationHead.SeekNthColumn(leftCrossLimit);
            }
            tries++;
        }

        // Here we start cloning the child genes from the left parent
        GeneColumn parentIterator = leftParent.Head; // Used to iterate over left parent
        GeneColumn childHead = new GeneColumn(parentIterator); // This head will be returned in the end
        GeneColumn childIterator = new GeneColumn(parentIterator.Next, childHead, null); // To actually iterate, this is used
        childHead.Next = childIterator;
        parentIterator = parentIterator.Next; // Pointing now to the second column
        // Now, starting from the 3rd column now, iterate until the crossover point is reached
        for (parentIterator = parentIterator.Next; parentIterator != leftParentCrossPoint; parentIterator = parentIterator.Next) {
            // We need to clone the parent's genes, but link to the child neighbor columns.
            GeneColumn nextChildColumn = new GeneColumn(parentIterator, childIterator, null);
            childIterator.Next = nextChildColumn;
            childIterator = nextChildColumn;
        }

        // Copy right parent genetic code to child, starting from crossover point.
        for (parentIterator = rightParentCrossPoint; parentIterator != null; parentIterator = parentIterator.Next) {
            GeneColumn nextChildColumn = new GeneColumn(parentIterator, childIterator, null);
            childIterator.Next = nextChildColumn;
            childIterator = nextChildColumn;
        }
        // Finally, create an individual from the child genetic code!
        MapIndividual childIndividual = new MapIndividual(childHead);
        return childIndividual;
    }

    private bool IsCrossOverXValid(GeneColumn leftParent, GeneColumn rightParent) {
        return Mathf.Abs(leftParent.GroundHeight - rightParent.GroundHeight) < jumpLimit;
    }

    internal void Mutate(float mutationRate)
    {
        Random r = new Random();
        for (   GeneColumn x = GenerationHead;
                x != null && x.GlobalX <= GenerationTail.GlobalX;
                x = x.Next
            ) {
            if (r.NextDouble() < mutationRate) {
                // Ground mutation
                mutateGroundHeightAtX(x);
            }
            if (r.NextDouble() < mutationRate) {
                // Spike mutation
                mutateSpikeAtX(x);
            }
            if (r.NextDouble() < mutationRate) {
                // Clock mutation
                mutateClockAtX(x);
            }
            if (r.NextDouble() < mutationRate) {
                // Clock extra time mutation
                mutateClockExtraTimeAtX(x);
            }
        }
        isFitnessUpdated = false;
    }

    private void mutateClockExtraTimeAtX(GeneColumn x)
    {
        x.ClockExtraTime += (float) random.NextDouble() - 0.5F;
    }

    private void mutateClockAtX(GeneColumn x)
    {
        x.HasClock = !x.HasClock;
    }

    private void mutateSpikeAtX(GeneColumn x)
    {
        x.HasSpike = !x.HasSpike;
    }

    Random random = new Random();
    private void mutateGroundHeightAtX(GeneColumn x)
    {   
        var changeDirection = random.Next(2) == 1 ? 1 : -1;
        var shift = changeDirection;
        x.GroundHeight += shift;
    }

    /*
        This method guarantees that the genetic code renders a playable map.
        The idea is to inspect each consecutive x coordinate starting from 
        the x in the mutable area that touches the immutable area towards 
        the other end of the mutable area. Verifications being made:
            - are consecutive x heights jumpable?
            - are consecutive spikes jumpable?
            - are holes jumpable?
    */
    public void forcePlayability() {

        int spikeLength = 0;
        int currentObstacleHeight = 0;

        for (var x = GenerationHead.Next; x != GenerationTail.Next; x = x.Next) {

            // Check for unreacheable height diffs
            var obstacleDiff = x.ObstacleHeight - x.Previous.ObstacleHeight;
            if (Mathf.Abs(obstacleDiff) > jumpLimit) {
                x.GroundHeight += -obstacleDiff + Mathf.Sign(obstacleDiff);
                currentObstacleHeight = x.ObstacleHeight; // Update height
            }

            // TODO: Check this
            // if (x.Previous.GroundHeight != 0) {
            //     var previousGroundDistance = Mathf.Abs(previousGroundIndex - x); // Horizontal dist.
            //     previousGroundDistance += Mathf.Abs(currentObstacleHeight - previousGroundHeight); // Approx. vertical dist.

            //     if (previousGroundDistance > jumpLimit) {
            //         x.GroundHeight += -(currentObstacleHeight - x.Previous.GroundHeight);
            //         currentObstacleHeight = x.GroundHeight; // Update height
            //     }
            // }

            // Check for lengthy spikes
            if (x.HasSpike) {
                var obsDiff = x.ObstacleHeight - x.Previous.ObstacleHeight;
                spikeLength += 1 + obsDiff;
                if (spikeLength > maxSpikeDistance) {
                    mutateSpikeAtX(x); // This toggles spike here
                }
            } else {
                if (spikeLength > maxSpikeDistance || x.GroundHeight == 0) {
                    if (x.Previous.HasSpike) {
                        mutateSpikeAtX(x.Previous); // This toggles spike here
                    }
                }
                spikeLength = 0;
            }
        }
    }
}