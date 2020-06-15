using System;
using Godot;

public class MapIndividual {

    public GeneColumn leftmostRef;
    public GeneColumn generationLeftRef;
    public GeneColumn generationRightRef;
    public int referenceChunkSize;
    public int generationChunkSize;
    public bool isLeftGeneration;
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

    public MapIndividual(GeneColumn leftmostRef, int referenceChunkSize, int generationChunkSize, bool isLeftGeneration) {
        this.leftmostRef = leftmostRef;
        this.referenceChunkSize = referenceChunkSize;
        this.generationChunkSize = generationChunkSize;
        this.isLeftGeneration = isLeftGeneration;
        GeneColumn iterator = leftmostRef;
        if (isLeftGeneration) {
            this.generationLeftRef = leftmostRef;
            for (int i = 0; i < generationChunkSize; i++) {
                iterator = iterator.RightColumn;
            }
            this.generationRightRef = iterator;
        } else {
            for (int i = 0; i < referenceChunkSize; i++) {
                iterator = iterator.RightColumn;
            }
            this.generationLeftRef = iterator;
            for (int i = 0; i < generationChunkSize - 1; i++) {
                iterator = iterator.RightColumn;
            }
            this.generationRightRef = iterator;
        }
        
    }

    public int GetFitness() {

        int fitness = 1000;

        fitness += getGroundFitness();
        fitness += getSpikeFitness();
        
        cachedFitness = fitness;
        isFitnessUpdated = true;

        return fitness;
    }

    static int maxSpikeDistance = 4;
    static int spikePenalty = 20;
    private int getSpikeFitness() {
        int spikeFitness = 0;
        int spikeLength = 0;
        for (GeneColumn x = this.leftmostRef; x != null; x = x.RightColumn) {
            if (x.HasSpike) {
                x = x.RightColumn;
                spikeLength = 1;
                while(x != null && x.HasSpike) {
                    x = x.RightColumn;
                    spikeLength++;
                }
                // Compute fitness penalty
                if (spikeLength > maxSpikeDistance) {
                    spikeFitness -= spikeLength * spikePenalty;
                }
            }
        }
        return spikeFitness;
    }

    int jumpLimit = 2;
    int maxJumpableHole = 4;
    private int getGroundFitness() {
        int groundFitness = 0;
        int holeLength = 0;

        for (GeneColumn x = this.leftmostRef.RightColumn; x != null; x = x.RightColumn) {
            if (x.LeftColumn.GroundHeight == 0) {
                holeLength++;
                if (holeLength > maxJumpableHole) {
                    groundFitness -= 100;
                }
            } else {
                holeLength = 0;
            }

            var ghDiff = Mathf.Abs(x.LeftColumn.GroundHeight - x.GroundHeight);
            if (ghDiff > jumpLimit) {
                groundFitness -= 100 * ghDiff;
            } else {
                groundFitness += 10;
            }
        }
        return groundFitness;
    }

    internal GeneColumn CrossoverWith(MapIndividual parent2)
    {
        Random random = new Random();
        int mutableXRange = this.generationChunkSize;
        int leftPoint = (mutableXRange / 8);
        int rightPoint = mutableXRange - (mutableXRange / 8);

        MapIndividual leftParent = random.Next(1) == 0 ? this : parent2;
        MapIndividual rightParent = leftParent == this ? parent2 : this;

        int maxTries = rightPoint - leftPoint + 1;
        int tries = 0;
        int originalCrossoverXPoint = random.Next(leftPoint, rightPoint);
        int crossoverXPoint = originalCrossoverXPoint;
        GeneColumn leftParentCol = leftParent.generationLeftRef.SeekNthColumn(crossoverXPoint);
        GeneColumn rightParentCol = rightParent.generationLeftRef.SeekNthColumn(crossoverXPoint);

        int lowestDiff = Mathf.Abs(leftParentCol.GroundHeight - rightParentCol.GroundHeight);
        int lowestDiffIndex = crossoverXPoint;
        // Make sure gene combination creates a valid individual
        int crossoverJump = 1;
        while (tries < maxTries && !IsCrossOverXValid(leftParentCol, rightParentCol)) {
            leftParentCol = leftParentCol.SeekNthColumn(crossoverJump);
            rightParentCol = rightParentCol.SeekNthColumn(crossoverJump);
            crossoverJump = -Mathf.Sign(crossoverJump) * (Mathf.Abs(crossoverJump) + 1);
            tries++;
        }

        GeneColumn childGenes = new GeneColumn(leftParent.leftmostRef);
        GeneColumn childLeftRef = childGenes;
        // Copy left parent genetic code to child
        for (GeneColumn x = leftParent.leftmostRef.RightColumn; x != leftParentCol; x = x.RightColumn) {
            GeneColumn temp = new GeneColumn(x);
            childGenes.RightColumn = temp;
            temp.LeftColumn = childGenes;
            childGenes = temp;
        }

        // Copy right parent genetic code to child
        for (GeneColumn x = rightParentCol; x != null; x = x.RightColumn) {
            GeneColumn temp = new GeneColumn(x);
            childGenes.RightColumn = temp;
            temp.LeftColumn = childGenes;
            childGenes = temp;
        }

        return childLeftRef;
    }

    private bool IsCrossOverXValid(GeneColumn leftParent, GeneColumn rightParent) {
        return Mathf.Abs(leftParent.GroundHeight - rightParent.GroundHeight) < jumpLimit;
    }

    internal void Mutate(float mutationRate)
    {
        Random r = new Random();
        for (GeneColumn x = generationLeftRef; x != null; x = x.RightColumn) {
            if (r.NextDouble() < mutationRate) {
                // Ground mutation
                mutateGroundHeightAtX(x);
            }
            if (r.NextDouble() < mutationRate) {
                // Spike mutation
                mutateSpikeAtX(x);
            }
            if (r.NextDouble() < mutationRate) {
                // Spike mutation
                mutateClockAtX(x);
            }
        }
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
        for (var x = generationLeftRef.RightColumn; x != generationRightRef.RightColumn; x = x.RightColumn) {

            // Check for unreacheable height diffs
            var obstacleDiff = x.GroundHeight - x.LeftColumn.GroundHeight;
            if (Mathf.Abs(obstacleDiff) > jumpLimit) {
                x.GroundHeight += -obstacleDiff + Mathf.Sign(obstacleDiff);
                currentObstacleHeight = x.GroundHeight; // Update height
            }

            if (x.LeftColumn.GroundHeight != 0) {
                var previousGroundDistance = Mathf.Abs(previousGroundIndex - x); // Horizontal dist.
                previousGroundDistance += Mathf.Abs(currentObstacleHeight - previousGroundHeight); // Approx. vertical dist.

                if (previousGroundDistance > jumpLimit) {
                    x.GroundHeight += -(currentObstacleHeight - x.LeftColumn.GroundHeight);
                    currentObstacleHeight = x.GroundHeight; // Update height
                }
            }

            // Check for lengthy spikes
            if (x.HasSpike) {
                var obsDiff = x.GroundHeight - x.LeftColumn.GroundHeight;
                spikeLength += 1 + obsDiff;
                if (spikeLength > maxSpikeDistance) {
                    mutateSpikeAtX(x); // This toggles spike here
                }
            } else {
                if (spikeLength > maxSpikeDistance || x.GroundHeight == 0) {
                    if (x.LeftColumn.HasSpike) {
                        mutateSpikeAtX(x.LeftColumn); // This toggles spike here
                    }
                }
                spikeLength = 0;
            }
        }
    }
}