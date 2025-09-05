using System;
using Vintagestory.Common.Convert;

namespace Vintagestory.Common;

public class ZStdCompressorImpl : IZStdCompressor
{
	private unsafe readonly ZstdNative.ZstdCCtx* cctx;

	private readonly int compressionLevel;

	public unsafe ZStdCompressorImpl(int compressionLevel)
	{
		cctx = ZstdNative.ZSTD_createCCtx();
		this.compressionLevel = compressionLevel;
		ZstdNative.ZSTD_CCtx_setParameter(cctx, ZstdNative.ZstdCParameter.ZSTD_c_compressionLevel, compressionLevel);
		ZstdNative.ZSTD_CCtx_setParameter(cctx, ZstdNative.ZstdCParameter.ZSTD_c_contentSizeFlag, 1);
	}

	public unsafe int Compress(byte[] output, byte[] input)
	{
		fixed (byte* dst = output)
		{
			fixed (byte* src = input)
			{
				return (int)ZstdNative.ZSTD_compressCCtx(cctx, dst, (nuint)output.Length, src, (nuint)input.Length, compressionLevel);
			}
		}
	}

	public unsafe int Compress(byte[] output, ReadOnlySpan<byte> input)
	{
		fixed (byte* dst = output)
		{
			fixed (byte* src = input)
			{
				return (int)ZstdNative.ZSTD_compressCCtx(cctx, dst, (nuint)output.Length, src, (nuint)input.Length, compressionLevel);
			}
		}
	}
}
