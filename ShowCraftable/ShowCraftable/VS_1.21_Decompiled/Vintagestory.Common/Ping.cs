namespace Vintagestory.Common;

public class Ping
{
	private int RoundtripTimeMilliseconds;

	private bool didReplyOnLast;

	private long timeSendMilliseconds;

	private int timeoutThreshold;

	public long TimeReceivedUdp;

	public bool DidReplyOnLastPing => didReplyOnLast;

	public long TimeSendMilliSeconds => timeSendMilliseconds;

	public Ping()
	{
		RoundtripTimeMilliseconds = 0;
		didReplyOnLast = true;
		timeSendMilliseconds = 0L;
		TimeReceivedUdp = 0L;
		timeoutThreshold = 15;
	}

	public int GetTimeoutThreshold()
	{
		return timeoutThreshold;
	}

	public void SetTimeoutThreshold(int value)
	{
		timeoutThreshold = value;
	}

	public void OnSend(long ElapsedMilliseconds)
	{
		timeSendMilliseconds = ElapsedMilliseconds;
		didReplyOnLast = false;
	}

	public void OnReceive(long ElapsedMilliseconds)
	{
		RoundtripTimeMilliseconds = (int)(ElapsedMilliseconds - timeSendMilliseconds);
		didReplyOnLast = true;
	}

	public void OnReceiveUdp(long elapsedMilliseconds)
	{
		TimeReceivedUdp = elapsedMilliseconds;
	}

	public bool DidUdpTimeout(long elapsedMilliseconds)
	{
		return elapsedMilliseconds - TimeReceivedUdp > 30000;
	}

	public bool DidTimeout(long ElapsedMilliseconds)
	{
		if ((ElapsedMilliseconds - timeSendMilliseconds) / 1000 > timeoutThreshold)
		{
			didReplyOnLast = true;
			return true;
		}
		return false;
	}

	internal int RoundtripTimeTotalMilliseconds()
	{
		return RoundtripTimeMilliseconds;
	}
}
