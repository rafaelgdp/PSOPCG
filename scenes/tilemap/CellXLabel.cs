using Godot;
using System;

public class CellXLabel : Node2D
{
    private int globalX = 0;
    public int GlobalX {
        get { return globalX; }
        set {
            globalX = value;
            if (label != null) {
                label.Text = $"{value}";
            } else {
                SetProcess(true);
            }
        }
    }

    private Label label;
    public override void _Ready()
    {
        label = GetNode("Label") as Label;
    }

    public override void _Process(float delta) {
        if (label != null) {
            label.Text = $"{globalX}";
            SetProcess(false);
        }
    }
}
