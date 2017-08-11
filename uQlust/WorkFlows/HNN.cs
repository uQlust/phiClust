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
            this.alg = alg;
            this.button2.Click -= new System.EventHandler(this.button2_Click);
            if(fileName!=null)
            {
                opt.ReadOptionFile(fileName);
                textBox2.Text = opt.hNNLabels;               
            }
            if (set.mode == INPUTMODE.USER_DEFINED)
                checkBox1.Visible = true;
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
                OmicsProfile om = new OmicsProfile();
                List<string> classLabels = om.ReadClassLabels(textBox1.Text,radioButton1.Checked,(int)numericUpDown1.Value);
                om.ReadOmicsFile(textBox1.Text);
                if (om.labelGenes[0].Count == classLabels.Count)
                    SaveFile(textBox2.Text + "_labels",om.labelGenes[0],classLabels);
                else
                    if (om.labelSamples[0].Count == classLabels.Count)
                        SaveFile(textBox2.Text + "_labels", om.labelSamples[0], classLabels);
                    else
                        throw new Exception("Incorrect numer of class labels");

                om.CombineTrainigTest(textBox1.Text + "_combine", textBox1.Text, textBox2.Text);
            }
            else
            {
                StreamReader w = new StreamReader(textBox1.Text);
                StreamWriter r=new StreamWriter(textBox1.Text+"_combine");
                StreamWriter rl = new StreamWriter(textBox2.Text + "_labels");
                string line = "";
                string remlabel = "";
                string remName = "";
                line = w.ReadLine();
                while(line!=null)
                {
                    if (!line.Contains(">"))
                    {
                        string[] aux = line.Split(' ');
                       // r.Write("pdb_FragBag sequence");
                        r.Write("pdb_FragBag profile");
                        for (int i = 0; i < aux.Length; i++)
                        {
                            if (i != numericUpDown1.Value - 1)
                                r.Write(" "+aux[i]);                            
                        }
                        remlabel = aux[(int)numericUpDown1.Value - 1];
                        rl.WriteLine(remName + " " + remlabel);
                        r.WriteLine();
                    }
                    else
                    {
                        r.WriteLine(line);
                        remName = line.Remove(0,1);
                        remName = remName.TrimEnd('\r', '\n');
                    }
                    
                    line = w.ReadLine();
                }
                w.Close();
                rl.Close();
                w = new StreamReader(textBox2.Text);
                line = w.ReadLine();
                while (line != null)
                {
                    if (!line.Contains(">"))
                        r.Write("pdb_FragBag profile ");
                    r.WriteLine(line);
                    line = w.ReadLine();
                }

                w.Close();
                r.Close();
            }

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
            opt.hNNLabels = textBox2.Text+"_labels";
            opt.profileFiles.Add(textBox1.Text+"_combine");
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
            this.Close();
        }


    }
}
