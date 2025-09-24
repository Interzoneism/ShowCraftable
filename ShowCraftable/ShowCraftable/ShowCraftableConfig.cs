namespace ShowCraftable;

public class ShowCraftableConfig
{
    public bool EnableFetchButton { get; set; } = true;

    public bool DisableFetchButtonOnServer { get; set; } = false;

    public bool UseDefaultFont { get; set; } = false;

    public int SearchDistanceItems { get; set; } = 20;

    public int AllStacksPartitions { get; set; } = -1;

    public void Normalize()
    {
        if (SearchDistanceItems < 0)
        {
            SearchDistanceItems = 0;
        }

        if (AllStacksPartitions < -1)
        {
            AllStacksPartitions = -1;
        }
    }
}
