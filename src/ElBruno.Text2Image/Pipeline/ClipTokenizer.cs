using System.Text.Json;
using System.Text.RegularExpressions;

namespace ElBruno.Text2Image.Pipeline;

/// <summary>
/// Pure C# implementation of a CLIP BPE tokenizer for Stable Diffusion text encoding.
/// Reads vocab.json and merges.txt downloaded with the model.
/// </summary>
internal sealed class ClipTokenizer
{
    private const int MaxLength = 77;
    private const int BosTokenId = 49406; // <|startoftext|>
    private const int EosTokenId = 49407; // <|endoftext|>

    private readonly Dictionary<string, int> _vocab;
    private readonly List<(string, string)> _merges;
    private readonly Dictionary<(string, string), int> _mergeRanks;
    private readonly Dictionary<int, char> _byteEncoder;
    private readonly Dictionary<char, int> _byteDecoder;

    private static readonly Regex _pattern = new(
        @"<\|startoftext\|>|<\|endoftext\|>|'s|'t|'re|'ve|'m|'ll|'d|[\p{L}]+|[\p{N}]|[^\s\p{L}\p{N}]+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private ClipTokenizer(
        Dictionary<string, int> vocab,
        List<(string, string)> merges)
    {
        _vocab = vocab;
        _merges = merges;
        _mergeRanks = new Dictionary<(string, string), int>();
        for (int i = 0; i < merges.Count; i++)
            _mergeRanks[merges[i]] = i;

        _byteEncoder = BuildByteEncoder();
        _byteDecoder = _byteEncoder.ToDictionary(kv => kv.Value, kv => kv.Key);
    }

    /// <summary>
    /// Loads the tokenizer from vocab.json and merges.txt files in the given directory.
    /// </summary>
    public static ClipTokenizer Load(string tokenizerDir)
    {
        var vocabPath = Path.Combine(tokenizerDir, "vocab.json");
        var mergesPath = Path.Combine(tokenizerDir, "merges.txt");

        if (!File.Exists(vocabPath))
            throw new FileNotFoundException($"Tokenizer vocab.json not found at {vocabPath}");
        if (!File.Exists(mergesPath))
            throw new FileNotFoundException($"Tokenizer merges.txt not found at {mergesPath}");

        var vocabJson = File.ReadAllText(vocabPath);
        var vocab = JsonSerializer.Deserialize<Dictionary<string, int>>(vocabJson)
            ?? throw new InvalidOperationException("Failed to parse vocab.json");

        var mergeLines = File.ReadAllLines(mergesPath);
        var merges = new List<(string, string)>();
        foreach (var line in mergeLines)
        {
            if (line.StartsWith("#") || string.IsNullOrWhiteSpace(line))
                continue;
            var parts = line.Split(' ', 2);
            if (parts.Length == 2)
                merges.Add((parts[0], parts[1]));
        }

        return new ClipTokenizer(vocab, merges);
    }

    /// <summary>
    /// Tokenizes a text prompt into token IDs, padded/truncated to 77 tokens.
    /// </summary>
    public int[] Tokenize(string text)
    {
        var tokens = new List<int> { BosTokenId };

        text = text.ToLowerInvariant();
        var matches = _pattern.Matches(text);

        foreach (Match match in matches)
        {
            var word = match.Value;
            var byteTokens = EncodeWord(word);
            tokens.AddRange(byteTokens);
        }

        // Add EOS token
        if (tokens.Count < MaxLength)
            tokens.Add(EosTokenId);

        // Truncate if too long
        if (tokens.Count > MaxLength)
            tokens = tokens.Take(MaxLength).ToList();

        // Pad to MaxLength
        while (tokens.Count < MaxLength)
            tokens.Add(EosTokenId);

        return tokens.ToArray();
    }

    /// <summary>
    /// Creates the unconditional input tokens (BOS + padding).
    /// </summary>
    public static int[] CreateUnconditionalTokens()
    {
        var tokens = new int[MaxLength];
        tokens[0] = BosTokenId;
        for (int i = 1; i < MaxLength; i++)
            tokens[i] = EosTokenId;
        return tokens;
    }

    private List<int> EncodeWord(string word)
    {
        // Convert word to bytes, then to unicode representation
        var bytes = System.Text.Encoding.UTF8.GetBytes(word);
        var unicodeWord = string.Concat(bytes.Select(b => _byteEncoder[b]));

        // Split into individual characters, append </w> to last
        var bpeTokens = new List<string>();
        for (int i = 0; i < unicodeWord.Length; i++)
        {
            if (i == unicodeWord.Length - 1)
                bpeTokens.Add(unicodeWord[i] + "</w>");
            else
                bpeTokens.Add(unicodeWord[i].ToString());
        }

        // Apply BPE merges
        bpeTokens = ApplyBpe(bpeTokens);

        // Map to token IDs
        var ids = new List<int>();
        foreach (var token in bpeTokens)
        {
            if (_vocab.TryGetValue(token, out var id))
                ids.Add(id);
        }

        return ids;
    }

    private List<string> ApplyBpe(List<string> tokens)
    {
        while (tokens.Count >= 2)
        {
            // Find the pair with the lowest merge rank
            int bestRank = int.MaxValue;
            int bestIdx = -1;

            for (int i = 0; i < tokens.Count - 1; i++)
            {
                var pair = (tokens[i], tokens[i + 1]);
                if (_mergeRanks.TryGetValue(pair, out var rank) && rank < bestRank)
                {
                    bestRank = rank;
                    bestIdx = i;
                }
            }

            if (bestIdx == -1)
                break;

            // Merge the best pair
            var merged = tokens[bestIdx] + tokens[bestIdx + 1];
            tokens[bestIdx] = merged;
            tokens.RemoveAt(bestIdx + 1);
        }

        return tokens;
    }

    /// <summary>
    /// Builds the GPT-2 byte-to-unicode encoder mapping.
    /// </summary>
    private static Dictionary<int, char> BuildByteEncoder()
    {
        var bs = new List<int>();
        var cs = new List<int>();

        // Printable ASCII ranges
        for (int i = (int)'!'; i <= (int)'~'; i++) { bs.Add(i); cs.Add(i); }
        for (int i = (int)'¡'; i <= (int)'¬'; i++) { bs.Add(i); cs.Add(i); }
        for (int i = (int)'®'; i <= (int)'ÿ'; i++) { bs.Add(i); cs.Add(i); }

        int n = 0;
        for (int b = 0; b < 256; b++)
        {
            if (!bs.Contains(b))
            {
                bs.Add(b);
                cs.Add(256 + n);
                n++;
            }
        }

        var encoder = new Dictionary<int, char>();
        for (int i = 0; i < bs.Count; i++)
            encoder[bs[i]] = (char)cs[i];

        return encoder;
    }
}
