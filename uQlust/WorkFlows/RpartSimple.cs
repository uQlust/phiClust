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
    public partial class RpartSimple : Form, IclusterType
    {
        protected Options opt=new Options();
        protected ResultWindow results;
        public string processName;
        public static int counter = 0;
        public bool previous = false;
        CommonDialog dialog;
        protected Settings set;
        public Form parent;
        ProfileTree tree = new ProfileTree();
        public string dataFileName;

        public RpartSimple()
        {
            InitializeComponent();
        }
        public INPUTMODE GetInputType()
        {
            return set.mode;
        }
        public override string ToString()
        {
            return "Rpart";
        }
        public void HideRmsdLike()
        {

        }
        public RpartSimple(Form parent, Settings set, ResultWindow results, string fileName = null,string dataFileName=null)
        {
            InitializeComponent();
            this.parent = parent;
            dialog = openFileDialog1;
            this.Location = parent.Location;
            this.set = set;
            this.dataFileName = dataFileName;
            if (fileName != null)
            {
                opt.ReadOptionFile(fileName);                
                SetProfileOptions();
            }
            if (dataFileName != null && dataFileName.Length > 0)
            {
                label1.Visible = false;
                textBox1.Visible = false;
                button1.Visible = false;                
            }

            if (set.mode == INPUTMODE.USER_DEFINED)
            {
                checkBox1.Checked = true;

            }
            this.results = results;
            if (opt.hash.profileName!=null)
            {
                tree.LoadProfiles(opt.hash.profileName);
                label9.Text = tree.GetStringActiveProfiles();
            }
        }
        public void ShowLabels()
        {
            label4.Visible = true;
            label5.Visible = true;
        }
        public virtual void SetProfileOptions()
        {
            if (set.mode == INPUTMODE.USER_DEFINED || set.mode==INPUTMODE.OMICS)
            {
                if (set.mode == INPUTMODE.USER_DEFINED)
                    label1.Text = "Choose file with profile";
                else
                    label1.Text = "Choose file with omics data";

                if (opt.profileFiles.Count > 0)
                    textBox1.Text = opt.profileFiles[0];
            }
            else
                if (opt.dataDir.Count > 0)
                    textBox1.Text = opt.dataDir[0];

            label3.Text = opt.hash.profileName;
            relevantC.Value = opt.hash.relClusters;
            percentData.Value = opt.hash.perData;
            refPoints.Value = opt.hash.refPoints;
            opt.clusterAlgorithm.Clear();
            opt.clusterAlgorithm.Add(ClusterAlgorithm.HashCluster);        
        }
        public void SetProfileName(string name)
        {
            opt.ReadOptionFile(name);
            SetProfileOptions();
            if (opt.hash.profileName!=null)
            {
                tree.LoadProfiles(opt.hash.profileName);
                label9.Text = tree.GetStringActiveProfiles();
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult res=dialog.ShowDialog();

            if(res==DialogResult.OK)
            {
                if(set.mode==INPUTMODE.USER_DEFINED || set.mode==INPUTMODE.OMICS)
                    textBox1.Text = ((OpenFileDialog)(dialog)).FileName;
                else
                    textBox1.Text = ((FolderBrowserDialog)(dialog)).SelectedPath;
            }
        }
        public virtual void GetData()
        {
            opt.dataDir.Clear();
            opt.profileFiles.Clear();
            if (dataFileName != null && dataFileName.Length > 0)
                opt.profileFiles.Add(dataFileName);
            else
                opt.profileFiles.Add(textBox1.Text);
            opt.hash.relClusters = (int)relevantC.Value;
            opt.hash.perData = (int)percentData.Value;
            opt.hash.refPoints = (int)refPoints.Value;
            if (checkBox1.Checked)
                opt.hash.GenerateAutomaticProfiles(textBox1.Text);
            set.Save();            
        }
        public virtual void button2_Click(object sender, EventArgs e)
        {
            GetData();            
            results.Show();
            results.Focus();
            results.BringToFront();
            results.Run(processName + "_" + counter++, opt);

        }

        void button4_Click(object sender, EventArgs e)
        {
            previous = true;
            parent.Show();
            this.Close();
        }        
        private void RpartSimple_FormClosed(object sender, FormClosedEventArgs e)
        {
                if(!previous)
                    parent.Close();           
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked)
            {
                label3.Text = ProfileAutomatic.similarityProfileName;
                label9.Visible = false;

            }
            else
            {
                label3.Text = opt.hash.profileName;                
                label9.Visible=true;
            }
        }

      
    }
}
