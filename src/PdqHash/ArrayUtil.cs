using CommunityToolkit.HighPerformance;

internal static class Matrices
{
    public static IEnumerable<(int X, int Y)> Range(int maxX, int maxY, int minX = 0, int minY = 0)
    {
        foreach (var x in Enumerable.Range(minX, maxX))
        {
            foreach (var y in Enumerable.Range(minY, maxY))
            {
                yield return (x, y);
            }
        }
    }


    public static T[][] Fill<T>(this T[][] array, T defaultValue, int depth)
    {
        for (var i = 0; i < array.Length; i++)
        {
            array[i] = Enumerable.Repeat(defaultValue, depth).ToArray();
        }
        return array;
    }

    public static T[,] Fill<T>(this T[,] array, T defaultValue, int width, int depth)
    {
        for (var i = 0; i < width; i++)
        {
            for (var x = 0; x < depth; x++)
            {
                array[i, x] = defaultValue;
            }
        }
        return array;
    }

    public static (double Min, double Max) JaggedMax(this Span2D<double> m, int numRows, int numCols)
    {
        double max;
        double min = max = m[0, 0];

        for (var i = 0; i < numRows; i++)
        {
            for (var j = 0; j < numCols; j++)
            {
                var v = m[i, j];
                if (v < min)
                {
                    min = v;
                }
                if (v > max)
                {
                    max = v;
                }
            }
        }
        return (min, max);
    }

    public static double Torben(this Span2D<double> m, int numRows, int numCols)
    {
        var n = numRows * numCols;
        var midn = (int)(n + 1) / 2;
        var less = 0;
        var greater = 0;
        var equal = 0;
        var guess = 0.0D;
        var maxltguess = 0.0D;
        var mingtguess = 0.0D;

        var (min, max) = m.JaggedMax(numRows, numCols);

        while (true)
        {
            guess = (float)(min + max) / 2;
            less = greater = equal = 0;
            maxltguess = min;
            mingtguess = max;

            for (var _i = 0; _i < numCols; _i++)
            {
                for (var _j = 0; _j < numCols; _j++)
                {
                    var v = m[_i, _j];
                    if (v < guess)
                    {
                        less++;
                        if (v > maxltguess)
                        {
                            maxltguess = v;
                        }
                    }
                    else if (v > guess)
                    {
                        greater++;
                        if (v < mingtguess)
                        {
                            mingtguess = v;
                        }
                    }
                    else
                    {
                        equal++;
                    }
                }
            }
            if (less <= midn && greater <= midn)
            {
                break;
            }
            else if (less > greater)
            {
                max = maxltguess;
            }
            else
            {
                min = mingtguess;
            }
        }

        if (less >= midn)
        {
            return maxltguess;
        }
        else if (less + equal >= midn)
        {
            return guess;
        }
        else
        {
            return mingtguess;
        }
    }
}