using System.Collections.Generic;
using System;

public class GAMapGenerator : MapGenerator {
    private List<MapIndividual> population;
    private int populationSize;
    private int maxIterations = Global.MaxIterations;
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
        this.populationSize = populationSize;
        this.mutationRate = mutationRate;
        this.maxIterations = Global.MaxIterations;
        createPopulation();
        RegenerateChunkCenteredAt(CenterGlobalX);
    }

    private void createPopulation() {
        population = new List<MapIndividual>(populationSize);
        var zeroIndexedMatrix = getZeroIndexedMatrix();
        for (int i = 0; i < populationSize; i++) {
            population.Add(new MapIndividual(matrix));
        }
    }

    private void initializePopulation(int generationLeft, int generationRight) {
        if (population.Count == 0) {
            createPopulation();
        }
        var zm = getZeroIndexedMatrix();
        var zgl = getLocalXIndexInZeroMatrixFromGlobalX(generationLeft);
        var zgr = getLocalXIndexInZeroMatrixFromGlobalX(generationRight);
        foreach (MapIndividual individual in population) {
            individual.UpdateGeneticMatrix(zm, zgl, zgr);
        }
    }

    private GeneColumn[] getZeroIndexedMatrix() {
        GeneColumn[] zm = new GeneColumn[width];
        int zx = 0;
        for (int x = LeftmostGlobalX; x <= RightmostGlobalX; x++) {
            for (int y = 0; y < height; y++) {
                zm[zx] = GetGlobalColumn(x);
            }
            zx++;
        }
        return zm;
    }

    private int getLocalXIndexInZeroMatrixFromGlobalX(int globalX) {
        var lxfg = getLocalXIndexFromGlobalX(globalX);
        if (lxfg < leftmostIndex) {
            return Width - leftmostIndex + getLocalXIndexFromGlobalX(globalX);
        } else {
            return getLocalXIndexFromGlobalX(globalX) - leftmostIndex;
        }
    }
    protected override void generateMap(int generationGlobalLeftmostX, int generationGlobalRightmostX) {
        
        initializePopulation(generationGlobalLeftmostX, generationGlobalRightmostX);
        
        // Evolve over generations
        int numberOfChildren = (int) ((1 - elitism) * (float) population.Count);
        MapIndividual[] children = new MapIndividual[numberOfChildren];

        var zgl = getLocalXIndexInZeroMatrixFromGlobalX(generationGlobalLeftmostX);
        var zgr = getLocalXIndexInZeroMatrixFromGlobalX(generationGlobalRightmostX);

        for (int i = 0; i < maxIterations; i++) {
            // Sort population
            population.Sort((x, y) => x.Fitness.CompareTo(y.Fitness));
            
            // Crossover
            for (int ci = 0; ci < numberOfChildren; ci++) {
                // Pick parents
                var parent1 = pickParent();
                var parent2 = pickParent();
                // Crossover parents
                GeneColumn[] childGenes = crossover(parent1, parent2);
                MapIndividual newChild = new MapIndividual(childGenes, zgl, zgr);
                children[ci] = newChild;
            }

            // Mutation
            mutatePopulation();
        }

        // Update world matrix with best individual
        var bestIndividual = BestIndividual;

        // Force playability of individual
        bestIndividual.forcePlayability();

        int zx = 0;
        for (int i = LeftmostGlobalX; i < RightmostGlobalX; i++) {
            for (int j = 0; j < Height; j++) {
                GetGlobalColumn(i).Clone(bestIndividual.GeneticMatrix[zx]);
            }
            zx++;
        }
    }

    private void mutatePopulation() {
        foreach (var individual in population) {
            individual.Mutate(mutationRate);
        }
    }

    private GeneColumn[] crossover(MapIndividual parent1, MapIndividual parent2)
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