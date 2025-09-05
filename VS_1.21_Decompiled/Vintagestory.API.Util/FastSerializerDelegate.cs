using Vintagestory.API.Datastructures;

namespace Vintagestory.API.Util;

public delegate void FastSerializerDelegate(FastMemoryStream ms, int id, ref int count, ref int position);
