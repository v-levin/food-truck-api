using FoodTruckApi.Application.Abstractions;
using SmartComponents.LocalEmbeddings;

namespace FoodTruckApi.Infrastructure.Matching;

/// <summary>
/// <see cref="IFoodEmbedder"/> backed by the on-device <c>bge-micro-v2</c> model
/// (SmartComponents.LocalEmbeddings). The model is a NuGet build-time download, runs on
/// CPU, and never leaves the machine. Vectors are normalized to unit length so callers can
/// take a dot product for cosine similarity.
/// </summary>
internal sealed class LocalFoodEmbedder : IFoodEmbedder, IDisposable
{
    private readonly LocalEmbedder _embedder = new();

    public float[] Embed(string text)
    {
        var vector = _embedder.Embed(text ?? string.Empty).Values.ToArray();
        NormalizeInPlace(vector);
        return vector;
    }

    public void Dispose() => _embedder.Dispose();

    private static void NormalizeInPlace(float[] vector)
    {
        double sumOfSquares = 0d;
        foreach (var component in vector)
        {
            sumOfSquares += component * (double)component;
        }

        var magnitude = Math.Sqrt(sumOfSquares);
        if (magnitude <= 0d)
        {
            return;
        }

        for (var i = 0; i < vector.Length; i++)
        {
            vector[i] = (float)(vector[i] / magnitude);
        }
    }
}
