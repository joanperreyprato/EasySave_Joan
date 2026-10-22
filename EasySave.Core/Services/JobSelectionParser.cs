namespace EasySave.Core.Services;

/// <summary>
/// Parses the job selection given on the command line or in the console menu
/// ("1-3" = jobs 1 to 3, "1;3" or "1,3" = jobs 1 and 3, "all" = every job).
/// </summary>
public static class JobSelectionParser
{
    private static readonly char[] Separators = [' ', ',', ';'];

    /// <summary>Returns the selected 1-based job numbers, sorted and without duplicates.</summary>
    /// <param name="input">The selection typed by the user.</param>
    /// <param name="jobCount">The number of configured jobs; numbers outside 1..jobCount are ignored.</param>
    public static List<int> Parse(string input, int jobCount)
    {
        if (input.Trim().Equals("all", StringComparison.OrdinalIgnoreCase))
            return Enumerable.Range(1, jobCount).ToList();

        var selected = new SortedSet<int>();
        foreach (var part in input.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        {
            var bounds = part.Split('-');
            if (bounds.Length == 1 && int.TryParse(part, out int single))
                AddRange(selected, single, single, jobCount);
            else if (bounds.Length == 2 && int.TryParse(bounds[0], out int from) && int.TryParse(bounds[1], out int to))
                AddRange(selected, from, to, jobCount);
        }
        return [.. selected];
    }

    private static void AddRange(SortedSet<int> selected, int from, int to, int jobCount)
    {
        for (int i = Math.Max(1, from); i <= Math.Min(jobCount, to); i++)
            selected.Add(i);
    }
}
