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

    public MapIndividual(char[,] geneMatrix, int ili, int iri) {
        this.geneMatrix = (char[,]) geneMatrix.Clone();
        updateMutabilityIndices(ili, iri);
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
    private int getGroundFitness() {
        int groundFitness = 0;
        int previousGroundHeight = groundHeightAtX(0);
        for (int x = 1; x < GeneticWidth; x++) {
            var gh = groundHeightAtX(x);
            if (Mathf.Abs(previousGroundHeight - gh) > jumpLimit) {
                groundFitness -= 100;
            } else {
                groundFitness += 1;
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

    public void UpdateGeneticMatrix(char[,] zeroGM, int ili, int iri) {
        isFitnessUpdated = false;
        updateMutabilityIndices(ili, iri);
        for (int x = ili; x <= iri; x++) {
            for (int y = 0; y < GeneticHeight; y++) {
                geneMatrix[x, y] = zeroGM[x, y];
            }
        }
    }

    private void updateMutabilityIndices(int ili, int iri)
    {
        this.immutableLeftIndex = ili;
        this.immutableRightIndex = iri;
        this.mutableLeftIndex = ili == 0 ? iri + 1 : 0;
        this.mutableRightIndex = iri == GeneticWidth - 1 ? ili - 1 : GeneticWidth - 1;
    }

    internal char[,] CrossoverWith(MapIndividual parent2)
    {
        Random random = new Random();
        int mutableXRange = mutableRightIndex - mutableLeftIndex;
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
        var changeAbs = random.Next(1, 2);
        var changeDirection = random.Next(1) == 1 ? 1 : -1;
        var gh = groundHeightAtX(x);
        var shift = changeDirection * changeAbs;
        for (var y = 0; y < GeneticHeight; y++) {
            var newY = y - shift;
            if (newY >= GeneticHeight) {
                geneMatrix[x,y] = 'B';
            } else if (newY < 0) {
                    geneMatrix[x,y] = 'G';
            } else {
                geneMatrix[x,y] = geneMatrix[x,newY];
            }
        }    
    }
}