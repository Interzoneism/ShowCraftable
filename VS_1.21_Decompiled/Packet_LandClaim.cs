public class Packet_LandClaim
{
	public byte[] Data;

	public const int DataFieldID = 1;

	public int size;

	public void SetData(byte[] value)
	{
		Data = value;
	}

	internal void InitializeValues()
	{
	}
}
