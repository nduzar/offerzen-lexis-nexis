namespace ProductCatalog.Api.Search;

public sealed class ProductSearchEngine
{
    private readonly object _gate = new();

    private readonly Dictionary<string, IReadOnlyList<Guid>> _cache = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<T> Search<T>(
        IEnumerable<T> items,
        string query,
        int maxResults,
        Func<T, IEnumerable<(string Text, int Weight)>> fields,
        Func<T, Guid> getId)
        where T : class
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return items.Take(maxResults).ToList();
        }

        var normalized = Normalize(query);

        IReadOnlyList<Guid>? cachedIds;
        lock (_gate)
        {
            _cache.TryGetValue(normalized, out cachedIds);
        }

        if (cachedIds is not null)
        {
            var idSet = cachedIds.ToHashSet();
            return items
                .Where(i => idSet.Contains(getId(i)))
                .Take(maxResults)
                .ToList();
        }

        var scored = items
            .Select(item => new
            {
                Item = item,
                Score = ScoreItem(item, normalized, fields)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => getId(x.Item))
            .Take(maxResults)
            .ToList();

        var result = scored.Select(x => x.Item).ToList();

        lock (_gate)
        {
            _cache[normalized] = scored.Select(x => getId(x.Item)).ToList();
        }

        return result;
    }

    private static int ScoreItem<T>(T item, string normalizedQuery, Func<T, IEnumerable<(string Text, int Weight)>> fields)
    {
        var total = 0;
        foreach (var (text, weight) in fields(item))
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            var normalizedText = Normalize(text);

            if (normalizedText.Contains(normalizedQuery, StringComparison.OrdinalIgnoreCase))
            {
                total += 100 * weight;
                continue;
            }

            var bestSimilarity = Similarity(normalizedQuery, normalizedText);
            foreach (var token in Tokenize(normalizedText))
            {
                bestSimilarity = Math.Max(bestSimilarity, Similarity(normalizedQuery, token));
            }

            if (bestSimilarity >= 0.6)
            {
                total += (int)(bestSimilarity * 80 * weight);
            }
        }

        return total;
    }

    private static double Similarity(string a, string b)
    {
        var distance = LevenshteinDistance(a, b);
        var max = Math.Max(a.Length, b.Length);
        if (max == 0)
        {
            return 0;
        }

        return 1.0 - (double)distance / max;
    }

    private static IEnumerable<string> Tokenize(string text)
    {
        var start = -1;

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            var isTokenChar = char.IsLetterOrDigit(c);

            if (isTokenChar && start < 0)
            {
                start = i;
            }

            if (!isTokenChar && start >= 0)
            {
                yield return text[start..i];
                start = -1;
            }
        }

        if (start >= 0)
        {
            yield return text[start..];
        }
    }

    private static string Normalize(string input)
    {
        return input.Trim().ToLowerInvariant();
    }

    private static int LevenshteinDistance(string a, string b)
    {
        if (a.Length == 0)
        {
            return b.Length;
        }

        if (b.Length == 0)
        {
            return a.Length;
        }

        var costs = new int[b.Length + 1];

        for (var j = 0; j < costs.Length; j++)
        {
            costs[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            var prevCost = costs[0];
            costs[0] = i;

            for (var j = 1; j <= b.Length; j++)
            {
                var currentCost = costs[j];

                var substitutionCost = a[i - 1] == b[j - 1] ? 0 : 1;
                costs[j] = Math.Min(
                    Math.Min(costs[j] + 1, costs[j - 1] + 1),
                    prevCost + substitutionCost);

                prevCost = currentCost;
            }
        }

        return costs[b.Length];
    }
}
