using System;
using System.Collections.Generic;
using Vintagestory.API.Client;

namespace Vintagestory.API.Common;

public delegate RichTextComponentBase Tag2RichTextDelegate(ICoreClientAPI capi, VtmlTagToken token, Stack<CairoFont> fontStack, Action<LinkTextComponent> didClickLink);
