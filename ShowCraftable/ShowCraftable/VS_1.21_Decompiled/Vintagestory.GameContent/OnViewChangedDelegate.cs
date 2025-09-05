using System.Collections.Generic;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

public delegate void OnViewChangedDelegate(List<FastVec2i> nowVisibleChunks, List<FastVec2i> nowHiddenChunks);
