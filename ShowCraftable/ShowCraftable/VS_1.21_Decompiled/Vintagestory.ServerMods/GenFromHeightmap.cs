using System;
using System.IO;
using SkiaSharp;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace Vintagestory.ServerMods;

public class GenFromHeightmap : ModSystem
{
	private ICoreServerAPI sapi;

	private ushort[] heights;

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
		api.Event.SaveGameLoaded += GameWorldLoaded;
		if (api.Server.CurrentRunPhase == EnumServerRunPhase.RunGame)
		{
			GameWorldLoaded();
		}
	}

	private void OnChunkColumnGen(IServerChunk[] chunks, int chunkX, int chunkZ)
	{
	}

	private void GameWorldLoaded()
	{
	}

	public unsafe void TryLoadHeightMap(string filename)
	{
		string text = Path.Combine(sapi.GetOrCreateDataPath("Heightmaps"), filename);
		if (!File.Exists(text))
		{
			return;
		}
		SKBitmap val = SKBitmap.Decode(text);
		heights = new ushort[val.Width * val.Height];
		byte* ptr = (byte*)((IntPtr)(nint)val.GetPixels()).ToPointer();
		byte* ptr2 = ptr + val.RowBytes;
		for (int i = 0; i < val.Height; i++)
		{
			ushort* ptr3 = (ushort*)ptr;
			for (int j = 0; j < val.Width; j++)
			{
				heights[i * val.Width + j] = ptr3[2];
				ptr3 += 4;
			}
			ptr = ptr2;
			ptr2 += val.RowBytes;
		}
		((SKNativeObject)val).Dispose();
	}
}
