using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProtoBuf;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.GameContent;

[ProtoContract]
public class PropickReading
{
	[ProtoMember(1)]
	public Vec3d Position = new Vec3d();

	[ProtoMember(2)]
	public Dictionary<string, OreReading> OreReadings = new Dictionary<string, OreReading>();

	public static double MentionThreshold = 0.002;

	[ProtoMember(3)]
	public string Guid { get; set; }

	public double HighestReading
	{
		get
		{
			double num = 0.0;
			foreach (KeyValuePair<string, OreReading> oreReading in OreReadings)
			{
				num = GameMath.Max(num, oreReading.Value.TotalFactor);
			}
			return num;
		}
	}

	public string ToHumanReadable(string languageCode, Dictionary<string, string> pageCodes)
	{
		List<KeyValuePair<double, string>> list = new List<KeyValuePair<double, string>>();
		List<string> list2 = new List<string>();
		string[] array = new string[6] { "propick-density-verypoor", "propick-density-poor", "propick-density-decent", "propick-density-high", "propick-density-veryhigh", "propick-density-ultrahigh" };
		foreach (KeyValuePair<string, OreReading> oreReading in OreReadings)
		{
			OreReading value = oreReading.Value;
			if (value.DepositCode == "unknown")
			{
				string l = Lang.GetL(languageCode, "propick-reading-unknown", oreReading.Key);
				list.Add(new KeyValuePair<double, string>(1.0, l));
			}
			else if (value.TotalFactor > 0.025)
			{
				list.Add(new KeyValuePair<double, string>(value: (!pageCodes.TryGetValue(oreReading.Key, out var value2)) ? oreReading.Key : Lang.GetL(languageCode, "propick-reading", Lang.GetL(languageCode, array[(int)GameMath.Clamp(value.TotalFactor * 7.5, 0.0, 5.0)]), value2, Lang.GetL(languageCode, "ore-" + oreReading.Key), value.PartsPerThousand.ToString("0.##")), key: value.TotalFactor));
			}
			else if (value.TotalFactor > MentionThreshold)
			{
				list2.Add(oreReading.Key);
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		if (list.Count >= 0 || list2.Count > 0)
		{
			IOrderedEnumerable<KeyValuePair<double, string>> orderedEnumerable = list.OrderByDescending((KeyValuePair<double, string> val) => val.Key);
			stringBuilder.AppendLine(Lang.GetL(languageCode, "propick-reading-title", list.Count));
			foreach (KeyValuePair<double, string> item in orderedEnumerable)
			{
				stringBuilder.AppendLine(item.Value);
			}
			if (list2.Count > 0)
			{
				StringBuilder stringBuilder2 = new StringBuilder();
				int num = 0;
				foreach (string item2 in list2)
				{
					if (num > 0)
					{
						stringBuilder2.Append(", ");
					}
					if (!pageCodes.TryGetValue(item2, out var value3))
					{
						value3 = item2;
					}
					string value4 = string.Format("<a href=\"handbook://{0}\">{1}</a>", value3, Lang.GetL(languageCode, "ore-" + item2));
					stringBuilder2.Append(value4);
					num++;
				}
				stringBuilder.Append(Lang.GetL(languageCode, "Miniscule amounts of {0}", stringBuilder2.ToString()));
				stringBuilder.AppendLine();
			}
		}
		else
		{
			stringBuilder.Append(Lang.GetL(languageCode, "propick-noreading"));
		}
		return stringBuilder.ToString();
	}

	internal double GetTotalFactor(string orecode)
	{
		if (!OreReadings.TryGetValue(orecode, out var value))
		{
			return 0.0;
		}
		return value.TotalFactor;
	}
}
