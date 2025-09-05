using System.Collections.Generic;

namespace Vintagestory.API.Common;

public class VtmlTagToken : VtmlToken
{
	public List<VtmlToken> ChildElements { get; set; } = new List<VtmlToken>();

	public string Name { get; set; }

	public Dictionary<string, string> Attributes { get; set; }

	public string ContentText
	{
		get
		{
			string text = "";
			foreach (VtmlToken childElement in ChildElements)
			{
				text = ((!(childElement is VtmlTextToken)) ? (text + (childElement as VtmlTagToken).ContentText) : (text + (childElement as VtmlTextToken).Text));
			}
			return text;
		}
	}
}
