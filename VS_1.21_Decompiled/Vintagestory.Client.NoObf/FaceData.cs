namespace Vintagestory.Client.NoObf;

public struct FaceData
{
	public const int size = 16;

	public float x;

	public float y;

	public float z;

	public int uv;

	public float dx1;

	public float dy1;

	public float dz1;

	public int uvSize;

	public int renderFlags0;

	public int renderFlags1;

	public int renderFlags2;

	public int renderFlags3;

	public float dx2;

	public float dy2;

	public float dz2;

	public int colormapData;

	public FaceData(float[] xyz, int i, float u1, float v1, float u2, float v2, int[] flags, int flagsIndex, int colorData, bool rotateUV)
	{
		x = xyz[i];
		y = xyz[i + 1];
		z = xyz[i + 2];
		dx1 = (xyz[i + 3] - x) / 2f;
		dy1 = (xyz[i + 4] - y) / 2f;
		dz1 = (xyz[i + 5] - z) / 2f;
		dx2 = (xyz[i + 9] - x) / 2f;
		dy2 = (xyz[i + 10] - y) / 2f;
		dz2 = (xyz[i + 11] - z) / 2f;
		uv = (int)(u1 * 32768f + 0.5f) + ((int)(v1 * 32768f + 0.5f) << 16);
		if (u2 < 0f)
		{
			u2 += 1f;
		}
		if (v2 < 0f)
		{
			v2 += 1f;
		}
		uvSize = (int)(u2 * 32768f + 0.5f) + ((int)(v2 * 32768f + 0.5f) << 16) + (rotateUV ? 32768 : 0);
		renderFlags0 = flags[flagsIndex];
		renderFlags1 = flags[flagsIndex + 1];
		renderFlags2 = flags[flagsIndex + 2];
		renderFlags3 = flags[flagsIndex + 3];
		colormapData = colorData;
	}
}
