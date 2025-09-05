using System;
using System.IO;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace Vintagestory.Client.NoObf;

public class SystemCinematicCamera : ClientSystem
{
	private ClientPlatformAbstract platform;

	private CameraPoint[] cameraPoints;

	private int cameraPointsCount;

	private ClientWorldPlayerData prevWData;

	private double previousPositionX;

	private double previousPositionY;

	private double previousPositionZ;

	private double previousOrientationX;

	private double previousOrientationY;

	private double previousOrientationZ;

	private double totalDistance;

	private double totalTime;

	private double currentLengthTraveled;

	private int currentPoint;

	private int currentLoop;

	private bool cameraLive;

	private bool teleportBack;

	private int quantityLoops;

	private bool closedPath;

	private double alpha = 0.10000000149011612;

	private IAviWriter avi;

	private double writeAccum;

	private bool firstFrameDone;

	private bool shouldDisableGui = true;

	private MeshData cameraPathModel;

	private MeshRef cameraPathModelRef;

	private BlockPos origin;

	private EnumAutoCamAngleMode camAngleMode = EnumAutoCamAngleMode.Point;

	private string videoFileName = "";

	private string prevPathsFile = Path.Combine(GamePaths.Cache, "campaths.txt");

	private double[] time = new double[4];

	private CameraPoint[] points = new CameraPoint[4];

	private double[] pointsX = new double[4];

	private double[] pointsY = new double[4];

	private double[] pointsZ = new double[4];

	private double[] pointsPitch = new double[4];

	private double[] pointsYaw = new double[4];

	private double[] pointsRoll = new double[4];

	private double tstart;

	private double tend;

	public override string Name => "cica";

	public SystemCinematicCamera(ClientMain game)
		: base(game)
	{
		InitModel();
		platform = game.Platform;
		cameraPoints = new CameraPoint[256];
		cameraPointsCount = 0;
		CommandArgumentParsers parsers = game.api.ChatCommands.Parsers;
		game.api.ChatCommands.Create("cam").WithPreCondition(checkPrecond).BeginSubCommand("tp")
			.WithDescription("Whether to teleport the player back to where he was previously (default on)")
			.WithArgs(parsers.OptionalBool("teleportBack"))
			.HandleWith(OnCmdTp)
			.EndSubCommand()
			.BeginSubCommand("gui")
			.WithDescription(">If one, will disable the guis during the duration of the recording (default on)")
			.WithArgs(parsers.OptionalBool("shouldDisableGui"))
			.HandleWith(OnCmdGui)
			.EndSubCommand()
			.BeginSubCommand("loop")
			.WithDescription("If the path should be looped")
			.WithArgs(parsers.OptionalInt("quantityLoops", 9999999))
			.HandleWith(OnCmdLoop)
			.EndSubCommand()
			.BeginSubCommand("p")
			.WithDescription("Add a point to path")
			.HandleWith(OnCmdAddPoint)
			.EndSubCommand()
			.BeginSubCommand("up")
			.WithDescription("Update nth point")
			.WithArgs(parsers.OptionalInt("point_index"))
			.HandleWith(OnCmdUp)
			.EndSubCommand()
			.BeginSubCommand("goto")
			.WithDescription("Teleport to nth point")
			.WithArgs(parsers.OptionalInt("point_index"))
			.HandleWith(OnCmdGoto)
			.EndSubCommand()
			.BeginSubCommand("cp")
			.WithDescription("Close path")
			.HandleWith(OnCmdCp)
			.EndSubCommand()
			.BeginSubCommand("rp")
			.WithDescription("Remove the last point from the path")
			.HandleWith(OnCmdRemovePoint)
			.EndSubCommand()
			.BeginSubCommand("play")
			.WithAlias("start")
			.WithAlias("rec")
			.WithDescription("Play/start path or play and record to .avi file using rec command")
			.WithArgs(parsers.OptionalDouble("totalTime", 10.0))
			.HandleWith(OnCmdPlay)
			.EndSubCommand()
			.BeginSubCommand("stop")
			.WithDescription("Stop playing and recording")
			.HandleWith(OnCmdStop)
			.EndSubCommand()
			.BeginSubCommand("clear")
			.WithDescription("Remove all points from path")
			.HandleWith(OnCmdClear)
			.EndSubCommand()
			.BeginSubCommand("save")
			.WithDescription("Copy path points to clipboard")
			.HandleWith(OnCmdSave)
			.EndSubCommand()
			.BeginSubCommand("load")
			.WithDescription("Load point to the path, seperated by ,")
			.WithArgs(parsers.Word("points"))
			.HandleWith(OnCmdLoad)
			.EndSubCommand()
			.BeginSubCommand("loadold")
			.WithDescription("Load point to the path from a pre 1.20 point list, seperated by ,")
			.WithArgs(parsers.Word("points"))
			.HandleWith(OnCmdLoadOld)
			.EndSubCommand()
			.BeginSubCommand("alpha")
			.WithDescription("Set/Show alpha")
			.WithArgs(parsers.OptionalFloat("alpha"))
			.HandleWith(OnCmdAlpha)
			.EndSubCommand()
			.BeginSubCommand("angles")
			.WithDescription("Set camera angle mode [point, direction]")
			.WithArgs(parsers.OptionalWord("mode"))
			.HandleWith(OnCmdAngles)
			.EndSubCommand();
		game.eventManager.RegisterRenderer(OnRenderFrame3D, EnumRenderStage.Opaque, "cinecam", 0.699999988079071);
		game.eventManager.RegisterRenderer(OnFinalizeFrame, EnumRenderStage.Done, "cinecam-done", 0.9800000190734863);
	}

	private TextCommandResult checkPrecond(TextCommandCallingArgs args)
	{
		IPlayer player = args.Caller.Player;
		if (player == null || (player.WorldData.CurrentGameMode != EnumGameMode.Creative && player.WorldData.CurrentGameMode != EnumGameMode.Spectator))
		{
			return TextCommandResult.Error("Only available in creative or spectator mode");
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdAngles(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing || args[0] as string == "point")
		{
			camAngleMode = EnumAutoCamAngleMode.Point;
		}
		else
		{
			camAngleMode = EnumAutoCamAngleMode.Direction;
		}
		return TextCommandResult.Success("Camera angle mode is now " + camAngleMode);
	}

	private TextCommandResult OnCmdAlpha(TextCommandCallingArgs args)
	{
		if (args.Parsers[0].IsMissing)
		{
			return TextCommandResult.Success("Current alpha is " + alpha);
		}
		alpha = (float)args[0];
		GenerateCameraPathModel();
		return TextCommandResult.Success("Alpha set to " + alpha);
	}

	private TextCommandResult OnCmdStop(TextCommandCallingArgs args)
	{
		StopAutoCamera();
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdPlay(TextCommandCallingArgs args)
	{
		if (cameraPointsCount < 2)
		{
			game.ShowChatMessage("Need at least 2 points!");
		}
		else
		{
			StartAutoCamera(args);
		}
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdLoop(TextCommandCallingArgs args)
	{
		quantityLoops = (int)args[0];
		return TextCommandResult.Success("Will loop " + quantityLoops + " times.");
	}

	private TextCommandResult OnCmdGui(TextCommandCallingArgs args)
	{
		shouldDisableGui = (bool)args[0];
		return TextCommandResult.Success("Disable guis now " + (shouldDisableGui ? "on" : "off"));
	}

	private TextCommandResult OnCmdTp(TextCommandCallingArgs args)
	{
		teleportBack = (bool)args[0];
		return TextCommandResult.Success("Teleport back to previous position now " + (teleportBack ? "on" : "off"));
	}

	private void InitModel()
	{
		cameraPathModel = new MeshData(4, 4, withNormals: false, withUv: false);
		cameraPathModel.SetMode(EnumDrawMode.LineStrip);
		cameraPathModelRef = null;
	}

	public void OnRenderFrame3D(float deltaTime)
	{
		if (game.ShouldRender2DOverlays && cameraPathModelRef != null)
		{
			ShaderProgramAutocamera autocamera = ShaderPrograms.Autocamera;
			autocamera.Use();
			game.Platform.GLLineWidth(2f);
			game.Platform.BindTexture2d(0);
			game.GlPushMatrix();
			game.GlLoadMatrix(game.MainCamera.CameraMatrixOrigin);
			Vec3d cameraPos = game.EntityPlayer.CameraPos;
			game.GlTranslate((float)((double)origin.X - cameraPos.X), (float)((double)origin.Y - cameraPos.Y), (float)((double)origin.Z - cameraPos.Z));
			autocamera.ProjectionMatrix = game.CurrentProjectionMatrix;
			autocamera.ModelViewMatrix = game.CurrentModelViewMatrix;
			game.Platform.RenderMesh(cameraPathModelRef);
			game.GlPopMatrix();
			autocamera.Stop();
		}
	}

	private TextCommandResult OnCmdLoad(TextCommandCallingArgs args)
	{
		return TextCommandResult.Success(string.Format("Camera points loaded: {0}", load(args).ToString() ?? ""));
	}

	private TextCommandResult OnCmdLoadOld(TextCommandCallingArgs args)
	{
		return TextCommandResult.Success(string.Format("Camera points loaded: {0}", load(args, (float)Math.PI / 2f).ToString() ?? ""));
	}

	private int load(TextCommandCallingArgs args, float yawOffset = 0f)
	{
		string[] array = (args[0] as string).Split(',');
		int num = (array.Length - 1) / 6;
		cameraPointsCount = 0;
		origin = game.EntityPlayer.Pos.AsBlockPos;
		for (int i = 0; i < num; i++)
		{
			CameraPoint cameraPoint = new CameraPoint();
			cameraPoint.x = (float)int.Parse(array[1 + i * 6]) / 100f;
			cameraPoint.y = (float)int.Parse(array[1 + i * 6 + 1]) / 100f;
			cameraPoint.z = (float)int.Parse(array[1 + i * 6 + 2]) / 100f;
			cameraPoint.pitch = (float)int.Parse(array[1 + i * 6 + 3]) / 1000f;
			cameraPoint.yaw = (float)int.Parse(array[1 + i * 6 + 4]) / 1000f + yawOffset;
			cameraPoint.roll = (float)int.Parse(array[1 + i * 6 + 5]) / 1000f;
			if (cameraPointsCount - 1 >= 0 && cameraPoints[cameraPointsCount - 1].PositionEquals(cameraPoint))
			{
				cameraPoint.x += 0.0010000000474974513;
			}
			cameraPoints[cameraPointsCount++] = cameraPoint;
		}
		GenerateCameraPathModel();
		return num;
	}

	private TextCommandResult OnCmdSave(TextCommandCallingArgs args)
	{
		string clipboardText = PointsToString();
		platform.XPlatInterface.SetClipboardText(clipboardText);
		return TextCommandResult.Success("Camera points copied to clipboard.");
	}

	protected string PointsToString()
	{
		string text = "1,";
		for (int i = 0; i < cameraPointsCount; i++)
		{
			CameraPoint cameraPoint = cameraPoints[i];
			text = string.Format("{0}{1},", text, ((int)(cameraPoint.x * 100.0)).ToString() ?? "");
			text = string.Format("{0}{1},", text, ((int)(cameraPoint.y * 100.0)).ToString() ?? "");
			text = string.Format("{0}{1},", text, ((int)(cameraPoint.z * 100.0)).ToString() ?? "");
			text = string.Format("{0}{1},", text, ((int)(cameraPoint.pitch * 1000f)).ToString() ?? "");
			text = string.Format("{0}{1},", text, ((int)(cameraPoint.yaw * 1000f)).ToString() ?? "");
			text = string.Format("{0}{1}", text, ((int)(cameraPoint.roll * 1000f)).ToString() ?? "");
			if (i != cameraPointsCount - 1)
			{
				text = $"{text},";
			}
		}
		return text;
	}

	private TextCommandResult OnCmdAddPoint(TextCommandCallingArgs args)
	{
		if (cameraPointsCount == 0)
		{
			origin = game.EntityPlayer.Pos.AsBlockPos;
		}
		CameraPoint cameraPoint = CameraPoint.FromEntityPos(game.EntityPlayer.Pos);
		if (cameraPointsCount - 1 >= 0 && cameraPoints[cameraPointsCount - 1].PositionEquals(cameraPoint))
		{
			cameraPoint.x += 0.0010000000474974513;
		}
		cameraPoints[cameraPointsCount++] = cameraPoint;
		if (cameraPointsCount > 1)
		{
			FixYaw(cameraPointsCount);
		}
		closedPath = false;
		GenerateCameraPathModel();
		return TextCommandResult.Success("Point added");
	}

	private TextCommandResult OnCmdUp(TextCommandCallingArgs args)
	{
		int num = (int)args[0];
		if (num < 0 || num >= cameraPointsCount)
		{
			return TextCommandResult.Success("Your supplied number is above the point count or negative");
		}
		CameraPoint cameraPoint = CameraPoint.FromEntityPos(game.EntityPlayer.Pos);
		cameraPoints[num] = cameraPoint;
		if (cameraPointsCount > 1)
		{
			FixYaw(cameraPointsCount);
		}
		GenerateCameraPathModel();
		return TextCommandResult.Success("Point updated");
	}

	private TextCommandResult OnCmdGoto(TextCommandCallingArgs args)
	{
		int num = (int)args[0];
		if (num < 0 || num >= cameraPointsCount)
		{
			return TextCommandResult.Success("Your supplied number is above the point count or negative");
		}
		CameraPoint cameraPoint = cameraPoints[num];
		game.EntityPlayer.Pos.X = cameraPoint.x;
		game.EntityPlayer.Pos.Y = cameraPoint.y;
		game.EntityPlayer.Pos.Z = cameraPoint.z;
		ClientMain clientMain = game;
		float mouseYaw = (game.EntityPlayer.Pos.Yaw = cameraPoint.yaw);
		clientMain.mouseYaw = mouseYaw;
		ClientMain clientMain2 = game;
		mouseYaw = (game.EntityPlayer.Pos.Pitch = cameraPoint.pitch);
		clientMain2.mousePitch = mouseYaw;
		game.EntityPlayer.Pos.Roll = cameraPoint.roll;
		return TextCommandResult.Success();
	}

	private TextCommandResult OnCmdCp(TextCommandCallingArgs args)
	{
		if (cameraPointsCount <= 1)
		{
			return TextCommandResult.Success("Requires at least 2 points");
		}
		cameraPoints[cameraPointsCount++] = cameraPoints[0].Clone();
		FixYaw(cameraPointsCount);
		game.ShowChatMessage("Path Closed");
		closedPath = true;
		GenerateCameraPathModel();
		return TextCommandResult.Success();
	}

	private void FixYaw(int pos)
	{
		double num = cameraPoints[pos - 2].yaw;
		double num2 = (double)cameraPoints[pos - 1].yaw - num;
		if (num2 > Math.PI)
		{
			cameraPoints[pos - 1].yaw -= (float)Math.PI * 2f;
		}
		if (num2 < -Math.PI)
		{
			cameraPoints[pos - 1].yaw += (float)Math.PI * 2f;
		}
	}

	private TextCommandResult OnCmdRemovePoint(TextCommandCallingArgs args)
	{
		cameraPointsCount = Math.Max(0, cameraPointsCount - 1);
		closedPath = false;
		GenerateCameraPathModel();
		return TextCommandResult.Success("Point removed");
	}

	private TextCommandResult OnCmdClear(TextCommandCallingArgs args)
	{
		closedPath = false;
		cameraPointsCount = 0;
		StopAutoCamera();
		InitModel();
		return TextCommandResult.Success("Camera points cleared.");
	}

	private void StartAutoCamera(TextCommandCallingArgs args)
	{
		if (!game.AllowFreemove)
		{
			game.ShowChatMessage("Free move not allowed.");
			return;
		}
		if (cameraPointsCount == 0)
		{
			game.ShowChatMessage("No points defined. Enter points with \".cam p\" Command.");
			return;
		}
		ClientWorldPlayerData worlddata = game.player.worlddata;
		prevWData = worlddata.Clone();
		game.player.worlddata.RequestMode(game, worlddata.MoveSpeedMultiplier, worlddata.PickingRange, EnumGameMode.Spectator, freeMove: true, noClip: true, EnumFreeMovAxisLock.None, ClientSettings.RenderMetaBlocks);
		currentPoint = 0;
		totalDistance = CalculateApproximateDistances();
		if (args.SubCmdCode == "rec")
		{
			string text = ((ClientSettings.VideoFileTarget == null) ? GamePaths.Videos : ClientSettings.VideoFileTarget);
			try
			{
				if (new DirectoryInfo(text).Parent != null && !Directory.Exists(text))
				{
					Directory.CreateDirectory(text);
				}
				avi = platform.CreateAviWriter(ClientSettings.RecordingFrameRate, ClientSettings.RecordingCodec);
				string text2 = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";
				avi.Open(Path.Combine(text, videoFileName = text2 + ".avi"), game.Width, game.Height);
			}
			catch (Exception ex)
			{
				game.ShowChatMessage("Could not start recording: " + ex.Message);
				return;
			}
			if (ClientSettings.GameTickFrameRate > 0f)
			{
				game.DeltaTimeLimiter = 1f / ClientSettings.GameTickFrameRate;
			}
		}
		totalTime = (double)args[0];
		firstFrameDone = false;
		currentPoint = 0;
		currentLoop = 0;
		currentLengthTraveled = 0.0;
		EntityPos pos = game.EntityPlayer.Pos;
		previousPositionX = pos.X;
		previousPositionY = pos.Y;
		previousPositionZ = pos.Z;
		previousOrientationX = pos.Pitch;
		previousOrientationY = pos.Yaw;
		previousOrientationZ = pos.Roll;
		if (shouldDisableGui)
		{
			game.ShouldRender2DOverlays = false;
		}
		game.AllowCameraControl = false;
		game.MainCamera.UpdateCameraPos = false;
		PrecalcCurrentPoint();
		cameraLive = true;
	}

	private void StopAutoCamera()
	{
		if (shouldDisableGui)
		{
			game.ShouldRender2DOverlays = true;
		}
		game.AllowCameraControl = true;
		game.MainCamera.UpdateCameraPos = true;
		if (cameraLive)
		{
			game.player.worlddata.RequestMode(game, prevWData.MoveSpeedMultiplier, prevWData.PickingRange, prevWData.CurrentGameMode, prevWData.FreeMove, prevWData.NoClip, prevWData.FreeMovePlaneLock, ClientSettings.RenderMetaBlocks);
			if (teleportBack)
			{
				EntityPos pos = game.EntityPlayer.Pos;
				pos.X = previousPositionX;
				pos.Y = previousPositionY;
				pos.Z = previousPositionZ;
				pos.Pitch = (float)previousOrientationX;
				pos.Yaw = (float)previousOrientationY;
				pos.Roll = (float)previousOrientationZ;
			}
		}
		if (avi != null)
		{
			avi.Close();
			avi = null;
			string text = ((ClientSettings.VideoFileTarget == null) ? GamePaths.Videos : ClientSettings.VideoFileTarget);
			game.ShowChatMessage(videoFileName + " saved to " + text);
		}
		cameraLive = false;
		game.DeltaTimeLimiter = -1f;
	}

	public void OnFinalizeFrame(float dt)
	{
		if (!cameraLive || game.IsPaused)
		{
			return;
		}
		UpdateAvi(dt);
		if (currentPoint == cameraPointsCount - 1)
		{
			StopAutoCamera();
			File.AppendAllText(prevPathsFile, PointsToString() + "\r\n");
			game.ShowChatMessage("Camera path saved to Cache folder, in case you loose it");
			return;
		}
		double num = totalDistance / totalTime;
		currentLengthTraveled += num * (double)dt;
		double num2 = currentLengthTraveled / points[1].distance;
		AdvanceTo(Math.Min(1.0, num2));
		if (!(num2 > 1.0))
		{
			return;
		}
		currentPoint++;
		if (currentPoint != cameraPointsCount - 1 || currentLoop < quantityLoops)
		{
			if (currentLoop < quantityLoops && currentPoint == cameraPointsCount - 1)
			{
				currentPoint = 0;
				currentLoop++;
			}
			PrecalcCurrentPoint();
			currentLengthTraveled = 0.0;
		}
	}

	public void AdvanceTo(double percent)
	{
		EntityPos pos = game.EntityPlayer.Pos;
		Vec3d cameraPos = game.EntityPlayer.CameraPos;
		double x = pos.X;
		double y = pos.Y;
		double z = pos.Z;
		double x2 = (pos.X = GameMath.CPCatmullRomSplineLerp(tstart + percent * (tend - tstart), pointsX, time));
		cameraPos.X = x2;
		x2 = (pos.Y = GameMath.CPCatmullRomSplineLerp(tstart + percent * (tend - tstart), pointsY, time));
		cameraPos.Y = x2;
		x2 = (pos.Z = GameMath.CPCatmullRomSplineLerp(tstart + percent * (tend - tstart), pointsZ, time));
		cameraPos.Z = x2;
		cameraPos.Add((double)game.MainCamera.CameraShakeStrength * GameMath.Cos(game.MainCamera.deltaSum * 100.0) / 10.0, (double)(0f - game.MainCamera.CameraShakeStrength) * GameMath.Cos(game.MainCamera.deltaSum * 100.0) / 10.0, (double)game.MainCamera.CameraShakeStrength * GameMath.Sin(game.MainCamera.deltaSum * 100.0) / 10.0);
		if (camAngleMode == EnumAutoCamAngleMode.Point)
		{
			ClientMain clientMain = game;
			float mousePitch = (pos.Pitch = (float)GameMath.CPCatmullRomSplineLerp(tstart + percent * (tend - tstart), pointsPitch, time));
			clientMain.mousePitch = mousePitch;
			ClientMain clientMain2 = game;
			mousePitch = (pos.Yaw = (float)GameMath.CPCatmullRomSplineLerp(tstart + percent * (tend - tstart), pointsYaw, time));
			clientMain2.mouseYaw = mousePitch;
			pos.Roll = (float)GameMath.CPCatmullRomSplineLerp(tstart + percent * (tend - tstart), pointsRoll, time);
			return;
		}
		double num6 = pos.X - x;
		double num7 = pos.Y - y;
		double num8 = pos.Z - z;
		double num9 = Math.Sqrt(num6 * num6 + num7 * num7 + num8 * num8);
		if (num9 > 0.0)
		{
			double num10 = GameMath.Asin(num7 / num9);
			double num11 = Math.Atan2(num6, num8) + 1.5707963705062866;
			pos.Pitch += (float)Math.Min(0.03, num10 - (double)(pos.Pitch % ((float)Math.PI * 2f)));
			pos.Yaw += (float)Math.Min(0.03, num11 - (double)(pos.Yaw % ((float)Math.PI * 2f)));
		}
	}

	private void PrecalcCurrentPoint()
	{
		points[1] = cameraPoints[currentPoint];
		points[2] = cameraPoints[currentPoint + 1];
		if (currentPoint > 0)
		{
			points[0] = cameraPoints[currentPoint - 1];
		}
		else
		{
			points[0] = (closedPath ? cameraPoints[cameraPointsCount - 2] : points[1].ExtrapolateFrom(points[2], 1));
		}
		if (currentPoint < cameraPointsCount - 2)
		{
			points[3] = cameraPoints[currentPoint + 2];
		}
		else
		{
			points[3] = (closedPath ? cameraPoints[1] : points[2].ExtrapolateFrom(points[1], 1));
		}
		time[0] = 0.0;
		time[1] = 1.0;
		time[2] = 2.0;
		time[3] = 3.0;
		double num = 0.0;
		for (int i = 1; i < 4; i++)
		{
			double num2 = points[i].x - points[i - 1].x;
			double num3 = points[i].y - points[i - 1].y;
			double num4 = points[i].z - points[i - 1].z;
			double num5 = Math.Pow(num2 * num2 + num3 * num3 + num4 * num4, alpha);
			num += num5;
			time[i] = num;
		}
		for (int j = 0; j < 4; j++)
		{
			pointsX[j] = points[j].x;
			pointsY[j] = points[j].y;
			pointsZ[j] = points[j].z;
			pointsPitch[j] = points[j].pitch;
			pointsYaw[j] = points[j].yaw;
			pointsRoll[j] = points[j].roll;
		}
		tstart = time[1];
		tend = time[2];
	}

	private void UpdateAvi(float dt)
	{
		if (avi == null)
		{
			return;
		}
		if (!firstFrameDone)
		{
			firstFrameDone = true;
			return;
		}
		writeAccum += dt;
		float recordingFrameRate = ClientSettings.RecordingFrameRate;
		if (writeAccum >= (double)(1f / recordingFrameRate))
		{
			writeAccum -= 1f / recordingFrameRate;
			avi.AddFrame();
		}
	}

	private double CalculateApproximateDistances()
	{
		double num = 0.0;
		Vec3d vec3d = new Vec3d();
		Vec3d vec3d2 = new Vec3d();
		int num2 = currentPoint;
		for (currentPoint = 0; currentPoint < cameraPointsCount - 1; currentPoint++)
		{
			PrecalcCurrentPoint();
			CameraPoint cameraPoint = cameraPoints[currentPoint];
			vec3d2.X = cameraPoint.x;
			vec3d2.Y = cameraPoint.y;
			vec3d2.Z = cameraPoint.z;
			double num3 = 0.0;
			for (int i = 0; i < 60; i++)
			{
				double num4 = (float)i / 60f;
				vec3d.X = GameMath.CPCatmullRomSplineLerp(tstart + num4 * (tend - tstart), pointsX, time);
				vec3d.Y = GameMath.CPCatmullRomSplineLerp(tstart + num4 * (tend - tstart), pointsY, time);
				vec3d.Z = GameMath.CPCatmullRomSplineLerp(tstart + num4 * (tend - tstart), pointsZ, time);
				num3 += Math.Sqrt(vec3d.SquareDistanceTo(vec3d2));
				vec3d2.X = vec3d.X;
				vec3d2.Y = vec3d.Y;
				vec3d2.Z = vec3d.Z;
			}
			cameraPoint.distance = num3;
			num += cameraPoint.distance;
		}
		currentPoint = num2;
		return num;
	}

	private void GenerateCameraPathModel()
	{
		InitModel();
		int num = 0;
		for (currentPoint = 0; currentPoint < cameraPointsCount - 1; currentPoint++)
		{
			PrecalcCurrentPoint();
			for (int i = 0; i <= 30; i++)
			{
				double num2 = (float)i / 30f;
				double num3 = GameMath.CPCatmullRomSplineLerp(tstart + num2 * (tend - tstart), pointsX, time);
				double num4 = GameMath.CPCatmullRomSplineLerp(tstart + num2 * (tend - tstart), pointsY, time);
				double num5 = GameMath.CPCatmullRomSplineLerp(tstart + num2 * (tend - tstart), pointsZ, time);
				cameraPathModel.AddVertexSkipTex((float)(num3 - (double)origin.X), (float)(num4 - (double)origin.Y), (float)(num5 - (double)origin.Z), (currentPoint % 2 == 0) ? (-1) : ColorUtil.ToRgba(255, 255, 50, 50));
				cameraPathModel.AddIndex(num++);
			}
		}
		currentPoint = 0;
		if (cameraPathModelRef != null)
		{
			platform.DeleteMesh(cameraPathModelRef);
		}
		cameraPathModelRef = platform.UploadMesh(cameraPathModel);
	}

	public override EnumClientSystemType GetSystemType()
	{
		return EnumClientSystemType.Misc;
	}
}
