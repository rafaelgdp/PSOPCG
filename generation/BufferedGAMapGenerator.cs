using Godot;
using System;

public class BufferedGAMapGenerator : GAMapGenerator
{
    private 
    public BufferedGAMapGenerator(int width, int height, int center, int populationSize = 100, float mutationRate = 0.05F)
        : base(width, height, center, populationSize, mutationRate) {

        }
}
