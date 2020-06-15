/*
    @author Rafael Pontes
*/
using System.Collections.Generic;
using System;
using Godot;

public class GAMapGenerator {

    public static char[] TileCodes = new char[] {
        'B', // Blank
        'G', // Ground
        'I', // Item
        'S', // Spike
        'E', // Enemy,
        'C', // ClockItem
    };

    public char DefaultCell { get { return TileCodes[0]; } } // Blank
    protected int maxHeight;
    public int MaxHeight { get { return maxHeight; } }
    public int LeftmostGlobalX { get { return leftmostColumnReference.GlobalX; } }
    public int RightmostGlobalX { get { return leftmostColumnReference.GlobalX; }}
    public int currentGlobalX { get { return currentColumnReference.GlobalX; }}
    protected Random random = new Random();
    protected int floorDefaultHeight = 3;

    // This is a reference to the leftmost GeneColumn (GC) in the current chunk
    private GeneColumn currentColumnReference = null;
    /* 
        Leftmost GC reference in the entire generation.
        This is used to append more cells or discard them 
        on the left side in case it's needed.
    */
    private GeneColumn leftmostColumnReference = null;
    /* 
        Rightmost GC reference in the entire generation.
        This is used to append more cells or discard them 
        on the right side in case it's needed.
    */
    private GeneColumn rightmostColumnReference = null;

    /*
        Each chunk generation uses these parameters for the
        Genetic Algorithm.
    */
    private int populationSize;
    private int maxIterations = Global.MaxIterations;
    private float mutationRate = 0.05F;
    private float elitism = 0.05F;

    /*
        Each chunk generation uses a reference map that is
        right beside it. It may be on its left or its right.
        If this reference weren't here, the generated map
        would possibly be completely unrelated to its neighbor,
        resulting in possibly unplayable discontinuous areas.
    */
    private int referenceChunkSize = 60;
    /*
        The actual amount of X columns generated beside the
        reference area in the base chunk.
    */
    private int generationChunkSize = 60;
    private int baseChunkSize { get { return referenceChunkSize + generationChunkSize; }}
    private List<MapIndividual> population;

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

    public GAMapGenerator(  int referenceChunkSize, int generationChunkSize,
                            int maxHeight, int initialOrigin,
                            int populationSize = 100, float mutationRate = 0.05F,
                            int numberOfChunks = 4
                        ) {
        this.referenceChunkSize = referenceChunkSize;
        this.generationChunkSize = generationChunkSize;
        this.populationSize = populationSize;
        this.mutationRate = mutationRate;
        this.maxIterations = Global.MaxIterations;
        this.maxHeight = maxHeight;
        createBaseChunkAroundX(initialOrigin);
        int numberOfChunksOnLeft = numberOfChunks / 2;
        int numberOfChunksOnRight = numberOfChunks - numberOfChunksOnLeft;
        generateChunksOnLeft(numberOfChunksOnLeft);
        generateChunksOnRight(numberOfChunksOnRight);
    }

    private void createBaseChunkAroundX(int initialOrigin) {
        // Here, we are initializing the first base reference chunk
        var chunkLeftmostX = initialOrigin - referenceChunkSize / 2;
        var currentX = chunkLeftmostX;
        GeneColumn previousGC = new GeneColumn(null, null, currentX, 3);
        this.currentColumnReference = previousGC;
        this.leftmostColumnReference = previousGC;
        for (++currentX; currentX < (this.LeftmostGlobalX + referenceChunkSize); currentX++) {
            GeneColumn currentGC = new GeneColumn(previousGC, null, currentX, 3);
            previousGC.RightColumn = currentGC;
            previousGC = currentGC;
        }
        rightmostColumnReference = previousGC;
    }

    private void generateChunksOnLeft(int numChunks) {
        // TODO
    }

    private void generateChunksOnRight(int numChunks) {
        // TODO
    }

    private void createPopulation(int leftmostX) {
        population = new List<MapIndividual>(populationSize);
        var zeroIndexedMatrix = getZeroIndexedMatrix();
        for (int i = 0; i < populationSize; i++) {
            population.Add(new MapIndividual(matrix, leftmostX));
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

    public char GetGlobalCell(int x, int y) {
        return GetGlobalColumn(x).CellAtY(y);
    }

    public void GenerateAppendableChunk(int newCenterOriginX) {
        
        // New limits
        int newLeftmostGlobalX = newCenterOriginX - (width / 2);
        int newRightmostGlobalX = newLeftmostGlobalX + width - 1;

        // Find intersection
        if (newRightmostGlobalX < LeftmostGlobalX || newLeftmostGlobalX > RightmostGlobalX) {
            // regenerate whole matrix
            leftmostGlobalX = newLeftmostGlobalX;
            leftmostIndex = 0;
            generateMap(newLeftmostGlobalX, newRightmostGlobalX);
        } else if (newLeftmostGlobalX != LeftmostGlobalX) {
            // There is partial intersection
            // Regenerate new needed area
            int intersectionLeftLimit = Mathf.Max(newLeftmostGlobalX, LeftmostGlobalX);
            int intersectionRightLimit = Mathf.Min(newRightmostGlobalX, RightmostGlobalX);

            int generationLeftLimit;
            int generationRightLimit;

            if (newLeftmostGlobalX < LeftmostGlobalX) {
                // Generate lefthand chunk
                generationLeftLimit = newLeftmostGlobalX;
                generationRightLimit = intersectionLeftLimit - 1;
            } else {
                // Generate righthand chunk
                generationLeftLimit = intersectionRightLimit + 1;
                generationRightLimit = newRightmostGlobalX;
            }

            var newLeftmostIndex = getLocalXIndexFromGlobalX(newLeftmostGlobalX);
            if (newLeftmostIndex == -1) {
                var intersectionWidth = intersectionRightLimit - intersectionLeftLimit;
                newLeftmostIndex = (leftmostIndex + intersectionWidth + 1) % Width;
            }
            leftmostGlobalX = newLeftmostGlobalX;
            leftmostIndex = newLeftmostIndex;
            generateMap(generationLeftLimit, generationRightLimit);
        }
    }

    /*
        Returns -1 if globalX is out of the buffered range.
        Otherwise, returns the matrix column that corresponds to globalX.
    */
    protected int getLocalXIndexFromGlobalX(int globalX) {
        if (globalX < LeftmostGlobalX || globalX > RightmostGlobalX) return -1;
        return (globalX - LeftmostGlobalX + leftmostIndex) % width;
    }
    protected virtual void generateMap(int generatedLeftmostX, int generatedRightmostX) {

        var floorHeight = Mathf.Abs(generatedLeftmostX);
        for (int i = generatedLeftmostX; i <= generatedRightmostX; i++) {
            floorHeight %= (MaxHeight - 7);
            SetGlobalColumn(i, new GeneColumn(i, floorHeight));
            floorHeight++;
        }
    }

    public override String ToString() {
        String r = "";
        for(int j = MaxHeight - 1; j >= 0; j--) {
            r += $"{j,6:000000}: ";
            for (int i = 0; i < Width; i++) {
                r += $"{matrix[i].CellAtY(j)}";
            }
            r += "\n";
        }
        int digitosLargura = (Width - 1).ToString().Length;
        for (int digito = 0; digito < digitosLargura; digito++) {
            r += "        ";
            for (int x = 0; x < Width; x++) {
                var xs = x.ToString();
                r += xs.Length > digito ? $"{xs[digito]}" : "0";
            }
            r += "\n";
        }
        return r;
    }

    public String ToRichTextString() {
        String r = "";

        // Actual tiles
        for(int j = MaxHeight - 1; j >= 0; j--) {
            r += $"{j,6:000000}: ";
            for (int i = 0; i < Width; i++) {
                switch(matrix[i].CellAtY(j)) {
                    case 'G':
                        r += "[color=gray]";
                        break;
                    case 'S':
                        r += "[color=red]";
                        break;
                    case 'N':
                        r += "[color=purple]";
                        break;
                    case 'C':
                    case 'c':
                        r += "[color=yellow]";
                        break;
                    default:
                        r += "[color=cyan]";
                        break;
                }
                r += $"{matrix[i].CellAtY(j)}";
                r += "[/color]";
            }
            r += "\n";
        }

        // Global X coordinates
        int digitosLargura = 4;
        for (int digito = 0; digito < digitosLargura; digito++) {
            r += "        ";
            for (int x = 0; x < Width; x++) {
                var xs = $"{x,4:0000}";
                if (x == leftmostIndex) {
                    // LeftmostX offset in matrix
                    r += "[color=blue]";
                    r += xs[digito];
                    r += "[/color]";
                } else if (x == ((leftmostIndex + (Width/2) - 1) % Width)) {
                    // Middle X
                    r += "[color=fuchsia]";
                    r += xs[digito];
                    r += "[/color]";
                } else {
                    r += "[color=lime]";
                    r += xs[digito];
                    r += "[/color]";
                }
            }
            r += "\n";
        }

        // Zero-based indices
        int[] zeroBasedX = new int[Width];
        for (int x = LeftmostGlobalX; x <= RightmostGlobalX; x++) {
            var i = getLocalXIndexFromGlobalX(x);
            zeroBasedX[i] = x;
        }
        digitosLargura = 4;
        for (int digito = 0; digito < digitosLargura; digito++) {
            r += "        ";
            for (int x = 0; x < zeroBasedX.Length; x++) {
                var xs = $"{zeroBasedX[x],4:0000}";
                if (zeroBasedX[x] < 0) {
                    r += "[color=purple]";
                    r += xs[digito+1];
                    r += "[/color]";
                } else {
                    r += "[color=aqua]";
                    r += xs[digito];
                    r += "[/color]";
                }
            }
            r += "\n";
        }

        return r;
    }

    public GeneColumn GetGlobalColumn(int x) {
        while(currentGlobalX != x) {
            if (x < currentGlobalX) {
                currentColumnReference = currentColumnReference.LeftColumn;
            } else {
                currentColumnReference = currentColumnReference.RightColumn;
            }
        }
        return currentColumnReference;
    }

    private GeneColumn[] getZeroIndexedMatrix() {
        GeneColumn[] zm = new GeneColumn[width];
        int zx = 0;
        for (int x = LeftmostGlobalX; x <= RightmostGlobalX; x++) {
            for (int y = 0; y < maxHeight; y++) {
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

            for (int ci = 0; ci < numberOfChildren; ci++) {
                population[ci] = children[ci];
            }

            Debug.Log($"BEFORE MUTATION: Iteration:{i}, population[0].GeneticMatrix[31]: {population[0].GeneticMatrix[31]}");
            // Mutation
            mutatePopulation();
            Debug.Log($"AFTER MUTATION: Iteration:{i}, population[0].GeneticMatrix[31]: {population[0].GeneticMatrix[31]}");
        }

        // Update world matrix with best individual
        var bestIndividual = BestIndividual;

        // Force playability of individual
        //bestIndividual.forcePlayability();

        int zx = 0;
        for (int i = LeftmostGlobalX; i < RightmostGlobalX; i++) {
            GetGlobalColumn(i).Clone(bestIndividual.GeneticMatrix[zx]);
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