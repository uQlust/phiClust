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
    public partial class  OmicsHeatMap:Form 
    {
        Form parent;
        bool previous = false;
        Options opt = new Options();
        public string processName = null;
        static int counter=0;
        ResultWindow results;
        CommonDialog dialog;
        string dataFileName = "";
        ProfileTree tree = new ProfileTree();

        public OmicsHeatMap(Form parent,ResultWindow results,string dataFileName=null)
        {
            this.parent = parent;
            this.dataFileName = dataFileName;           
            this.results = results;
            InitializeComponent();
            if (dataFileName != null)
            {
                label6.Visible = false;
                textBox1.Visible = false;
                button2.Visible = false;
            }
            opt.ReadOptionFile("workFlows/omicsHeatMap/uQlust_config_file_Tree.txt");
            SetProfileOptions();
        }
        void SetProfileOptions()
        {
                if (opt.profileFiles.Count > 0)
                    textBox1.Text = opt.profileFiles[0];

            opt.clusterAlgorithm.Clear();
            opt.clusterAlgorithm.Add(ClusterAlgorithm.OmicsHeatMap);
            label7.Text = opt.hash.profileName;
            relevantC.Value = opt.hash.relClusters;
            if (opt.hash.combine)
                radioButton1.Checked = true;
            else
                Hash.Checked = true;

            if (opt.hash.profileName != null)
            {
                tree.LoadProfiles(opt.hash.profileName);
                label9.Text = tree.GetStringActiveProfiles();
            }
            numericUpDown1.Value = opt.hash.refPoints;
        }

/*        public override void button1_Click(object sender, EventArgs e)
        {
            //processName = "OmicsHeatMap" + "_" + counter++;
            //SaveOptions();
            Settings set = new Settings();
            set.Load();
            set.mode = INPUTMODE.OMICS;
            opt.dataDir.Clear();
            opt.profileFiles.Clear();
            if (set.mode == INPUTMODE.USER_DEFINED || set.mode == INPUTMODE.OMICS)
                opt.profileFiles.Add(textBox1.Text);

            opt.hash.relClusters = (int)relevantC.Value;
            opt.hash.reqClusters = (int)numericUpDown4.Value;
            opt.hash.perData = 90;
            if (radioButton1.Checked)
                opt.hash.combine = true;
            else
                opt.hash.combine = false;
            opt.hierarchical.distance = DistanceMeasures.HAMMING;
            opt.hierarchical.reference1DjuryH = true;
            opt.profiles1DJuryFile = "profiles/omics.profiles";
            opt.clusterAlgorithm.Clear();
            opt.clusterAlgorithm.Add(ClusterAlgorithm.OmicsHeatMap);
            results.Show();
            results.Focus();
            results.BringToFront();
            set.Save();

            results.Run(processName, opt);
        }*/

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult res = openFileDialog1.ShowDialog();

            if (res == DialogResult.OK)
                    textBox1.Text = openFileDialog1.FileName;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            //processName = "OmicsHeatMap" + "_" + counter++;
            //((Omics)parent).processName = processName;
            //((Omics)parent).SaveOptions();
            Settings set = new Settings();
            set.Load();
            set.mode = INPUTMODE.OMICS;
            opt.dataDir.Clear();
            opt.profileFiles.Clear();
            if(dataFileName==null)
                opt.profileFiles.Add(textBox1.Text);
            else
                opt.profileFiles.Add(dataFileName);
            opt.hash.relClusters = (int)relevantC.Value;
            opt.hash.reqClusters = (int)numericUpDown4.Value;
            opt.hash.useConsensusStates = consensus.Checked;
            opt.hash.perData = 90;

            opt.hash.combine = radioButton1.Checked;
            opt.hash.fcolumns = !radioButton1.Checked;

            opt.hierarchical.uHTree = radioHTree.Checked;
            opt.hierarchical.distance = distanceControl1.distDef;
            opt.hierarchical.reference1DjuryH = true;
            opt.profiles1DJuryFile = "profiles/omics.profiles";
            opt.clusterAlgorithm.Clear();
            opt.clusterAlgorithm.Add(ClusterAlgorithm.OmicsHeatMap);
            opt.hash.refPoints = (int)numericUpDown1.Value;
            results.Show();
            results.Focus();
            results.BringToFront();
            set.Save();
            counter++;
            OmicsProfile aux = new OmicsProfile();
            aux.processName = processName + "-" + counter + ".genprof";
            aux.heatmap = false;
            aux.SaveOmicsSettings();
            results.Run(aux.processName, opt);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            previous = true;
            parent.Show();
            this.Close();
        }

        private void OmicsHeatMap_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (parent != null && !previous)
                parent.Close();
        }

        private void Hash_CheckedChanged(object sender, EventArgs e)
        {
            if (Hash.Checked)
            {
                consensus.Visible = true;
                label4.Visible = false;
                numericUpDown1.Visible = false;
            }
            else
            {
                consensus.Visible = false;
                label4.Visible = true;
                numericUpDown1.Visible = true;
            }
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            distanceControl1.Visible = !radioHTree.Checked;
            radioButton1.Visible = !radioHTree.Checked;
            if (radioHTree.Checked)
                Hash.Checked = true;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }
    }
}
