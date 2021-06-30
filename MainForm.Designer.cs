namespace kirancrooks.Sharp8
{
	partial class MainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.btnLoadROM = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.renderView = new kirancrooks.Sharp8.NearestSampling();
            this.tester = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.renderView)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tester)).BeginInit();
            this.SuspendLayout();
            // 
            // btnLoadROM
            // 
            this.btnLoadROM.BackColor = System.Drawing.Color.White;
            this.btnLoadROM.FlatAppearance.BorderSize = 0;
            this.btnLoadROM.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLoadROM.Font = new System.Drawing.Font("IBM Plex Sans SemiBold", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnLoadROM.Location = new System.Drawing.Point(0, 320);
            this.btnLoadROM.Name = "btnLoadROM";
            this.btnLoadROM.Size = new System.Drawing.Size(320, 25);
            this.btnLoadROM.TabIndex = 1;
            this.btnLoadROM.Text = "Load ROM";
            this.btnLoadROM.UseVisualStyleBackColor = false;
            this.btnLoadROM.Click += new System.EventHandler(this.LoadROM_Click);
            // 
            // btnStart
            // 
            this.btnStart.BackColor = System.Drawing.Color.White;
            this.btnStart.FlatAppearance.BorderSize = 0;
            this.btnStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnStart.Font = new System.Drawing.Font("IBM Plex Sans SemiBold", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnStart.Location = new System.Drawing.Point(320, 320);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(320, 25);
            this.btnStart.TabIndex = 2;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = false;
            this.btnStart.Click += new System.EventHandler(this.Start_Click);
            // 
            // renderView
            // 
            this.renderView.Location = new System.Drawing.Point(0, 0);
            this.renderView.Name = "renderView";
            this.renderView.Sampling = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            this.renderView.Size = new System.Drawing.Size(640, 320);
            this.renderView.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.renderView.TabIndex = 0;
            this.renderView.TabStop = false;
            // 
            // tester
            // 
            this.tester.Image = global::Sharp8.Properties.Resources.test;
            this.tester.Location = new System.Drawing.Point(0, 0);
            this.tester.Name = "tester";
            this.tester.Size = new System.Drawing.Size(640, 320);
            this.tester.TabIndex = 3;
            this.tester.TabStop = false;
            this.tester.Visible = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(640, 345);
            this.Controls.Add(this.tester);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.btnLoadROM);
            this.Controls.Add(this.renderView);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "MainForm";
            this.Text = "Sharp-8";
            this.DragDrop += new System.Windows.Forms.DragEventHandler(this.MainForm_DragDrop);
            this.DragEnter += new System.Windows.Forms.DragEventHandler(this.MainForm_DragEnter);
            this.DragLeave += new System.EventHandler(this.MainForm_DragLeave);
            ((System.ComponentModel.ISupportInitialize)(this.renderView)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tester)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion

		private NearestSampling renderView;
		private System.Windows.Forms.Button btnLoadROM;
		private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.PictureBox tester;
    }
}

