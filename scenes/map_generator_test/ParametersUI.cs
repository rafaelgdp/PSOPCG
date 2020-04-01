using Godot;
using System;

public class ParametersUI : VBoxContainer
{
    Label populationLabel;
    HSlider populationSlider;
    Label geneticWidthLabel;
    HSlider geneticWidthSlider;
    Label mutationRateLabel;
    HSlider mutationRateSlider;
    Label maxIterationsLabel;
    HSlider maxIterationsSlider;

    public override void _Ready() {
        
        populationLabel = GetNode<Label>("PopulationLabel");
        populationSlider = GetNode<HSlider>("PopulationHSlider");
        geneticWidthLabel = GetNode<Label>("GeneticWidthLabel");
        geneticWidthSlider = GetNode<HSlider>("GeneticWidthHSlider");
        mutationRateLabel = GetNode<Label>("MutationRateLabel");
        mutationRateSlider = GetNode<HSlider>("MutationRateHSlider");
        maxIterationsLabel = GetNode<Label>("MaxIterationsLabel");
        maxIterationsSlider = GetNode<HSlider>("MaxIterationsHSlider");

        populationLabel.Text = $"Population: {Global.Population}";
        geneticWidthLabel.Text = $"Genetic Width: {Global.GeneticWidth}";
        mutationRateLabel.Text = $"Mutation Rate: {Global.MutationRate}";
        maxIterationsLabel.Text = $"Max Iterations: {Global.MaxIterations}";

        populationSlider.Value = Global.Population;
        geneticWidthSlider.Value = Global.GeneticWidth;
        mutationRateSlider.Value = Global.MutationRate;
        maxIterationsSlider.Value = Global.MaxIterations;

    }

    public override void _Process(float delta) {
        populationLabel.Text = $"Population: {(int)populationSlider.Value}";
        geneticWidthLabel.Text = $"Genetic Width: {(int)geneticWidthSlider.Value}";
        mutationRateLabel.Text = $"Mutation Rate: {(float)mutationRateSlider.Value}";
        maxIterationsLabel.Text = $"Max Iterations: {(int)maxIterationsSlider.Value}";
    }

    private void OnSetParams() {
        Global.Population = (int) populationSlider.Value;
        Global.MutationRate = (float) mutationRateSlider.Value;
        Global.GeneticWidth = (int) geneticWidthSlider.Value;
        Global.MaxIterations = (int) maxIterationsSlider.Value;
        GetTree().ReloadCurrentScene();
    }

}
