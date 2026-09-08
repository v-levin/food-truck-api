namespace FoodTruckApi.Application.Abstractions;

/// <summary>Produces a semantic embedding vector for a piece of food text.</summary>
public interface IFoodEmbedder
{
    /// <summary>A unit-length embedding vector for <paramref name="text"/>.</summary>
    float[] Embed(string text);
}
