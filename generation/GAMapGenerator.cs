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
    public int LeftmostGlobalX { get { return globalHead.GlobalX; } }
    public int RightmostGlobalX { get { return globalTail.GlobalX; }}
    public int currentGlobalX { get { return movingHead.GlobalX; }}
    protected Random random = new Random();
    protected int floorDefaultHeight = 3;

    // This is a reference to the head and tail GeneColumn (GC) in the current chunk
    private GeneColumn movingHead = null;
    public GeneColumn CurrentChunkHead = null;
    public GeneColumn CurrentChunkTail = null;
    /* 
        Leftmost GC reference in the entire generation.
        This is used to append more cells or discard them 
        on the left side in case it's needed.
    */
    private GeneColumn globalHead = null;
    /* 
        Rightmost GC reference in the entire generation.
        This is used to append more cells or discard them 
        on the right side in case it's needed.
    */
    private GeneColumn globalTail = null;

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
        Debug.Log("All done?");
    }

    private void createBaseChunkAroundX(int initialOrigin) {
        // Here, we are initializing the first base reference chunk
        // TODO: add a GA generation for this chunk. For now, it's static.
        var chunkLeftmostX = initialOrigin - referenceChunkSize / 2;
        var currentX = chunkLeftmostX;
        GeneColumn previousGC = new GeneColumn(null, null, currentX, 3, false, false, false);
        this.movingHead = previousGC;
        this.globalHead = previousGC;
        for (++currentX; currentX < (this.LeftmostGlobalX + referenceChunkSize); currentX++) {
            GeneColumn currentGC = new GeneColumn(previousGC, null, currentX, 3, false, false, false);
            previousGC.Next = currentGC;
            previousGC = currentGC;
        }
        globalTail = previousGC;
    }

    private void generateChunksOnLeft(int numChunks) {

        for (int chunkIndex = 0; chunkIndex < numChunks; chunkIndex++) {
            List<MapIndividual> population = createLeftPopulation();

            // Evolve over generations
            int numberOfChildren = (int) ((1 - elitism) * (float) population.Count);
            MapIndividual[] children = new MapIndividual[numberOfChildren];

            for (int i = 0; i < maxIterations; i++) {

                Debug.Log($"Iteration {i}\n");

                // Sort population
                population.Sort((x, y) => x.Fitness.CompareTo(y.Fitness));
                
                // Crossover
                for (int ci = 0; ci < numberOfChildren; ci++) {
                    // Pick parents
                    var parent1 = pickParentFromPopulation(population);
                    var parent2 = pickParentFromPopulation(population);
                    // Crossover parents
                    MapIndividual newChild = crossover(parent1, parent2);
                    children[ci] = newChild;
                }

                for (int ci = 0; ci < numberOfChildren; ci++) {
                    population[ci] = children[ci];
                }

                // Mutation
                mutatePopulation(population);
            }

            // Update world matrix with best individual
            MapIndividual bestIndividual = population[0];
            foreach (MapIndividual individual in population) {
                if (individual.GetFitness() > bestIndividual.GetFitness()) {
                    bestIndividual = individual;
                }
            }

            // Force playability of individual
            //bestIndividual.forcePlayability();

            // Make best individual genes unmutable
            for (GeneColumn iterator = bestIndividual.GenerationHead;
                iterator != null && iterator.IsMutable;
                iterator = iterator.Next) {
                    iterator.IsMutable = false;
            }

            globalHead.Previous = bestIndividual.GenerationTail;
            bestIndividual.GenerationTail.Next = globalHead;
            globalHead = bestIndividual.GenerationHead;
        }
    }

    private void generateChunksOnRight(int numChunks) {

        for (int chunkIndex = 0; chunkIndex < numChunks; chunkIndex++) {
            
            List<MapIndividual> population = createRightPopulation();

            // Evolve over generations
            int numberOfChildren = (int) ((1 - elitism) * (float) population.Count);
            MapIndividual[] children = new MapIndividual[numberOfChildren];

            for (int i = 0; i < maxIterations; i++) {

                Debug.Log($"Iteration {i}\n");

                // Sort population
                population.Sort((x, y) => x.Fitness.CompareTo(y.Fitness));
                
                // Crossover
                for (int ci = 0; ci < numberOfChildren; ci++) {
                    // Pick parents
                    var parent1 = pickParentFromPopulation(population);
                    var parent2 = pickParentFromPopulation(population);
                    // Crossover parents
                    MapIndividual newChild = crossover(parent1, parent2);
                    children[ci] = newChild;
                }

                for (int ci = 0; ci < numberOfChildren; ci++) {
                    population[ci] = children[ci];
                }

                // Mutation
                mutatePopulation(population);
            }

            // Update world matrix with best individual
            MapIndividual bestIndividual = population[0];
            foreach (MapIndividual individual in population) {
                if (individual.GetFitness() > bestIndividual.GetFitness()) {
                    bestIndividual = individual;
                }
            }

            // Force playability of individual
            //bestIndividual.forcePlayability();

            // Make best individual genes unmutable
            for (GeneColumn iterator = bestIndividual.GenerationHead;
                iterator != null && iterator.IsMutable;
                iterator = iterator.Next) {
                    iterator.IsMutable = false;
            }

            globalTail.Next = bestIndividual.GenerationHead;
            bestIndividual.GenerationHead.Previous = globalTail;
            globalTail = bestIndividual.GenerationTail;
        }
    }

    private GeneColumn getLeftReferenceChunkClone() {
        GeneColumn globalReferenceIterator = globalHead;
        GeneColumn referenceHead = new GeneColumn(globalReferenceIterator);
        GeneColumn referenceCloneIterator = new GeneColumn(globalReferenceIterator.Next, referenceHead, null);
        referenceHead.Next = referenceCloneIterator;
        globalReferenceIterator = globalReferenceIterator.Next;
        for (int i = 0; i < referenceChunkSize - 1; i++) {
            if (globalReferenceIterator == null) {
                break;
            }
            GeneColumn nextColumn = new GeneColumn(globalReferenceIterator, referenceCloneIterator, null);
            referenceCloneIterator.Next = nextColumn;
            referenceCloneIterator = nextColumn;
            globalReferenceIterator = globalReferenceIterator.Next;
        }
        return referenceHead;
    }

    private GeneColumn getRightReferenceChunkClone() {
        // On the right reference clone, we start from the global tail
        GeneColumn globalReferenceIterator = globalTail;
        GeneColumn referenceTail = new GeneColumn(globalReferenceIterator);
        GeneColumn referenceCloneIterator = new GeneColumn(globalReferenceIterator.Previous, null, referenceTail);
        referenceTail.Previous = referenceCloneIterator;
        globalReferenceIterator = globalReferenceIterator.Previous;
        for (int i = 0; i < referenceChunkSize - 1; i++) {
            if (globalReferenceIterator == null) {
                break;
            }
            GeneColumn previousColumn = new GeneColumn(globalReferenceIterator, null, referenceCloneIterator);
            referenceCloneIterator.Previous = previousColumn;
            referenceCloneIterator = previousColumn;
            globalReferenceIterator = globalReferenceIterator.Previous;
        }
        return referenceTail;
    }

    private List<MapIndividual> createLeftPopulation() {
        List<MapIndividual> population = new List<MapIndividual>(populationSize);
        for (int i = 0; i < populationSize; i++) {
            GeneColumn referenceHead = getLeftReferenceChunkClone();
            GeneColumn generationHead = getRandomGenerationChunkOnLeftAndLink(referenceHead);
            population.Add(new MapIndividual(generationHead, generationHead, referenceHead.Previous, true));
        }
        return population;
    }

    private List<MapIndividual> createRightPopulation() {
        List<MapIndividual> population = new List<MapIndividual>(populationSize);
        for (int i = 0; i < populationSize; i++) {
            GeneColumn referenceTail = getRightReferenceChunkClone();
            GeneColumn referenceHead = referenceTail;
            while (referenceHead.Previous != null) { referenceHead = referenceHead.Previous; }
            GeneColumn generationTail = getRandomGenerationChunkOnRightAndLink(referenceTail);
            population.Add(new MapIndividual(referenceHead, referenceTail.Next, generationTail, false));
        }
        return population;
    }

    private GeneColumn getRandomGenerationChunkOnLeftAndLink(GeneColumn referenceHead) {
        // Creating generation tail and linking to reference head
        GeneColumn generationIterator = new GeneColumn(referenceHead.GlobalX - 1, null, referenceHead);
        referenceHead.Previous = generationIterator;
        for (int i = 1; i < generationChunkSize; i++) {
            GeneColumn generationPreviousColumn = new GeneColumn(generationIterator.GlobalX - 1, null, generationIterator);
            generationIterator.Previous = generationPreviousColumn;
            generationIterator = generationPreviousColumn;
        }
        return generationIterator; // generation chunk head
    }

    private GeneColumn getRandomGenerationChunkOnRightAndLink(GeneColumn referenceTail) {
        // Creating generation head and linking to reference tail
        GeneColumn generationIterator = new GeneColumn(referenceTail.GlobalX + 1, referenceTail, null);
        referenceTail.Next = generationIterator;
        for (int i = 1; i < generationChunkSize; i++) {
            GeneColumn generationNextColumn = new GeneColumn(generationIterator.GlobalX + 1, generationIterator, null);
            generationIterator.Next = generationNextColumn;
            generationIterator = generationNextColumn;
        }
        return generationIterator; // generation chunk tail
    }

    private GeneColumn getRightReferenceChunk() {
        GeneColumn referenceTail = new GeneColumn(globalTail);
        GeneColumn iterator = new GeneColumn(referenceTail.Previous);
        referenceTail.Next = null;
        referenceTail.Previous = iterator;
        iterator.Next = referenceTail;
        for (int i = 0; i < referenceChunkSize; i++) {
            GeneColumn temp = new GeneColumn(iterator.Previous);
            temp.Next = iterator;
            iterator.Previous = temp;
            iterator = temp;
        }
        iterator.Previous = null;
        return referenceTail;
    }

    internal void SetCurrentChunkHead(int leftChunkLimit)
    {
        if (CurrentChunkHead != null) {
            if (CurrentChunkHead.GlobalX >= leftChunkLimit) {
                while(CurrentChunkHead.GlobalX != leftChunkLimit) {
                    CurrentChunkHead = CurrentChunkHead.Previous;
                }
            } else {
                while(CurrentChunkHead.GlobalX != leftChunkLimit) {
                    CurrentChunkHead = CurrentChunkHead.Next;
                }
            }
        } else {
            this.CurrentChunkHead = GetGlobalColumn(leftChunkLimit);
        }
    }

    internal void SetCurrentChunkTail(int rightChunkLimit)
    {
        if (CurrentChunkTail != null) {
            if (CurrentChunkTail.GlobalX >= rightChunkLimit) {
                while(CurrentChunkTail.GlobalX != rightChunkLimit) {
                    CurrentChunkTail = CurrentChunkTail.Previous;
                }
            } else {
                while(CurrentChunkTail.GlobalX != rightChunkLimit) {
                    CurrentChunkTail = CurrentChunkTail.Next;
                }
            }
        } else {
            this.CurrentChunkTail = GetGlobalColumn(rightChunkLimit);
        }
    }

    public char GetGlobalCell(int x, int y) {
        return GetGlobalColumn(x).CellAtY(y);
    }

    public override String ToString() {
        String r = "";

        if (CurrentChunkHead != null) {
            for(int j = MaxHeight - 1; j >= 0; j--) {
                r += $"{j,6:000000}: ";
                for (GeneColumn iterator = CurrentChunkHead; iterator != CurrentChunkTail; iterator = iterator.Next) {
                    r += $"{iterator.CellAtY(j)}";
                }
                r += "\n";
            }
            int Width = CurrentChunkTail.GlobalX - CurrentChunkHead.GlobalX + 1;
            int digitosLargura = (Width - 1).ToString().Length;
            for (int digito = 0; digito < digitosLargura; digito++) {
                r += "        ";
                for (int x = 0; x < Width; x++) {
                    var xs = x.ToString();
                    r += xs.Length > digito ? $"{xs[digito]}" : "0";
                }
                r += "\n";
            }
        }
        return r;
    }

    public String ToRichTextString() {
        String r = "";

        // Actual tiles
        for(int j = MaxHeight - 1; j >= 0; j--) {
            r += $"{j,6:000000}: ";
            for (GeneColumn iterator = CurrentChunkHead; iterator != CurrentChunkTail; iterator = iterator.Next) {
                switch(iterator.CellAtY(j)) {
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
                r += $"{iterator.CellAtY(j)}";
                r += "[/color]";
            }
            r += "\n";
        }

        // Global X coordinates
        int digitosLargura = 4;
        for (int digito = 0; digito < digitosLargura; digito++) {
            r += "        ";
            for (GeneColumn iterator = CurrentChunkHead; iterator != CurrentChunkTail; iterator = iterator.Next) {
                var xs = $"{iterator.GlobalX,4:0000}";
                r += "[color=lime]";
                r += xs[digito];
                r += "[/color]";
            }
            r += "\n";
        }
        return r;
    }

    public GeneColumn GetGlobalColumn(int x) {
        while(currentGlobalX != x) {
            if (x < currentGlobalX) {
                movingHead = movingHead.Previous;
            } else {
                movingHead = movingHead.Next;
            }
        }
        return movingHead;
    }

    private void mutatePopulation(List<MapIndividual> population) {
        foreach (var individual in population) {
            individual.Mutate(mutationRate);
        }
    }

    private MapIndividual crossover(MapIndividual parent1, MapIndividual parent2)
    {
        return parent1.CrossoverWith(parent2);
    }

    private MapIndividual pickParentFromPopulation(List<MapIndividual> population)
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