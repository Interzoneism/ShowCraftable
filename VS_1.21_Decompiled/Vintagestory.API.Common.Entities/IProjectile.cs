namespace Vintagestory.API.Common.Entities;

public interface IProjectile
{
	Entity? FiredBy { get; set; }

	float Damage { get; set; }

	int DamageTier { get; set; }

	EnumDamageType DamageType { get; set; }

	bool IgnoreInvFrames { get; set; }

	ItemStack? ProjectileStack { get; set; }

	ItemStack? WeaponStack { get; set; }

	float DropOnImpactChance { get; set; }

	bool DamageStackOnImpact { get; set; }

	bool NonCollectible { get; set; }

	bool EntityHit { get; }

	float Weight { get; set; }

	bool Stuck { get; set; }

	void PreInitialize();
}
