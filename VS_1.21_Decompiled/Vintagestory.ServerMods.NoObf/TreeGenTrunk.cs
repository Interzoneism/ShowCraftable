using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Vintagestory.API.MathTools;

namespace Vintagestory.ServerMods.NoObf;

public class TreeGenTrunk : TreeGenBranch
{
	public float dx = 0.5f;

	public float dz = 0.5f;

	public float probability = 1f;

	public int segment;

	public void InheritFrom(TreeGenTrunk treeGenTrunk, string[] skip)
	{
		FieldInfo[] fields = GetType().GetFields();
		foreach (FieldInfo fieldInfo in fields)
		{
			if (!skip.Contains(fieldInfo.Name) && fieldInfo.Name != "inherit")
			{
				fieldInfo.SetValue(this, treeGenTrunk.GetType().GetField(fieldInfo.Name).GetValue(treeGenTrunk));
			}
		}
	}

	[OnDeserialized]
	internal new void OnDeserializedMethod(StreamingContext context)
	{
		if (angleVert == null)
		{
			angleVert = NatFloat.createUniform((float)Math.PI, 0f);
		}
	}

	public override int getBlockId(IRandom rand, float width, TreeGenBlocks blocks, TreeGen gen, int treeSubType)
	{
		if (segment != 0 && width >= 0.3f)
		{
			return blocks.trunkSegmentBlockIds[segment - 1];
		}
		return base.getBlockId(rand, width, blocks, gen, treeSubType);
	}
}
