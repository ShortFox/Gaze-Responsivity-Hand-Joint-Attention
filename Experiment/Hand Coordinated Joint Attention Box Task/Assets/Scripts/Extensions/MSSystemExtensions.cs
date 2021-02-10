//System extension method to shuffle array in O(n) time. Taken from: https://stackoverflow.com/questions/108819/best-way-to-randomize-an-array-with-net
namespace System
{
    using Collections.Generic;
    public static class MSSystemExtenstions
    {
        private static Random rng = new Random();
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}