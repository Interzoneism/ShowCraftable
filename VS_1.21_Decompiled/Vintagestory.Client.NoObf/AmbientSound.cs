using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class AmbientSound : IEquatable<AmbientSound>, IEqualityComparer<AmbientSound>
{
	public ILoadedSound Sound;

	public int QuantityNearbyBlocks;

	public AssetLocation AssetLoc;

	public List<Cuboidi> BoundingBoxes = new List<Cuboidi>();

	public Vec3i SectionPos;

	public float Ratio = 10f;

	public float VolumeMul = 1f;

	public EnumSoundType SoundType = EnumSoundType.Ambient;

	public double MaxDistanceMerge = 3.0;

	private Vec3f tmp = new Vec3f();

	private Vec3f tmpout = new Vec3f();

	public float AdjustedVolume => GameMath.Clamp(GameMath.Sqrt(QuantityNearbyBlocks) / Ratio, 1f / Ratio, 1f) * VolumeMul;

	public double DistanceTo(AmbientSound sound)
	{
		double num = 9999999.0;
		for (int i = 0; i < BoundingBoxes.Count; i++)
		{
			for (int j = 0; j < sound.BoundingBoxes.Count; j++)
			{
				num = Math.Min(num, BoundingBoxes[i].ShortestDistanceFrom(sound.BoundingBoxes[j]));
			}
		}
		return num;
	}

	public bool Equals(AmbientSound other)
	{
		if (AssetLoc.Equals(other.AssetLoc))
		{
			return SectionPos.Equals(other.SectionPos);
		}
		return false;
	}

	public bool Equals(AmbientSound x, AmbientSound y)
	{
		return x.Equals(y);
	}

	public override bool Equals(object obj)
	{
		if (obj is AmbientSound)
		{
			return (obj as AmbientSound).Equals(this);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return AssetLoc.GetHashCode() * 23 + SectionPos.GetHashCode();
	}

	public void FadeToNewVolumne()
	{
		float adjustedVolume = AdjustedVolume;
		if ((double)Math.Abs(adjustedVolume - Sound.Params.Volume) > 0.02)
		{
			Sound.FadeTo(adjustedVolume, 1f, null);
		}
	}

	public int GetHashCode(AmbientSound obj)
	{
		return obj.AssetLoc.GetHashCode() * 23 + obj.SectionPos.GetHashCode();
	}

	internal void updatePosition(EntityPos position)
	{
		double num = 999999.0;
		tmpout.Set(-99999f, -99999f, -99999f);
		foreach (Cuboidi boundingBox in BoundingBoxes)
		{
			tmp.X = (float)GameMath.Clamp(position.X, boundingBox.X1, boundingBox.X2);
			tmp.Y = (float)GameMath.Clamp(position.Y, boundingBox.Y1, boundingBox.Y2);
			tmp.Z = (float)GameMath.Clamp(position.Z, boundingBox.Z1, boundingBox.Z2);
			double num2 = tmp.DistanceSq(position.X, position.Y, position.Z);
			if (num2 < num)
			{
				num = num2;
				tmpout.Set(tmp);
			}
		}
		Sound.SetPosition(tmpout);
	}

	public void RenderWireFrame(ClientMain game, WireframeCube wireframe)
	{
		foreach (Cuboidi boundingBox in BoundingBoxes)
		{
			float scalex = boundingBox.X2 - boundingBox.X1;
			float scaley = boundingBox.Y2 - boundingBox.Y1;
			float scalez = boundingBox.Z2 - boundingBox.Z1;
			float num = boundingBox.X1;
			float num2 = boundingBox.Y1;
			float num3 = boundingBox.Z1;
			wireframe.Render(game.api, num, num2, num3, scalex, scaley, scalez, 1f);
		}
	}
}
