using Vintagestory.API.Server;

namespace Vintagestory.API.Common;

public delegate bool CanPlaceOrBreakDelegate(IServerPlayer byPlayer, BlockSelection blockSel, out string claimant);
