using System.Numerics;
using System.Text;

namespace Warmup;

public static class BookExercises
{
    public static bool IsPowerOfTwo(int bookId) => bookId > 0 && BitOperations.PopCount((uint)bookId) == 1;

    public static string ReverseTitle(string title)
    {
        ArgumentNullException.ThrowIfNull(title);

        return string.Concat(title.Reverse());
    }

    public static string RepeatTitle(string title, int times)
    {
        ArgumentNullException.ThrowIfNull(title);
        ArgumentOutOfRangeException.ThrowIfNegative(times);

        return new StringBuilder(checked(title.Length * times)).Insert(0, title, times).ToString();
    }

    public static IEnumerable<int> OddBookIdsUpTo(int limit)
    {
        // (limit + 1) / 2, without overflowing at int.MaxValue.
        var count = limit < 1 ? 0 : limit / 2 + limit % 2;

        return Enumerable.Range(0, count).Select(n => 2 * n + 1);
    }
}
