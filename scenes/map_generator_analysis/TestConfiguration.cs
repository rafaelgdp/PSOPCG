using Godot;
using System;
using System.Collections.Generic;

public class TestConfiguration {
	int TestId;
	int PopulationSize;
	int MaxIterations;
	int RefChunkSize;
	int GenChunkSize;
	public PSOMapGenerator Generator;
	public TestResult Result;

	private double paramW = 0.4;
	private double paramCMax = 3.5;
	private double paramCMin = 0.5;
	private double paramRMin = 0;
	private double paramRMax = 0.2;

	public TestConfiguration(
		int populationSize,
		int maxIterations,
		int genChunkSize,
		int refChunkSize,
		double paramW = 0.01,
		double paramCMin = 0.05,
		double paramCMax = 0.35,
		double paramRMin = 0.0,
		double paramRMax = 2.0) {

		this.TestId = 1;
		this.PopulationSize = populationSize;
		this.MaxIterations = maxIterations;
		this.RefChunkSize = refChunkSize;
		this.GenChunkSize = genChunkSize;
		this.Generator = new PSOMapGenerator(5, genChunkSize, 12, 0, populationSize, 10, false, paramW, paramCMin, paramCMax, paramRMin, paramRMax);

		this.paramW = paramW;
		this.paramCMin = paramCMin;
		this.paramCMax = paramCMax;
		this.paramRMin = paramRMin;
		this.paramRMax = paramRMax;
	}

	public void StartTest(RTLLog log = null) {

		System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.CreateSpecificCulture("en-US");

		File testData = new File();
		string fileName = "test.csv";
		if (testData.FileExists(fileName)) {
			testData.Open(fileName, File.ModeFlags.ReadWrite);
			testData.GetCsvLine();
			while(!testData.EofReached()) {
				Int32.TryParse(testData.GetCsvLine()[0], out int tId);
				if (this.TestId <= tId) {
					this.TestId++;
				}
			}
		} else {
			testData.Open(fileName, File.ModeFlags.Write);
			testData.StoreCsvLine(GenerationResult.GetHeaderStringArray());
		}
		if (log != null) {
			log.LogLine($"Initiating test id {this.TestId} with config: " +
			$"popSize {this.PopulationSize}, " + 
			$"maxIterations {MaxIterations}, " +
			$"paramW {paramW}, " +
			$"paramCMin {paramCMin}, " +
			$"paramCMax {paramCMax}, " +
			$"paramRMin {paramRMin}, " +
			$"paramRMax {paramRMax}."
			);
		}
		// First generation of this test batch
		// Start timer to track ellapsed time per generation
		var watch = System.Diagnostics.Stopwatch.StartNew();
		double duration = 0;
		// the code that you want to measure comes here
		int genNumber = 0;
		List<MapParticle> population = Generator.EvolveTestChunkOnRight();
		MapParticle bestIndividual = population[population.Count - 1];
		GenerationResult currentGenerationResult = new GenerationResult {
			TestId = this.TestId,
			BestFitness = bestIndividual.Fitness,
			GenerationNumber = genNumber,
			FivePercentBestFitnessAvg = getFivePercentAvg(population),
			IsBestPlayable = bestIndividual.IsPlayable,
			FitnessAverage = getFitnessAverage(population),
			FitnessMedian =  getFitnessMedian(population),
			PopulationSize = this.PopulationSize,
			GenChunkSize = this.GenChunkSize,
			w = bestIndividual.w,
			c1 = bestIndividual.c1,
			c2 = bestIndividual.c2,
			r1 = bestIndividual.r1,
			r2 = bestIndividual.r2,
			paramW = paramW,
			paramCMin = paramCMin,
			paramCMax = paramCMax,
			paramRMin = paramRMin,
			paramRMax = paramRMax,
		};
		watch.Stop();
		currentGenerationResult.ExecutionTimeMs = watch.ElapsedMilliseconds;
		testData.StoreCsvLine(currentGenerationResult.GetCsvStringArray());

		for (genNumber = genNumber + 1; genNumber <= MaxIterations; genNumber++) {
			watch = System.Diagnostics.Stopwatch.StartNew();
			population = Generator.EvolveTestChunkOnRight(population);
			bestIndividual = population[population.Count - 1];
			currentGenerationResult = new GenerationResult {
				TestId = this.TestId,
				BestFitness = bestIndividual.Fitness,
				GenerationNumber = genNumber,
				FivePercentBestFitnessAvg = getFivePercentAvg(population),
				IsBestPlayable = bestIndividual.IsPlayable,
				FitnessAverage = getFitnessAverage(population),
				FitnessMedian =  getFitnessMedian(population),
				PopulationSize = this.PopulationSize,
				GenChunkSize = this.GenChunkSize,
				w = bestIndividual.w,
				c1 = bestIndividual.c1,
				c2 = bestIndividual.c2,
				r1 = bestIndividual.r1,
				r2 = bestIndividual.r2,
				paramW = paramW,
				paramCMin = paramCMin,
				paramCMax = paramCMax,
				paramRMin = paramRMin,
				paramRMax = paramRMax,
			};
			watch.Stop();
			currentGenerationResult.ExecutionTimeMs = watch.ElapsedMilliseconds;
			duration += watch.ElapsedMilliseconds;
			testData.StoreCsvLine(currentGenerationResult.GetCsvStringArray());
		}
		// Close file at the end of this test
		testData.Close();
		TimeSpan t = TimeSpan.FromMilliseconds(duration);
		string durationString = string.Format("{0:D2}h:{1:D2}m:{2:D2}s:{3:D3}ms",
										t.Hours, t.Minutes, t.Seconds, t.Milliseconds);
		if (log != null) log.LogLine($"Done with test {this.TestId} in {durationString}!");
	}

	private float getFitnessAverage(List<MapParticle> population) {
		float fitAvg = 0F;
		for (int i = 0; i < population.Count; i++) {
			fitAvg += population[i].Fitness;
		}
		fitAvg = fitAvg / population.Count;
		return fitAvg;
	}

	private float getFitnessMedian(List<MapParticle> population) {
		int count = population.Count;
		int mid  = population.Count / 2;
		if (count % 2 == 0) {
			return (population[mid - 1].Fitness + population[mid].Fitness) / 2F;
		}
		return population[mid].Fitness;
	}

	float getFivePercentAvg(List<MapParticle> population) {
		float fivePerBestAvg = 0F;
		int fivePercentCount = (int) Mathf.Ceil((population.Count / 100F) * 5F);
		int lowerIndex = Mathf.Max(population.Count - fivePercentCount, 0);
		for (int i = population.Count - 1; i >= lowerIndex; i--) {
			fivePerBestAvg += population[i].Fitness;
		}
		fivePerBestAvg = fivePerBestAvg / fivePercentCount;
		return fivePerBestAvg;
	}

}
