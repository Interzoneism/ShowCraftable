using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Server;

public interface IWorldGenBlockAccessor : IBlockAccessor
{
	IServerWorldAccessor WorldgenWorldAccessor { get; }

	void ScheduleBlockUpdate(BlockPos pos);

	void ScheduleBlockLightUpdate(BlockPos pos, int oldBlockid, int newBlockId);

	void RunScheduledBlockLightUpdates(int chunkx, int chunkz);

	void AddEntity(Entity entity);

	void BeginColumn();
}
