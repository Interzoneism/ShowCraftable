using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;

namespace Vintagestory.GameContent;

public record GaitMeta
{
	public required string Code { get; set; }

	public float YawMultiplier { get; set; } = 3.5f;

	public float MoveSpeed { get; set; }

	public bool Backwards { get; set; }

	public float StaminaCost { get; set; }

	public string? FallbackGaitCode { get; set; }

	public bool IsSprint { get; set; }

	public required AssetLocation Sound { get; set; }

	[CompilerGenerated]
	[SetsRequiredMembers]
	protected GaitMeta(GaitMeta original)
	{
		Code = original.Code;
		YawMultiplier = original.YawMultiplier;
		MoveSpeed = original.MoveSpeed;
		Backwards = original.Backwards;
		StaminaCost = original.StaminaCost;
		FallbackGaitCode = original.FallbackGaitCode;
		IsSprint = original.IsSprint;
		Sound = original.Sound;
	}

	public GaitMeta()
	{
	}
}
