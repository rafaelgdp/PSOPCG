using Godot;
using System;
using System.Collections.Generic;
using System.Security.AccessControl;

public class ParametersUI : VBoxContainer
{

    AlgorithmParameter[] parameters = {
        new AlgorithmParameter("Population Size", 10, 100, 1, "Population", "The number of individuals in the population."),
        new AlgorithmParameter("Max Iterations", 10, 200, 1, "MaxIterations", "The number of PSO Iterations."),
        new AlgorithmParameter("Particle Width", 10, 100, 1, "ParticleWidth", "The horizontal width (in blocks) of each generated chunk."),
        new AlgorithmParameter("Particle Reference Width", 10, 100, 1, "ParticleReferenceWidth", "The horizontal width (in blocks) of the adjacent static reference for the newly generated chunk."),
        new AlgorithmParameter("W", 0.0, 1, 0.01, "W", "W PSO parameter."),
        new AlgorithmParameter("C min", 0.0, 1, 0.01, "Cmin", "Minimum value for C PSO parameter."),
        new AlgorithmParameter("C max", 0.0, 1, 0.01, "Cmax", "Maximum value for C PSO parameter."),
        new AlgorithmParameter("R min", 0.0, 1, 0.01, "Rmin", "Minimum value for R PSO parameter."),
        new AlgorithmParameter("R max", 0.0, 1, 0.01, "Rmax", "Maximum value for R PSO parameter.")
    };
    Dictionary<string, Label> paramLabels = new Dictionary<string, Label>();
    Dictionary<string, HSlider> paramSliders = new Dictionary<string, HSlider>();

    public override void _Ready() {

        foreach (AlgorithmParameter p in parameters) {

            var pLabel = new Label() {
                Text = p.Name,
                MouseFilter = MouseFilterEnum.Stop,
                HintTooltip = p.Description
            };
            paramLabels[p.Key] = pLabel;
            AddChild(pLabel);

            var pSlider = new HSlider() {
                MinValue = p.Min,
                MaxValue = p.Max,
                Step = p.Step,
                Value = (float) Global.Parameters[p.Key],
            };
            paramSliders[p.Key] = pSlider;
            AddChild(pSlider);

        }

        MoveChild(GetNode<Button>("SetParamsButton"), GetChildCount() - 1);
        MoveChild(GetNode<Panel>("TimeLeft"), GetChildCount() - 1);

    }

    public override void _Process(float delta) {
        foreach (AlgorithmParameter p in parameters) {
            paramLabels[p.Key].Text = $"{p.Name}: {paramSliders[p.Key].Value}";
        }
    }

    private void OnSetParams() {
        foreach (AlgorithmParameter p in parameters) {
            Global.Parameters[p.Key] = paramSliders[p.Key].Value;
        }
        GetTree().ReloadCurrentScene();
    }

}
