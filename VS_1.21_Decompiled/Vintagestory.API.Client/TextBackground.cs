namespace Vintagestory.API.Client;

public class TextBackground
{
	public int HorPadding;

	public int VerPadding;

	public double Radius;

	public double[] FillColor = new double[4];

	public double[] BorderColor = GuiStyle.DialogBorderColor;

	public double BorderWidth;

	public bool Shade;

	public double[] ShadeColor = new double[4]
	{
		GuiStyle.DialogLightBgColor[0] * 1.4,
		GuiStyle.DialogStrongBgColor[1] * 1.4,
		GuiStyle.DialogStrongBgColor[2] * 1.4,
		1.0
	};

	public int Padding
	{
		set
		{
			HorPadding = value;
			VerPadding = value;
		}
	}

	public TextBackground Clone()
	{
		return new TextBackground
		{
			HorPadding = HorPadding,
			VerPadding = VerPadding,
			Radius = Radius,
			FillColor = (double[])FillColor.Clone(),
			BorderColor = (double[])BorderColor.Clone(),
			ShadeColor = (double[])ShadeColor.Clone(),
			Shade = Shade,
			BorderWidth = BorderWidth
		};
	}
}
