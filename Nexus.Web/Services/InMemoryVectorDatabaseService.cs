namespace Nexus.Web.Services;

public record VectorDocument(string Id, string Source, string Text, IReadOnlyDictionary<string, double> Vector);

public interface IVectorDatabaseService
{
    IReadOnlyList<VectorDocument> Search(string query, IEnumerable<(string Id, string Source, string Text)> documents, int limit = 5);
}

public class InMemoryVectorDatabaseService : IVectorDatabaseService
{
    public IReadOnlyList<VectorDocument> Search(string query, IEnumerable<(string Id, string Source, string Text)> documents, int limit = 5)
    {
        var queryVector = Vectorize(query);
        return documents
            .Where(document => !string.IsNullOrWhiteSpace(document.Text))
            .Select(document => new VectorDocument(document.Id, document.Source, document.Text, Vectorize(document.Text)))
            .Select(document => new { Document = document, Score = CosineSimilarity(queryVector, document.Vector) })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Document.Text.Length)
            .Take(limit)
            .Select(item => item.Document)
            .ToList();
    }

    private static IReadOnlyDictionary<string, double> Vectorize(string value) => Tokenize(value)
        .GroupBy(token => token, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => (double)group.Count(), StringComparer.OrdinalIgnoreCase);

    private static double CosineSimilarity(IReadOnlyDictionary<string, double> left, IReadOnlyDictionary<string, double> right)
    {
        var dot = left.Sum(pair => right.TryGetValue(pair.Key, out var value) ? pair.Value * value : 0);
        var leftMagnitude = Math.Sqrt(left.Values.Sum(value => value * value));
        var rightMagnitude = Math.Sqrt(right.Values.Sum(value => value * value));
        return leftMagnitude == 0 || rightMagnitude == 0 ? 0 : dot / (leftMagnitude * rightMagnitude);
    }

    private static IEnumerable<string> Tokenize(string value) => value.ToLowerInvariant()
        .Split([' ', '\r', '\n', '\t', '.', ',', ';', ':', '?', '!', '(', ')', '[', ']', '/', '\\', '-'], StringSplitOptions.RemoveEmptyEntries)
        .Where(token => token.Length > 2);
}
