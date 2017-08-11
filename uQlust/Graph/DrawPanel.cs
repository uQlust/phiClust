using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Graph
{
    public partial class DrawPanel : Form
    {
        public delegate void DrawOnPictureBox();

        public DrawOnPictureBox drawPic;
        public Bitmap bmp;
        public DrawPanel(string name)
        {            
            InitializeComponent();
            this.Text = name;
        }
        public void CreateBMP()
        {
            bmp = new Bitmap(pictureBox1.Width, pictureBox1.Height);
        }
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            drawPic();
            e.Graphics.DrawImage(bmp,0,0);
        }

        private void pictureBox1_Resize(object sender, EventArgs e)
        {
            CreateBMP();
            pictureBox1.Refresh();
        }
    }
}
