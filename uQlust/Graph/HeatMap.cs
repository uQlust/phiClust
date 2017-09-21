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
        List<Color> colorMap;
        List<int> indexLabels;
        List<Color> barMap;
        Dictionary<Region, KeyValuePair<bool, string>> regionBarColor = new Dictionary<Region, KeyValuePair<bool, string>>();
        Dictionary<string, Color> labelToColor = new Dictionary<string, Color>();
        DrawHierarchical upper;
        DrawHierarchical left;        
        Bitmap upperBitMap;
        Bitmap leftBitMap;
        bool showLabels = false;
        HClusterNode colorNodeUpper = null;
        HClusterNode colorNodeLeft = null;
        HClusterNode upperNode,auxUpper;
        HClusterNode leftNode,auxLeft;
        List<KeyValuePair<string, List<byte>>> rowOmicsProfiles;
        Dictionary<string, Dictionary<string,int>> omicsProfiles;
        Dictionary<int, int> distV = new Dictionary<int, int>();
        ClusterOutput outp;
        public void ToFront()
        {
            this.BringToFront();
        }

        public HeatMap(HClusterNode upperNode,HClusterNode leftNode,Dictionary<string,string> labels,ClusterOutput outp)
        {
            upperNode.ClearColors(Color.Black);
            leftNode.ClearColors(Color.Black);
            this.outp = outp;
            this.upperNode = auxUpper=upperNode;
            this.Text = outp.alignFile;
            this.leftNode = auxLeft=leftNode;
            List<KeyValuePair<string, List<byte>>> colOmicsProfiles=new List<KeyValuePair<string,List<byte>>>();
            rowOmicsProfiles = new List<KeyValuePair<string, List<byte>>>();
            string[] aux = outp.name.Split(';');
            List<HClusterNode> leaves = leftNode.GetLeaves();
            Dictionary<string, List<byte>> dic1 = new Dictionary<string, List<byte>>();
            Dictionary<string, List<byte>> dic2 = new Dictionary<string, List<byte>>();
            /*foreach(var item in leaves)
            {
                for (int i = 0; i < item.setProfiles.Count; i++)
                    //dic1.Add(item.setStruct[i], item.setProfiles[i]);
                    rowOmicsProfiles.Add(new KeyValuePair<string,List<byte>>(item.setStruct[i],item.setProfiles[i]));
            }*/

            rowOmicsProfiles = OmicsProfile.ReadOmicsProfile(/*"omics_Omics_profile"+"_"+*/aux[0]);
            //colOmicsProfiles = OmicsProfile.ReadOmicsProfile(/*"omics_Omics_profile"+"_"+*/aux[0]+"_transpose");
            omicsProfiles = new Dictionary<string, Dictionary<string, int>>();
            for (int i = 0; i < rowOmicsProfiles.Count; i++)
            {
                if (!omicsProfiles.ContainsKey(rowOmicsProfiles[i].Key))
                    omicsProfiles.Add(rowOmicsProfiles[i].Key, new Dictionary<string, int>());
                
                for (int j = 0; j < outp.aux1.Count; j++)
                {
                    if (!omicsProfiles[rowOmicsProfiles[i].Key].ContainsKey(outp.aux1[j]))
                        omicsProfiles[rowOmicsProfiles[i].Key].Add(outp.aux1[j], rowOmicsProfiles[i].Value[j]);

                }
            }
            colorMap = outp.profilesColor;
            indexLabels = outp.auxInt;
            InitializeComponent();
            this.Name = "HeatMap " + outp.dirName;
            for (int i = 1; i < outp.aux2.Count;i++ )
                comboBox1.Items.Add(outp.aux2[i]);
            upperBitMap = new Bitmap(pictureBox2.Width, pictureBox2.Height);
            leftBitMap = new Bitmap(pictureBox3.Width, pictureBox3.Height);
            upper = new DrawHierarchical(upperNode, outp.measure, labels, upperBitMap, true);
            //upper.viewType = true;
            left = new DrawHierarchical(leftNode, outp.measure, labels, leftBitMap, false);
            //left.viewType = true;
            foreach (var item in rowOmicsProfiles)
                foreach (var v in item.Value)
                    if (!distV.ContainsKey(v))
                        distV.Add(v,0);
            BarColors();
            pictureBox2.Refresh();
        }
        void BarColors()
        {
/*            int[] tab = new int[4];

            tab[0] = 0; tab[1] = 85; tab[2] = 170; tab[3] = 255;
            barMap = new List<Color>();
            for (int i = 0; i < tab.Length; i++)
                for (int j = 0; j < tab.Length; j++)
                    for (int n = 0; n < tab.Length; n++)
                        barMap.Add(Color.FromArgb(tab[i], tab[j], tab[n]));            */
            barMap = new List<Color>();
            barMap.Add(Color.Black);
            barMap.Add(Color.Red);
            barMap.Add(Color.Blue);
            barMap.Add(Color.Green);
            barMap.Add(Color.Orange);
            barMap.Add(Color.Plum);
            barMap.Add(Color.Navy);
            barMap.Add(Color.LightGreen);
            barMap.Add(Color.MediumTurquoise);
            barMap.Add(Color.Olive);
            barMap.Add(Color.Yellow);
            barMap.Add(Color.CornflowerBlue);
            barMap.Add(Color.Ivory);


        }
        double CalculateProfilesAccuracy(List<string> profiles, List<string> leaves)
        {
            double res = 0;

            for (int i = 0; i < profiles.Count; i++)
            {
                for (int j = 0; j < leaves.Count; j++)
                {
                    int ind = omicsProfiles[profiles[i]][leaves[j]];
                    if (ind == omicsProfiles[profiles[0]][leaves[j]])
                        res++;
                }
            }
            res /= leaves.Count * profiles.Count;
            return res;

        }
        double DrawHeatMapNode(Bitmap bmp,List<string> profiles,List<string> leaves)
        {
            Graphics g = Graphics.FromImage(bmp);
            int width = bmp.Width;
            int height = bmp.Height;
            float xStep=(float)width;
            float yStep = (float)height;


            //if(upperLeaves.Count>1)
                xStep = (float)width /(leaves.Count);
            
            //if(profiles.Count>1)
                yStep = (float)height /(profiles.Count);

            int currentX = 0;
            int currentY = 0;
            SolidBrush b = new SolidBrush(Color.Black);
            for (int i = 0; i < profiles.Count;i++ )
            {
                currentY = (int)(i * yStep);
                for (int j = 0; j < leaves.Count; j++)
                {
                    int ind = omicsProfiles[profiles[i]][leaves[j]];
                    ind--;
                    if (colorMap.Count <= ind)
                        throw new Exception("Color map is to small");                    

                    Color c = colorMap[ind];
                    b.Color = c;
                    currentX = (int)(j * xStep);
                    g.FillRectangle(b, currentX, currentY, xStep, yStep);
                    
                }
                
            }

            return CalculateProfilesAccuracy(profiles, leaves);
        }
        void DrawHeatMap(Graphics g)
        {
            List<HClusterNode> upperLeaves =auxUpper.GetLeaves();
            List<HClusterNode> leftLeaves = auxLeft.GetLeaves();
            
            upperLeaves=upperLeaves.OrderByDescending(o => o.gNode.x).Reverse().ToList();
            leftLeaves=leftLeaves.OrderByDescending(o => o.gNode.y).Reverse().ToList();
            SolidBrush b = new SolidBrush(Color.Black);
            double yPos1, yPos2;
            for(int i=0;i<leftLeaves.Count;i++)
            {

                int y = leftLeaves[i].gNode.y;
                if (i == 0)
                {
                    yPos1 = y - (leftLeaves[i + 1].gNode.y - y) / 2.0;
                    yPos2 = y + (leftLeaves[i + 1].gNode.y - y) / 2.0;

                }
                else
                    if (i + 1 < leftLeaves.Count)
                    {
                        yPos1 = y - (y - leftLeaves[i - 1].gNode.y) / 2.0;
                        yPos2 = y + (leftLeaves[i + 1].gNode.y - y) / 2.0;

                    }
                    else
                    {
                        yPos1 = y - (y - leftLeaves[i - 1].gNode.y) / 2.0;
                        yPos2 = y + (y - leftLeaves[i - 1].gNode.y) / 2.0;

                    }


                double xPos1, xPos2;
                for(int j=0;j<upperLeaves.Count;j++)
                {
                    int x = upperLeaves[j].gNode.x;
                    double vv = 0;

                    if (j + 1 < upperLeaves.Count)
                        vv = (upperLeaves[j + 1].gNode.x - x) / 2.0;
                    if (j == 0)
                    {
                        xPos1 = x - vv;
                        xPos2 = x + vv;

                    }
                    else
                        if (j + 1 < upperLeaves.Count)
                        {
                            xPos1 = x - (x - upperLeaves[j - 1].gNode.x) / 2.0;
                            xPos2 = x + vv;
                        }
                        else
                        {
                            xPos1 = x - (x - upperLeaves[j - 1].gNode.x) / 2.0;
                            xPos2 = x + (x - upperLeaves[j - 1].gNode.x) / 2.0;
                        }
                    if ((xPos2 - xPos1) == 0)
                        continue;
                    if (!omicsProfiles.ContainsKey(leftLeaves[i].refStructure))
                        throw new Exception("Omics profile does not contain " + leftLeaves[i].refStructure);
                    if(!omicsProfiles[leftLeaves[i].refStructure].ContainsKey(upperLeaves[j].refStructure))
                        throw new Exception("Omics profile does not contain " + upperLeaves[j].refStructure);
                    int ind = omicsProfiles[leftLeaves[i].refStructure][upperLeaves[j].refStructure];
                    ind--;
                    if (colorMap.Count <= ind)
                        throw new Exception("Color map is to small");
                    Color c = colorMap[ind];
                    b.Color = c;                   
                    if(yPos2-yPos1>0)
                        g.FillRectangle(b, (float)xPos1,(float) yPos1, (float)(xPos2-xPos1), (float)(yPos2-yPos1));                    
                }
            }

        }   
        void PrepareBarRegions(Graphics g)
        {
            int x = 50, y = 0;
            bool regionTest = false;

            if (upper.labColor == null || upper.labColor.Keys.Count == 0)
                return;

            if (regionBarColor.Count > 0)
                regionTest = true;

            List<string> labKeys = new List<string>(upper.labColor.Keys);
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
                    upper.labColor[regItem.Value.Value] = Color.Empty;
            }

        }
        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = Graphics.FromImage(upperBitMap);
            PrepareBarRegions(e.Graphics);            
            g.Clear(this.BackColor);
            //e.Graphics.Clear(pictureBox2.BackColor);
            
            upper.DrawOnBuffer(upperBitMap, false, 1, Color.Empty);
            e.Graphics.DrawImage(upperBitMap, 0, 0);
            if ( upper.labColor!=null &&  upper.labColor.Count>0)
            {
                Font drawFont = new System.Drawing.Font("Arial", 8);
                foreach(var item in regionBarColor)
                {
                    RectangleF rec = item.Key.GetBounds(e.Graphics);
                    SolidBrush drawBrush = new System.Drawing.SolidBrush(upper.labColor[item.Value.Value]);
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
            left.posStart = 5;
            left.DrawOnBuffer(leftBitMap, false, 1, Color.Empty);
            e.Graphics.DrawImage(leftBitMap, 0, 0);
        }


        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(this.BackColor);
            DrawHeatMap(e.Graphics);
        }

        private void HeatMap_ResizeEnd(object sender, EventArgs e)
        {
            upperBitMap = new Bitmap(pictureBox2.Width, pictureBox2.Height);
            leftBitMap = new Bitmap(pictureBox3.Width, pictureBox3.Height);
            left.PrepareGraphNodes(leftBitMap);
            upper.PrepareGraphNodes(upperBitMap);
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

            List<int> ordered = distV.Keys.ToList();
            ordered.Sort();
                foreach(var item in ordered)
                {
                    b.Color=colorMap[item-1];
                    e.Graphics.FillRectangle(b,xPos,yPos,15,10);
                //e.Graphics.DrawString(item.ToString(), drawFont, drawBrush, xPos+25,yPos-3);
                e.Graphics.DrawString(indexLabels[item-1].ToString(), drawFont, drawBrush, xPos + 25, yPos - 3);
                yPos += 25;
                    if(yPos>pictureBox4.Height)
                    {
                        yPos = 5;
                        xPos += 40;
                    }
                }
             
        }

        private void tableLayoutPanel1_Paint_1(object sender, PaintEventArgs e)
        {

        }

        private void pictureBox2_MouseMove(object sender, MouseEventArgs e)
        {
            bool test = false;
            if (colorNodeUpper != null)
            {
                upper.ChangeColors(colorNodeUpper, Color.Black);
                test = true;
            }

            colorNodeUpper = upper.FindClosestNode(e.X, e.Y);

            if(colorNodeUpper!=null)
            {
                upper.ChangeColors(colorNodeUpper, Color.Red);
                test = true;
            }
            if (test)
            {
                Graphics g = Graphics.FromImage(upperBitMap);
                g.Clear(pictureBox2.BackColor);
               
                if (colorNodeUpper != null)
                {
                    float v = ((float)colorNodeUpper.setStruct.Count) / upperNode.setStruct.Count * 360;
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
                                upper.labColor = new Dictionary<string, Color>();
                                foreach (var itemColor in labelToColor)
                                    upper.labColor.Add(itemColor.Key, itemColor.Value);
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
                        List<HClusterNode> leftLeaves = auxLeft.GetLeaves();
                        leftLeaves = leftLeaves.OrderByDescending(o => o.gNode.y).Reverse().ToList();
                        List<string> refList = new List<string>();
                        foreach (var item in leftLeaves)
                            refList.Add(item.refStructure);
                        pn.drawPic = delegate { double res = DrawHeatMapNode(pn.bmp, refList, nodeC.setStruct); pn.Text = res.ToString(); };
                        pn.Show();
                    }
                    else
                    {
                        if (colorNodeUpper != null && colorNodeUpper.joined != null)
                            auxUpper = colorNodeUpper;
                    }
                    break;      
                case MouseButtons.Right:
                        auxUpper = upperNode;
                        break;
            }
            if (auxUpper != null)
            {
                if(colorNodeUpper!=null)
                    upper.ChangeColors(colorNodeUpper, Color.Black);
                colorNodeUpper = null;
                upper.rootNode = auxUpper;
                upper.PrepareGraphNodes(upperBitMap);
                Graphics g = Graphics.FromImage(upperBitMap);
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
                left.ChangeColors(colorNodeLeft, Color.Black);
            }

            colorNodeLeft = left.FindClosestNode(e.X, e.Y);

            if (colorNodeLeft != null)
            {
                left.ChangeColors(colorNodeLeft, Color.Red);
                test = true;
            }
            if (test)
            {
                Graphics g = Graphics.FromImage(leftBitMap);
                g.Clear(pictureBox3.BackColor);
                
                if(colorNodeLeft!=null)
                { 
                    float v=((float)colorNodeLeft.setStruct.Count)/leftNode.setStruct.Count*360;
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
                         List<HClusterNode> upperLeaves = auxUpper.GetLeaves();
                         upperLeaves = upperLeaves.OrderByDescending(o => o.gNode.y).Reverse().ToList();
                         List<string> refList = new List<string>();
                        foreach(var item in upperLeaves)
                            refList.Add(item.refStructure);
                        pn.drawPic = delegate { double res = DrawHeatMapNode(pn.bmp, nodeC.setStruct, refList); pn.Text = res.ToString(); };
                         pn.Show();
                         pn.pictureBox1.Invalidate();
                    }
                    else
                        if (colorNodeLeft != null && colorNodeLeft.joined!=null)
                            auxLeft = colorNodeLeft;
                    break;
                case MouseButtons.Right:
                    auxLeft = leftNode;
                    break;
            }
            if (auxLeft != null)
            {
                if (colorNodeLeft != null)
                    left.ChangeColors(colorNodeLeft, Color.Black);
                colorNodeLeft = null;
                left.rootNode = auxLeft;
                left.PrepareGraphNodes(leftBitMap);
                Graphics g = Graphics.FromImage(leftBitMap);
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
            upper.showLabels = showLabels;
            numericUpDown1.Visible = showLabels;
            numericUpDown1.Value = upper.labelSize;
            left.showLabels = showLabels;
            Graphics g = Graphics.FromImage(upperBitMap);
            g.Clear(pictureBox2.BackColor);
            g = Graphics.FromImage(leftBitMap);
            g.Clear(pictureBox3.BackColor);

            pictureBox3.Refresh();
            pictureBox2.Refresh();
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            left.labelSize = (int)numericUpDown1.Value;
            upper.labelSize = (int)numericUpDown1.Value;

            Graphics g = Graphics.FromImage(upperBitMap);
            g.Clear(pictureBox2.BackColor);
            g = Graphics.FromImage(leftBitMap);
            g.Clear(pictureBox3.BackColor);

            pictureBox3.Refresh();
            pictureBox2.Refresh();

        }

        private void pictureBox2_MouseLeave(object sender, EventArgs e)
        {
            if (colorNodeUpper != null)
            {             
                upper.ChangeColors(colorNodeUpper, Color.Black);
                colorNodeUpper = null;
                Graphics g = Graphics.FromImage(upperBitMap);
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
                List<HClusterNode> nodes = leftNode.GetLeaves();
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
                nodes = upperNode.GetLeaves();
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

        private void pictureBox3_MouseLeave(object sender, EventArgs e)
        {
            if (colorNodeLeft != null)
            {         
                left.ChangeColors(colorNodeLeft, Color.Black);
                colorNodeLeft = null;
                Graphics g = Graphics.FromImage(leftBitMap);
                g.Clear(pictureBox3.BackColor);

                //g.DrawArc(new Pen(Color.DarkGreen),new Rectangle(e.X,e.Y,10,10), 0.0, v);
                pictureBox3.Refresh();
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            List<HClusterNode> leaveas = upperNode.GetLeaves();                
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
                        labelToColor.Add(aux[comboBox1.SelectedIndex + 1], barMap[0]);
                    }
                }
            }
            List<string> labels = new List<string>(labelToColor.Keys);
            labelToColor.Clear();
            foreach (var item in labels)
                labelToColor.Add(item, barMap[index++]);

            upper.labColor = new Dictionary<string, Color>();
            foreach (var item in labelToColor)            
                upper.labColor.Add(item.Key,item.Value);
            
            
            upper.currentLabelIndex = comboBox1.SelectedIndex+1;
            
            
            //int step = (barMap.Count-1) / labels.Count;

            if (labels.Count > barMap.Count)
            {
                MessageBox.Show("To many colors need to be used!");
                return;
            }
            

            pictureBox2.Refresh();
        }

      
    }
}
