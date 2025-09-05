using System;
using System.IO;
using Vintagestory.API.Common;

namespace Vintagestory.API.Datastructures;

public interface IAttribute
{
	void ToBytes(BinaryWriter stream);

	void FromBytes(BinaryReader stream);

	int GetAttributeId();

	new Type GetType();

	object GetValue();

	string ToJsonToken();

	bool Equals(IWorldAccessor worldForResolve, IAttribute attr);

	IAttribute Clone();
}
