using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public class HealOverTimeConfig
{
	public float Health { get; set; } = 1f;

	public float ApplicationTimeSec { get; set; } = 2f;

	public float MaxApplicationTimeSec { get; set; } = 10f;

	public int Ticks { get; set; } = 10;

	public float EffectDurationSec { get; set; } = 10f;

	public bool CancelInAir { get; set; } = true;

	public bool CancelWhileSwimming { get; set; }

	public AssetLocation? Sound { get; set; } = new AssetLocation("game:sounds/player/poultice");

	public AssetLocation? AppliedSound { get; set; } = new AssetLocation("game:sounds/player/poultice-applied");

	public float SoundRange { get; set; } = 8f;

	public bool CanRevive { get; set; } = true;

	public bool AffectedByArmor { get; set; } = true;

	public float DelayToCancelSec { get; set; } = 0.5f;
}
