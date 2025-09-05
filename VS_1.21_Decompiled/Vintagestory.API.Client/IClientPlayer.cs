using Vintagestory.API.Common;

namespace Vintagestory.API.Client;

public interface IClientPlayer : IPlayer
{
	float CameraPitch { get; set; }

	float CameraRoll { get; set; }

	float CameraYaw { get; set; }

	EnumCameraMode CameraMode { get; }

	void ShowChatNotification(string message);

	void TriggerFpAnimation(EnumHandInteract anim);
}
