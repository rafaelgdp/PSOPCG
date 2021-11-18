using Godot;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MapGeneratorAnalyzer : Node
{
    int[] populationSizes = { 10, 25, 50, 75, 100, 125, 150 };
    int[] maxIterations = { 1000 };
    double[] paramW = { 0.01, 0.1 };
    double[] paramCMax = { 0.3, 0.5 };
    double[] paramCMin = { 0.01, 0.1 };
    double[] paramRMin = { -0.2, -0.01 };
    double[] paramRMax = { 0.01, 0.2 };
    RTLLog logLabel;

    List<TestConfiguration> configurations = new List<TestConfiguration>();
    public override void _Ready()
    {
        logLabel = GetNode("CanvasLayer/RTLLog") as RTLLog;
        logLabel.LogLine("Prepping configs...");
        foreach (var populationSize in populationSizes)
            foreach (var maxIteration in maxIterations)
                foreach (var pw in paramW)
                    foreach (var cMin in paramCMin)
                        foreach (var cMax in paramCMax)
                            foreach (var rMin in paramRMin)
                                foreach (var rMax in paramRMax)
                                {
                                    GD.Print($"{populationSize}, {maxIteration}");
                                    var config = new TestConfiguration(populationSize, maxIteration, 100, 5, pw, cMin, cMax, rMin, rMax);
                                    configurations.Add(config);
                                }

        logLabel.LogLine("Configs ready.");
    }

    void _on_AnalysisScene_ready()
    {
        Task.Run(() => runTests());
    }

    void runTests()
    {
        foreach (var configuration in configurations)
        {
            configuration.StartTest(logLabel);
        }
        logLabel.LogLine("Done!");
    }

}
