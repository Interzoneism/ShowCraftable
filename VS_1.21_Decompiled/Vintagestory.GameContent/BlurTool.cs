namespace Vintagestory.GameContent;

public class BlurTool
{
	public static void Blur(byte[] data, int sizeX, int sizeZ, int range)
	{
		BoxBlurHorizontal(data, range, 0, 0, sizeX, sizeZ);
		BoxBlurVertical(data, range, 0, 0, sizeX, sizeZ);
	}

	public unsafe static void BoxBlurHorizontal(byte[] map, int range, int xStart, int yStart, int xEnd, int yEnd)
	{
		fixed (byte* ptr = map)
		{
			int num = xEnd - xStart;
			int num2 = range / 2;
			int num3 = yStart * num;
			byte[] array = new byte[num];
			for (int i = yStart; i < yEnd; i++)
			{
				int num4 = 0;
				int num5 = 0;
				for (int j = xStart - num2; j < xEnd; j++)
				{
					int num6 = j - num2 - 1;
					if (num6 >= xStart)
					{
						byte b = ptr[num3 + num6];
						if (b != 0)
						{
							num5 -= b;
						}
						num4--;
					}
					int num7 = j + num2;
					if (num7 < xEnd)
					{
						byte b2 = ptr[num3 + num7];
						if (b2 != 0)
						{
							num5 += b2;
						}
						num4++;
					}
					if (j >= xStart)
					{
						byte b3 = (byte)(num5 / num4);
						array[j] = b3;
					}
				}
				for (int k = xStart; k < xEnd; k++)
				{
					ptr[num3 + k] = array[k];
				}
				num3 += num;
			}
		}
	}

	public unsafe static void BoxBlurVertical(byte[] map, int range, int xStart, int yStart, int xEnd, int yEnd)
	{
		fixed (byte* ptr = map)
		{
			int num = xEnd - xStart;
			int num2 = yEnd - yStart;
			int num3 = range / 2;
			byte[] array = new byte[num2];
			int num4 = -(num3 + 1) * num;
			int num5 = num3 * num;
			for (int i = xStart; i < xEnd; i++)
			{
				int num6 = 0;
				int num7 = 0;
				int num8 = yStart * num - num3 * num + i;
				for (int j = yStart - num3; j < yEnd; j++)
				{
					if (j - num3 - 1 >= yStart)
					{
						byte b = ptr[num8 + num4];
						if (b != 0)
						{
							num7 -= b;
						}
						num6--;
					}
					if (j + num3 < yEnd)
					{
						byte b2 = ptr[num8 + num5];
						if (b2 != 0)
						{
							num7 += b2;
						}
						num6++;
					}
					if (j >= yStart)
					{
						byte b3 = (byte)(num7 / num6);
						array[j] = b3;
					}
					num8 += num;
				}
				for (int k = yStart; k < yEnd; k++)
				{
					ptr[k * num + i] = array[k];
				}
			}
		}
	}
}
