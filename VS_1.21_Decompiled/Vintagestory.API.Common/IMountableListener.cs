namespace Vintagestory.API.Common;

public interface IMountableListener
{
	void DidUnmount(EntityAgent entityAgent);

	void DidMount(EntityAgent entityAgent);
}
