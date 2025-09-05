public class BoxedArray
{
	public byte[] buffer;

	public BoxedArray CheckCreated()
	{
		if (buffer == null)
		{
			buffer = new byte[32];
		}
		return this;
	}

	public virtual void Dispose()
	{
		buffer = null;
	}
}
