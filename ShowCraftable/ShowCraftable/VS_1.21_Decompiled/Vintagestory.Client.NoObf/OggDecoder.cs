using System;
using System.IO;
using Vintagestory.API.Common;
using csogg;
using csvorbis;

namespace Vintagestory.Client.NoObf;

public class OggDecoder
{
	private const int buffersize = 8192;

	[ThreadStatic]
	private static byte[] convbuffer;

	public AudioMetaData OggToWav(Stream ogg, IAsset asset)
	{
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Expected O, but got Unknown
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Expected O, but got Unknown
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected O, but got Unknown
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Expected O, but got Unknown
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Expected O, but got Unknown
		AudioMetaData audioMetaData = new AudioMetaData(asset);
		audioMetaData.Loaded = 1;
		TextWriter textWriter = new StringWriter();
		Stream stream = null;
		MemoryStream memoryStream = null;
		stream = ogg;
		memoryStream = new MemoryStream();
		SyncState val = new SyncState();
		StreamState val2 = new StreamState();
		Page val3 = (Page)(object)new OggPage();
		Packet val4 = new Packet();
		Info val5 = new Info();
		Comment val6 = new Comment();
		DspState val7 = new DspState();
		Block val8 = new Block(val7);
		int num = 0;
		val.init();
		int num2 = 0;
		int offset = val.buffer(4096);
		byte[] data = val.data;
		try
		{
			num = stream.Read(data, offset, 4096);
		}
		catch (Exception ex)
		{
			textWriter.WriteLine(LoggerBase.CleanStackTrace(ex.ToString()));
		}
		val.wrote(num);
		if (val.pageout(val3) != 1)
		{
			if (num < 4096)
			{
				goto IL_04fd;
			}
			textWriter.WriteLine("Input does not appear to be an Ogg bitstream.");
		}
		val2.init(val3.serialno());
		val5.init();
		val6.init();
		if (val2.pagein(val3) < 0)
		{
			textWriter.WriteLine("Error reading first page of Ogg bitstream data.");
		}
		if (val2.packetout(val4) != 1)
		{
			textWriter.WriteLine("Error reading initial header packet.");
		}
		if (val5.synthesis_headerin(val6, val4) < 0)
		{
			textWriter.WriteLine("This Ogg bitstream does not contain Vorbis audio data.");
		}
		int i = 0;
		while (i < 2)
		{
			while (i < 2)
			{
				switch (val.pageout(val3))
				{
				case 1:
					val2.pagein(val3);
					for (; i < 2; val5.synthesis_headerin(val6, val4), i++)
					{
						switch (val2.packetout(val4))
						{
						case -1:
							textWriter.WriteLine("Corrupt secondary header.  Exiting.");
							continue;
						default:
							continue;
						case 0:
							break;
						}
						break;
					}
					continue;
				default:
					continue;
				case 0:
					break;
				}
				break;
			}
			offset = val.buffer(4096);
			data = val.data;
			try
			{
				num = stream.Read(data, offset, 4096);
			}
			catch (Exception ex2)
			{
				textWriter.WriteLine(LoggerBase.CleanStackTrace(ex2.ToString()));
			}
			if (num == 0 && i < 2)
			{
				textWriter.WriteLine("End of file before finding all Vorbis headers!");
			}
			val.wrote(num);
		}
		byte[][] user_comments = val6.user_comments;
		for (int j = 0; j < val6.user_comments.Length && user_comments[j] != null; j++)
		{
			textWriter.WriteLine(val6.getComment(j));
		}
		textWriter.WriteLine("\nBitstream is " + val5.channels + " channel, " + val5.rate + "Hz");
		textWriter.WriteLine("Encoded by: " + val6.getVendor() + "\n");
		audioMetaData.Channels = val5.channels;
		audioMetaData.Rate = val5.rate;
		int num3 = 4096 / val5.channels;
		val7.synthesis_init(val5);
		val8.init(val7);
		float[][][] array = new float[1][][];
		int[] array2 = new int[val5.channels];
		if (convbuffer == null)
		{
			convbuffer = new byte[8192];
		}
		while (num2 == 0)
		{
			while (num2 == 0)
			{
				switch (val.pageout(val3))
				{
				case -1:
					textWriter.WriteLine("Corrupt or missing data in bitstream; continuing...");
					continue;
				default:
					val2.pagein(val3);
					while (true)
					{
						switch (val2.packetout(val4))
						{
						case -1:
							continue;
						default:
						{
							if (val8.synthesis(val4) == 0)
							{
								val7.synthesis_blockin(val8);
							}
							int num4;
							while ((num4 = val7.synthesis_pcmout(array, array2)) > 0)
							{
								float[][] array3 = array[0];
								bool flag = false;
								int num5 = ((num4 < num3) ? num4 : num3);
								for (i = 0; i < val5.channels; i++)
								{
									int num6 = i * 2;
									int num7 = array2[i];
									for (int k = 0; k < num5; k++)
									{
										int num8 = (int)((double)array3[i][num7 + k] * 32767.0);
										if (num8 > 32767)
										{
											num8 = 32767;
											flag = true;
										}
										if (num8 < -32768)
										{
											num8 = -32768;
											flag = true;
										}
										if (num8 < 0)
										{
											num8 |= 0x8000;
										}
										convbuffer[num6] = (byte)num8;
										convbuffer[num6 + 1] = (byte)((uint)num8 >> 8);
										num6 += 2 * val5.channels;
									}
								}
								memoryStream.Write(convbuffer, 0, 2 * val5.channels * num5);
								val7.synthesis_read(num5);
							}
							continue;
						}
						case 0:
							break;
						}
						break;
					}
					if (val3.eos() != 0)
					{
						num2 = 1;
					}
					continue;
				case 0:
					break;
				}
				break;
			}
			if (num2 == 0)
			{
				offset = val.buffer(4096);
				data = val.data;
				try
				{
					num = stream.Read(data, offset, 4096);
				}
				catch (Exception ex3)
				{
					textWriter.WriteLine(LoggerBase.CleanStackTrace(ex3.ToString()));
				}
				val.wrote(num);
				if (num == 0)
				{
					num2 = 1;
				}
			}
		}
		val2.clear();
		val8.clear();
		val7.clear();
		val5.clear();
		goto IL_04fd;
		IL_04fd:
		val.clear();
		stream.Close();
		audioMetaData.Pcm = memoryStream.ToArray();
		return audioMetaData;
	}
}
