using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace kirancrooks.Sharp8
{
    static class Font
	{
		public const long char0 = 0xF0909090F0;
		public const long char1 = 0x2060202070;
		public const long char2 = 0xF010F080F0;
		public const long char3 = 0xF010F010F0;
		public const long char4 = 0x9090F01010;
		public const long char5 = 0xF080F010F0;
		public const long char6 = 0xF080F090F0;
		public const long char7 = 0xF010204040;
		public const long char8 = 0xF090F090F0;
		public const long char9 = 0xF090F010F0;
		public const long charA = 0xF090F09090;
		public const long charB = 0xE090E090E0;
		public const long charC = 0xF0808080F0;
		public const long charD = 0xE0909090E0;
		public const long charE = 0xF080F080F0;
		public const long charF = 0xF080F08080;
	}

	struct Decode
	{
		public ushort OPCODE;
		public ushort NNN;
		public byte NN, Vx, Vy, N;

		public override string ToString()
		{
			return $"{OPCODE:X4} (X: {Vx:X}, Y: {Vy:X}, N: {N:X}, NN: {NN:X2}, NNN: {NNN:X3})";
		}
	}

	class NearestSampling : PictureBox
	{
		public InterpolationMode Sampling { get; set; }

		protected override void OnPaint(PaintEventArgs args)
		{
			args.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
			args.Graphics.InterpolationMode = Sampling;
			base.OnPaint(args);
		}
	}
}
