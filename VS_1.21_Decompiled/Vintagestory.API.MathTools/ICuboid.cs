using System;

namespace Vintagestory.API.MathTools;

internal interface ICuboid<T, C> : IEquatable<C>
{
	C Set(T x1, T y1, T z1, T x2, T y2, T z2);

	C Set(IVec3 min, IVec3 max);

	C Translate(T posX, T posY, T posZ);

	C Translate(IVec3 vec);

	bool ContainsOrTouches(T x, T y, T z);

	bool ContainsOrTouches(IVec3 vec);

	C GrowToInclude(int x, int y, int z);

	C GrowToInclude(IVec3 vec);

	double ShortestDistanceFrom(T x, T y, T z);

	double ShortestDistanceFrom(IVec3 vec);

	double pushOutX(C from, T x, ref EnumPushDirection direction);

	double pushOutY(C from, T y, ref EnumPushDirection direction);

	double pushOutZ(C from, T z, ref EnumPushDirection directione);

	C RotatedCopy(T degX, T degY, T degZ, Vec3d origin);

	C RotatedCopy(IVec3 vec, Vec3d origin);

	C OffsetCopy(T x, T y, T z);

	C OffsetCopy(IVec3 vec);

	C Clone();
}
