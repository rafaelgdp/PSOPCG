using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MapGeneratorAnalyzer : Node
{
    int[] populationSizes = {10, 25, 50, 100, 250, 500, 750, 1000};
    float[] mutationRates = {0.01F, 0.025F, 0.05F, 0.1F};
    int[] maxIterations = {1000};
    
    RTLLog logLabel;

    List<TestConfiguration> configurations = new List<TestConfiguration>();
    public override void _Ready() {
        logLabel = GetNode("CanvasLayer/RTLLog") as RTLLog;
        logLabel.LogLine("Prepping configs...");
        foreach (var populationSize in populationSizes) {
            foreach (var mutationRate in mutationRates) {
                foreach (var maxIteration in maxIterations) {
                    GD.Print($"{populationSize}, {mutationRate}, {maxIteration}");
                    var config = new TestConfiguration(
                                        populationSize, // int popupationSize,
                                        mutationRate, // float mutationRate,
                                        maxIteration, // int maxIterations,
                                        100, // int genChunkSize,
                                        5 // int refChunkSize
                                    );
                    configurations.Add(config);
                }
            }
        }
        logLabel.LogLine("Configs ready.");
    }

    void _on_AnalysisScene_ready() {
        Task.Run(() => runTests());
    }

    void runTests() {
        foreach (var configuration in configurations) {
            configuration.StartTest(logLabel);
        }
        logLabel.LogLine("Done!");
    }

}
