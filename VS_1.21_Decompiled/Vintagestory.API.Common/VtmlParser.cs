using System.Collections.Generic;

namespace Vintagestory.API.Common;

public class VtmlParser
{
	public enum ParseState
	{
		SeekKey,
		ParseTagName,
		ParseKey,
		SeekValue,
		ParseQuotedValue,
		ParseValue
	}

	public static VtmlToken[] Tokenize(ILogger errorLogger, string vtml)
	{
		if (vtml == null)
		{
			return new VtmlToken[0];
		}
		List<VtmlToken> list = new List<VtmlToken>();
		Stack<VtmlTagToken> stack = new Stack<VtmlTagToken>();
		string text = "";
		string text2 = "";
		bool flag = false;
		for (int i = 0; i < vtml.Length; i++)
		{
			if (vtml[i] == '<')
			{
				flag = true;
				if (text.Length > 0)
				{
					text = text.Replace("&gt;", ">").Replace("&lt;", "<").Replace("&nbsp;", " ");
					if (stack.Count > 0)
					{
						stack.Peek().ChildElements.Add(new VtmlTextToken
						{
							Text = text
						});
					}
					else
					{
						list.Add(new VtmlTextToken
						{
							Text = text
						});
					}
				}
				text = "";
			}
			else if (vtml[i] == '>')
			{
				if (!flag)
				{
					errorLogger.Error("Found closing tag char > but no tag was opened at " + i + ". Use &gt;/&lt; if you want to display them as plain characters. See debug log for full text.");
					errorLogger.VerboseDebug(vtml);
				}
				flag = false;
				if (text2.Length > 0 && text2[0] == '/')
				{
					if (stack.Count == 0 || stack.Peek().Name != text2.Substring(1))
					{
						if (stack.Count == 0)
						{
							errorLogger.Error("Found closing tag <" + text2.Substring(1) + "> at position " + i + " but it was never opened. See debug log for full text.");
						}
						else
						{
							errorLogger.Error("Found closing tag <" + text2.Substring(1) + "> at position " + i + " but <" + stack.Peek().Name + "> should be closed first. See debug log for full text.");
						}
						errorLogger.VerboseDebug(vtml);
					}
					if (stack.Count > 0)
					{
						stack.Pop();
					}
					text2 = "";
				}
				else if (text2 == "br")
				{
					VtmlTagToken item = new VtmlTagToken
					{
						Name = "br"
					};
					if (stack.Count > 0)
					{
						stack.Peek().ChildElements.Add(item);
					}
					else
					{
						list.Add(item);
					}
					text2 = "";
				}
				else if (i > 0 && vtml[i - 1] == '/')
				{
					VtmlTagToken item = parseTagAttributes(text2.Substring(0, text2.Length - 1));
					if (stack.Count > 0)
					{
						stack.Peek().ChildElements.Add(item);
					}
					else
					{
						list.Add(item);
					}
					text2 = "";
				}
				else
				{
					VtmlTagToken item = parseTagAttributes(text2);
					if (stack.Count > 0)
					{
						stack.Peek().ChildElements.Add(item);
					}
					else
					{
						list.Add(item);
					}
					stack.Push(item);
					text2 = "";
				}
			}
			else if (flag)
			{
				text2 += vtml[i];
			}
			else
			{
				text += vtml[i];
			}
		}
		if (text.Length > 0)
		{
			text = text.Replace("&gt;", ">").Replace("&lt;", "<").Replace("&nbsp;", " ");
			list.Add(new VtmlTextToken
			{
				Text = text
			});
		}
		return list.ToArray();
	}

	private static VtmlTagToken parseTagAttributes(string tag)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		string text = null;
		ParseState parseState = ParseState.ParseTagName;
		char c = '\0';
		string text2 = "";
		string text3 = "";
		for (int i = 0; i < tag.Length; i++)
		{
			bool flag = tag[i] == ' ' || tag[i] == '\t' || tag[i] == '\r' || tag[i] == '\n';
			bool flag2 = tag[i] == '\'' || tag[i] == '"';
			switch (parseState)
			{
			case ParseState.ParseTagName:
				if (flag)
				{
					parseState = ParseState.SeekKey;
				}
				else
				{
					text += tag[i];
				}
				break;
			case ParseState.SeekKey:
				if (!flag)
				{
					text2 = tag[i].ToString() ?? "";
					parseState = ParseState.ParseKey;
				}
				break;
			case ParseState.ParseKey:
				if (tag[i] == '=')
				{
					parseState = ParseState.SeekValue;
					text3 = "";
				}
				else if (flag)
				{
					dictionary[text2] = null;
					parseState = ParseState.SeekKey;
				}
				else
				{
					text2 += tag[i];
				}
				break;
			case ParseState.SeekValue:
				if (!flag)
				{
					if (flag2)
					{
						parseState = ParseState.ParseQuotedValue;
						c = tag[i];
					}
					else
					{
						parseState = ParseState.ParseValue;
						text3 = tag[i].ToString() ?? "";
					}
				}
				break;
			case ParseState.ParseValue:
				if (flag)
				{
					dictionary[text2.ToLowerInvariant()] = text3;
					parseState = ParseState.SeekKey;
				}
				else
				{
					text3 += tag[i];
				}
				break;
			case ParseState.ParseQuotedValue:
				if (tag[i] == c && tag[i - 1] != '\\')
				{
					dictionary[text2.ToLowerInvariant()] = text3;
					parseState = ParseState.SeekKey;
				}
				else
				{
					text3 += tag[i];
				}
				break;
			}
		}
		if (parseState == ParseState.ParseValue || parseState == ParseState.SeekValue)
		{
			dictionary[text2] = text3;
		}
		return new VtmlTagToken
		{
			Name = text,
			Attributes = dictionary
		};
	}
}
