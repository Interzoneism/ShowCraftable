using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

[DocumentAsJson]
public class BlendedOverlayTexture
{
	[DocumentAsJson]
	public AssetLocation Base;

	[DocumentAsJson]
	public EnumColorBlendMode BlendMode;

	public BlendedOverlayTexture Clone()
	{
		return new BlendedOverlayTexture
		{
			Base = Base.Clone(),
			BlendMode = BlendMode
		};
	}

	public override string ToString()
	{
		return Base.ToString() + "-b" + BlendMode;
	}

	public void ToString(StringBuilder sb)
	{
		sb.Append(Base.ToString());
		sb.Append("-b");
		sb.Append(BlendMode);
	}
}
