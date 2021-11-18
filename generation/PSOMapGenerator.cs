/*
	@authors Rafael Pontes, Igor Seabra, Herman Gomes

*/
using System.Collections.Generic;
using System;
using Godot;

public class PSOMapGenerator {

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
	public int LeftmostGlobalX { get { return GlobalHead.GlobalX; } }
	public int RightmostGlobalX { get { return GlobalTail.GlobalX; }}
	public int currentGlobalX { get { return movingHead.GlobalX; }}
	protected int floorDefaultHeight = 3;
	
	// This is a reference to the head and tail ParticleColumn (GC) in the current chunk
	private ParticleColumn movingHead = null;
	public ParticleColumn CurrentChunkHead = null;
	public ParticleColumn CurrentChunkTail = null;
	/* 
		Leftmost GC reference in the entire generation.
		This is used to append more cells or discard them 
		on the left side in case it's needed.
	*/
	public ParticleColumn GlobalHead = null;
	/* 
		Rightmost GC reference in the entire generation.
		This is used to append more cells or discard them 
		on the right side in case it's needed.
	*/
	public ParticleColumn GlobalTail = null;

	/*
		Each chunk generation uses these parameters for the
		Genetic Algorithm.
	*/
	private int populationSize;
	private int maxIterations = Global.MaxIterations;

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

	private double paramW = 0.01;
	private double paramCMin = 0.05;
	private double paramCMax = 0.35;
	private double paramRMin = 0.0;
	private double paramRMax = 0.2;

	public PSOMapGenerator(  int referenceChunkSize, int generationChunkSize,
							int maxHeight, int initialOrigin,
							int populationSize = 100,
							int numberOfChunks = 10, bool shouldPregen = true,
							double paramW = 0.01, double paramCMin = 0.05, double paramCMax = 0.35, double paramRMin = 0, double paramRMax = 0.2
						) {
		this.referenceChunkSize = referenceChunkSize;
		this.generationChunkSize = generationChunkSize;
		this.populationSize = populationSize;
		this.maxIterations = Global.MaxIterations;
		this.maxHeight = maxHeight;

		this.paramW = paramW;
		this.paramCMin = paramCMin;
		this.paramCMax = paramCMax;
		this.paramRMin = paramRMin;
		this.paramRMax = paramRMax;

		createBaseChunkAroundX(initialOrigin, 5);
		if (shouldPregen) {
			int numberOfChunksOnLeft = numberOfChunks / 2;
			int numberOfChunksOnRight = numberOfChunks - numberOfChunksOnLeft;
			GenerateChunks(numberOfChunksOnLeft, true);
			Global.IsLeftGenBusy = false; // Allow parallel left gens
			GenerateChunks(numberOfChunksOnRight, false);
			Global.IsRightGenBusy = false; // Allow parallel right gens
		}
	}

	public PSOMapGenerator(  int referenceChunkSize, int generationChunkSize,
							int maxHeight, int initialOrigin,
							int populationSize = 100,
							bool shouldPregen = true
						) : this(referenceChunkSize, generationChunkSize, maxHeight,
								initialOrigin, populationSize, 10, shouldPregen) {}

	private void createBaseChunkAroundX(int initialOrigin, int baseChunkSize) {
		// Here, we are initializing the first base reference chunk
		// TODO: add a GA generation for this chunk. For now, it's static.
		var chunkLeftmostX = initialOrigin - baseChunkSize / 2;
		var currentX = chunkLeftmostX;
		ParticleColumn previousGC = new ParticleColumn(null, null, currentX, 3, -1, -1, 3, false);
		this.movingHead = previousGC;
		this.GlobalHead = previousGC;
		for (++currentX; currentX < (this.LeftmostGlobalX + baseChunkSize); currentX++) {
			ParticleColumn currentGC = new ParticleColumn(previousGC, null, currentX, 3, -1, -1, 3, false);
			previousGC.Next = currentGC;
			previousGC = currentGC;
		}
		GlobalTail = previousGC;
	}

	public void GenerateChunks(int numChunks, bool isLeft) {
		for (int chunkIndex = 0; chunkIndex < numChunks; chunkIndex++) {

			List<MapParticle> population = isLeft ? createLeftPopulation() : createRightPopulation();

			// Generate initially random velocities; a velocity for a MapParticle is a collection of ParticleColumns
			// that represent the velocities of the ParticleColumns in that MapParticle.
			List<List<ParticleColumn>> velocities = new List<List<ParticleColumn>>(population.Count);
			for (int i = 0; i < population.Count; i++)
			{
				velocities.Add(createVelocity(populationSize));
			}
			
			// A list of ParticleColumn for each MapParticle, representing a snapshot of their own personal best.
			List<List<ParticleColumn>> personalBests = new List<List<ParticleColumn>>(population.Count);
			for (int i = 0; i < population.Count; i++) 
			{
				var pb = population[i].GetShallowCopy();
				personalBests.Add(pb);
			}

			MapParticle bestIndividual = null;
			
			PerformIterations(ref population, ref velocities, ref personalBests, ref bestIndividual);

			// Update world matrix with best individual
			foreach (MapParticle individual in population) {
				if (individual.GetFitness() > bestIndividual.GetFitness()) {
					bestIndividual = individual;
				}
			}

			// Force playability of individual
			//bestIndividual.forcePlayability();

			// Make best individual genes unmutable
			for (ParticleColumn iterator = bestIndividual.GenerationHead;
				iterator != null && iterator.IsMutable;
				iterator = iterator.Next) {
					iterator.IsMutable = false;
			}

			if (isLeft)
			{
				GlobalHead.Previous = bestIndividual.GenerationTail;
				bestIndividual.GenerationTail.Next = GlobalHead;
				GlobalHead = bestIndividual.GenerationHead;
			}
			else
			{
				GlobalTail.Next = bestIndividual.GenerationHead;
				bestIndividual.GenerationHead.Previous = GlobalTail;
				GlobalTail = bestIndividual.GenerationTail;
			}
		}
	}

	private double Lerp(double init, double final, double value) {
		return init + value * (final - init);
	}

	private void PerformIterations(
		ref List<MapParticle> population,
		ref List<List<ParticleColumn>> velocities,
		ref List<List<ParticleColumn>> personalBests,
		ref MapParticle bestIndividual)
	{
		for (int i = 0; i < maxIterations; i++)
		{
			// Sort population
			population.Sort((x, y) => x.Fitness.CompareTo(y.Fitness));
			bestIndividual = population[population.Count-1];
			
			double w = paramW * (i - maxIterations) / (maxIterations * maxIterations) + paramW;
			double c1 = Lerp(paramCMax, paramCMin, (double)i / maxIterations);
			double c2 = Lerp(paramCMin, paramCMax, (double)i / maxIterations);
			double r1 = Lerp(paramRMin, paramRMax, Global.random.NextDouble());
			double r2 = Lerp(paramRMin, paramRMax, Global.random.NextDouble());
			
			// Move individuals to new position; Move method also returns new velocity
			for (int j = 0; j < population.Count; j++)
			{
				var previousFitness = population[j].Fitness;

				velocities[j] = population[j].Move(w, velocities[j], r1, c1, personalBests[j], r2, c2, bestIndividual);

				if (population[j].Fitness > previousFitness)
				{
					personalBests[j] = population[j].GetShallowCopy();
				}
			}
		}
	}

	public List<MapParticle> EvolveTestChunkOnRight(List<MapParticle> pop = null) {

		List<MapParticle> population;

		if (pop == null) {
			population = createRightPopulation();
		} else {
			population = pop;
		}
		
		// Generate initially random velocities; a velocity for a MapParticle is a collection of ParticleColumns
		// that represent the velocities of the ParticleColumns in that MapParticle.
		List<List<ParticleColumn>> velocities = new List<List<ParticleColumn>>(population.Count);
		for (int i = 0; i < population.Count; i++)
		{
			velocities.Add(createVelocity(populationSize));
		}
		
		// A list of ParticleColumn for each MapParticle, representing a snapshot of their own personal best.
        List<List<ParticleColumn>> personalBests = new List<List<ParticleColumn>>(population.Count);
		for (int i = 0; i < population.Count; i++)
		{
			var pb = population[i].GetShallowCopy();
			personalBests.Add(pb);
		}
		
		MapParticle bestIndividual = null;
		
		PerformIterations(ref population, ref velocities, ref personalBests, ref bestIndividual);
		
		population.Sort((x, y) => x.Fitness.CompareTo(y.Fitness));
		
		return population;
	}

	private ParticleColumn getLeftReferenceChunkClone() {
		ParticleColumn globalReferenceIterator = GlobalHead;
		ParticleColumn referenceHead = new ParticleColumn(globalReferenceIterator);
		ParticleColumn referenceCloneIterator = new ParticleColumn(globalReferenceIterator.Next, referenceHead, null);
		referenceHead.Next = referenceCloneIterator;
		globalReferenceIterator = globalReferenceIterator.Next;
		for (int i = 0; i < referenceChunkSize - 1; i++) {
			if (globalReferenceIterator == null) {
				break;
			}
			ParticleColumn nextColumn = new ParticleColumn(globalReferenceIterator, referenceCloneIterator, null);
			referenceCloneIterator.Next = nextColumn;
			referenceCloneIterator = nextColumn;
			globalReferenceIterator = globalReferenceIterator.Next;
		}
		return referenceHead;
	}

	private ParticleColumn getRightReferenceChunkClone() {
		// On the right reference clone, we start from the global tail
		ParticleColumn globalReferenceIterator = GlobalTail;
		ParticleColumn referenceTail = new ParticleColumn(globalReferenceIterator);
		ParticleColumn referenceCloneIterator = new ParticleColumn(globalReferenceIterator.Previous, null, referenceTail);
		referenceTail.Previous = referenceCloneIterator;
		globalReferenceIterator = globalReferenceIterator.Previous;
		for (int i = 0; i < referenceChunkSize - 1; i++) {
			if (globalReferenceIterator == null) {
				break;
			}
			ParticleColumn previousColumn = new ParticleColumn(globalReferenceIterator, null, referenceCloneIterator);
			referenceCloneIterator.Previous = previousColumn;
			referenceCloneIterator = previousColumn;
			globalReferenceIterator = globalReferenceIterator.Previous;
		}
		return referenceTail;
	}

	private List<MapParticle> createLeftPopulation() {
		List<MapParticle> population = new List<MapParticle>(populationSize);
		for (int i = 0; i < populationSize; i++) {
			ParticleColumn referenceHead = getLeftReferenceChunkClone();
			ParticleColumn generationHead = getRandomGenerationChunkOnLeftAndLink(referenceHead);
			population.Add(new MapParticle(generationHead, generationHead, referenceHead.Previous, true));
		}
		return population;
	}

	private List<MapParticle> createRightPopulation() {
		List<MapParticle> population = new List<MapParticle>(populationSize);
		for (int i = 0; i < populationSize; i++) {
			ParticleColumn referenceTail = getRightReferenceChunkClone();
			ParticleColumn referenceHead = referenceTail;
			while (referenceHead.Previous != null) { referenceHead = referenceHead.Previous; }
			ParticleColumn generationTail = getRandomGenerationChunkOnRightAndLink(referenceTail);
			population.Add(new MapParticle(referenceHead, referenceTail.Next, generationTail, false));
		}
		return population;
	}

	private List<ParticleColumn> createVelocity(int count) {
		List<ParticleColumn> velocities = new List<ParticleColumn>(count);
		for (int i = 0; i < count; i++){
			var gc = new ParticleColumn(0, null, null);
			gc._floatGroundHeight -= 2;
			velocities.Add(gc);
		}
		return velocities;
	}

	private ParticleColumn getRandomGenerationChunkOnLeftAndLink(ParticleColumn referenceHead) {
		// Creating generation tail and linking to reference head
		ParticleColumn generationIterator = new ParticleColumn(referenceHead.GlobalX - 1, null, referenceHead);
		referenceHead.Previous = generationIterator;
		for (int i = 1; i < generationChunkSize; i++) {
			ParticleColumn generationPreviousColumn = new ParticleColumn(generationIterator.GlobalX - 1, null, generationIterator);
			generationIterator.Previous = generationPreviousColumn;
			generationIterator = generationPreviousColumn;
		}
		return generationIterator; // generation chunk head
	}

	private ParticleColumn getRandomGenerationChunkOnRightAndLink(ParticleColumn referenceTail) {
		// Creating generation head and linking to reference tail
		ParticleColumn generationIterator = new ParticleColumn(referenceTail.GlobalX + 1, referenceTail, null);
		referenceTail.Next = generationIterator;
		for (int i = 1; i < generationChunkSize; i++) {
			ParticleColumn generationNextColumn = new ParticleColumn(generationIterator.GlobalX + 1, generationIterator, null);
			generationIterator.Next = generationNextColumn;
			generationIterator = generationNextColumn;
		}
		return generationIterator; // generation chunk tail
	}

	private ParticleColumn getRightReferenceChunk() {
		ParticleColumn referenceTail = new ParticleColumn(GlobalTail);
		ParticleColumn iterator = new ParticleColumn(referenceTail.Previous);
		referenceTail.Next = null;
		referenceTail.Previous = iterator;
		iterator.Next = referenceTail;
		for (int i = 0; i < referenceChunkSize; i++) {
			ParticleColumn temp = new ParticleColumn(iterator.Previous);
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
				for (ParticleColumn iterator = CurrentChunkHead; iterator != CurrentChunkTail; iterator = iterator.Next) {
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
			for (ParticleColumn iterator = CurrentChunkHead; iterator != CurrentChunkTail.Next; iterator = iterator.Next) {
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
		int playerX = Global.MappedPlayerX;
		for (int digito = 0; digito < digitosLargura; digito++) {
			r += "        ";
			for (ParticleColumn iterator = CurrentChunkHead; iterator != CurrentChunkTail.Next; iterator = iterator.Next) {
				var gX = iterator.GlobalX;
				string color;
				if (gX == playerX) {
					color = "yellow";
				} else if(gX < 0) {
					color = "purple";
				} else {
					color = "lime";
				}
				var xs = $"{Mathf.Abs(gX),4:0000}";
				r += $"[color={color}]";
				r += xs[digito];
				r += "[/color]";
			}
			r += "\n";
		}
		return r;
	}

	public ParticleColumn GetGlobalColumn(int x) {
		while(currentGlobalX != x) {
			if (x < currentGlobalX) {
				movingHead = movingHead.Previous;
			} else {
				movingHead = movingHead.Next;
			}
		}
		return movingHead;
	}

	private MapParticle crossover(MapParticle parent1, MapParticle parent2)
	{
		return parent1.CrossoverWith(parent2);
	}

	private MapParticle pickParentFromPopulation(List<MapParticle> population)
	{
		var maxFitness = population[population.Count-1].Fitness;
		var minFitness = population[0].Fitness;
		int chosenFitness = Global.random.Next(minFitness, maxFitness);
		int pi = Global.random.Next(population.Count - 1);
		while(population[pi].Fitness < chosenFitness) {
			pi++;
		}
		return population[pi];
	}
}
