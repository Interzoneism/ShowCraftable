using System;
using ProtoBuf;

namespace Vintagestory.API.Common;

[ProtoContract]
public class BlockCropProperties
{
	[ProtoMember(1)]
	public EnumSoilNutrient RequiredNutrient;

	[ProtoMember(2)]
	public float NutrientConsumption;

	[ProtoMember(3)]
	public int GrowthStages;

	[ProtoMember(4)]
	public float TotalGrowthDays;

	[ProtoMember(11)]
	public float TotalGrowthMonths;

	[ProtoMember(5)]
	public bool MultipleHarvests;

	[ProtoMember(6)]
	public int HarvestGrowthStageLoss;

	[ProtoMember(7)]
	public float ColdDamageBelow;

	[ProtoMember(8)]
	public float DamageGrowthStuntMul;

	[ProtoMember(9)]
	public float ColdDamageRipeMul;

	[ProtoMember(10)]
	public float HeatDamageAbove;

	public CropBehavior[] Behaviors = Array.Empty<CropBehavior>();
}
