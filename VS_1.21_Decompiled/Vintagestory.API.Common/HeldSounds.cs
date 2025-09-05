using System.Runtime.Serialization;

namespace Vintagestory.API.Common;

[DocumentAsJson]
public class HeldSounds
{
	[DocumentAsJson]
	public AssetLocation Idle;

	[DocumentAsJson]
	public AssetLocation Equip;

	[DocumentAsJson]
	public AssetLocation Unequip;

	[DocumentAsJson]
	public AssetLocation Attack;

	[DocumentAsJson]
	public AssetLocation InvPickup;

	[DocumentAsJson]
	public AssetLocation InvPlace;

	public static AssetLocation InvPickUpDefault = new AssetLocation("sounds/player/clayformhi");

	public static AssetLocation InvPlaceDefault = new AssetLocation("sounds/player/clayform");

	public HeldSounds Clone()
	{
		return new HeldSounds
		{
			Idle = ((Idle == null) ? null : Idle.Clone()),
			Equip = ((Equip == null) ? null : Equip.Clone()),
			Unequip = ((Unequip == null) ? null : Unequip.Clone()),
			Attack = ((Attack == null) ? null : Attack.Clone()),
			InvPickup = ((InvPickup == null) ? null : InvPickup.Clone()),
			InvPlace = ((InvPlace == null) ? null : InvPlace.Clone())
		};
	}

	[OnDeserialized]
	public void OnDeserializedMethod(StreamingContext context)
	{
		Idle?.WithPathPrefixOnce("sounds/");
		Equip?.WithPathPrefixOnce("sounds/");
		Unequip?.WithPathPrefixOnce("sounds/");
		Attack?.WithPathPrefixOnce("sounds/");
		InvPickup?.WithPathPrefixOnce("sounds/");
		InvPlace?.WithPathPrefixOnce("sounds/");
	}
}
