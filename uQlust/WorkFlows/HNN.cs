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

namespace WorkFlows
{
    public partial class HNN : RpartSimple
    {
        OMICS_CHOOSE alg;
        public HNN(Form parent, Settings set, ResultWindow results, OMICS_CHOOSE alg,string fileName = null,string dataFileName=null): base(parent, set, results, fileName,dataFileName)
        {
            InitializeComponent();
            this.label10.Visible = false;
            this.refPoints.Visible = false;
            this.alg = alg;            
            this.button2.Click -= new System.EventHandler(this.button2_Click);
            if (alg != OMICS_CHOOSE.HNN)
            {
                label1.Text = "Choose file with class labels";
                label11.Text= "Choose file without class labels";
                this.Text = "GuidedClustering";
            }
            if (fileName!=null)
            {
                opt.ReadOptionFile(fileName);
                textBox2.Text = opt.hnn.testFile;
                textBox3.Text = opt.hnn.labelsFile;
                if(opt.profileFiles!=null && opt.profileFiles.Count>0)
                    textBox1.Text = opt.profileFiles[0];
                
            
            }
            
            if (set.mode == INPUTMODE.USER_DEFINED)
                checkBox1.Visible = true;
        }
        public HNN(OmicsInput om,Form parent, Settings set, ResultWindow results, OMICS_CHOOSE alg, string fileName = null, string dataFileName = null):this(parent, set,results,alg,fileName , dataFileName)
        {
            opt.omics = om;
        }
        void SaveFile(string fileName,List<string> labels,List<string> classLabels)
        {
            StreamWriter w = new StreamWriter(fileName);
            for (int i = 0; i < labels.Count; i++)
                w.WriteLine(labels[i] + " " + classLabels[i]);
            w.Close();
        }

        private void buttonHNN_Click(object sender, EventArgs e)
        {
            opt.dataDir.Clear();
            opt.clusterAlgorithm.Clear();
            
            opt.hash.hashCluster = true;
            opt.hash.selectionMethod = COL_SELECTION.ENTROPY;

            if (alg==OMICS_CHOOSE.HNN)
                opt.clusterAlgorithm.Add(ClusterAlgorithm.HNN);
            else
                opt.clusterAlgorithm.Add(ClusterAlgorithm.GuidedHashCluster);
            if (radioButton1.Checked)
            {                
                opt.hash.hashCluster = false;
                opt.hash.combine = true;
            }

            opt.profileFiles.Clear();
            opt.hnn.testFile = textBox2.Text;
            opt.hnn.labelsFile = textBox3.Text;
            if (textBox3.Text.Length > 0)
                opt.hnn.labelsFile = textBox3.Text;
            
            if (opt.hash.profileName.Contains("omics"))
            {        
                opt.profileFiles.Add(this.dataFileName);
            }
            else
                opt.profileFiles.Add(textBox1.Text);
            opt.hash.relClusters = (int)relevantC.Value;
            opt.hash.perData = (int)percentData.Value;
            opt.hash.useConsensusStates = checkBox1.Checked;
            set.Save();
            results.Show();
            results.Focus();
            results.BringToFront();
            results.Run(processName + "_" + counter++, opt);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DialogResult res = openFileDialog1.ShowDialog();

            if (res == DialogResult.OK)
                 textBox2.Text = ((OpenFileDialog)(openFileDialog1)).FileName;
        }

        private void HNN_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!previous)
                parent.Close();   
        }

        private void button4_Click(object sender, EventArgs e)
        {
            previous = true;
            parent.Show();
            this.Hide();
            //this.Close();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            DialogResult res = openFileDialog1.ShowDialog();

            if (res == DialogResult.OK)
                textBox3.Text = ((OpenFileDialog)(openFileDialog1)).FileName;

        }
    }
}
