namespace Vintagestory.API.Common;

public delegate EnumWorldAccessResponse TestBlockAccessClaimDelegate(IPlayer player, BlockSelection blockSel, EnumBlockAccessFlags accessType, ref string claimant, LandClaim claim, EnumWorldAccessResponse response);
