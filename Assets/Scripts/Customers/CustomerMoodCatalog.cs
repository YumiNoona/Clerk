using UnityEngine;

[CreateAssetMenu(
    fileName = "Customer Mood Catalog",
    menuName = "Store System/Customers/Mood Catalog")]
public sealed class CustomerMoodCatalog : ScriptableObject
{
    public Texture2D Excited;
    public Texture2D Happy;
    public Texture2D Neutral;
    public Texture2D Annoyed;
    public Texture2D Angry;
    public Texture2D Furious;

    public Texture2D GetTexture(CustomerMood mood)
    {
        switch (mood)
        {
            case CustomerMood.Excited:
                return Excited;
            case CustomerMood.Happy:
                return Happy;
            case CustomerMood.Neutral:
                return Neutral;
            case CustomerMood.Annoyed:
                return Annoyed;
            case CustomerMood.Angry:
                return Angry;
            case CustomerMood.Furious:
                return Furious;
            default:
                return Neutral;
        }
    }
}
