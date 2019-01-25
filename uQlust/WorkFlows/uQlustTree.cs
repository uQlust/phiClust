using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using phiClustCore;
using phiClustCore.Profiles;

namespace WorkFlows
{
    public partial class uQlustTreeSimple : Form, IclusterType
    {
        protected Options opt = new Options();
        protected ResultWindow results;
        Settings set;
        public string processName;
        bool previous = false;
        static int counter = 0;
        CommonDialog dialog;
        Form parent;
        string dataFileName = "";
        ProfileTree tree = new ProfileTree();

        public uQlustTreeSimple(Form parent, Settings set,ResultWindow results, string fileName = null,string dataFileName=null)
        {
            InitializeComponent();
            this.dataFileName = dataFileName;

            if(dataFileName!=null)
            {
                label1.Visible = false;
                textBox1.Visible = false;
                button1.Visible = false;
            }

            distanceControl1.profileInfo = false;
            distanceControl1.hideReference = true;
            distanceControl1.hideSetup = true;
            this.parent = parent;
            this.Location = parent.Location;
            dialog = folderBrowserDialog1;
            if (set.mode == INPUTMODE.USER_DEFINED || set.mode==INPUTMODE.OMICS)
            {
                dialog = openFileDialog1;
                label1.Text = "Choose user defined file with profiles";
            }
          
            this.set = set; 
           
            if (fileName != null)
            {
                opt.ReadOptionFile(fileName);
                SetProfileOptions();
            }
            if (set.mode == INPUTMODE.USER_DEFINED)
            {
                checkBox1.Checked = true;

            }
            numericUpDown1.Value = opt.hash.refPoints;
            this.results = results;
        }
        public override string ToString()
        {
            return "uQlustTree";
        }
        public INPUTMODE GetInputType()
        {
            return set.mode;
        }
        void SetProfileOptions()
        {
            if (set.mode == INPUTMODE.USER_DEFINED|| set.mode==INPUTMODE.OMICS)
            {
                if (opt.profileFiles.Count > 0)
                    textBox1.Text = opt.profileFiles[0];
            }
            else
                if (opt.dataDir.Count > 0)
                    textBox1.Text = opt.dataDir[0];

           
            label3.Text = opt.hash.profileName;
            relevantC.Value = opt.hash.relClusters;
            distanceControl1.distDef = opt.hierarchical.distance;
            distanceControl1.profileName = opt.hierarchical.hammingProfile;
            if (opt.hash.combine)
                radioButton1.Checked = true;
            else            
                Hash.Checked = true;
            
            switch (set.mode)
            {
                case INPUTMODE.USER_DEFINED:
                case INPUTMODE.OMICS:
                    distanceControl1.distDef = DistanceMeasures.COSINE;
                    break;
/*                case INPUTMODE.PROTEIN:
                case INPUTMODE.RNA:
                    distanceControl1.HideCosine = true;
                    break;*/

            }
            if (opt.hash.profileName != null)
            {
                tree.LoadProfiles(opt.hash.profileName);
                label9.Text = tree.GetStringActiveProfiles();
            }
        }
        public void SetProfileName(string name)
        {
            opt.ReadOptionFile(name);
            SetProfileOptions();
        }
        private void button2_Click(object sender, EventArgs e)
        {
            opt.dataDir.Clear();
            opt.profileFiles.Clear();
            if (dataFileName != null && dataFileName.Length > 0)
                opt.profileFiles.Add(dataFileName);
            else
                opt.profileFiles.Add(textBox1.Text);

            opt.hash.relClusters = (int)relevantC.Value;
            opt.hash.perData = 90;
            if (radioButton1.Checked)
                opt.hash.combine = true;
            else
                opt.hash.combine = false;
            opt.clusterAlgorithm.Clear();
            if (!radioHTree.Checked)
                opt.clusterAlgorithm.Add(ClusterAlgorithm.uQlustTree);
            else
                opt.clusterAlgorithm.Add(ClusterAlgorithm.HTree);
            opt.hierarchical.distance = distanceControl1.distDef;
            opt.hierarchical.uHTree = radioHTree.Checked;
            opt.hash.refPoints = (int)numericUpDown1.Value;

            if (opt.hierarchical.distance == DistanceMeasures.HAMMING || opt.hierarchical.distance==DistanceMeasures.COSINE)
                opt.hierarchical.reference1DjuryH = true;
            else
                opt.hierarchical.reference1DjuryH = false;
            if (checkBox1.Checked)
            {
                opt.hash.profileName = ProfileAutomatic.similarityProfileName;
                opt.hash.profileNameReg = ProfileAutomatic.similarityProfileName;
                opt.hierarchical.GenerateAutomaticProfiles(textBox1.Text);
            }
            if (Hash.Checked)
            {
                opt.hash.fcolumns = true;
                opt.hash.selectionMethod = COL_SELECTION.ENTROPY;
            }
            results.Show();
            results.Focus();
            results.BringToFront();
            set.Save();
            
            results.Run(processName+"_"+counter++, opt);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            previous = true;
            parent.Show();
            this.Close();

        }

        private void uQlustTree_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!previous)
                parent.Close();     
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult res = dialog.ShowDialog();

            if (res == DialogResult.OK)
            {
                if (set.mode == INPUTMODE.USER_DEFINED|| set.mode==INPUTMODE.OMICS)
                    textBox1.Text = ((OpenFileDialog)(dialog)).FileName;
                else
                    textBox1.Text = ((FolderBrowserDialog)(dialog)).SelectedPath;
            }
        }

        private void distanceControl1_Load(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked)
            {
                label3.Text = ProfileAutomatic.similarityProfileName;
                label9.Enabled = false;
                label8.Enabled = false;
            }
            else
            {
                label3.Text = opt.hash.profileName;
                label9.Enabled = true;
                label8.Enabled = true;
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if(radioButton1.Checked)
            {
                label4.Visible = true;
                numericUpDown1.Visible = true;
            }
            else
            {
                label4.Visible = false;
                numericUpDown1.Visible = false;
            }
        }

        private void radioHTree_CheckedChanged(object sender, EventArgs e)
        {
            distanceControl1.Visible = !radioHTree.Checked;

        }
    }
}
