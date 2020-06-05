using System;
using Godot;

public class MapIndividual {

    GeneColumn[] geneMatrix;
    public GeneColumn[] GeneticMatrix { get { return geneMatrix; } }
    int GeneticWidth {get { return geneMatrix.GetLength(0); }}
    int GeneticHeight {get { return geneMatrix.GetLength(1); }}

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

    // Immutable region:
    int immutableLeftIndex;
    int immutableRightIndex;
    int mutableLeftIndex;
    int mutableRightIndex;
    public MapIndividual(GeneColumn[] geneMatrix) {
        this.geneMatrix = (GeneColumn[]) geneMatrix.Clone();
        updateMutabilityIndices(0, GeneticWidth - 1);
    }

    public MapIndividual(GeneColumn[] geneMatrix, int gl = -1, int gr = -1) {
        this.geneMatrix = (GeneColumn[]) geneMatrix.Clone();
        updateMutabilityIndices(gl, gr);
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
        for (int x = 0; x < GeneticWidth; x++) {
            bool hasSpike = hasSpikeAtX(x);
            if (hasSpike) {
                x++;
                spikeLength = 1;
                while(x < GeneticWidth && hasSpikeAtX(x)) {
                    x++;
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
        int previousGroundHeight = groundHeightAtX(0);
        int holeLength = 0;
        for (int x = 1; x < GeneticWidth; x++) {
            if (previousGroundHeight == 0) {
                holeLength++;
                if (holeLength > maxJumpableHole) {
                    groundFitness -= 100;
                }
            } else {
                holeLength = 0;
            }
            var gh = groundHeightAtX(x);
            var ghDiff = Mathf.Abs(previousGroundHeight - gh);
            if (ghDiff > jumpLimit) {
                groundFitness -= 50 * ghDiff;
            } else {
                groundFitness += 10;
            }
            previousGroundHeight = gh;
        }
        return groundFitness;
    }

    private int groundHeightAtX(int x) {
        return geneMatrix[x].GroundHeight;
    }

    private bool hasSpikeAtX(int x) {
        return geneMatrix[x].HasSpike;
    }

    private int obstacleHeightAtX(int x) {
        int extraBlock = hasSpikeAtX(x) ? 1 : 0;
        return groundHeightAtX(x) + extraBlock;
    }

    private bool hasAHoleAtX(int x) {
        return groundHeightAtX(x) == 0;
    }

    public void UpdateGeneticMatrix(GeneColumn[] zeroGM, int gl, int gr) {
        isFitnessUpdated = false;
        // Immutable left and right limits:
        int ili = gl == 0 ? gr + 1 : 0;
        int iri = gr == GeneticWidth - 1 ? gl - 1 : GeneticWidth - 1;
        updateMutabilityIndices(gl, gr, ili, iri);
        for (int x = ili; x <= iri; x++) {
            geneMatrix[x] = zeroGM[x];
        }
    }

    private void updateMutabilityIndices(int gl, int gr)
    {
        this.mutableLeftIndex = gl;
        this.mutableRightIndex = gr;
        if (gl < 0 || gr < 0) {
            this.immutableLeftIndex = 0;
            this.immutableRightIndex = geneMatrix.Length - 1;
        } else {
            int ili = gl == 0 ? gr + 1 : 0;
            int iri = gr == geneMatrix.Length - 1 ? gl - 1 : geneMatrix.Length - 1;
            this.immutableLeftIndex = ili;
            this.immutableRightIndex = iri;
        }
    }

    private void updateMutabilityIndices(int gl, int gr, int ili, int iri)
    {
        this.mutableLeftIndex = gl;
        this.mutableRightIndex = gr;
        this.immutableLeftIndex = ili;
        this.immutableRightIndex = iri;
    }

    internal GeneColumn[] CrossoverWith(MapIndividual parent2)
    {
        Random random = new Random();
        int mutableXRange = mutableRightIndex - mutableLeftIndex + 1;
        int leftPoint = mutableLeftIndex + mutableXRange / 4;
        int rightPoint = mutableRightIndex - mutableXRange / 4;
        var leftParent = random.Next(1) == 0 ? this : parent2;
        var rightParent = leftParent == this ? parent2 : this;

        int maxTries = rightPoint - leftPoint + 1;
        int tries = 0;
        int originalCrossoverXPoint = random.Next(leftPoint, rightPoint);
        int crossoverXPoint = originalCrossoverXPoint;

        int lowestDiff = Mathf.Abs(leftParent.GeneticMatrix[crossoverXPoint].GroundHeight - rightParent.GeneticMatrix[crossoverXPoint].GroundHeight);
        int lowestDiffIndex = crossoverXPoint;
        // Make sure gene combination creates a valid individual
        while (tries < maxTries && !IsCrossOverXValid(crossoverXPoint, leftParent, rightParent)) {
            crossoverXPoint = Mathf.Wrap(crossoverXPoint + 1, leftPoint, rightPoint);
            int diff = Mathf.Abs(leftParent.GeneticMatrix[crossoverXPoint].GroundHeight - rightParent.GeneticMatrix[crossoverXPoint].GroundHeight);
            if (diff < lowestDiff) {
                lowestDiff = diff;
                lowestDiffIndex = crossoverXPoint;
            }
            tries++;
        }

        GeneColumn[] childGenes = new GeneColumn[GeneticWidth];
        // Copy left parent genetic code to child
        for (int x = mutableLeftIndex; x < leftPoint; x++) {
            childGenes[x] = leftParent.GeneticMatrix[x];
        }
        // Copy right parent genetic code to child
        for (int x = leftPoint; x <= rightPoint; x++) {
            childGenes[x] = rightParent.GeneticMatrix[x];
        }

        // Copy immutable genetic region from either parent
        for (int x = immutableLeftIndex; x <= immutableRightIndex; x++) {
            childGenes[x] = leftParent.GeneticMatrix[x];
        }

        if (tries >= maxTries) { // Adjust genes around crossover x point.
            for(int i = mutableLeftIndex; i < mutableRightIndex; i++) {
                childGenes[i].GroundHeight = Mathf.Min(childGenes[i].GroundHeight, jumpLimit);
            }
        }

        return childGenes;
    }

    private bool IsCrossOverXValid(int crossoverXPoint, MapIndividual leftParent, MapIndividual rightParent)
    {
        bool heightOK = Mathf.Abs(leftParent.GeneticMatrix[crossoverXPoint].GroundHeight - rightParent.GeneticMatrix[crossoverXPoint].GroundHeight) < jumpLimit;
        // Maybe check for spikes and clocks too
        return heightOK;
    }

    internal void Mutate(float mutationRate)
    {
        Random r = new Random();
        for (int x = mutableLeftIndex; x <= mutableRightIndex; x++) {
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

    private void mutateClockAtX(int x)
    {
        geneMatrix[x].HasClock = !geneMatrix[x].HasClock;
    }

    private void mutateSpikeAtX(int x)
    {
        geneMatrix[x].HasSpike = !geneMatrix[x].HasSpike;
    }

    Random random = new Random();
    private void mutateGroundHeightAtX(int x)
    {   
        var changeAbs = random.Next(1,3);
        var changeDirection = random.Next(2) == 1 ? 1 : -1;
        var gh = groundHeightAtX(x);
        var shift = changeDirection * changeAbs;
        geneMatrix[x].GroundHeight += shift;
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
        int checkDirection = 1; // 1 to right, -1 to left
        int mutableLength = mutableRightIndex - mutableLeftIndex;
        bool partialMutation = mutableLength != GeneticWidth;
        if (partialMutation && mutableLeftIndex == 0) // It's from right to left
            checkDirection = -1;
        int start = checkDirection == 1 ? mutableLeftIndex : mutableRightIndex;
        int previousIndex = partialMutation ? start - checkDirection : start;
        var previousObstacleHeight = obstacleHeightAtX(previousIndex);
        int previousGroundIndex = -1; // No clear x found
        int previousGroundHeight = -1;
        if (!hasEnemyAtX(previousIndex))
            previousGroundIndex = previousIndex;
        else if (!hasEnemyAtX(start)) previousGroundIndex = start;
        if (previousGroundIndex != -1) previousGroundHeight = groundHeightAtX(previousGroundIndex);
        bool hadSpike = hasSpikeAtX(start);
        int spikeLength = 0;
        for (var x = start; x <= mutableRightIndex && x >= mutableLeftIndex; x += checkDirection) {

            // Check for unreacheable height diffs
            var currentObstacleHeight = obstacleHeightAtX(x);
            var obstacleDiff = currentObstacleHeight - previousObstacleHeight;
            if (Mathf.Abs(obstacleDiff) > jumpLimit) {
                geneMatrix[x].GroundHeight += -obstacleDiff + Mathf.Sign(obstacleDiff);
                currentObstacleHeight = obstacleHeightAtX(x); // Update height
            }

            if (previousGroundHeight != -1) {
                var previousGroundDistance = Mathf.Abs(previousGroundIndex - x); // Horizontal dist.
                previousGroundDistance += Mathf.Abs(currentObstacleHeight - previousGroundHeight); // Approx. vertical dist.

                if (previousGroundDistance > jumpLimit) {
                    geneMatrix[x].GroundHeight += -(currentObstacleHeight - previousGroundHeight);
                    currentObstacleHeight = obstacleHeightAtX(x); // Update height
                }
            }

            // Check for lengthy spikes
            bool hasSpike = hasSpikeAtX(x);
            if (hasSpike) {
                var obsDiff = obstacleHeightAtX(x) - previousObstacleHeight;
                spikeLength += 1 + obsDiff;
                hadSpike = true;
                if (spikeLength > maxSpikeDistance) {
                    mutateSpikeAtX(x); // This toggles spike here
                }
            } else {
                if (spikeLength > maxSpikeDistance || hasAHoleAtX(x)) {
                    if (hadSpike) {
                        mutateSpikeAtX(previousIndex); // This toggles spike here
                    }
                }
                spikeLength = 0;
            }

            // Updates for next iteration
            previousObstacleHeight = obstacleHeightAtX(x);
            hadSpike = hasSpikeAtX(x);
            previousIndex = x;

            if (!hasEnemyAtX(x)) {
                previousGroundIndex = x;
                previousGroundHeight = groundHeightAtX(x);
            }

        }
    }
    private bool hasEnemyAtX(int x) {
        return hasSpikeAtX(x); // || hasCrazyEnemyAtX...
    }
}