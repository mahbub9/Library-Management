using Warmup;

foreach (var bookId in (int[])[1, 2, 3, 64, 100, 1024])
{
    Console.WriteLine($"IsPowerOfTwo({bookId}) = {BookExercises.IsPowerOfTwo(bookId)}");
}

Console.WriteLine();
Console.WriteLine($"ReverseTitle(\"Moby Dick\") = \"{BookExercises.ReverseTitle("Moby Dick")}\"");

Console.WriteLine();
Console.WriteLine($"RepeatTitle(\"Read\", 3) = \"{BookExercises.RepeatTitle("Read", 3)}\"");

Console.WriteLine();
Console.WriteLine($"OddBookIdsUpTo(100) = {string.Join(", ", BookExercises.OddBookIdsUpTo(100))}");
