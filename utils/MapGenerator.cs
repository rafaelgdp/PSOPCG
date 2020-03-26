// @author Rafael Guerra de Pontes

using Godot;
using System;

/*
    This class represents a Map Generator. The map is stored as a matrix of
    character. Each cell is encoded as a character that represents a block in the
    Enviroment. The buffer is initialized with a width and a height and generates
    a map within the world limits.
    Whenever a cell that lies outside the initial world bounds, a new partial
    chunk is generated, appended, the origin is adjusted with the respective
    offset and the chunk that remains farther from the new area, is erased from
    memory. The strategy for generating the chunks is based on an Evolutionary
    Algorithm.
*/
public class MapGenerator {
    public static char[] TileCodes = new char[] {
        'B', // Blank
        'G', // Ground
        'I', // Item
        'S', // Spike
        'E', // Enemy
    };

    public char DefaultCell { get { return TileCodes[0]; } } // Blank
    private int width;
    public int Width { get { return width; } }
    private int height;
    public int Height { get { return height; } }
    private int leftmostGlobalX;
    public int LeftmostGlobalX { get { return leftmostGlobalX; } }
    private int leftmostIndex = 0;
    public int RightmostGlobalX { get { return leftmostGlobalX + width; }}
    private char[,] matrix;
    private Random random = new Random();
    private int floorZeroHeight = 3;
    public MapGenerator(int width = 200, int height = 12, int globalOriginX = 0) {
        this.width = width;
        this.height = height;
        this.leftmostIndex = 0;
        this.matrix = new char[width + 2, height]; // +2 is workaround for corner cases
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                this.matrix[i, j] = 'B';
            }
        }
        initializeGround();
    }

    private void initializeGround() {
        for (int i = 0; i < this.width; i++) {
            for (int j = 0; j < floorZeroHeight; j++) {
                matrix[i, j] = 'G';
            }
        }
    }

    public char GetGlobalCell(int x, int y) {
        if (x < LeftmostGlobalX || x > RightmostGlobalX) {
            regenerateChunkCenteredAt(x);
        }

        if (y > 0 || y < -Height) {
            // If below or above y limits, simply return default (e.g. Blank).
            return DefaultCell;
        }

        int localX = (leftmostIndex + x - LeftmostGlobalX) % width;
        int localY = -y;
        return matrix[localX, localY];
    }

    private void regenerateChunkCenteredAt(int newCenterOriginX)
    {
        int newLeftmostX = newCenterOriginX - (width / 2);
        int newRightmostX = newLeftmostX + width;
        int generatedRightLimit = Mathf.Min(newLeftmostX, LeftmostGlobalX);
        int generatedLeftLimit = newLeftmostX;
        
        
        generateGround(generatedLeftLimit, generatedRightLimit, newCenterOriginX);
    }
    private void generateGround(int generatedLeftmostX, int generatedRightmostX, int newCenterOriginX) {
        for (int i = 0; i < )
    }
}
