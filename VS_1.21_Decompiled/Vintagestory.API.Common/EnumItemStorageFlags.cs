using System;

namespace Vintagestory.API.Common;

[Flags]
public enum EnumItemStorageFlags
{
	General = 1,
	Backpack = 2,
	Metallurgy = 4,
	Jewellery = 8,
	Alchemy = 0x10,
	Agriculture = 0x20,
	Currency = 0x40,
	Outfit = 0x80,
	Offhand = 0x100,
	Arrow = 0x200,
	Skill = 0x400,
	Custom1 = 0x800,
	Custom2 = 0x1000,
	Custom3 = 0x2000,
	Custom4 = 0x4000,
	Custom5 = 0x8000,
	Custom6 = 0x10000,
	Custom7 = 0x20000,
	Custom8 = 0x40000,
	Custom9 = 0x80000,
	Custom10 = 0x100000
}
