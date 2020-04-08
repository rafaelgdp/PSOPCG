using System;
using Godot;

public class MapIndividual {

    char[,] geneMatrix;
    public char[,] GeneticMatrix { get { return geneMatrix; } }
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
    public MapIndividual(char[,] geneMatrix) {
        this.geneMatrix = (char[,]) geneMatrix.Clone();
        updateMutabilityIndices(0, GeneticWidth - 1);
    }

    public MapIndividual(char[,] geneMatrix, int gl = -1, int gr = -1) {
        this.geneMatrix = (char[,]) geneMatrix.Clone();
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
        bool hadSpike = false;
        int spikeLength = 0;
        int previousIndex = 0;
        for (int x = 0; x < GeneticWidth; x++) {
            bool hasSpike = hasSpikeAtX(x);
            if (hasSpike) {
                var obsDiff = obstacleHeightAtX(x) - obstacleHeightAtX(previousIndex);
                spikeLength += 1 + Mathf.Abs(obsDiff);
                hadSpike = true;
            } else if (hadSpike) {
                // Compute fitness penalty
                if (spikeLength > maxSpikeDistance) {
                    spikeFitness -= spikeLength * spikePenalty;
                }
                spikeLength = 0;
            }
            previousIndex = x;
        }
        if (spikeLength > maxSpikeDistance) {
            spikeFitness -= spikeLength * spikePenalty;
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
        int y = 0;
        int groundHeight = 0;
        while (y < GeneticHeight && geneMatrix[x, y] == 'G') {
            y++;
            groundHeight++;
        }
        return groundHeight;
    }

    private bool hasSpikeAtX(int x) {
        int y = groundHeightAtX(x);
        if (y >= GeneticHeight) return false;
        return geneMatrix[x, y] == 'S';
    }

    private int obstacleHeightAtX(int x) {
        int extraBlock = hasSpikeAtX(x) ? 1 : 0;
        return groundHeightAtX(x) + extraBlock;
    }

    private bool hasAHoleAtX(int x) {
        return groundHeightAtX(x) == 0;
    }

    public void UpdateGeneticMatrix(char[,] zeroGM, int gl, int gr) {
        isFitnessUpdated = false;
        // Immutable left and right limits:
        int ili = gl == 0 ? gr + 1 : 0;
        int iri = gr == GeneticWidth - 1 ? gl - 1 : GeneticWidth - 1;
        updateMutabilityIndices(gl, gr, ili, iri);
        for (int x = ili; x <= iri; x++) {
            for (int y = 0; y < GeneticHeight; y++) {
                geneMatrix[x, y] = zeroGM[x, y];
            }
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

    internal char[,] CrossoverWith(MapIndividual parent2)
    {
        Random random = new Random();
        int mutableXRange = mutableRightIndex - mutableLeftIndex + 1;
        int leftPoint = mutableLeftIndex + mutableXRange / 4;
        int rightPoint = mutableRightIndex - mutableXRange / 4;
        int crossoverXPoint = random.Next(leftPoint, rightPoint);
        var leftParent = random.Next(1) == 0 ? this : parent2;
        
        char[,] childGenes = new char[GeneticWidth, GeneticHeight];
        // Copy left parent genetic code to child
        for (int x = mutableLeftIndex; x < leftPoint; x++) {
            for (int y = 0; y < GeneticHeight; y++) {
                childGenes[x, y] = leftParent.GeneticMatrix[x, y];
            }
        }
        // Copy right parent genetic code to child
        for (int x = leftPoint; x <= rightPoint; x++) {
            for (int y = 0; y < GeneticHeight; y++) {
                childGenes[x, y] = leftParent.GeneticMatrix[x, y];
            }
        }

        // Copy immutable genetic region from either parent
        for (int x = immutableLeftIndex; x <= immutableRightIndex; x++) {
            for (int y = 0; y < GeneticHeight; y++) {
                childGenes[x, y] = leftParent.GeneticMatrix[x, y];
            }
        }

        return childGenes;
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
        }
    }

    private void mutateSpikeAtX(int x)
    {
        int y = groundHeightAtX(x);
        if (y >= GeneticHeight) return;
        switch (geneMatrix[x, y]) {
            case 'B':
                geneMatrix[x, y] = 'S';
                break;
            case 'S':
                geneMatrix[x, y] = 'B';
                break;
            case 'C':
                geneMatrix[x, y] = 'S';
                if (y + 1 < GeneticHeight)
                    geneMatrix[x, y + 1] = 'C';
                break;
        }

    }

    Random random = new Random();
    private void mutateGroundHeightAtX(int x)
    {   
        var changeAbs = random.Next(1,3);
        var changeDirection = random.Next(2) == 1 ? 1 : -1;
        var gh = groundHeightAtX(x);
        var shift = changeDirection * changeAbs;
        var newHeight = gh + shift;
        shiftYAtX(x, shift);    
    }

    private void shiftYAtX(int x, int shift) {
        if (shift > 0) {
            for (var y = GeneticHeight - 1; y >= 0; y--) {
                var shiftedY = y - shift;
                if (shiftedY >= 0)
                    geneMatrix[x, y] = geneMatrix[x, shiftedY];
                else
                    geneMatrix[x, y] = 'G';
            }
        } else if (shift < 0) {
            for (var y = 0; y < GeneticHeight; y++) {
                var shiftedY = y - shift;
                if (shiftedY < GeneticHeight)
                    geneMatrix[x, y] = geneMatrix[x, shiftedY];
                else
                    geneMatrix[x, y] = 'B';
            }
        }
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
                shiftYAtX(x, -obstacleDiff + Mathf.Sign(obstacleDiff));
                currentObstacleHeight = obstacleHeightAtX(x); // Update height
            }

            if (previousGroundHeight != -1) {
                var previousGroundDistance = Mathf.Abs(previousGroundIndex - x); // Horizontal dist.
                previousGroundDistance += Mathf.Abs(currentObstacleHeight - previousGroundHeight); // Approx. vertical dist.

                if (previousGroundDistance > jumpLimit) {
                    shiftYAtX(x, -(currentObstacleHeight - previousGroundHeight));
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