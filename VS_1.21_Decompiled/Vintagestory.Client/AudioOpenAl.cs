using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OpenTK.Audio.OpenAL;
using OpenTK.Mathematics;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace Vintagestory.Client;

public class AudioOpenAl : IDisposable
{
	private ALContext Context = ALContext.Null;

	private ALDevice Device;

	public static bool UseHrtf;

	public static bool HasEffectsExtension;

	private const int ALC_HRTF_SOFT = 6546;

	private const int ALC_OUTPUT_MODE_SOFT = 6572;

	private const int HrtfEnabled = 5377;

	private const int HrtfDisabled = 6574;

	private const int ALC_FREQUENCY = 4103;

	public const int AL_REMIX_UNMATCHED_SOFT = 2;

	public const int AL_DIRECT_CHANNELS_SOFT = 4147;

	private static ReverbEffect[] reverbEffectsByReverbness = new ReverbEffect[24];

	private static ReverbEffect NoReverb = new ReverbEffect();

	public static int EchoFilterId;

	public IList<string> Devices => ALC.GetString((AlcGetStringList)4115).ToList();

	public string CurrentDevice => ALC.GetString(Device, (AlcGetString)4101);

	public float MasterSoundLevel
	{
		get
		{
			return AL.GetListener((ALListenerf)4106);
		}
		set
		{
			AL.Listener((ALListenerf)4106, value);
		}
	}

	public static ReverbEffect GetOrCreateReverbEffect(float reverbness)
	{
		if (reverbness < 0.25f)
		{
			return NoReverb;
		}
		float num = 0.5f;
		float num2 = 7f - num;
		int num3 = Math.Min(Math.Max(0, (int)((reverbness - num) / num2 * 24f)), 23);
		ReverbEffect reverbEffect = reverbEffectsByReverbness[num3];
		if (reverbEffect == null && HasEffectsExtension)
		{
			int num4 = EFX.GenAuxiliaryEffectSlot();
			int num5 = EFX.GenEffect();
			EFX.Effect(num5, (EffectInteger)32769, 1);
			EFX.Effect(num5, (EffectFloat)5, (float)num3 / 23f * num2 + num);
			EFX.AuxiliaryEffectSlot(num4, (EffectSlotInteger)1, num5);
			ReverbEffect[] array = reverbEffectsByReverbness;
			ReverbEffect obj = new ReverbEffect
			{
				reverbEffectId = num5,
				ReverbEffectSlot = num4
			};
			ReverbEffect reverbEffect2 = obj;
			array[num3] = obj;
			reverbEffect = reverbEffect2;
		}
		return reverbEffect;
	}

	public AudioOpenAl(ILogger logger)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		initContext(logger);
	}

	~AudioOpenAl()
	{
		Dispose(disposing: false);
	}

	protected virtual void Dispose(bool disposing)
	{
		//IL_005f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0064: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_0093: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		LoadedSoundNative.DisposeAllSounds();
		for (int i = 0; i < reverbEffectsByReverbness.Length; i++)
		{
			ReverbEffect reverbEffect = reverbEffectsByReverbness[i];
			if (reverbEffect != null)
			{
				if (HasEffectsExtension)
				{
					EFX.DeleteEffect(reverbEffect.reverbEffectId);
					EFX.DeleteAuxiliaryEffectSlot(reverbEffect.ReverbEffectSlot);
				}
				reverbEffectsByReverbness[i] = null;
			}
		}
		if (HasEffectsExtension)
		{
			EFX.DeleteFilter(EchoFilterId);
			EchoFilterId = 0;
		}
		if (Device != ALDevice.Null)
		{
			ALC.MakeContextCurrent(ALContext.Null);
			ALC.DestroyContext(Context);
			ALC.CloseDevice(Device);
			Device = ALDevice.Null;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private void initContext(ILogger logger)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			if (Device != ALDevice.Null)
			{
				ALC.MakeContextCurrent(ALContext.Null);
				ALC.DestroyContext(Context);
				ALC.CloseDevice(Device);
			}
			string desiredDevice = ClientSettings.AudioDevice;
			if (!Devices.Any((string d) => d.Equals(desiredDevice)))
			{
				desiredDevice = null;
				ClientSettings.AudioDevice = null;
			}
			Device = ALC.OpenDevice(desiredDevice);
			UseHrtf = ClientSettings.UseHRTFAudio;
			int[] array;
			if (ClientSettings.AllowSettingHRTFAudio)
			{
				array = ((!UseHrtf) ? new int[4] { 6546, 0, 6572, 6574 } : ((!ClientSettings.Force48kHzHRTFAudio) ? new int[4] { 6546, 1, 6572, 5377 } : new int[6] { 6546, 1, 6572, 5377, 4103, 48000 }));
			}
			else
			{
				array = Array.Empty<int>();
				UseHrtf = false;
			}
			Context = ALC.CreateContext(Device, array);
			ALC.MakeContextCurrent(Context);
			CheckALError(logger, "Start");
			AL.Listener((ALListener3f)4102, 0f, 0f, 0f);
		}
		catch (Exception e)
		{
			logger.Error("Failed creating audio context");
			logger.Error(e);
			return;
		}
		ALContextAttributes contextAttributes = ALC.GetContextAttributes(Device);
		logger.Notification("OpenAL Initialized. Available Mono/Stereo Sources: {0}/{1}", contextAttributes.MonoSources, contextAttributes.StereoSources);
		HasEffectsExtension = EFX.IsExtensionPresent(Device);
		if (!HasEffectsExtension)
		{
			logger.Notification("OpenAL Effects Extension not found. Disabling extra sound effects now.");
		}
	}

	public static void CheckALError(ILogger logger, string str)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		ALError error = AL.GetError();
		if ((int)error != 0)
		{
			logger.Warning("ALError at '" + str + "': " + AL.GetErrorString(error));
		}
	}

	internal void RecreateContext(Logger logger)
	{
		Dispose(disposing: true);
		initContext(logger);
	}

	public static byte[] LoadWave(Stream stream, out int channels, out int bits, out int rate)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream");
		}
		using BinaryReader binaryReader = new BinaryReader(stream);
		if (new string(binaryReader.ReadChars(4)) != "RIFF")
		{
			throw new NotSupportedException("Specified stream is not a wave file.");
		}
		binaryReader.ReadInt32();
		if (new string(binaryReader.ReadChars(4)) != "WAVE")
		{
			throw new NotSupportedException("Specified stream is not a wave file.");
		}
		if (new string(binaryReader.ReadChars(4)) != "fmt ")
		{
			throw new NotSupportedException("Specified wave file is not supported.");
		}
		binaryReader.ReadInt32();
		binaryReader.ReadInt16();
		int num = binaryReader.ReadInt16();
		int num2 = binaryReader.ReadInt32();
		binaryReader.ReadInt32();
		binaryReader.ReadInt16();
		int num3 = binaryReader.ReadInt16();
		if (new string(binaryReader.ReadChars(4)) != "data")
		{
			throw new NotSupportedException("Specified wave file is not supported.");
		}
		binaryReader.ReadInt32();
		channels = num;
		bits = num3;
		rate = num2;
		return binaryReader.ReadBytes((int)binaryReader.BaseStream.Length);
	}

	public static ALFormat GetSoundFormat(int channels, int bits)
	{
		switch (channels)
		{
		case 1:
			if (bits == 8)
			{
				return (ALFormat)4352;
			}
			return (ALFormat)4353;
		case 2:
			if (bits == 8)
			{
				return (ALFormat)4354;
			}
			return (ALFormat)4355;
		default:
			throw new NotSupportedException("The specified sound format is not supported (channels: " + channels + ").");
		}
	}

	public AudioMetaData GetSampleFromArray(IAsset asset)
	{
		Stream stream = new MemoryStream(asset.Data);
		if (stream.ReadByte() == 82 && stream.ReadByte() == 73 && stream.ReadByte() == 70 && stream.ReadByte() == 70)
		{
			stream.Position = 0L;
			int channels;
			int bits;
			int rate;
			byte[] pcm = LoadWave(stream, out channels, out bits, out rate);
			return new AudioMetaData(asset)
			{
				Pcm = pcm,
				BitsPerSample = bits,
				Channels = channels,
				Rate = rate,
				Loaded = 1
			};
		}
		stream.Position = 0L;
		return new OggDecoder().OggToWav(stream, asset);
	}

	public void UpdateListener(Vector3 position, Vector3 orientation)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			AL.Listener((ALListener3f)4100, position.X, position.Y, position.Z);
			Vector3 unitY = Vector3.UnitY;
			AL.Listener((ALListenerfv)4111, ref orientation, ref unitY);
		}
		catch
		{
		}
	}
}
