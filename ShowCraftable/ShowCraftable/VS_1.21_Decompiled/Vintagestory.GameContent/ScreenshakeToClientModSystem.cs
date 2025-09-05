using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace Vintagestory.GameContent;

public class ScreenshakeToClientModSystem : ModSystem
{
	private ICoreClientAPI capi;

	private ICoreServerAPI sapi;

	public override void Start(ICoreAPI api)
	{
		api.Network.RegisterChannel("screenshake").RegisterMessageType<ScreenshakePacket>();
	}

	public override void StartServerSide(ICoreServerAPI api)
	{
		sapi = api;
	}

	public void ShakeScreen(Vec3d pos, float strength, float range)
	{
		IPlayer[] allOnlinePlayers = sapi.World.AllOnlinePlayers;
		for (int i = 0; i < allOnlinePlayers.Length; i++)
		{
			IServerPlayer serverPlayer = (IServerPlayer)allOnlinePlayers[i];
			if (serverPlayer.ConnectionState == EnumClientState.Playing)
			{
				float num = (float)serverPlayer.Entity.ServerPos.DistanceTo(pos);
				float num2 = Math.Min(1f, (range - num) / num) * strength;
				if ((double)num2 > 0.05)
				{
					sapi.Network.GetChannel("screenshake").SendPacket(new ScreenshakePacket
					{
						Strength = num2
					}, serverPlayer);
				}
			}
		}
	}

	public override void StartClientSide(ICoreClientAPI api)
	{
		capi = api;
		api.Network.GetChannel("screenshake").SetMessageHandler<ScreenshakePacket>(onScreenshakePacket);
	}

	private void onScreenshakePacket(ScreenshakePacket packet)
	{
		capi.World.AddCameraShake(packet.Strength);
	}
}
