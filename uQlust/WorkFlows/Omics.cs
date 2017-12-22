using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using phiClustCore;
using phiClustCore.Profiles;

namespace WorkFlows
{
    public enum OMICS_CHOOSE
    {
        HNN,
        GUIDED_HASH,
        HEATMAP,
        CLUSTERING,
        NONE
    }
    public partial class Omics : Form
    {
        Form parent;
        bool previous = false;
        public string processName = null;
        OMICS_CHOOSE nextWindow;
        int numValue;
        static int counter = 0;
        List<int> sampleLabelsPos = new List<int>();
        public Omics()
        {
            parent = null;
            InitializeComponent();
        }
        public Omics(Form parent,OMICS_CHOOSE nextWindow)
        {
            this.parent = parent;
            this.nextWindow = nextWindow;
            InitializeComponent();
            foreach (var item in Enum.GetValues(typeof(CodingAlg)))            
                comboBox1.Items.Add(item);
            comboBox1.SelectedIndex = 0;
            numValue = (int)numericUpDown3.Value;
            counter++;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            previous = true;
            parent.Show();
            this.Close();
        }

        private void Genome_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!previous)
                parent.Close();    
        }
        private string GetProcessName()
        {
            return Path.GetFileNameWithoutExtension(textBox1.Text);
        }
        public void SaveOptions()
        {
            StreamWriter w = new StreamWriter(OmicsProfile.omicsSettings);
            w.WriteLine("Column " + numericUpDown2.Value);
            w.WriteLine("Rows " + numericUpDown1.Value);
            w.WriteLine("Use gene labels " + checkBox4.Checked);
            w.WriteLine("Label Genes " + geneLabelPosition.Value);
            w.WriteLine("Label Samples " + textBox2.Text);
            w.WriteLine("Label Number of rows 1");
            w.WriteLine("Use sample labels " + checkBox5.Checked);
            w.WriteLine("Label Number of rows 1");
            w.WriteLine("States " + numericUpDown3.Value);
            w.WriteLine("transposition " + checkBox1.Checked);
            w.WriteLine("Coding Algorithm " + comboBox1.SelectedItem);
            w.WriteLine("OutputName " +GetProcessName());
            w.WriteLine("Gene Position Rows " + radioButton3.Checked);
            w.WriteLine("Z-score " + checkBox2.Checked);
            w.WriteLine("Quantile " + checkBox3.Checked);
            if (textBox3.Text.Length > 0 &&!checkBox6.Checked)
                w.WriteLine("Selected genes " + textBox3.Text);
            if (checkBox6.Checked)
                w.WriteLine("Select genes "+numericUpDown4.Value);
            w.Close();

        }
        public virtual void button1_Click(object sender, EventArgs e)
        {
            if(textBox1.Text.Length==0 || textBox2.Text.Length==0)
            {
                this.DialogResult = DialogResult.Ignore;
                return;
            }


            SaveOptions();
            Settings set = new Settings();
            set.Load();
            set.mode = INPUTMODE.OMICS;
            Form c=null;
            switch(nextWindow)
            {
                case OMICS_CHOOSE.NONE:
                    c = new ClusteringChoose(set, this,textBox1.Text);                    
                    break;
                case OMICS_CHOOSE.HNN:  
                    c = new HNN(this, set, Rna_Protein_UserDef.results,nextWindow, "workFlows" + Path.DirectorySeparatorChar + "omics" + Path.DirectorySeparatorChar + "uQlust_config_file_Rpart.txt",textBox1.Text);
                    ((HNN)c).processName = GetProcessName();
                    break;
                case OMICS_CHOOSE.GUIDED_HASH:
                    c = new HNN(this, set, Rna_Protein_UserDef.results,nextWindow, "workFlows" + Path.DirectorySeparatorChar + "omics" + Path.DirectorySeparatorChar + "uQlust_config_file_GuidedHash.txt",textBox1.Text);
                    ((HNN)c).processName = GetProcessName();
                    break;
                case OMICS_CHOOSE.HEATMAP:
                    c = new OmicsHeatMap(this, Rna_Protein_UserDef.results,textBox1.Text);
                    ((OmicsHeatMap)c).processName = GetProcessName();
                    break;              
            }
            c.Show();
            this.Hide();
            counter++;
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
            {
                label_samples.Text = label_genes.Text;
                label_genes.Text = "Labels in column";                
            }
            else
            {
                label_samples.Text = label_genes.Text;
                label_genes.Text = "Labels in row";
            }
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox4.Checked)
            {
                geneLabelPosition.Enabled = true;
                label_genes.Enabled = true;
            }
            else
            {
                geneLabelPosition.Enabled = false;
                label_genes.Enabled = false;
            }
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox5.Checked)
            {
                label_samples.Enabled = true;
                textBox2.Enabled = true;
            }
            else
            {
                label_samples.Enabled = false;
                textBox2.Enabled = false;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult res = openFileDialog1.ShowDialog();

            if (res == DialogResult.OK)
            {
                textBox1.Text = openFileDialog1.FileName;
                if(Path.GetExtension(textBox1.Text).Contains("gct"))
                {
                    StreamReader r= new StreamReader(textBox1.Text);
                    string line = r.ReadLine();
                    if(line.Contains("#1.3"))
                    {
                        line = r.ReadLine();
                        string[] aux = line.Split('\t');
                        if (aux.Length == 4)
                        {
                            numericUpDown2.Value = Convert.ToInt32(aux[2]) + 2;
                            numericUpDown1.Value = Convert.ToInt32(aux[3]) + 3;
                        }
                    }
                    r.Close();
                }
            }

        }

        private void button3_Click(object sender, EventArgs e)
        {
            DialogResult res = openFileDialog1.ShowDialog();

            if (res == DialogResult.OK)
                textBox3.Text = openFileDialog1.FileName;

        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            label6.Enabled = !checkBox6.Checked;
            textBox3.Enabled = !checkBox6.Checked;
            button3.Enabled = !checkBox6.Checked;
            numericUpDown4.Enabled = checkBox6.Checked;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if ((CodingAlg)comboBox1.SelectedItem == CodingAlg.Z_SCORE)
                if (numericUpDown3.Value < 3)
                    numericUpDown3.Value = 3;
                else
                    if (((int)numericUpDown3.Value) % 2 == 0)
                        numericUpDown3.Value = numericUpDown3.Value + 1;
        }

        private void numericUpDown3_ValueChanged(object sender, EventArgs e)
        {
            if ((CodingAlg)comboBox1.SelectedItem == CodingAlg.Z_SCORE)
                if (numericUpDown3.Value < 3)
                    numericUpDown3.Value = 3;
                else                    
                    if (((int)numericUpDown3.Value) % 2 == 0)
                        if(numValue<numericUpDown3.Value)
                            numericUpDown3.Value = numericUpDown3.Value + 1;
                        else
                            numericUpDown3.Value = numericUpDown3.Value - 1;

            numValue = (int)numericUpDown3.Value;

        }
    }
}
