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
                textBox1.Text = opt.hnn.trainingFile;
                
                if(opt.hnn.labelPosition==-1)
                {
                    radioButton1.Checked = false;
                    radioButton2.Checked = false;
                    radioButton3.Checked = true;
                }
                if(opt.hnn.labelPosition>0)
                    this.numericUpDown1.Value = opt.hnn.labelPosition;
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

        void CombineTrainTest()
        {
            if(opt.hash.profileName.Contains("omics"))
            {
                OmicsProfile om = new OmicsProfile(opt);
                //om.Load(OmicsInput.fileName);
                if (radioButton2.Checked)
                    om.oInput.transpose = true;
                om.Save(OmicsInput.fileName);
                List<string> classLabels = om.ReadClassLabels(this.dataFileName,radioButton1.Checked,(int)numericUpDown1.Value);
                om.ReadOmicsFile(this.dataFileName);
                Console.WriteLine("labL=" + om.labelGenes[0][om.labelGenes[0].Count - 1]+" "+classLabels[classLabels.Count-1]);

                StreamWriter fileF = new StreamWriter("log");
                for(int i=0;i< om.labelGenes[0].Count;i++)
                {
                    fileF.WriteLine(om.labelGenes[0][i]);
                }
                fileF.WriteLine("Next");
                for (int i = 0; i < classLabels.Count; i++)
                {
                    fileF.WriteLine(classLabels[i]);
                }
                fileF.Close();
                if (om.labelGenes[0].Count == classLabels.Count)
                    SaveFile(textBox2.Text + "_labels",om.labelGenes[0],classLabels);
                else
                    if (om.labelSamples[0].Count == classLabels.Count)
                        SaveFile(textBox2.Text + "_labels", om.labelSamples[0], classLabels);
                    else
                        throw new Exception("Incorrect numer of class labels");

                om.CombineTrainigTest(ChangeFileName(this.dataFileName,"combine"), this.dataFileName, textBox2.Text);
            }

        }
        string ChangeFileName(string fileName,string prefix)
        {
            string path = Path.GetDirectoryName(fileName);
            string fName = Path.GetFileName(fileName);
            fName = path + Path.DirectorySeparatorChar+"combine_" + fName;

            return fName;
        }
        private void buttonHNN_Click(object sender, EventArgs e)
        {
            opt.dataDir.Clear();
            opt.clusterAlgorithm.Clear();

            CombineTrainTest();
                
            if(alg==OMICS_CHOOSE.HNN)
                opt.clusterAlgorithm.Add(ClusterAlgorithm.HNN);
            else
                opt.clusterAlgorithm.Add(ClusterAlgorithm.GuidedHashCluster);
            opt.profileFiles.Clear();
            opt.hnn.testFile = textBox2.Text;
            if(!radioButton3.Checked)
                opt.hnn.labelPosition = (int)numericUpDown1.Value;
            else
                opt.hnn.labelPosition = -1;
            opt.hnn.trainingFile = textBox1.Text;
            if (opt.hash.profileName.Contains("omics"))
            {        
                opt.profileFiles.Add(ChangeFileName(this.dataFileName,"combine"));
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


    }
}
