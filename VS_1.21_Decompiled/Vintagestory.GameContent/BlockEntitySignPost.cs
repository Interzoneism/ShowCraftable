using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class BlockEntitySignPost : BlockEntity
{
	public string[] textByCardinalDirection = new string[8];

	private BlockEntitySignPostRenderer signRenderer;

	private int color;

	private int tempColor;

	private ItemStack tempStack;

	private MeshData signMesh;

	private GuiDialogSignPost dlg;

	public string GetTextForDirection(Cardinal dir)
	{
		return textByCardinalDirection[dir.Index];
	}

	public BlockEntitySignPost()
	{
		for (int i = 0; i < 8; i++)
		{
			textByCardinalDirection[i] = "";
		}
	}

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		if (api is ICoreClientAPI)
		{
			CairoFont font = new CairoFont(20.0, GuiStyle.StandardFontName, new double[4] { 0.0, 0.0, 0.0, 0.8 });
			signRenderer = new BlockEntitySignPostRenderer(Pos, (ICoreClientAPI)api, font);
			if (textByCardinalDirection.Length != 0)
			{
				signRenderer.SetNewText(textByCardinalDirection, color);
			}
			Shape shape = Shape.TryGet(api, AssetLocation.Create("shapes/block/wood/signpost/sign.json"));
			if (shape != null)
			{
				(api as ICoreClientAPI).Tesselator.TesselateShape(base.Block, shape, out signMesh);
			}
		}
	}

	public override void OnBlockRemoved()
	{
		base.OnBlockRemoved();
		signRenderer?.Dispose();
		signRenderer = null;
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
	{
		base.FromTreeAttributes(tree, worldForResolving);
		color = tree.GetInt("color");
		if (color == 0)
		{
			color = ColorUtil.BlackArgb;
		}
		for (int i = 0; i < 8; i++)
		{
			textByCardinalDirection[i] = tree.GetString("text" + i, "");
		}
		signRenderer?.SetNewText(textByCardinalDirection, color);
	}

	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetInt("color", color);
		for (int i = 0; i < 8; i++)
		{
			tree.SetString("text" + i, textByCardinalDirection[i]);
		}
	}

	public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
	{
		if (packetid == 1002)
		{
			using (MemoryStream input = new MemoryStream(data))
			{
				BinaryReader binaryReader = new BinaryReader(input);
				for (int i = 0; i < 8; i++)
				{
					textByCardinalDirection[i] = binaryReader.ReadString();
					if (textByCardinalDirection[i] == null)
					{
						textByCardinalDirection[i] = "";
					}
				}
			}
			color = tempColor;
			MarkDirty(redrawOnClient: true);
			Api.World.BlockAccessor.GetChunkAtBlockPos(Pos).MarkModified();
			if (Api.World.Rand.NextDouble() < 0.85)
			{
				player.InventoryManager.TryGiveItemstack(tempStack);
			}
		}
		if (packetid == 1003 && tempStack != null)
		{
			player.InventoryManager.TryGiveItemstack(tempStack);
			tempStack = null;
		}
	}

	public override void OnReceivedServerPacket(int packetid, byte[] data)
	{
		if (packetid == 1001)
		{
			using MemoryStream input = new MemoryStream(data);
			BinaryReader binaryReader = new BinaryReader(input);
			binaryReader.ReadString();
			string dialogTitle = binaryReader.ReadString();
			for (int i = 0; i < 8; i++)
			{
				textByCardinalDirection[i] = binaryReader.ReadString();
				if (textByCardinalDirection[i] == null)
				{
					textByCardinalDirection[i] = "";
				}
			}
			_ = (IClientWorldAccessor)Api.World;
			CairoFont signPostFont = new CairoFont(20.0, GuiStyle.StandardFontName, new double[4] { 0.0, 0.0, 0.0, 0.8 });
			if (dlg != null && dlg.IsOpened())
			{
				return;
			}
			dlg = new GuiDialogSignPost(dialogTitle, Pos, textByCardinalDirection, Api as ICoreClientAPI, signPostFont);
			dlg.OnTextChanged = DidChangeTextClientSide;
			dlg.OnCloseCancel = delegate
			{
				signRenderer.SetNewText(textByCardinalDirection, color);
				(Api as ICoreClientAPI).Network.SendBlockEntityPacket(Pos, 1003);
			};
			dlg.OnClosed += delegate
			{
				dlg.Dispose();
				dlg = null;
			};
			dlg.TryOpen();
		}
		if (packetid != 1000)
		{
			return;
		}
		using MemoryStream input2 = new MemoryStream(data);
		BinaryReader binaryReader2 = new BinaryReader(input2);
		for (int num = 0; num < 8; num++)
		{
			textByCardinalDirection[num] = binaryReader2.ReadString();
			if (textByCardinalDirection[num] == null)
			{
				textByCardinalDirection[num] = "";
			}
		}
		if (signRenderer != null)
		{
			signRenderer.SetNewText(textByCardinalDirection, color);
		}
	}

	private void DidChangeTextClientSide(string[] textByCardinalDirection)
	{
		signRenderer?.SetNewText(textByCardinalDirection, tempColor);
		this.textByCardinalDirection = textByCardinalDirection;
		MarkDirty(redrawOnClient: true);
	}

	public void OnRightClick(IPlayer byPlayer)
	{
		if (byPlayer == null || byPlayer.Entity?.Controls?.ShiftKey != true)
		{
			return;
		}
		ItemSlot activeHotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
		if (activeHotbarSlot == null || activeHotbarSlot.Itemstack?.ItemAttributes?["pigment"]?["color"].Exists != true)
		{
			return;
		}
		JsonObject jsonObject = activeHotbarSlot.Itemstack.ItemAttributes["pigment"]["color"];
		int r = jsonObject["red"].AsInt();
		int g = jsonObject["green"].AsInt();
		int b = jsonObject["blue"].AsInt();
		tempColor = ColorUtil.ToRgba(255, r, g, b);
		tempStack = activeHotbarSlot.TakeOut(1);
		activeHotbarSlot.MarkDirty();
		if (!(Api.World is IServerWorldAccessor))
		{
			return;
		}
		byte[] data;
		using (MemoryStream memoryStream = new MemoryStream())
		{
			BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
			binaryWriter.Write("BlockEntityTextInput");
			binaryWriter.Write(Lang.Get("Edit Sign Text"));
			for (int i = 0; i < 8; i++)
			{
				binaryWriter.Write(textByCardinalDirection[i]);
			}
			data = memoryStream.ToArray();
		}
		((ICoreServerAPI)Api).Network.SendBlockEntityPacket((IServerPlayer)byPlayer, Pos, 1001, data);
	}

	public override void OnBlockUnloaded()
	{
		base.OnBlockUnloaded();
		signRenderer?.Dispose();
	}

	public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
	{
		for (int i = 0; i < 8; i++)
		{
			if (textByCardinalDirection[i].Length != 0)
			{
				Cardinal obj = Cardinal.ALL[i];
				float num = 0f;
				switch (obj.Index)
				{
				case 0:
					num = 180f;
					break;
				case 1:
					num = 135f;
					break;
				case 2:
					num = 90f;
					break;
				case 3:
					num = 45f;
					break;
				case 5:
					num = 315f;
					break;
				case 6:
					num = 270f;
					break;
				case 7:
					num = 225f;
					break;
				}
				mesher.AddMeshData(signMesh.Clone().Rotate(new Vec3f(0.5f, 0.5f, 0.5f), 0f, num * ((float)Math.PI / 180f), 0f));
			}
		}
		return false;
	}
}
