using Godot;
using System;

public class GenerationResult {
	public int TestId;
	public int BestFitness;
	public int GenerationNumber;
	public float FivePercentBestFitnessAvg;
	public bool IsBestPlayable;
	public float FitnessAverage;
	public float FitnessMedian;
	public int PopulationSize;
	public int GenChunkSize;
	public double paramW;
	public double paramCMin;
	public double paramCMax;
	public double paramRMin;
	public double paramRMax;
	public double w;
	public double c1;
	public double c2;
	public double r1;
	public double r2;
	public double ExecutionTimeMs;

	public static string[] GetHeaderStringArray() {
		string[] csvHeaderArray = {
			"TestId",
			"BestFitness",
			"GenerationNumber",
			"FivePercentBestFitnessAvg",
			"IsBestPlayable",
			"FitnessAverage",
			"FitnessMedian",
			"PopulationSize",
			"GenChunkSize",
			"BestIndividualW",
			"BestIndividualC1",
			"BestIndividualC2",
			"BestIndividualR1",
			"BestIndividualR2",
			"paramW",
			"paramCMin",
			"paramCMax",
			"paramRMin",
			"paramRMax",
			"ExecutionTimeMs"
		};
		return csvHeaderArray;
	}
	public string[] GetCsvStringArray() {
		string[] csvStringArray = {
			$"{TestId}",
			$"{BestFitness}",
			$"{GenerationNumber}",
			$"{FivePercentBestFitnessAvg}",
			$"{IsBestPlayable}",
			$"{FitnessAverage}",
			$"{FitnessMedian}",
			$"{PopulationSize}",
			$"{GenChunkSize}",
			$"{w}",
			$"{c1}",
			$"{c2}",
			$"{r1}",
			$"{r2}",
			$"{paramW}",
			$"{paramCMin}",
			$"{paramCMax}",
			$"{paramRMin}",
			$"{paramRMax}",
			$"{ExecutionTimeMs}"
		};
		return csvStringArray;
	}
}
