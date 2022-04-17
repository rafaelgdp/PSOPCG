using System.Runtime.CompilerServices;
using Godot;

class AlgorithmParameter {

    public AlgorithmParameter(
                string name,
                double min = 0,
                double max = 100,
                double step = 1,
                string key = null,
                string description = null)
    {

        Name = name;
        Description = description ?? name;
        Range[0] = min;
        Range[1] = max;
        Step = step;
        Key = key;
    }

    private string name;

    public string Name {
        get { return name; }
        set { name = value; }
    }

    private string description;
    public string Description {
        get { return description; }
        set { description = value; }
    }

    private double[] range = new double[2];
    public double[] Range {
        get { 
            return range;
        }

        set {
            range = value;
        }
    }
    private double step = 1;
    public double Step {
        get { return step; }
        set { step = value; }
    }

    public string Key { get; set; }

    public double Min {
        get { return Range[0]; }
    }

    public double Max {
        get { return Range[1]; }
    }

}