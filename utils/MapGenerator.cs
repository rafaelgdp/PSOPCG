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
    protected int width;
    public int Width { get { return width; } }
    protected int height;
    public int Height { get { return height; } }
    protected int leftmostGlobalX;
    public int LeftmostGlobalX { get { return leftmostGlobalX; } }
    protected int leftmostIndex = 0;
    public int RightmostGlobalX { get { return leftmostGlobalX + width; }}
    protected char[,] matrix;
    protected Random random = new Random();
    protected int floorZeroHeight = 3;
    public MapGenerator(int width = 15, int height = 12, int globalOriginX = 0) {
        this.width = width;
        this.height = height;
        this.leftmostIndex = 0;
        this.matrix = new char[width + 2, height]; // +2 is workaround for corner cases
        for (int i = 0; i < width; i++) {
            for (int j = 0; j < height; j++) {
                this.matrix[i, j] = 'B';
            }
        }
        initializeMap();
    }

    protected void initializeMap() {
        for (int i = LeftmostGlobalX; i < RightmostGlobalX; i++) {
            for (int j = 0; j < floorZeroHeight; j++) {
                SetGlobalCell(i, j, 'G');
            }
        }
    }

    public char GetGlobalCell(int x, int y) {
        y = Mathf.Abs(y); // Compatible with positive and negative Y values.
        if (y >= Height) {
            // If above y limits, simply return default (e.g. Blank).
            return DefaultCell;
        }

        if (x < LeftmostGlobalX || x > RightmostGlobalX) {
            RegenerateChunkCenteredAt(x);
        }

        int localX = (leftmostIndex + x - LeftmostGlobalX) % width;
        int localY = y;
        return matrix[localX, localY];
    }

    public char SetGlobalCell(int x, int y, char newValue) {
        
        y = Mathf.Abs(y); // Compatible with positive and negative Y values.
        if (y >= Height) {
            // If above y limits, simply return default (e.g. Blank).
            // And do NOTHING.
            return DefaultCell;
        }

        if (x < LeftmostGlobalX || x > RightmostGlobalX) {
            RegenerateChunkCenteredAt(x);
        }

        int localX = (leftmostIndex + x - LeftmostGlobalX) % width;
        int localY = y;
        var oldValue = matrix[localX, localY];
        matrix[localX, localY] = newValue;
        return oldValue;
    }

    public void RegenerateChunkCenteredAt(int newCenterOriginX)
    {
        // New limits
        int newLeftmostGlobalX = newCenterOriginX - (width / 2);
        int newRightmostGlobalX = newLeftmostGlobalX + width;

        // Find intersection
        if (newRightmostGlobalX < LeftmostGlobalX || newLeftmostGlobalX > RightmostGlobalX) {
            // regenerate whole matrix
            leftmostGlobalX = newLeftmostGlobalX;
            leftmostIndex = 0;
            generateMap(newLeftmostGlobalX, newRightmostGlobalX);
        } else {
            // There is partial intersection
            // Regenerate new needed area
            int intersectionLeftLimit = Mathf.Max(newLeftmostGlobalX, LeftmostGlobalX);
            int intersectionRightLimit = Mathf.Min(newRightmostGlobalX, RightmostGlobalX);

            int generationLeftLimit;
            int generationRightLimit;
            if (newLeftmostGlobalX < LeftmostGlobalX) {
                generationLeftLimit = newLeftmostGlobalX;
                generationRightLimit = LeftmostGlobalX - 1;
            } else {
                generationLeftLimit = RightmostGlobalX + 1;
                generationRightLimit = newRightmostGlobalX;
            }

            var newLeftmostIndex = getLocalXIndexFromGlobalX(newLeftmostGlobalX);
            if (newLeftmostIndex == -1) {
                var intersectionWidth = intersectionRightLimit - intersectionLeftLimit;
                newLeftmostIndex = (leftmostIndex + intersectionWidth + 1) % width;
            }
            leftmostGlobalX = newLeftmostGlobalX;
            leftmostIndex = newLeftmostIndex;
            // var isNewChunkOnLeft = newLeftmostIndex < intersectionLeftLimit;
            generateMap(generationLeftLimit, generationRightLimit);
        }
    }

    /*
        Returns -1 if globalX is out of the buffered range.
        Otherwise, returns the matrix column that corresponds to globalX.
    */
    protected int getLocalXIndexFromGlobalX(int globalX) {
        if (globalX < LeftmostGlobalX || globalX > RightmostGlobalX) return -1;
        return (globalX - LeftmostGlobalX + leftmostIndex - 1) % width;
    }
    protected virtual void generateMap(int generatedLeftmostX, int generatedRightmostX) {

        for (int i = generatedLeftmostX; i < generatedRightmostX; i++) {
            for (int j = 0; j < floorZeroHeight; j++) {
                SetGlobalCell(i, j, 'N');
            }
        }
    }

    public override String ToString() {
        String r = "";
        for(int j = Height - 1; j >= 0; j--) {
            r += $"{j,6:000000}: ";
            for (int i = 0; i < Width; i++) {
                r += $"{matrix[i,j]}";
            }
            r += "\n";
        }
        int digitosLargura = (Width - 1).ToString().Length;
        for (int digito = 0; digito < digitosLargura; digito++) {
            r += "        ";
            for (int x = 0; x < Width; x++) {
                var xs = x.ToString();
                r += xs.Length > digito ? $"{xs[digito]}" : "0";
            }
            r += "\n";
        }
        return r;
    }

    public String ToRichTextString() {
        String r = "";
        for(int j = Height - 1; j >= 0; j--) {
            r += $"{j,6:000000}: ";
            for (int i = 0; i < Width; i++) {
                switch(matrix[i,j]) {
                    case 'G':
                        r += "[color=red]";
                        break;
                    default:
                        r += "[color=cyan]";
                        break;
                }
                r += $"{matrix[i,j]}";
                r += "[/color]";
            }
            r += "\n";
        }
        int digitosLargura = 4;//(Width - 1).ToString().Length;
        for (int digito = 0; digito < digitosLargura; digito++) {
            r += "        ";
            for (int x = 0; x < Width; x++) {
                var xs = $"{x,4:0000}";
                if (x == leftmostIndex) {
                    r += "[color=blue]";
                    r += xs[digito];
                    r += "[/color]";
                } else {
                    r += "[color=lime]";
                    r += xs[digito];
                    r += "[/color]";
                }
            }
            r += "\n";
        }

        int[] zeroBasedX = new int[width];
        for (int x = LeftmostGlobalX; x <= RightmostGlobalX; x++) {
            zeroBasedX[getLocalXIndexFromGlobalX(x)] = x;
        }
        digitosLargura = 4;//(RightmostGlobalX).ToString().Length;
        for (int digito = 0; digito < digitosLargura; digito++) {
            r += "        ";
            for (int x = 0; x < zeroBasedX.Length; x++) {
                var xs = $"{zeroBasedX[x],4:0000}";
                if (zeroBasedX[x] < 0) {
                    r += "[color=purple]";
                    r += xs[digito+1];
                    //r += xs.Length > digito ? $"{xs[digito]}" : "0";
                    r += "[/color]";
                } else {
                    r += "[color=aqua]";
                    r += xs[digito];
                    // r += xs.Length > digito ? $"{xs[digito]}" : "0";
                    r += "[/color]";
                }
                
            }
            r += "\n";
        }

        return r;
    }
}
