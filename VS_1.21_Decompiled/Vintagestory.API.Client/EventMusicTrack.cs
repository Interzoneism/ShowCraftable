using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

[JsonObject(/*Could not decode attribute arguments.*/)]
public class EventMusicTrack : SurfaceMusicTrack
{
	[JsonProperty]
	public string SchematicCode;

	public override void Initialize(IAssetManager assetManager, ICoreClientAPI capi, IMusicEngine musicEngine)
	{
		base.Priority = 3f;
		base.Initialize(assetManager, capi, musicEngine);
	}

	public override bool ShouldPlay(TrackedPlayerProperties props, ClimateCondition conds, BlockPos pos)
	{
		if (base.IsActive || !SurfaceMusicTrack.ShouldPlayMusic)
		{
			return false;
		}
		if (capi.World.ElapsedMilliseconds < SurfaceMusicTrack.globalCooldownUntilMs)
		{
			return false;
		}
		if (musicEngine.LastPlayedTrack == this)
		{
			return false;
		}
		SurfaceMusicTrack.tracksCooldownUntilMs.TryGetValue(base.Name, out var value);
		if (capi.World.ElapsedMilliseconds < value)
		{
			return false;
		}
		if (SchematicCode != null)
		{
			int regionX = pos.X / capi.World.BlockAccessor.RegionSize;
			int regionZ = pos.Z / capi.World.BlockAccessor.RegionSize;
			IMapRegion mapRegion = capi.World.BlockAccessor.GetMapRegion(regionX, regionZ);
			if (mapRegion == null)
			{
				return false;
			}
			bool flag = false;
			foreach (GeneratedStructure generatedStructure in mapRegion.GeneratedStructures)
			{
				if (generatedStructure.Code.Contains(SchematicCode) && generatedStructure.Location.Contains(pos))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}
}
