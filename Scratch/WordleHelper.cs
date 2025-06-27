namespace Scratch;

public static class WordleHelper
{
    private const string Header = 
        """
          Please input wordle progress in the following format with each letter separated by a space, or qq to exit:
              - Yellow letters: [letter]y[list of invalid spaces]
                  Note: a '-' can be added to the end for each additional duplicate yellow letter.
              - Green letters: [letter]g[space #]

              E.g. Given a puzzle with a green H and yellow L with the pattern L_H__ or __H_L, input 'ly15 hg3'
                   or, given two yellow Fs and a green E in the pattern F__EF, input 'fy15- eg4

          """;

    public static void WordleLoop()
    {
        while (true)
        {
            WriteHeader();

            var input = GetInput();
            if (input == null) return;

            WriteSolutions(GetSolutionsForInput(input));
        }
    }

    private static void WriteHeader()
    {
        Console.WriteLine(Header);
    }

    private static void WriteSolutions(List<char[]> solutions)
    {
        Console.WriteLine("\nPotential solutions:");

        if (solutions.Count == 0)
        {
            Console.WriteLine("\tNo solutions found.\n");
            return;
        }

        foreach (var solution in solutions)
        {
            Console.WriteLine($"\t{new string(solution)}");
        }
    
        Console.Write("\nPress any key to continue...");
        Console.ReadKey();
    }

    private static string? GetInput()
    {
        string? input;
        do
        {
            input = Console.ReadLine();
            if (input?.Trim().Equals("qq", StringComparison.OrdinalIgnoreCase) ?? false) return null;
        } while (string.IsNullOrWhiteSpace(input));

        return input;
    }

    private static List<char[]> GetSolutionsForInput(string input)
    {
        try
        {
            var wh = WordleSolution.FromPattern(input);
            return wh.GetPossiblePatterns();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return [];
        }
    }
}
public class WordleSolution
{
    private readonly List<Letter> _letters;
    private WordleSolution(List<Letter> letters)
    {
        _letters = letters;
    }

    public static WordleSolution FromPattern(string pattern)
    {
        return new WordleSolution(
            pattern.ToLower().Split(',', ' ')
                .Select(Letter.LetterFromPattern)
                .ToList());
    }

    public List<char[]> GetPossiblePatterns()
    {
        var basePattern = "_____".ToCharArray();
        if (_letters.Count == 0) return [basePattern];

        var greenLetters = _letters.OfType<GreenLetter>()
            .GroupBy(x => x.Space)
            .Select(x => x.First())
            .ToList();

        var yellowLetters = _letters.OfType<YellowLetter>().ToList();

        foreach (var green in greenLetters)
        {
            if (basePattern[green.Space - 1] != '_')
            {
                throw new ArgumentException($"Cannot have multiple green letters in space {green.Value}");
            }
            basePattern[green.Space - 1] = green.Value;
        }

        List<char[]> solutions = [basePattern];
        var spaces = Enumerable.Range(1, 5).ToList();

        foreach (var yellow in yellowLetters)
        {
            for (var i = 0; i < yellow.Count; i++)
            {
                List<char[]> newSolutions = [];

                foreach (var location in spaces.Except(yellow.Spaces))
                {
                    foreach (var potentialSolution in solutions)
                    {
                        if (potentialSolution[location - 1] != '_') continue;
                        var newSolution = potentialSolution.Select(x => x).ToArray();
                        newSolution[location - 1] = yellow.Value;
                        newSolutions.Add(newSolution);
                    }
                }

                solutions = newSolutions;
            }
        }

        return solutions.Distinct(new CharArraySame()).ToList();
    }

    protected abstract class Letter(char value, List<int> spaces)
    {
        public char Value { get; set; } = value;
        public List<int> Spaces { get; set; } = spaces;

        public static Letter LetterFromPattern(string pattern)
        {
            pattern = pattern.ToLower();
            if (pattern.Length < 3 || !char.IsAsciiLetter(pattern[0])) throw new ArgumentNullException(nameof(pattern));

            switch (pattern[1])
            {
                case 'g':
                    if (pattern.Length != 3
                        || !char.IsAsciiDigit(pattern[2])
                        || !int.TryParse($"{pattern[2]}", out var greenSpace))
                    {
                        throw new ArgumentException($"Invalid pattern for yellow letter {pattern[0]}: {pattern}");
                    }
                    return new GreenLetter(char.ToUpper(pattern[0]), greenSpace);

                case 'y':
                    var count = pattern.Count(c => c == '-');
                    var excluded = pattern.Skip(2).SkipLast(count).Select(x => 
                        int.TryParse($"{x}", out var val)
                            ? val is >= 1 and <= 5
                                ? val
                                : throw new ArgumentException($"{val} is not a valid wordle space (1-5 only).")
                            : throw new ArgumentException($"Invalid pattern for yellow letter {pattern[0]}: {pattern}"))
                        .Distinct()
                        .ToList();
                    return new YellowLetter(char.ToUpper(pattern[0]), excluded, count + 1);
                
                default:
                    throw new ArgumentOutOfRangeException($"Pattern {pattern} is invalid.");
            }
        }
    }
    
    private class CharArraySame : EqualityComparer<char[]>
    {
        public override bool Equals(char[]? a1, char[]? a2)
        {
            if (a1 == null && a2 == null)
                return true;
            if (a1 == null || a2 == null)
                return false;

            return a1.SequenceEqual(a2);
        }

        public override int GetHashCode(char[]? arr)
        {
            return (arr ?? []).Aggregate(0, (current, c) => current ^ c.GetHashCode()).GetHashCode();
        }
    }

    protected class YellowLetter(char value, List<int> spaces, int count) : Letter(value, spaces)
    {
        public int Count => count;
    }

    protected class GreenLetter(char value, int space) : Letter(value, [space])
    {
        public int Space => space;
    }
}