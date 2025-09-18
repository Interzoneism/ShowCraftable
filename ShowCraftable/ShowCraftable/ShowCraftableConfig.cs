namespace ShowCraftable;

public class ShowCraftableConfig
{
    public bool EnableFetchButton { get; set; } = true;

    public int SearchDistanceItems { get; set; } = 20;

    public int AllStacksPartitions { get; set; } = -1;

    public bool alwaysCheckCraftable = false;

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
