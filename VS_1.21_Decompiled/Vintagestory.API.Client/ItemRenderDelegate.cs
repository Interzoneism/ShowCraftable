using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public delegate void ItemRenderDelegate(ItemSlot inSlot, ItemRenderInfo renderInfo, Matrixf modelMat, double posX, double posY, double posZ, float size, int color, bool rotate = false, bool showStackSize = true);
