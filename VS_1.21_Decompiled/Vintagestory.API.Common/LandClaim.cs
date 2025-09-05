using System.Collections.Generic;
using System.Linq;
using ProtoBuf;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.API.Common;

[ProtoContract]
public class LandClaim
{
	[ProtoMember(1)]
	public List<Cuboidi> Areas = new List<Cuboidi>();

	[ProtoMember(2)]
	public int ProtectionLevel;

	[ProtoMember(3)]
	public long OwnedByEntityId;

	[ProtoMember(4)]
	public string OwnedByPlayerUid;

	[ProtoMember(5)]
	public uint OwnedByPlayerGroupUid;

	[ProtoMember(6)]
	public string LastKnownOwnerName;

	[ProtoMember(7)]
	public string Description;

	[ProtoMember(8)]
	public Dictionary<int, EnumBlockAccessFlags> PermittedPlayerGroupIds = new Dictionary<int, EnumBlockAccessFlags>();

	[ProtoMember(9)]
	public Dictionary<string, EnumBlockAccessFlags> PermittedPlayerUids = new Dictionary<string, EnumBlockAccessFlags>();

	[ProtoMember(10)]
	public Dictionary<string, string> PermittedPlayerLastKnownPlayerName = new Dictionary<string, string>();

	[ProtoMember(11)]
	public bool AllowUseEveryone;

	[ProtoMember(12)]
	public bool AllowTraverseEveryone;

	public BlockPos Center
	{
		get
		{
			if (Areas.Count == 0)
			{
				return new BlockPos(0, 0, 0);
			}
			Vec3d vec3d = new Vec3d();
			int num = 0;
			foreach (Cuboidi area in Areas)
			{
				_ = area.Center;
				num += area.SizeXYZ;
			}
			foreach (Cuboidi area2 in Areas)
			{
				Vec3i center = area2.Center;
				Vec3d vec3d2 = new Vec3d(center.X, center.Y, center.Z);
				vec3d += vec3d2 * ((double)area2.SizeXYZ / (double)num);
			}
			return new BlockPos((int)(vec3d.X / (double)Areas.Count), (int)(vec3d.Y / (double)Areas.Count), (int)(vec3d.Z / (double)Areas.Count));
		}
	}

	public int SizeXZ
	{
		get
		{
			int num = 0;
			foreach (Cuboidi area in Areas)
			{
				_ = area.Center;
				num += area.SizeXZ;
			}
			return num;
		}
	}

	public int SizeXYZ
	{
		get
		{
			int num = 0;
			foreach (Cuboidi area in Areas)
			{
				_ = area.Center;
				num += area.SizeXYZ;
			}
			return num;
		}
	}

	public static LandClaim CreateClaim(IPlayer player, int protectionLevel = 1)
	{
		return new LandClaim
		{
			OwnedByPlayerUid = player.PlayerUID,
			ProtectionLevel = protectionLevel,
			LastKnownOwnerName = Lang.Get("Player " + player.PlayerName)
		};
	}

	public static LandClaim CreateClaim(EntityAgent entity, int protectionLevel = 1)
	{
		string name = entity.GetName();
		return new LandClaim
		{
			OwnedByEntityId = entity.EntityId,
			ProtectionLevel = protectionLevel,
			LastKnownOwnerName = Lang.Get("item-creature-" + entity.Code) + ((name == null) ? "" : (" " + name))
		};
	}

	public static LandClaim CreateClaim(string ownerName, int protectionLevel = 1)
	{
		return new LandClaim
		{
			ProtectionLevel = protectionLevel,
			LastKnownOwnerName = ownerName
		};
	}

	public EnumPlayerAccessResult TestPlayerAccess(IPlayer player, EnumBlockAccessFlags claimFlag)
	{
		if (player.PlayerUID.Equals(OwnedByPlayerUid))
		{
			return EnumPlayerAccessResult.OkOwner;
		}
		if (OwnedByPlayerGroupUid != 0 && player.Groups.Any((PlayerGroupMembership ms) => ms.GroupUid == OwnedByPlayerGroupUid))
		{
			return EnumPlayerAccessResult.OkGroup;
		}
		if (player.Role.PrivilegeLevel > ProtectionLevel && player.WorldData.CurrentGameMode == EnumGameMode.Creative)
		{
			return EnumPlayerAccessResult.OkPrivilege;
		}
		if (PermittedPlayerUids.TryGetValue(player.PlayerUID, out var value) && (value & claimFlag) > EnumBlockAccessFlags.None)
		{
			return EnumPlayerAccessResult.OkGrantedPlayer;
		}
		PlayerGroupMembership[] groups = player.Groups;
		foreach (PlayerGroupMembership playerGroupMembership in groups)
		{
			if (PermittedPlayerGroupIds.TryGetValue(playerGroupMembership.GroupUid, out value) && (value & claimFlag) > EnumBlockAccessFlags.None)
			{
				return EnumPlayerAccessResult.OkGrantedGroup;
			}
		}
		return EnumPlayerAccessResult.Denied;
	}

	public bool PositionInside(Vec3d position)
	{
		for (int i = 0; i < Areas.Count; i++)
		{
			if (Areas[i].Contains(position))
			{
				return true;
			}
		}
		return false;
	}

	public bool PositionInside(BlockPos position)
	{
		for (int i = 0; i < Areas.Count; i++)
		{
			if (Areas[i].Contains(position))
			{
				return true;
			}
		}
		return false;
	}

	public EnumClaimError AddArea(Cuboidi cuboidi)
	{
		if (Areas.Count == 0)
		{
			Areas.Add(cuboidi);
			return EnumClaimError.NoError;
		}
		for (int i = 0; i < Areas.Count; i++)
		{
			if (Areas[i].Intersects(cuboidi))
			{
				return EnumClaimError.Overlapping;
			}
		}
		for (int j = 0; j < Areas.Count; j++)
		{
			if (Areas[j].IsAdjacent(cuboidi))
			{
				Areas.Add(cuboidi);
				return EnumClaimError.NoError;
			}
		}
		return EnumClaimError.NotAdjacent;
	}

	public bool Intersects(Cuboidi cuboidi)
	{
		for (int i = 0; i < Areas.Count; i++)
		{
			if (Areas[i].Intersects(cuboidi))
			{
				return true;
			}
		}
		return false;
	}

	public bool Intersects2d(HorRectanglei rec)
	{
		for (int i = 0; i < Areas.Count; i++)
		{
			if (Areas[i].Intersects(rec))
			{
				return true;
			}
		}
		return false;
	}

	public LandClaim Clone()
	{
		List<Cuboidi> list = new List<Cuboidi>();
		for (int i = 0; i < Areas.Count; i++)
		{
			list.Add(Areas[i].Clone());
		}
		return new LandClaim
		{
			Areas = list,
			Description = Description,
			LastKnownOwnerName = LastKnownOwnerName,
			OwnedByEntityId = OwnedByEntityId,
			OwnedByPlayerGroupUid = OwnedByPlayerGroupUid,
			OwnedByPlayerUid = OwnedByPlayerUid,
			PermittedPlayerGroupIds = new Dictionary<int, EnumBlockAccessFlags>(PermittedPlayerGroupIds),
			PermittedPlayerUids = new Dictionary<string, EnumBlockAccessFlags>(PermittedPlayerUids),
			ProtectionLevel = ProtectionLevel
		};
	}
}
