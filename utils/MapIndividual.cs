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

    int maxSpikeDistance = 2;
    private int getSpikeFitness() {
        return 0;
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
                mutateGroundHeightAtX(x);
            }
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
        for (var y = 0; y < GeneticHeight; y++) {
            if (y < newHeight) {
                geneMatrix[x,y] = 'G';
            } else {
                geneMatrix[x,y] = 'B';
            }
        }    
    }
}