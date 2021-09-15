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
		readonly Action<bool[,]> doDraw;
		readonly Action<int> doBeep;

		// Buffers
		readonly bool[,] renderBuffer = new bool[sWidth, sHeight];
		readonly bool[,] flushBuffer = new bool[sWidth, sHeight];
		bool isPendingFlush = true;

		// Registers
		readonly byte[] V = new byte[16];
		byte DELAY, SP;
		ushort I, PC = 0x200;
		readonly HashSet<byte> KEYREG = new HashSet<byte>();

		// Stack and emulated RAM
		readonly ushort[] STACK = new ushort[16];
		readonly byte[] MEM = new byte[0x1000];

		// OPCODE dictionaries
		readonly Dictionary<byte, Action<Decode>> OPCODES;
		readonly Dictionary<byte, Action<Decode>> OPCODES_MISC;

		// Random number generator
		readonly Random RNG = new Random();

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
				{ 0x8 , LOGIC }, // 8xy0 to 8xyE
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
				{ 0x07 , LD_Vx_DT }, // Set Vx = delay timer value
				{ 0x0A , LD_Vx_K }, // Wait for a key press, store the value of the key in Vx
				{ 0x15 , LD_DT_Vx }, // Set delay timer = Vx
				{ 0x18 , LD_ST_Vx }, // Set sound timer = Vx
				{ 0x1E , ADD_I_Vx }, // Set I = I + Vx
				{ 0x29 , LD_F_Vx }, // Set I = location of sprite for digit Vx
				{ 0x33 , LD_B_Vx }, // Store BCD representation of Vx in memory locations I, I+1, and I+2
				{ 0x55 , LD_I_Vx }, // Store registers V0 through Vx in memory starting at location I
				{ 0x65 , LD_Vx_I }, // Read registers V0 through Vx from memory starting at location I
			};
		}

		void DrawFont()
		{
			var offset = 0x0;
			DrawFont(5 * offset++, Font.char0); // 0
			DrawFont(5 * offset++, Font.char1); // 1
			DrawFont(5 * offset++, Font.char2); // 2
			DrawFont(5 * offset++, Font.char3); // 3
			DrawFont(5 * offset++, Font.char4); // 4
			DrawFont(5 * offset++, Font.char5); // 5
			DrawFont(5 * offset++, Font.char6); // 6
			DrawFont(5 * offset++, Font.char7); // 7
			DrawFont(5 * offset++, Font.char8); // 8
			DrawFont(5 * offset++, Font.char9); // 9
			DrawFont(5 * offset++, Font.charA); // A
			DrawFont(5 * offset++, Font.charB); // B
			DrawFont(5 * offset++, Font.charC); // C
			DrawFont(5 * offset++, Font.charD); // D
			DrawFont(5 * offset++, Font.charE); // E
			DrawFont(5 * offset++, Font.charF); // F
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

		public void DoTick()
		{
			var tickop = (ushort)(MEM[PC++] << 8 | MEM[PC++]);

			var op = new Decode()
			{
				OPCODE = tickop,
				NNN = (ushort)(tickop & 0x0FFF),
				NN = (byte)(tickop & 0x00FF),
				N = (byte)(tickop & 0x000F),
				Vx = (byte)((tickop & 0x0F00) >> 8),
				Vy = (byte)((tickop & 0x00F0) >> 4),
			};

			OPCODES[(byte)(tickop >> 12)](op);
		}

		public void DoTick60()
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
			if (V[op.Vx] == op.NN)
				PC += 2;
		}

		void SNE_Vx(Decode op)
		{
			if (V[op.Vx] != op.NN)
				PC += 2;
		}

		void SE_Vx_Vy(Decode op)
		{
			if (V[op.Vx] == V[op.Vy])
				PC += 2;
		}

		void SNE_Vx_Vy(Decode op)
		{
			if (V[op.Vx] != V[op.Vy])
				PC += 2;
		}

		void LD_Vx(Decode op)
		{
			V[op.Vx] = op.NN;
		}

		void ADD_Vx(Decode op)
		{
			V[op.Vx] += op.NN;
		}

		void LOGIC(Decode op)
		{
			switch (op.N)
			{
				case 0x0: // Set Vx = Vy
					V[op.Vx] = V[op.Vy];
					break;
				case 0x1: // Set Vx = Vx OR Vy
					V[op.Vx] |= V[op.Vy];
					break;
				case 0x2: // Set Vx = Vx AND Vy
					V[op.Vx] &= V[op.Vy];
					break;
				case 0x3: // Set Vx = Vx XOR Vy
					V[op.Vx] ^= V[op.Vy];
					break;
				case 0x4: // Set Vx = Vx + Vy, set VF = carry
					V[0xF] = (byte)(V[op.Vx] + V[op.Vy] > 0xFF ? 1 : 0);
					V[op.Vx] += V[op.Vy];
					break;
				case 0x5: // Set Vx = Vx - Vy, set VF = NOT borrow
					V[0xF] = (byte)(V[op.Vx] > V[op.Vy] ? 1 : 0);
					V[op.Vx] -= V[op.Vy];
					break;
				case 0x6: // Set Vx = Vx SHR 1
					V[0xF] = (byte)((V[op.Vx] & 0x1) != 0 ? 1 : 0);
					V[op.Vx] /= 2;
					break;
				case 0x7: // Set Vx = Vy - Vx, set VF = NOT borrow
					V[0xF] = (byte)(V[op.Vy] > V[op.Vx] ? 1 : 0);
					V[op.Vy] -= V[op.Vx];
					break;
				case 0xE: // Set Vx = Vx SHL 1
					V[0xF] = (byte)((V[op.Vx] & 0xF) != 0 ? 1 : 0);
					V[op.Vx] *= 2;
					break;
			}
		}

		void LD_I(Decode op)
		{
			I = op.NNN;
		}

		void RND_Vx(Decode op)
		{
			V[op.Vx] = (byte)(RNG.Next(0, 256) & op.NN);
		}

		void DRW_Vx_Vy(Decode op)
		{
			var startX = V[op.Vx];
			var startY = V[op.Vy];

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
				(op.NN == 0x9E && KEYREG.Contains(V[op.Vx]))
				PC += 2;
		}

		void SKNP_Vx(Decode op)
		{
			if 
				(op.NN == 0xA1 && !KEYREG.Contains(V[op.Vx]))
				PC += 2;
		}

		void LD_Vx_K(Decode op)
		{
			if (KEYREG.Count != 0)
				V[op.Vx] = KEYREG.First();
			else
				PC -= 2;
		}

		void LD_Vx_DT(Decode op)
		{
			V[op.Vx] = DELAY;
		}

		void LD_DT_Vx(Decode op)
		{
			DELAY = V[op.Vx];
		}

		void LD_ST_Vx(Decode op)
		{
			doBeep((int)(V[op.Vx] * (1000f / 60)));
		}

		void ADD_I_Vx(Decode op)
		{
			I += V[op.Vx];
		}

		void LD_F_Vx(Decode op)
		{
			I = (ushort)(V[op.Vx] * 5);
		}

		void LD_B_Vx(Decode op)
		{
			MEM[I + 0] = (byte)((V[op.Vx] / 100) % 10);
			MEM[I + 1] = (byte)((V[op.Vx] / 10) % 10);
			MEM[I + 2] = (byte)(V[op.Vx] % 10);
		}

		void LD_I_Vx(Decode op)
		{
			for (var i = 0; i <= op.Vx; i++)
				MEM[I + i] = V[i];
		}

		void LD_Vx_I(Decode op)
		{
			for (var i = 0; i <= op.Vx; i++)
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
