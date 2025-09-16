namespace ShowCraftable;

public class ShowCraftableConfig
{
    public bool EnableFetchButton { get; set; } = true;

    public int SearchDistanceItems { get; set; } = 20;

    public void Normalize()
    {
        if (SearchDistanceItems < 0)
        {
            SearchDistanceItems = 0;
        }
    }
}
