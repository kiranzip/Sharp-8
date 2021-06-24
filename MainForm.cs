using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace kirancrooks.Sharp8
{
	public partial class MainForm : Form
	{
		readonly Sharp8 Sharp8;
		readonly Bitmap Screen;
		readonly Stopwatch clock = Stopwatch.StartNew();
		readonly TimeSpan Elapsed60 = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
		readonly TimeSpan Elapsed1000 = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 1000);

		string ROM;
		bool isRendering = false;

		TimeSpan lastTime;

		public MainForm()
		{
			InitializeComponent();

			Screen = new Bitmap(64, 32);
			renderView.Image = Screen;

			Sharp8 = new Sharp8(Draw, DoBeep);
			
			KeyDown += SetKeyDown;
			KeyUp += SetKeyUp;
		}

		readonly Dictionary<Keys, byte> keyMapping = new Dictionary<Keys, byte>
		{
			{ Keys.D1, 0x1 },
			{ Keys.D2, 0x2 },
			{ Keys.D3, 0x3 },
			{ Keys.D4, 0xC },
			{ Keys.Q, 0x4 },
			{ Keys.W, 0x5 },
			{ Keys.E, 0x6 },
			{ Keys.R, 0xD },
			{ Keys.A, 0x7 },
			{ Keys.S, 0x8 },
			{ Keys.D, 0x9 },
			{ Keys.F, 0xE },
			{ Keys.Z, 0xA },
			{ Keys.X, 0x0 },
			{ Keys.C, 0xB },
			{ Keys.V, 0xF },
		};

		private void LoadROM_Click(object sender, EventArgs e)
		{
			OpenFileDialog chooseROM = new OpenFileDialog
			{
				Filter = "CHIP-8 ROMs (*.ch8)|*.ch8|All files (*.*)|*.*",
				InitialDirectory = @"C:\",
				Title = "Please select a valid CHIP-8 ROM file."
			};

			if (chooseROM.ShowDialog() == DialogResult.OK)
			{
				ROM = chooseROM.FileName;
				Sharp8.LoadROM(File.ReadAllBytes(ROM));
			}
		}

		private void Start_Click(object sender, EventArgs e)
		{
			if (isRendering == true)
				return;
			else
			{
				if (ROM == null)
					MessageBox.Show("Please load a ROM.");
				else
					StartRendering();
			}
		}

		void Draw(bool[,] renderBuffer)
		{
			var bits = Screen.LockBits(new Rectangle(0, 0, Screen.Width, Screen.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

			unsafe
			{
				byte* pointer = (byte*)bits.Scan0;

				for (var y = 0; y < Screen.Height; y++)
				{
					for (var x = 0; x < Screen.Width; x++)
					{
						pointer[2] = renderBuffer[x, y] ? (byte)247 : (byte)42; //R
						pointer[1] = renderBuffer[x, y] ? (byte)246 : (byte)47; //G
						pointer[0] = renderBuffer[x, y] ? (byte)238 : (byte)52; //B
						pointer[3] = 255; //A

						pointer += 4;
					}
				}
			}

			Screen.UnlockBits(bits);
		}

		void DoBeep(int ms)
		{
			Console.Beep(500, ms);
		}

		void SetKeyDown(object sender, KeyEventArgs e)
		{
			if (keyMapping.ContainsKey(e.KeyCode))
				Sharp8.KeyDown(keyMapping[e.KeyCode]);
		}

		void SetKeyUp(object sender, KeyEventArgs e)
		{
			if (keyMapping.ContainsKey(e.KeyCode))
				Sharp8.KeyUp(keyMapping[e.KeyCode]);
		}

		void StartRendering()
		{
			isRendering = true;
			Task.Run(RenderLoop);
		}

		Task RenderLoop()
		{
			while (true)
			{
				var current = clock.Elapsed;
				var elapsed = current - lastTime;

				while (elapsed >= Elapsed60)
				{
					this.Invoke((Action)Tick60);
					elapsed -= Elapsed60;
					lastTime += Elapsed60;
				}

				this.Invoke((Action)Tick);

				Thread.Sleep(Elapsed1000);
			}
		}

		void Tick()
		{
			Sharp8.DoTick();
		}

		void Tick60()
		{
			Sharp8.DoTick60();
			renderView.Refresh();
		}
	}
}
