using System;
using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Util;

public interface IWithFastSerialize
{
	byte[] FastSerialize(FastMemoryStream ms)
	{
		throw new NotImplementedException("Probably, VintagestorySourcegen source generator did not succeed. Possible causes include not having Microsoft.CodeAnalysis.CSharp version 4.12.0 installed, discuss with th3dilli or radfast.");
	}
}
