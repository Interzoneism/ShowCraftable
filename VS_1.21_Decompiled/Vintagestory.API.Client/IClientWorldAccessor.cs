using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Client;

public interface IClientWorldAccessor : IWorldAccessor
{
	new IClientGameCalendar Calendar { get; }

	bool ForceLiquidSelectable { get; set; }

	bool AmbientParticles { get; set; }

	IClientPlayer Player { get; }

	Dictionary<long, Entity> LoadedEntities { get; }

	int MapSizeY { get; }

	Dictionary<int, IMiniDimension> MiniDimensions { get; }

	ColorMapData GetColorMapData(Block block, int posX, int posY, int posZ);

	int ApplyColorMapOnRgba(string climateColorMap, string seasonColorMap, int color, int posX, int posY, int posZ, bool flipRb = true);

	int ApplyColorMapOnRgba(ColorMap climateColorMap, ColorMap seasonColorMap, int color, int posX, int posY, int posZ, bool flipRb = true);

	int ApplyColorMapOnRgba(string climateColorMap, string seasonColorMap, int color, int rain, int temp, bool flipRb = true);

	ILoadedSound LoadSound(SoundParams param);

	void AddCameraShake(float strengh);

	void SetCameraShake(float strengh);

	void ReduceCameraShake(float amount);

	void IncurBlockDamage(BlockSelection blockSelection, EnumTool? withTool, float damage);

	void CloneBlockDamage(BlockPos sourcePos, BlockPos targetPos);

	void TryAttackEntity(EntitySelection sele);

	IMiniDimension GetOrCreateDimension(int dimId, Vec3d pos);

	bool TryGetMiniDimension(Vec3i origin, out IMiniDimension dimension);

	void SetBlocksPreviewDimension(int dimId);

	int PlaySoundAtAndGetDuration(AssetLocation sound, double x, double y, double z, IPlayer ignorePlayerUid = null, bool randomizePitch = true, float range = 32f, float volume = 1f);

	void SetChunkColumnVisible(int cx, int cz, int dimension);
}
