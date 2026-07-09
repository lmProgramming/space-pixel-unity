namespace LMPro.DataStructures
{
    public static class Helpers
    {
        public static T[,] Make2DArray<T>(T[] input, int height, int width)
        {
            var output = new T[width, height];
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                output[x, y] = input[y * width + x];

            return output;
        }
    }
}