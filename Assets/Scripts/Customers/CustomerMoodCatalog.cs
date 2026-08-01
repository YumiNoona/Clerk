using UnityEngine;

[CreateAssetMenu(
    fileName = "Customer Mood Catalog",
    menuName = "Clerk/Customers/Mood Catalog")]
public sealed class CustomerMoodCatalog : ScriptableObject
{
    public Texture2D Excited;
    public Texture2D Happy;
    public Texture2D Neutral;
    public Texture2D Annoyed;
    public Texture2D Angry;
    public Texture2D Furious;

    private readonly Sprite[] sprites = new Sprite[6];

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

    public Sprite GetSprite(CustomerMood mood)
    {
        int index = Mathf.Clamp((int)mood,0,sprites.Length - 1);
        if (sprites[index] != null)
        {
            return sprites[index];
        }

        Texture2D texture = GetTexture(mood);
        if (texture == null)
        {
            return null;
        }

        sprites[index] = Sprite.Create(
            texture,
            new Rect(0f,0f,texture.width,texture.height),
            new Vector2(0.5f,0.5f),
            Mathf.Max(texture.width,texture.height));
        sprites[index].name = mood + " Mood Sprite";
        return sprites[index];
    }
}
