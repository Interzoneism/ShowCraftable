using System.IO;

namespace Vintagestory.API.Client;

public class JsonDialogSettings
{
	public string Code;

	public EnumDialogArea Alignment;

	public float PosX;

	public float PosY;

	public DialogRow[] Rows;

	public double SizeMultiplier = 1.0;

	public double Padding = 10.0;

	public bool DisableWorldInteract = true;

	public OnValueSetDelegate OnSet;

	public OnValueGetDelegate OnGet;

	public void ToBytes(BinaryWriter writer)
	{
		writer.Write(Code);
		writer.Write((int)Alignment);
		writer.Write(PosX);
		writer.Write(PosY);
		writer.Write(Rows.Length);
		for (int i = 0; i < Rows.Length; i++)
		{
			Rows[i].ToBytes(writer);
		}
		writer.Write(SizeMultiplier);
	}

	public void FromBytes(BinaryReader reader)
	{
		Code = reader.ReadString();
		Alignment = (EnumDialogArea)reader.ReadInt32();
		PosX = reader.ReadSingle();
		PosY = reader.ReadSingle();
		Rows = new DialogRow[reader.ReadInt32()];
		for (int i = 0; i < Rows.Length; i++)
		{
			Rows[i] = new DialogRow();
			Rows[i].FromBytes(reader);
		}
		SizeMultiplier = reader.ReadSingle();
	}
}
