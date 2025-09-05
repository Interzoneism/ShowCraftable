using System;
using System.Collections;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Common;

namespace Vintagestory.API.Client;

public abstract class GuiDialog : IDisposable
{
	public class DlgComposers : IEnumerable<KeyValuePair<string, GuiComposer>>, IEnumerable
	{
		protected ConcurrentSmallDictionary<string, GuiComposer> dialogComposers = new ConcurrentSmallDictionary<string, GuiComposer>();

		protected GuiDialog dialog;

		public IEnumerable<GuiComposer> Values => dialogComposers.Values;

		public GuiComposer this[string key]
		{
			get
			{
				dialogComposers.TryGetValue(key, out var value);
				return value;
			}
			set
			{
				dialogComposers[key] = value;
				value.OnFocusChanged = dialog.OnFocusChanged;
			}
		}

		public DlgComposers(GuiDialog dialog)
		{
			this.dialog = dialog;
		}

		public void ClearComposers()
		{
			foreach (KeyValuePair<string, GuiComposer> dialogComposer in dialogComposers)
			{
				dialogComposer.Value?.Dispose();
			}
			dialogComposers.Clear();
		}

		public void Dispose()
		{
			foreach (KeyValuePair<string, GuiComposer> dialogComposer in dialogComposers)
			{
				dialogComposer.Value?.Dispose();
			}
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return dialogComposers.GetEnumerator();
		}

		IEnumerator<KeyValuePair<string, GuiComposer>> IEnumerable<KeyValuePair<string, GuiComposer>>.GetEnumerator()
		{
			return dialogComposers.GetEnumerator();
		}

		public bool ContainsKey(string key)
		{
			return dialogComposers.ContainsKey(key);
		}

		public void Remove(string key)
		{
			dialogComposers.Remove(key);
		}

		public GuiComposer[] ToArray()
		{
			GuiComposer[] array = new GuiComposer[dialogComposers.Count];
			dialogComposers.Values.CopyTo(array, 0);
			return array;
		}
	}

	[Flags]
	public enum EnumPosFlag
	{
		RightMid = 1,
		RightTop = 2,
		RightBot = 4,
		LeftMid = 8,
		LeftTop = 0x10,
		LeftBot = 0x20,
		Right2Mid = 0x40,
		Right2Top = 0x80,
		Right2Bot = 0x100,
		Left2Mid = 0x200,
		Left2Top = 0x400,
		Left2Bot = 0x800,
		Right3Mid = 0x1000,
		Right3Top = 0x2000,
		Right3Bot = 0x4000,
		Left3Mid = 0x8000,
		Left3Top = 0x10000,
		Left3Bot = 0x20000
	}

	public DlgComposers Composers;

	public bool ignoreNextKeyPress;

	protected bool opened;

	protected bool focused;

	protected ICoreClientAPI capi;

	public string MouseOverCursor;

	public GuiComposer SingleComposer
	{
		get
		{
			return Composers["single"];
		}
		set
		{
			Composers["single"] = value;
		}
	}

	public virtual string DebugName => GetType().Name;

	public virtual float ZSize => 150f;

	public virtual bool Focused => focused;

	public virtual bool Focusable => true;

	public virtual EnumDialogType DialogType => EnumDialogType.Dialog;

	public virtual double DrawOrder => 0.1;

	public virtual double InputOrder => 0.5;

	public virtual bool UnregisterOnClose => false;

	public virtual bool PrefersUngrabbedMouse => RequiresUngrabbedMouse();

	public virtual bool DisableMouseGrab => false;

	public abstract string ToggleKeyCombinationCode { get; }

	public event Action OnOpened;

	public event Action OnClosed;

	protected virtual void OnFocusChanged(bool on)
	{
		if (on != focused && (DialogType != EnumDialogType.Dialog || opened))
		{
			if (on)
			{
				capi.Gui.RequestFocus(this);
			}
			else
			{
				focused = false;
			}
		}
	}

	public GuiDialog(ICoreClientAPI capi)
	{
		Composers = new DlgComposers(this);
		this.capi = capi;
	}

	public virtual void OnBlockTexturesLoaded()
	{
		string toggleKeyCombinationCode = ToggleKeyCombinationCode;
		if (toggleKeyCombinationCode != null)
		{
			capi.Input.SetHotKeyHandler(toggleKeyCombinationCode, OnKeyCombinationToggle);
		}
	}

	public virtual void OnLevelFinalize()
	{
	}

	public virtual void OnOwnPlayerDataReceived()
	{
	}

	public virtual void OnGuiOpened()
	{
	}

	public virtual void OnGuiClosed()
	{
	}

	public virtual bool TryOpen()
	{
		return TryOpen(withFocus: true);
	}

	public virtual bool TryOpen(bool withFocus)
	{
		bool flag = opened;
		if (!capi.Gui.LoadedGuis.Contains(this))
		{
			capi.Gui.RegisterDialog(this);
		}
		opened = true;
		if (DialogType == EnumDialogType.Dialog && withFocus)
		{
			capi.Gui.RequestFocus(this);
		}
		if (!flag)
		{
			OnGuiOpened();
			this.OnOpened?.Invoke();
			capi.Gui.TriggerDialogOpened(this);
		}
		return true;
	}

	public virtual bool TryClose()
	{
		bool num = opened;
		opened = false;
		UnFocus();
		if (num)
		{
			OnGuiClosed();
			this.OnClosed?.Invoke();
		}
		focused = false;
		if (num)
		{
			capi.Gui.TriggerDialogClosed(this);
		}
		return true;
	}

	public virtual void UnFocus()
	{
		focused = false;
	}

	public virtual void Focus()
	{
		if (Focusable)
		{
			focused = true;
		}
	}

	public virtual void Toggle()
	{
		if (IsOpened())
		{
			TryClose();
		}
		else
		{
			TryOpen();
		}
	}

	public virtual bool IsOpened()
	{
		return opened;
	}

	public virtual bool IsOpened(string dialogComposerName)
	{
		return IsOpened();
	}

	public virtual void OnBeforeRenderFrame3D(float deltaTime)
	{
	}

	public virtual void OnRenderGUI(float deltaTime)
	{
		foreach (KeyValuePair<string, GuiComposer> item in (IEnumerable<KeyValuePair<string, GuiComposer>>)Composers)
		{
			item.Value.Render(deltaTime);
			MouseOverCursor = item.Value.MouseOverCursor;
		}
	}

	public virtual void OnFinalizeFrame(float dt)
	{
		foreach (KeyValuePair<string, GuiComposer> item in (IEnumerable<KeyValuePair<string, GuiComposer>>)Composers)
		{
			item.Value.PostRender(dt);
		}
	}

	internal virtual bool OnKeyCombinationToggle(KeyCombination viaKeyComb)
	{
		HotKey hotKeyByCode = capi.Input.GetHotKeyByCode(ToggleKeyCombinationCode);
		if (hotKeyByCode == null)
		{
			return false;
		}
		if (hotKeyByCode.KeyCombinationType == HotkeyType.CreativeTool && capi.World.Player.WorldData.CurrentGameMode != EnumGameMode.Creative)
		{
			return false;
		}
		Toggle();
		return true;
	}

	public virtual void OnKeyDown(KeyEvent args)
	{
		GuiComposer[] array = Composers.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnKeyDown(args, focused);
			if (args.Handled)
			{
				return;
			}
		}
		HotKey hotKeyByCode = capi.Input.GetHotKeyByCode(ToggleKeyCombinationCode);
		if (hotKeyByCode != null && hotKeyByCode.DidPress(args, capi.World, capi.World.Player, allowCharacterControls: true) && TryClose())
		{
			args.Handled = true;
		}
	}

	public virtual void OnKeyPress(KeyEvent args)
	{
		if (ignoreNextKeyPress)
		{
			ignoreNextKeyPress = false;
			args.Handled = true;
		}
		else
		{
			if (args.Handled)
			{
				return;
			}
			GuiComposer[] array = Composers.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				array[i].OnKeyPress(args);
				if (args.Handled)
				{
					break;
				}
			}
		}
	}

	public virtual void OnKeyUp(KeyEvent args)
	{
		GuiComposer[] array = Composers.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].OnKeyUp(args);
			if (args.Handled)
			{
				break;
			}
		}
	}

	public virtual bool OnEscapePressed()
	{
		if (DialogType == EnumDialogType.HUD)
		{
			return false;
		}
		return TryClose();
	}

	public virtual bool OnMouseEnterSlot(ItemSlot slot)
	{
		GuiComposer[] array = Composers.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].OnMouseEnterSlot(slot))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool OnMouseLeaveSlot(ItemSlot itemSlot)
	{
		GuiComposer[] array = Composers.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].OnMouseLeaveSlot(itemSlot))
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool OnMouseClickSlot(ItemSlot itemSlot)
	{
		return false;
	}

	public virtual void OnMouseDown(MouseEvent args)
	{
		if (args.Handled)
		{
			return;
		}
		GuiComposer[] array = Composers.ToArray();
		GuiComposer[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].OnMouseDown(args);
			if (args.Handled)
			{
				return;
			}
		}
		if (!IsOpened())
		{
			return;
		}
		array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			if (array2[i].Bounds.PointInside(args.X, args.Y))
			{
				args.Handled = true;
				break;
			}
		}
	}

	public virtual void OnMouseUp(MouseEvent args)
	{
		if (args.Handled)
		{
			return;
		}
		GuiComposer[] array = Composers.ToArray();
		GuiComposer[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].OnMouseUp(args);
			if (args.Handled)
			{
				return;
			}
		}
		array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			if (array2[i].Bounds.PointInside(args.X, args.Y))
			{
				args.Handled = true;
				break;
			}
		}
	}

	public virtual void OnMouseMove(MouseEvent args)
	{
		if (args.Handled)
		{
			return;
		}
		GuiComposer[] array = Composers.ToArray();
		GuiComposer[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].OnMouseMove(args);
			if (args.Handled)
			{
				return;
			}
		}
		array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			if (array2[i].Bounds.PointInside(args.X, args.Y))
			{
				args.Handled = true;
				break;
			}
		}
	}

	public virtual void OnMouseWheel(MouseWheelEventArgs args)
	{
		GuiComposer[] array = Composers.ToArray();
		GuiComposer[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].OnMouseWheel(args);
			if (args.IsHandled)
			{
				return;
			}
		}
		if (!focused)
		{
			return;
		}
		array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			if (array2[i].Bounds.PointInside(capi.Input.MouseX, capi.Input.MouseY))
			{
				args.SetHandled();
			}
		}
	}

	public virtual bool ShouldReceiveRenderEvents()
	{
		return opened;
	}

	public virtual bool ShouldReceiveKeyboardEvents()
	{
		return focused;
	}

	public virtual bool ShouldReceiveMouseEvents()
	{
		return IsOpened();
	}

	[Obsolete("Use PrefersUngrabbedMouse instead")]
	public virtual bool RequiresUngrabbedMouse()
	{
		return true;
	}

	public virtual bool CaptureAllInputs()
	{
		return false;
	}

	public virtual bool CaptureRawMouse()
	{
		return false;
	}

	public virtual void Dispose()
	{
		Composers?.Dispose();
	}

	public void ClearComposers()
	{
		Composers?.ClearComposers();
	}

	public virtual bool IsInRangeOf(Vec3d pos)
	{
		Vec3d pos2 = capi.World.Player.Entity.Pos.XYZ.Add(capi.World.Player.Entity.LocalEyePos);
		return (double)pos.DistanceTo(pos2) <= (double)capi.World.Player.WorldData.PickingRange + 0.5;
	}

	public EnumPosFlag GetFreePos(string code)
	{
		Array values = Enum.GetValues(typeof(EnumPosFlag));
		posFlagDict().TryGetValue(code, out var value);
		foreach (EnumPosFlag item in values)
		{
			if ((int)((uint)value & (uint)item) <= 0)
			{
				return item;
			}
		}
		return (EnumPosFlag)0;
	}

	public void OccupyPos(string code, EnumPosFlag pos)
	{
		posFlagDict().TryGetValue(code, out var value);
		posFlagDict()[code] = value | (int)pos;
	}

	public void FreePos(string code, EnumPosFlag pos)
	{
		posFlagDict().TryGetValue(code, out var value);
		posFlagDict()[code] = value & (int)(~pos);
	}

	private Dictionary<string, int> posFlagDict()
	{
		capi.ObjectCache.TryGetValue("dialogCount", out var value);
		Dictionary<string, int> dictionary = value as Dictionary<string, int>;
		if (dictionary == null)
		{
			dictionary = (Dictionary<string, int>)(capi.ObjectCache["dialogCount"] = new Dictionary<string, int>());
		}
		return dictionary;
	}

	protected bool IsRight(EnumPosFlag flag)
	{
		if (flag != EnumPosFlag.RightBot && flag != EnumPosFlag.RightMid && flag != EnumPosFlag.RightTop && flag != EnumPosFlag.Right2Top && flag != EnumPosFlag.Right2Mid && flag != EnumPosFlag.Right2Bot && flag != EnumPosFlag.Right3Top && flag != EnumPosFlag.Right3Mid)
		{
			return flag == EnumPosFlag.Right3Bot;
		}
		return true;
	}

	protected float YOffsetMul(EnumPosFlag flag)
	{
		switch (flag)
		{
		case EnumPosFlag.RightTop:
		case EnumPosFlag.LeftTop:
		case EnumPosFlag.Right2Top:
		case EnumPosFlag.Left2Top:
		case EnumPosFlag.Right3Top:
		case EnumPosFlag.Left3Top:
			return -1f;
		case EnumPosFlag.RightBot:
		case EnumPosFlag.LeftBot:
		case EnumPosFlag.Right2Bot:
		case EnumPosFlag.Left2Bot:
		case EnumPosFlag.Right3Bot:
		case EnumPosFlag.Left3Bot:
			return 1f;
		default:
			return 0f;
		}
	}

	protected float XOffsetMul(EnumPosFlag flag)
	{
		switch (flag)
		{
		case EnumPosFlag.Right2Mid:
		case EnumPosFlag.Right2Top:
		case EnumPosFlag.Right2Bot:
			return -1f;
		case EnumPosFlag.Left2Mid:
		case EnumPosFlag.Left2Top:
		case EnumPosFlag.Left2Bot:
			return 1f;
		case EnumPosFlag.Right3Mid:
		case EnumPosFlag.Right3Top:
		case EnumPosFlag.Right3Bot:
			return -2f;
		case EnumPosFlag.Left3Mid:
		case EnumPosFlag.Left3Top:
		case EnumPosFlag.Left3Bot:
			return 2f;
		default:
			return 0f;
		}
	}
}
