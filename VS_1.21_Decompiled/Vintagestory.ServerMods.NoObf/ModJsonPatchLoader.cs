using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JsonPatch.Operations;
using JsonPatch.Operations.Abstractions;
using Newtonsoft.Json.Linq;
using Tavis;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace Vintagestory.ServerMods.NoObf;

public class ModJsonPatchLoader : ModSystem
{
	private ICoreAPI api;

	private ITreeAttribute worldConfig;

	public override bool ShouldLoad(EnumAppSide side)
	{
		return true;
	}

	public override double ExecuteOrder()
	{
		return 0.05;
	}

	public override void AssetsLoaded(ICoreAPI api)
	{
		this.api = api;
		worldConfig = api.World.Config;
		if (worldConfig == null)
		{
			worldConfig = new TreeAttribute();
		}
		ApplyPatches();
	}

	public void ApplyPatches(string forPartialPath = null)
	{
		List<IAsset> many = api.Assets.GetMany("patches/");
		int applied = 0;
		int notFound = 0;
		int errorCount = 0;
		int num = 0;
		int num2 = 0;
		HashSet<string> hashSet = new HashSet<string>(api.ModLoader.Mods.Select((Mod m) => m.Info.ModID).ToList());
		foreach (IAsset item in many)
		{
			JsonPatch[] array = null;
			try
			{
				array = item.ToObject<JsonPatch[]>();
			}
			catch (Exception e)
			{
				api.Logger.Error("Failed loading patches file {0}:", item.Location);
				api.Logger.Error(e);
			}
			for (int num3 = 0; array != null && num3 < array.Length; num3++)
			{
				JsonPatch jsonPatch = array[num3];
				if (!jsonPatch.Enabled)
				{
					continue;
				}
				if (jsonPatch.Condition != null)
				{
					IAttribute attribute = worldConfig[jsonPatch.Condition.When];
					if (attribute == null)
					{
						continue;
					}
					if (jsonPatch.Condition.useValue)
					{
						jsonPatch.Value = new JsonObject(JToken.Parse(attribute.ToJsonToken()));
					}
					else if (!jsonPatch.Condition.IsValue.Equals(attribute.GetValue()?.ToString() ?? "", StringComparison.InvariantCultureIgnoreCase))
					{
						api.Logger.VerboseDebug("Patch file {0}, patch {1}: Unmet IsValue condition ({2}!={3})", item.Location, num3, jsonPatch.Condition.IsValue, attribute.GetValue()?.ToString() ?? "");
						num2++;
						continue;
					}
				}
				if (jsonPatch.DependsOn != null)
				{
					bool flag = true;
					PatchModDependence[] dependsOn = jsonPatch.DependsOn;
					foreach (PatchModDependence patchModDependence in dependsOn)
					{
						bool flag2 = hashSet.Contains(patchModDependence.modid);
						flag = flag && (flag2 ^ patchModDependence.invert);
					}
					if (!flag)
					{
						num2++;
						api.Logger.VerboseDebug("Patch file {0}, patch {1}: Unmet DependsOn condition ({2})", item.Location, num3, string.Join(",", jsonPatch.DependsOn.Select((PatchModDependence pd) => (pd.invert ? "!" : "") + pd.modid)));
						continue;
					}
				}
				if (forPartialPath == null || jsonPatch.File.PathStartsWith(forPartialPath))
				{
					num++;
					ApplyPatch(num3, item.Location, jsonPatch, ref applied, ref notFound, ref errorCount);
				}
			}
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("JsonPatch Loader: ");
		if (num == 0)
		{
			stringBuilder.Append("Nothing to patch");
		}
		else
		{
			stringBuilder.Append($"{num} patches total");
			if (applied > 0)
			{
				stringBuilder.Append($", successfully applied {applied} patches");
			}
			if (notFound > 0)
			{
				stringBuilder.Append($", missing files on {notFound} patches");
			}
			if (num2 > 0)
			{
				stringBuilder.Append($", unmet conditions on {num2} patches");
			}
			if (errorCount > 0)
			{
				stringBuilder.Append($", had errors on {errorCount} patches");
			}
			else
			{
				stringBuilder.Append(string.Format(", no errors", errorCount));
			}
		}
		api.Logger.Notification(stringBuilder.ToString());
		api.Logger.VerboseDebug("Patchloader finished");
	}

	public void ApplyPatch(int patchIndex, AssetLocation patchSourcefile, JsonPatch jsonPatch, ref int applied, ref int notFound, ref int errorCount)
	{
		//IL_04e0: Expected O, but got Unknown
		//IL_035a: Unknown result type (might be due to invalid IL or missing references)
		//IL_035f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0366: Unknown result type (might be due to invalid IL or missing references)
		//IL_0370: Expected O, but got Unknown
		//IL_0371: Expected O, but got Unknown
		//IL_03de: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f4: Expected O, but got Unknown
		//IL_03f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03fb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0405: Expected O, but got Unknown
		//IL_0406: Expected O, but got Unknown
		//IL_0408: Unknown result type (might be due to invalid IL or missing references)
		//IL_040d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0414: Unknown result type (might be due to invalid IL or missing references)
		//IL_041e: Expected O, but got Unknown
		//IL_041e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0425: Unknown result type (might be due to invalid IL or missing references)
		//IL_042f: Expected O, but got Unknown
		//IL_0430: Expected O, but got Unknown
		//IL_0432: Unknown result type (might be due to invalid IL or missing references)
		//IL_0437: Unknown result type (might be due to invalid IL or missing references)
		//IL_043e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0448: Expected O, but got Unknown
		//IL_0448: Unknown result type (might be due to invalid IL or missing references)
		//IL_045a: Expected O, but got Unknown
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d8: Expected O, but got Unknown
		//IL_02d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ea: Expected O, but got Unknown
		//IL_032d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0332: Unknown result type (might be due to invalid IL or missing references)
		//IL_0339: Unknown result type (might be due to invalid IL or missing references)
		//IL_0343: Expected O, but got Unknown
		//IL_0343: Unknown result type (might be due to invalid IL or missing references)
		//IL_0355: Expected O, but got Unknown
		//IL_0464: Unknown result type (might be due to invalid IL or missing references)
		//IL_046a: Expected O, but got Unknown
		//IL_03b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ca: Expected O, but got Unknown
		//IL_03ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dc: Expected O, but got Unknown
		if (((!jsonPatch.Side.HasValue) ? jsonPatch.File.Category.SideType : jsonPatch.Side.Value) != EnumAppSide.Universal && jsonPatch.Side != api.Side)
		{
			return;
		}
		if (jsonPatch.File == null)
		{
			api.World.Logger.Error("Patch {0} in {1} failed because it is missing the target file property", patchIndex, patchSourcefile);
			return;
		}
		AssetLocation assetLocation = jsonPatch.File.Clone();
		if (jsonPatch.File.Path.EndsWith('*'))
		{
			foreach (IAsset item in api.Assets.GetMany(jsonPatch.File.Path.TrimEnd('*'), jsonPatch.File.Domain, loadAsset: false))
			{
				jsonPatch.File = item.Location;
				ApplyPatch(patchIndex, patchSourcefile, jsonPatch, ref applied, ref notFound, ref errorCount);
			}
			jsonPatch.File = assetLocation;
			return;
		}
		if (!assetLocation.Path.EndsWithOrdinal(".json"))
		{
			assetLocation.Path += ".json";
		}
		IAsset asset = api.Assets.TryGet(assetLocation);
		if (asset == null)
		{
			if (jsonPatch.File.Category == null)
			{
				api.World.Logger.VerboseDebug("Patch {0} in {1}: File {2} not found. Wrong asset category", patchIndex, patchSourcefile, assetLocation);
			}
			else
			{
				EnumAppSide sideType = jsonPatch.File.Category.SideType;
				if (sideType != EnumAppSide.Universal && api.Side != sideType)
				{
					api.World.Logger.VerboseDebug("Patch {0} in {1}: File {2} not found. Hint: This asset is usually only loaded {3} side", patchIndex, patchSourcefile, assetLocation, sideType);
				}
				else
				{
					api.World.Logger.VerboseDebug("Patch {0} in {1}: File {2} not found", patchIndex, patchSourcefile, assetLocation);
				}
			}
			notFound++;
			return;
		}
		Operation val = null;
		switch (jsonPatch.Op)
		{
		case EnumJsonPatchOp.Add:
			if (jsonPatch.Value == null)
			{
				api.World.Logger.Error("Patch {0} in {1} failed probably because it is an add operation and the value property is not set or misspelled", patchIndex, patchSourcefile);
				errorCount++;
				return;
			}
			val = (Operation)new AddReplaceOperation
			{
				Path = new JsonPointer(jsonPatch.Path),
				Value = jsonPatch.Value.Token
			};
			break;
		case EnumJsonPatchOp.AddEach:
			if (jsonPatch.Value == null)
			{
				api.World.Logger.Error("Patch {0} in {1} failed probably because it is an add each operation and the value property is not set or misspelled", patchIndex, patchSourcefile);
				errorCount++;
				return;
			}
			val = (Operation)new AddEachOperation
			{
				Path = new JsonPointer(jsonPatch.Path),
				Value = jsonPatch.Value.Token
			};
			break;
		case EnumJsonPatchOp.Remove:
			val = (Operation)new RemoveOperation
			{
				Path = new JsonPointer(jsonPatch.Path)
			};
			break;
		case EnumJsonPatchOp.Replace:
			if (jsonPatch.Value == null)
			{
				api.World.Logger.Error("Patch {0} in {1} failed probably because it is a replace operation and the value property is not set or misspelled", patchIndex, patchSourcefile);
				errorCount++;
				return;
			}
			val = (Operation)new ReplaceOperation
			{
				Path = new JsonPointer(jsonPatch.Path),
				Value = jsonPatch.Value.Token
			};
			break;
		case EnumJsonPatchOp.Copy:
			val = (Operation)new CopyOperation
			{
				Path = new JsonPointer(jsonPatch.Path),
				FromPath = new JsonPointer(jsonPatch.FromPath)
			};
			break;
		case EnumJsonPatchOp.Move:
			val = (Operation)new MoveOperation
			{
				Path = new JsonPointer(jsonPatch.Path),
				FromPath = new JsonPointer(jsonPatch.FromPath)
			};
			break;
		case EnumJsonPatchOp.AddMerge:
			val = (Operation)new AddMergeOperation
			{
				Path = new JsonPointer(jsonPatch.Path),
				Value = jsonPatch.Value.Token
			};
			break;
		}
		PatchDocument val2 = new PatchDocument((Operation[])(object)new Operation[1] { val });
		JToken val3;
		try
		{
			val3 = JToken.Parse(asset.ToText());
		}
		catch (Exception e)
		{
			api.World.Logger.Error("Patch {0} (target: {2}) in {1} failed probably because the syntax of the value is broken:", patchIndex, patchSourcefile, assetLocation);
			api.World.Logger.Error(e);
			errorCount++;
			return;
		}
		try
		{
			val2.ApplyTo(val3);
		}
		catch (PathNotFoundException ex)
		{
			PathNotFoundException ex2 = ex;
			api.World.Logger.Error("Patch {0} (target: {4}) in {1} failed because supplied path {2} is invalid: {3}", patchIndex, patchSourcefile, jsonPatch.Path, ((Exception)(object)ex2).Message, assetLocation);
			errorCount++;
			return;
		}
		catch (Exception e2)
		{
			api.World.Logger.Error("Patch {0} (target: {2}) in {1} failed, following Exception was thrown:", patchIndex, patchSourcefile, assetLocation);
			api.World.Logger.Error(e2);
			errorCount++;
			return;
		}
		string s = ((object)val3).ToString();
		asset.Data = Encoding.UTF8.GetBytes(s);
		asset.IsPatched = true;
		applied++;
	}
}
