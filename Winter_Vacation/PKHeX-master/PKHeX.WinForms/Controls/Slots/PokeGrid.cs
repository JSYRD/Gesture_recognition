﻿using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PKHeX.Drawing.PokeSprite;

namespace PKHeX.WinForms.Controls
{
    public partial class PokeGrid : UserControl
    {
        public PokeGrid()
        {
            InitializeComponent();
        }

        public readonly List<PictureBox> Entries = new();
        public int Slots { get; private set; }

        private int sizeW = 40;
        private int sizeH = 30;

        public bool InitializeGrid(int width, int height, SpriteBuilder info)
        {
            var newCount = width * height;
            if (Slots == newCount)
            {
                if (info.Width == sizeW && info.Height == sizeH)
                    return false;
            }

            sizeW = info.Width;
            sizeH = info.Height;
            Generate(width, height);
            Slots = newCount;

            return true;
        }

        private const int padEdge = 1; // edges
        private const int border = 1; // between

        private void Generate(int width, int height)
        {
            SuspendLayout();
            Controls.Clear();
            Entries.Clear();

            int colWidth = sizeW;
            int rowHeight = sizeH;

            Location = new Point(0, Location.Y); // prevent auto-expanding parent if position changes (centered)
            for (int row = 0; row < height; row++)
            {
                var y = padEdge + (row * (rowHeight + border));
                for (int column = 0; column < width; column++)
                {
                    var x = padEdge + (column * (colWidth + border));
                    var pb = GetControl(sizeW, sizeH);
                    pb.Location = new Point(x, y);
                    Entries.Add(pb);
                }
            }

            int w = (2 * padEdge) + border + (width * (colWidth + border));
            int h = (2 * padEdge) + border + (height * (rowHeight + border));
            Size = new Size(w, h);
            Controls.AddRange(Entries.Cast<Control>().ToArray());
            Debug.WriteLine($"{Name} -- Width: {Width}, Height: {Height}");
            ResumeLayout();
        }

        public void SetBackground(Image img) => BackgroundImage = img;

        public static PictureBox GetControl(int width, int height) => new()
        {
            AutoSize = false,
            SizeMode = PictureBoxSizeMode.CenterImage,
            BackColor = Color.Transparent,
            Width = width + (2 * 1),
            Height = height + (2 * 1),
            Padding = Padding.Empty,
            Margin = Padding.Empty,
            BorderStyle = BorderStyle.FixedSingle,
        };
    }
}
