using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using phiClustCore;
using phiClustCore.Profiles;
using phiClustCore.Interface;

namespace Graph
{
    public partial class HeatMap : Form,IVisual
    {
        HeatMapDraw draw = null;
        Dictionary<Region, KeyValuePair<bool, string>> regionBarColor = new Dictionary<Region, KeyValuePair<bool, string>>();
        Dictionary<string, Color> labelToColor = new Dictionary<string, Color>();
        bool showLabels = false;
        HClusterNode colorNodeUpper = null;
        HClusterNode colorNodeLeft = null;
        public void ToFront()
        {
            this.BringToFront();
        }

        public HeatMap(HClusterNode upperNode,HClusterNode leftNode,Dictionary<string,string> labels,ClusterOutput outp)
        {
            upperNode.ClearColors(Color.Black);
            leftNode.ClearColors(Color.Black);
            InitializeComponent();
            this.Text = outp.alignFile;
            //draw=new HeatMapDraw(new Bitmap(tableLayoutPanel1.Width, tableLayoutPanel1.Height), upperNode, leftNode, labels, outp);
            draw = new HeatMapDraw(new Bitmap(pictureBox2.Width,pictureBox2.Height),new Bitmap(pictureBox3.Width,pictureBox3.Height),
                new Bitmap(pictureBox1.Width,pictureBox1.Height),new Bitmap(pictureBox4.Width,pictureBox4.Height),upperNode, leftNode, labels, outp);
        }
        public void PrepareDataForHeatMap()
        {
            draw.PrepareDataForHeatMap();

            this.Name = "HeatMap " + draw.outp.dirName;
            
                        
            for (int i = 1; i < draw.outp.aux2.Count; i++)
                comboBox1.Items.Add(draw.outp.aux2[i]);

            if (comboBox1.Items.Count > 0)
                comboBox1.SelectedIndex = 0;
            else
                comboBox1.Visible = false;


            pictureBox2.Refresh();
        }
        void PrepareBarRegions(Graphics g)
        {
            int x = 50, y = 0;
            bool regionTest = false;

            if (draw.upper.labColor == null || draw.upper.labColor.Keys.Count == 0)
                return;

            if (regionBarColor.Count > 0)
                regionTest = true;

            List<string> labKeys = new List<string>(draw.upper.labColor.Keys);
            Font drawFont = new System.Drawing.Font("Arial", 8);
            foreach (var item in labKeys)
            {
                if (!regionTest)
                {
                    Region reg = new Region(new Rectangle(x, y, 15, 10));
                    regionBarColor.Add(reg, new KeyValuePair<bool, string>(false, item));
                    SizeF textSize = g.MeasureString(item, drawFont);
                    x += 25 + (int)textSize.Width;
                    if (x > this.Width)
                    {
                        y += 150;
                        x = 25;
                    }
                }
            }
            foreach (var regItem in regionBarColor)
            {
                if (regItem.Value.Key)
                    draw.upper.labColor[regItem.Value.Value] = Color.Empty;
            }

        }
        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = Graphics.FromImage(draw.upperBitMap);
            PrepareBarRegions(e.Graphics);            
            g.Clear(this.BackColor);
            //e.Graphics.Clear(pictureBox2.BackColor);
            
            draw.upper.DrawOnBuffer(draw.upperBitMap, false, 1, Color.Empty);
            e.Graphics.DrawImage(draw.upperBitMap, 0, 0);
            if ( draw.upper.labColor!=null &&  draw.upper.labColor.Count>0)
            {
                Font drawFont = new System.Drawing.Font("Arial", 8);
                foreach(var item in regionBarColor)
                {
                    RectangleF rec = item.Key.GetBounds(e.Graphics);
                    SolidBrush drawBrush = new System.Drawing.SolidBrush(draw.upper.labColor[item.Value.Value]);
                    e.Graphics.FillRectangle(drawBrush,rec.X, rec.Y,rec.Width, rec.Height);

                    SizeF textSize = e.Graphics.MeasureString(item.Value.Value, drawFont);
                    drawBrush = new System.Drawing.SolidBrush(Color.Black);
             
                    e.Graphics.DrawString(item.Value.Value, drawFont, drawBrush,rec.X + 20, rec.Y);
                    if(item.Value.Key)
                    {
                        Pen p = new Pen(Color.Black);
                        e.Graphics.DrawLine(p, rec.X, rec.Y, rec.X + rec.Width, rec.Y + rec.Height);
                        e.Graphics.DrawLine(p, rec.X + rec.Width, rec.Y, rec.X, rec.Y + rec.Height);
                    }
                }

            }
          
        }

        private void pictureBox3_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(this.BackColor);
            draw.left.posStart = 5;
            draw.left.DrawOnBuffer(draw.leftBitMap, false, 1, Color.Empty);
            e.Graphics.DrawImage(draw.leftBitMap, 0, 0);
        }


        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(this.BackColor);
            draw.DrawHeatMap(e.Graphics);
        }

        private void HeatMap_ResizeEnd(object sender, EventArgs e)
        {
            draw.upperBitMap = new Bitmap(pictureBox2.Width, pictureBox2.Height);
            draw.leftBitMap = new Bitmap(pictureBox3.Width, pictureBox3.Height);
            draw.left.PrepareGraphNodes(draw.leftBitMap);
            draw.upper.PrepareGraphNodes(draw.upperBitMap);
            pictureBox1.Refresh();
            pictureBox2.Refresh();
            pictureBox3.Refresh();
            //this.Invalidate();
        }

        private void pictureBox4_Paint(object sender, PaintEventArgs e)
        {
            SolidBrush b=new SolidBrush(Color.Black);
            int xPos,yPos;
            xPos=5;yPos=5;
            System.Drawing.Font drawFont = new System.Drawing.Font("Arial", 10);
            System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
            System.Drawing.StringFormat drawFormat = new System.Drawing.StringFormat();

            List<int> ordered = draw.distV.Keys.ToList();
            ordered.Sort();
            foreach (var item in ordered)
            {
                b.Color = draw.colorMap[item - 1];
                e.Graphics.FillRectangle(b, xPos, yPos, 15, 10);
                //e.Graphics.DrawString(item.ToString(), drawFont, drawBrush, xPos+25,yPos-3);
                e.Graphics.DrawString(draw.indexLabels[item - 1].ToString(), drawFont, drawBrush, xPos + 25, yPos - 3);
                yPos += 25;
                if (yPos > pictureBox4.Height)
                {
                    yPos = 5;
                    xPos += 40;
                }
            }
            //test.Paint();             
        }

        private void tableLayoutPanel1_Paint_1(object sender, PaintEventArgs e)
        {

        }

        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            bool test = false;
            if (colorNodeUpper != null)
            {
                draw.upper.ChangeColors(colorNodeUpper, Color.Black);
                test = true;
            }

            colorNodeUpper = draw.upper.FindClosestNode(e.X, e.Y);

            if(colorNodeUpper!=null)
            {
                draw.upper.ChangeColors(colorNodeUpper, Color.Red);
                test = true;
            }
            if (test)
            {
                Graphics g = Graphics.FromImage(draw.upperBitMap);
                g.Clear(pictureBox2.BackColor);
               
                if (colorNodeUpper != null)
                {
                    float v = ((float)colorNodeUpper.setStruct.Count) / draw.upperNode.setStruct.Count * 360;
                    g.FillPie(new SolidBrush(Color.Black), new Rectangle(e.X - 20, e.Y, 15, 15), v, 360 - v);
                    g.FillPie(new SolidBrush(Color.Red), new Rectangle(e.X - 20, e.Y, 15, 15), 0, v);
                }
                pictureBox2.Refresh(); 
              
            }
        }
        private void pictureBox2_MouseClick(object sender, MouseEventArgs e)
        {

            
            switch(e.Button)
            {

                case MouseButtons.Left:

                    if (regionBarColor.Count > 0)
                    {
                        List<Region> l = new List<Region>(regionBarColor.Keys);
                        bool testFlag = false;
                        foreach (var item in l)
                            if (item.IsVisible(e.X, e.Y))
                            {
                                regionBarColor[item] = new KeyValuePair<bool, string>(!regionBarColor[item].Key, regionBarColor[item].Value) ;
                                draw.upper.labColor = new Dictionary<string, Color>();
                                foreach (var itemColor in labelToColor)
                                    draw.upper.labColor.Add(itemColor.Key, itemColor.Value);
                                testFlag = true;                         
                            }
                        if (testFlag)
                        {
                            pictureBox2.Refresh();
                            return;
                        }
                    }
                    HClusterNode nodeC = colorNodeUpper;//upper.CheckClick(upper.rootNode,e.X,e.Y);
                    if (nodeC != null && nodeC.joined == null)
                    {
                        TextBoxView rr = new TextBoxView(nodeC.setStruct);
                       
                        rr.Show();

                        DrawPanel pn = new DrawPanel("Leave profile: "+nodeC.refStructure);
                        int tmp = pn.Height;
                        pn.Height = pn.Width;
                        pn.Width = tmp;
                        pn.CreateBMP();
                        List<HClusterNode> leftLeaves = draw.auxLeft.GetLeaves();
                        leftLeaves = leftLeaves.OrderByDescending(o => o.gNode.y).Reverse().ToList();
                        List<string> refList = new List<string>();
                        foreach (var item in leftLeaves)
                            refList.Add(item.refStructure);
                        pn.drawPic = delegate { draw.DrawHeatMapNode(pn.bmp, refList, nodeC.setStruct); pn.Text = nodeC.consistency.ToString(); };
                        pn.Show();
                    }
                    else
                    {
                        if (colorNodeUpper != null && colorNodeUpper.joined != null)
                            draw.auxUpper = colorNodeUpper;
                    }
                    break;      
                case MouseButtons.Right:
                        draw.auxUpper = draw.upperNode;
                        break;
            }
            if (draw.auxUpper != null)
            {
                if(colorNodeUpper!=null)
                    draw.upper.ChangeColors(colorNodeUpper, Color.Black);
                colorNodeUpper = null;
                draw.upper.rootNode = draw.auxUpper;
                draw.upper.PrepareGraphNodes(draw.upperBitMap);
                Graphics g = Graphics.FromImage(draw.upperBitMap);
                g.Clear(pictureBox2.BackColor);
                pictureBox2.Refresh();
                pictureBox1.Refresh();
            }
        }

        private void pictureBox3_MouseMove(object sender, MouseEventArgs e)
        {
            bool test = false;
            if (colorNodeLeft != null)
            {
                test = true;
                draw.left.ChangeColors(colorNodeLeft, Color.Black);
            }

            colorNodeLeft = draw.left.FindClosestNode(e.X, e.Y);

            if (colorNodeLeft != null)
            {
                draw.left.ChangeColors(colorNodeLeft, Color.Red);
                test = true;
            }
            if (test)
            {
                Graphics g = Graphics.FromImage(draw.leftBitMap);
                g.Clear(pictureBox3.BackColor);
                
                if(colorNodeLeft!=null)
                { 
                    float v=((float)colorNodeLeft.setStruct.Count)/draw.leftNode.setStruct.Count*360;
                    g.FillPie(new SolidBrush(Color.Black), new Rectangle(e.X - 20, e.Y, 15, 15), v,360-v);
                    g.FillPie(new SolidBrush(Color.Red), new Rectangle(e.X-20, e.Y, 15, 15), 0, v);
                }
                //g.DrawArc(new Pen(Color.DarkGreen),new Rectangle(e.X,e.Y,10,10), 0.0, v);
                pictureBox3.Refresh();
            }
        }
       
        private void pictureBox3_MouseClick(object sender, MouseEventArgs e)
        {
            switch (e.Button)
            {

                case MouseButtons.Left:
                    HClusterNode nodeC = colorNodeLeft;//  left.CheckClick(left.rootNode,e.X,e.Y);
                    
                    if (nodeC != null && nodeC.joined==null)
                    {
                        TextBoxView rr = new TextBoxView(nodeC.setStruct);
                        rr.Show();
                        DrawPanel pn = new DrawPanel("Leave profile: " + nodeC.refStructure);
                         pn.CreateBMP();
                         List<HClusterNode> upperLeaves = draw.auxUpper.GetLeaves();
                         upperLeaves = upperLeaves.OrderByDescending(o => o.gNode.y).Reverse().ToList();
                         List<string> refList = new List<string>();
                        foreach(var item in upperLeaves)
                            refList.Add(item.refStructure);
                        pn.drawPic = delegate {draw.DrawHeatMapNode(pn.bmp, nodeC.setStruct, refList); pn.Text = nodeC.consistency.ToString(); };
                         pn.Show();
                         pn.pictureBox1.Invalidate();
                    }
                    else
                        if (colorNodeLeft != null && colorNodeLeft.joined!=null)
                            draw.auxLeft = colorNodeLeft;
                    break;
                case MouseButtons.Right:
                    draw.auxLeft = draw.leftNode;
                    break;
            }
            if (draw.auxLeft != null)
            {
                if (colorNodeLeft != null)
                    draw.left.ChangeColors(colorNodeLeft, Color.Black);
                colorNodeLeft = null;
                draw.left.rootNode = draw.auxLeft;
                draw.left.PrepareGraphNodes(draw.leftBitMap);
                Graphics g = Graphics.FromImage(draw.leftBitMap);
                g.Clear(pictureBox3.BackColor);
                pictureBox3.Refresh();
                pictureBox1.Refresh();
            }
        }

        private void labels_Click(object sender, EventArgs e)
        {
           

        }

        private void toolStripLabel1_Click(object sender, EventArgs e)
        {
            showLabels = !showLabels;
            draw.upper.showLabels = showLabels;
            numericUpDown1.Visible = showLabels;
            numericUpDown1.Value = draw.upper.labelSize;
            draw.left.showLabels = showLabels;
            Graphics g = Graphics.FromImage(draw.upperBitMap);
            g.Clear(pictureBox2.BackColor);
            g = Graphics.FromImage(draw.leftBitMap);
            g.Clear(pictureBox3.BackColor);

            pictureBox3.Refresh();
            pictureBox2.Refresh();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            draw.left.labelSize = (int)numericUpDown1.Value;
            draw.upper.labelSize = (int)numericUpDown1.Value;

            Graphics g = Graphics.FromImage(draw.upperBitMap);
            g.Clear(pictureBox2.BackColor);
            g = Graphics.FromImage(draw.leftBitMap);
            g.Clear(pictureBox3.BackColor);

            pictureBox3.Refresh();
            pictureBox2.Refresh();

        }

        private void pictureBox2_MouseLeave(object sender, EventArgs e)
        {
            if (colorNodeUpper != null)
            {             
                draw.upper.ChangeColors(colorNodeUpper, Color.Black);
                colorNodeUpper = null;
                Graphics g = Graphics.FromImage(draw.upperBitMap);
                g.Clear(pictureBox2.BackColor);
                pictureBox2.Refresh();                 
            }
        }

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            DialogResult res=saveFileDialog1.ShowDialog();
            if(res==DialogResult.OK)
            {
                string fileName = saveFileDialog1.FileName;
                List<List<string>> clusters = new List<List<string>>();
                List<HClusterNode> nodes = draw.leftNode.GetLeaves();
                List<double> cons = new List<double>();
                foreach (var item in nodes)
                {
                    clusters.Add(item.setStruct);
                    cons.Add(item.consistency);
                }
                
                StreamWriter wS = new StreamWriter(fileName + "_gene_microclusters.dat");
                ClusterOutput.Save(clusters,cons,wS,true);
                wS.Close();

                clusters.Clear();
                cons = new List<double>();
                nodes = draw.upperNode.GetLeaves();
                foreach (var item in nodes)
                {
                    clusters.Add(item.setStruct);
                    cons.Add(item.consistency);
                }
                
                wS = new StreamWriter(fileName + "_sample_microclusters.dat");
                ClusterOutput.Save(clusters, cons,wS, true);
                wS.Close();


            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox3_MouseLeave(object sender, EventArgs e)
        {
            if (colorNodeLeft != null)
            {         
                draw.left.ChangeColors(colorNodeLeft, Color.Black);
                colorNodeLeft = null;
                Graphics g = Graphics.FromImage(draw.leftBitMap);
                g.Clear(pictureBox3.BackColor);

                //g.DrawArc(new Pen(Color.DarkGreen),new Rectangle(e.X,e.Y,10,10), 0.0, v);
                pictureBox3.Refresh();
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            List<HClusterNode> leaveas = draw.upperNode.GetLeaves();                
            int index=1;
            labelToColor.Clear();
            regionBarColor.Clear();
            foreach(var item in leaveas)
            {                
                if(item.refStructure.Contains(";"))
                {
                    string[] aux = item.refStructure.Split(';');
                    if (!labelToColor.ContainsKey(aux[comboBox1.SelectedIndex + 1]))
                    {
                        labelToColor.Add(aux[comboBox1.SelectedIndex + 1], draw.barMap[0]);
                    }
                }
            }
            List<string> labels = new List<string>(labelToColor.Keys);
            labelToColor.Clear();
            foreach (var item in labels)
                labelToColor.Add(item, draw.barMap[index++]);

            draw.upper.labColor = new Dictionary<string, Color>();
            foreach (var item in labelToColor)            
                draw.upper.labColor.Add(item.Key,item.Value);
            
            
            draw.upper.currentLabelIndex = comboBox1.SelectedIndex+1;
            
            
            //int step = (barMap.Count-1) / labels.Count;

            if (labels.Count > draw.barMap.Count)
            {
                MessageBox.Show("To many colors need to be used!");
                return;
            }
            

            pictureBox2.Refresh();
        }

      
    }
}
