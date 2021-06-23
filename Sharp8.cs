using System;
using System.Collections.Generic;
using System.Linq;

namespace kirancrooks.Sharp8
{
	class Sharp8
	{
		// Define screen area [64x32]
		const int sWidth = 64, sHeight = 32;

		// Actions
		Action<bool[,]> doDraw;
		Action<int> doBeep;

		// Buffers
		bool[,] renderBuffer = new bool[sWidth, sHeight];
		bool[,] flushBuffer = new bool[sWidth, sHeight];
		bool isPendingFlush = true;

		// Registers
		byte[] V = new byte[16];
		byte DELAY, SP;
		ushort I, PC = 0x200;
		HashSet<byte> KEYREG = new HashSet<byte>();

		// Stack and emulated RAM
		ushort[] STACK = new ushort[16];
		byte[] MEM = new byte[0x1000];

		// OPCODE dictionaries
		Dictionary<byte, Action<Decode>> OPCODES;
		Dictionary<byte, Action<Decode>> OPCODES_MISC;

		// Random number generator
		Random RNG = new Random();

		public Sharp8(Action<bool[,]> doDraw, Action<int> doBeep)
		{
			this.doDraw = doDraw;
			this.doBeep = doBeep;

			DrawFont();

			OPCODES = new Dictionary<byte, Action<Decode>>
			{
				{ 0x0 , CLSRET }, // Clear the display OR Return out of subroutine
				{ 0x1 , JP }, // Jump to location nnn
				{ 0x2 , CALL }, // Call subroutine at nnn
				{ 0x3 , SE_Vx }, // Skip next instruction if Vx = kk
				{ 0x4 , SNE_Vx }, // Skip next instruction if Vx != kk
				{ 0x5 , SE_Vx_Vy }, // Skip next instruction if Vx = Vy
				{ 0x6 , LD_Vx }, // Set Vx = kk
				{ 0x7 , ADD_Vx }, // Set Vx = Vx + kk
				{ 0x8 , LOGIC },
				{ 0x9 , SNE_Vx_Vy }, // Skip next instruction if Vx != Vy
				{ 0xA , LD_I }, // Set I = nnn
				{ 0xB , JP_V0 }, // Jump to location nnn + V0
				{ 0xC , RND_Vx }, // Set Vx = random byte AND kk
				{ 0xD , DRW_Vx_Vy }, // Display n-byte sprite starting at memory location I at (Vx, Vy), set VF = collision
				{ 0xE , SKP_Vx }, // Skip next instruction if key with the value of Vx is pressed
				{ 0xA1 , SKNP_Vx }, // Skip next instruction if key with the value of Vx is not pressed
				{ 0xF , MSC },
			};
			OPCODES_MISC = new Dictionary<byte, Action<Decode>>
			{
				{ 0x07 , LD_Vx_DT },
				{ 0x0A , LD_Vx_K },
				{ 0x15 , LD_DT_Vx },
				{ 0x18 , LD_ST_Vx },
				{ 0x1E , ADD_I_Vx },
				{ 0x29 , LD_F_Vx },
				{ 0x33 , LD_B_Vx },
				{ 0x55 , LD_I_Vx },
				{ 0x65 , LD_Vx_I },
			};
		}

		void DrawFont()
		{
			var offset = 0x0;
			DrawFont(5 * offset++, Font.char0);
			DrawFont(5 * offset++, Font.char1);
			DrawFont(5 * offset++, Font.char2);
			DrawFont(5 * offset++, Font.char3);
			DrawFont(5 * offset++, Font.char4);
			DrawFont(5 * offset++, Font.char5);
			DrawFont(5 * offset++, Font.char6);
			DrawFont(5 * offset++, Font.char7);
			DrawFont(5 * offset++, Font.char8);
			DrawFont(5 * offset++, Font.char9);
			DrawFont(5 * offset++, Font.charA);
			DrawFont(5 * offset++, Font.charB);
			DrawFont(5 * offset++, Font.charC);
			DrawFont(5 * offset++, Font.charD);
			DrawFont(5 * offset++, Font.charE);
			DrawFont(5 * offset++, Font.charF);
		}

		void DrawFont(int address, long fData)
		{
			MEM[address + 0] = (byte)((fData & 0xF000000000) >> (8 * 4));
			MEM[address + 1] = (byte)((fData & 0x00F0000000) >> (8 * 3));
			MEM[address + 2] = (byte)((fData & 0x0000F00000) >> (8 * 2));
			MEM[address + 3] = (byte)((fData & 0x000000F000) >> (8 * 1));
			MEM[address + 4] = (byte)((fData & 0x00000000F0) >> (8 * 0));
		}

		public void LoadROM(byte[] op)
		{
			Array.Copy(op, 0, MEM, 0x200, op.Length);
		}

		public void doTick()
		{
			var tOPCODE = (ushort)(MEM[PC++] << 8 | MEM[PC++]);

			var op = new Decode()
			{
				OPCODE = tOPCODE,
				NNN = (ushort)(tOPCODE & 0x0FFF),
				NN = (byte)(tOPCODE & 0x00FF),
				N = (byte)(tOPCODE & 0x000F),
				X = (byte)((tOPCODE & 0x0F00) >> 8),
				Y = (byte)((tOPCODE & 0x00F0) >> 4),
			};

			OPCODES[(byte)(tOPCODE >> 12)](op);
		}

		public void doTick60()
		{
			if (DELAY > 0)
				DELAY--;

			if (isPendingFlush)
			{
				isPendingFlush = false;
				doDraw(renderBuffer);
			}
		}

		void MSC(Decode op)
		{
			if (OPCODES_MISC.ContainsKey(op.NN))
				OPCODES_MISC[op.NN](op);
		}

		public void KeyDown(byte key)
		{
			KEYREG.Add(key);
		}

		public void KeyUp(byte key)
		{
			KEYREG.Remove(key);
		}

		void CLSRET(Decode op)
		{
			if (op.NN == 0xE0)
			{
				for (var x = 0; x < sWidth; x++)
					for (var y = 0; y < sHeight; y++)
						renderBuffer[x, y] = false;
			}
			else if (op.NN == 0xEE)
				PC = Pop();
		}

		void JP(Decode op)
		{
			PC = op.NNN;
		}

		void JP_V0(Decode op)
		{
			PC = (ushort)(op.NNN + V[0]);
		}

		void CALL(Decode op)
		{
			Push(PC);
			PC = op.NNN;
		}

		void SE_Vx(Decode op)
		{
			if (V[op.X] == op.NN)
				PC += 2;
		}

		void SNE_Vx(Decode op)
		{
			if (V[op.X] != op.NN)
				PC += 2;
		}

		void SE_Vx_Vy(Decode op)
		{
			if (V[op.X] == V[op.Y])
				PC += 2;
		}

		void SNE_Vx_Vy(Decode op)
		{
			if (V[op.X] != V[op.Y])
				PC += 2;
		}

		void LD_Vx(Decode op)
		{
			V[op.X] = op.NN;
		}

		void ADD_Vx(Decode op)
		{
			V[op.X] += op.NN;
		}

		void LOGIC(Decode op)
		{
			switch (op.N)
			{
				case 0x0:
					V[op.X] = V[op.Y];
					break;
				case 0x1:
					V[op.X] |= V[op.Y];
					break;
				case 0x2:
					V[op.X] &= V[op.Y];
					break;
				case 0x3:
					V[op.X] ^= V[op.Y];
					break;
				case 0x4:
					V[0xF] = (byte)(V[op.X] + V[op.Y] > 0xFF ? 1 : 0);
					V[op.X] += V[op.Y];
					break;
				case 0x5:
					V[0xF] = (byte)(V[op.X] > V[op.Y] ? 1 : 0);
					V[op.X] -= V[op.Y];
					break;
				case 0x6:
					V[0xF] = (byte)((V[op.X] & 0x1) != 0 ? 1 : 0);
					V[op.X] /= 2;
					break;
				case 0x7:
					V[0xF] = (byte)(V[op.Y] > V[op.X] ? 1 : 0);
					V[op.Y] -= V[op.X];
					break;
				case 0xE:
					V[0xF] = (byte)((V[op.X] & 0xF) != 0 ? 1 : 0);
					V[op.X] *= 2;
					break;
			}
		}

		void LD_I(Decode op)
		{
			I = op.NNN;
		}

		void RND_Vx(Decode op)
		{
			V[op.X] = (byte)(RNG.Next(0, 256) & op.NN);
		}

		void DRW_Vx_Vy(Decode op)
		{
			var startX = V[op.X];
			var startY = V[op.Y];

			for (var x = 0; x < sWidth; x++)
			{
				for (var y = 0; y < sHeight; y++)
				{
					if (flushBuffer[x, y])
					{
						if (renderBuffer[x, y])
							isPendingFlush = true;

						flushBuffer[x, y] = false;
						renderBuffer[x, y] = false;
					}
				}
			}

			V[0xF] = 0;
			for (var i = 0; i < op.N; i++)
			{
				var spriteLine = MEM[I + i];

				for (var bit = 0; bit < 8; bit++)
				{
					var x = (startX + bit) % sWidth;
					var y = (startY + i) % sHeight;

					var spriteBit = ((spriteLine >> (7 - bit)) & 1);
					var oldBit = renderBuffer[x, y] ? 1 : 0;

					if (oldBit != spriteBit)
						isPendingFlush = true;

					var newBit = oldBit ^ spriteBit;

					if (newBit != 0)
						renderBuffer[x, y] = true;
					else
						flushBuffer[x, y] = true;

					if (oldBit != 0 && newBit == 0)
						V[0xF] = 1;
				}
			}
		}

		void SKP_Vx(Decode op)
		{
			if
				(op.NN == 0x9E && KEYREG.Contains(V[op.X]))
				PC += 2;
		}

		void SKNP_Vx(Decode op)
        {
			if 
				(op.NN == 0xA1 && !KEYREG.Contains(V[op.X]))
				PC += 2;
		}

		void LD_Vx_K(Decode op)
		{
			if (KEYREG.Count != 0)
				V[op.X] = KEYREG.First();
			else
				PC -= 2;
		}

		void LD_Vx_DT(Decode op)
		{
			V[op.X] = DELAY;
		}

		void LD_DT_Vx(Decode op)
		{
			DELAY = V[op.X];
		}

		void LD_ST_Vx(Decode op)
		{
			doBeep((int)(V[op.X] * (1000f / 60)));
		}

		void ADD_I_Vx(Decode op)
		{
			I += V[op.X];
		}

		void LD_F_Vx(Decode op)
		{
			I = (ushort)(V[op.X] * 5);
		}

		void LD_B_Vx(Decode op)
		{
			MEM[I + 0] = (byte)((V[op.X] / 100) % 10);
			MEM[I + 1] = (byte)((V[op.X] / 10) % 10);
			MEM[I + 2] = (byte)(V[op.X] % 10);
		}

		void LD_I_Vx(Decode op)
		{
			for (var i = 0; i <= op.X; i++)
				MEM[I + i] = V[i];
		}

		void LD_Vx_I(Decode op)
		{
			for (var i = 0; i <= op.X; i++)
				V[i] = MEM[I + i];
		}

		void Push(ushort v)
		{
			STACK[SP++] = v;
		}

		ushort Pop()
		{
			return STACK[--SP];
		}
	}
}
