using System.Collections.Generic;
using System;

public class GAMapGenerator : MapGenerator {
    private List<MapIndividual> population;
    private int maxIterations = 10;
    private float mutationRate = 0.05F;
    private float elitism = 0.05F;
    private MapIndividual BestIndividual { get {
        int bestFitness = Int32.MinValue;
        MapIndividual bestIndividual = null;
        foreach (var individual in population) {
            var fitness = individual.GetFitness();
            if (fitness > bestFitness) {
                bestFitness = fitness;
                bestIndividual = individual;
            }
        }
        return bestIndividual;
    }}

    public GAMapGenerator(int width, int height, int center, int populationSize = 100, float mutationRate = 0.05F) :
    base(width, height, center) {
        population = new List<MapIndividual>(populationSize);
        var zeroIndexedMatrix = getZeroIndexedMatrix();
        for (int i = 0; i < populationSize; i++) {
            population.Add(new MapIndividual(matrix));
        }
        this.mutationRate = mutationRate;
    }

    private void initializePopulation(int ili, int iri) {
        var zm = getZeroIndexedMatrix();
        foreach (MapIndividual individual in population) {
            individual.UpdateGeneticMatrix(zm, ili, iri);
        }
    }

    private char[,] getZeroIndexedMatrix() {
        char[,] zm = new char[width + 2, height];
        int zx = 0;
        for (int x = LeftmostGlobalX; x <= RightmostGlobalX; x++) {
            for (int y = 0; y < height; y++) {
                zm[zx, y] = GetGlobalCell(x, y);
            }
            zx++;
        }
        return zm;
    }

    private int getLocalXIndexInZeroMatrixFromGlobalX(int globalX) {
        return getLocalXIndexFromGlobalX(globalX) - leftmostIndex;
    }
    protected override void generateMap(int generationGlobalLeftmostX, int generationGlobalRightmostX) {
        
        // Immutable left and right limits:
        int ili = generationGlobalLeftmostX > LeftmostGlobalX ? 0 : (LeftmostGlobalX - generationGlobalLeftmostX + 1);
        int iri = generationGlobalRightmostX < RightmostGlobalX ? Width - 1 : (generationGlobalLeftmostX - LeftmostGlobalX - 1);
        
        initializePopulation(ili, iri);
        
        // Evolve over generations
        int numberOfChildren = (int) ((1 - elitism) * (float) population.Count);
        MapIndividual[] children = new MapIndividual[numberOfChildren];
        for (int i = 0; i < maxIterations; i++) {
            // Sort population
            population.Sort((x, y) => x.Fitness.CompareTo(y.Fitness));
            // population.Sort(comparison: MapIndividualComparer);
            
            // Crossover
            for (int ci = 0; ci < numberOfChildren; ci++) {
                // Pick parents
                var parent1 = pickParent();
                var parent2 = pickParent();
                // Crossover parents
                char[,] childGenes = crossover(parent1, parent2);
                MapIndividual newChild = new MapIndividual(childGenes, ili, iri);
                children[ci] = newChild;
            }

            // Mutation
            mutatePopulation();
        }

        // Update world matrix with best individual
        var bestIndividual = BestIndividual;
        int zx = 0;
        for (int i = LeftmostGlobalX; i < RightmostGlobalX; i++) {
            for (int j = 0; j < Height; j++) {
                SetGlobalCell(i, j, bestIndividual.GeneticMatrix[zx, j]);
            }
            zx++;
        }
    }

    // private int MapIndividualComparer(MapIndividual x, MapIndividual y)
    // {
    //     var xf = x.GetFitness();
    //     var yf = y.GetFitness();
    //     if (xf < yf) return -1;
    //     if (xf > yf) return 1;
    //     return 0;
    // }

    private void mutatePopulation() {
        foreach (var individual in population) {
            individual.Mutate(mutationRate);
        }
    }

    private char[,] crossover(MapIndividual parent1, MapIndividual parent2)
    {
        return parent1.CrossoverWith(parent2);
    }

    private MapIndividual pickParent()
    {
        var maxFitness = population[population.Count-1].Fitness;
        var minFitness = population[0].Fitness;
        int chosenFitness = random.Next(minFitness, maxFitness);
        int pi = random.Next(population.Count - 1);
        while(population[pi].Fitness < chosenFitness) {
            pi++;
        }
        return population[pi];
    }
}