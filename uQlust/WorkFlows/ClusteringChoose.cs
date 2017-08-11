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

namespace WorkFlows
{

    public partial class ClusteringChoose : Form
    {
        static string userDefinedPath = "workFlows" + Path.DirectorySeparatorChar + "userDefined" + Path.DirectorySeparatorChar;
        static string genomePath = "workFlows" + Path.DirectorySeparatorChar + "omics" + Path.DirectorySeparatorChar;
        static  Dictionary<string, string> profiles = new Dictionary<string, string>()
        {             
                                        {"Rpart","uQlust_config_file_Rpart.txt"},
                                        {"Hash","uQlust_config_file_Hash.txt"},
                                        {"1DJury","uQlust_config_file_1DJury.txt"},
                                        {"uQlustTree","uQlust_config_file_Tree.txt"}
        };
        public Settings set;
        ResultWindow results;// = new ResultWindow();
        bool previus = false;
        string dataFileName = "";
        Form parent;
        public ClusteringChoose(Settings set, Form parent,string dataFileName=null)
        {
            InitializeComponent();
            this.dataFileName = dataFileName;
            this.set = set;
            this.parent = parent;
            this.results = Rna_Protein_UserDef.results;
            switch (set.mode)
            {
                case INPUTMODE.PROTEIN:
                    this.Text = "Proteins clustering";
                    break;
                case INPUTMODE.RNA:
                    this.Text = "Rna clustering";
                    break;
                case INPUTMODE.USER_DEFINED:
                    this.Text = "User define profiles clustering";
                    break;
                case INPUTMODE.OMICS:
                    this.Text = "Omics clustering";
                    break;

            }

        }

        void button4_Click(object sender, EventArgs e)
        {
            previus = true;
            parent.Show();
            this.Close();
        }
        string GetProcessName(object o)
        {
            return "WorkFlow_"+set.mode.ToString()+"_"+o.ToString();
        }
        void button1_Click(object sender, EventArgs e)
        {
                RpartSimple rpart;
                if(set.mode==INPUTMODE.OMICS)
                    rpart = new RpartSimple(this, set, results, genomePath + profiles["Rpart"],dataFileName);
                else
                    rpart = new RpartSimple(this, set, results, userDefinedPath + profiles["Rpart"]);
                rpart.processName = GetProcessName(rpart);
                rpart.Show();
            this.Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            HashSimple hash;
            if(set.mode==INPUTMODE.OMICS)
                hash = new HashSimple(this, set, results, genomePath + profiles["Hash"],dataFileName);
            else
                hash = new HashSimple(this, set, results, userDefinedPath + profiles["Hash"]);
                hash.processName = GetProcessName(hash);
                hash.Show();
            this.Hide();

        }

        private void ClusteringChoose_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!previus)
            {
                parent.Close();
                results.Close();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //uQlustTreeSimple tree=new uQlustTreeSimple(this,set,results,profiles[set.mode]["uQlustTree"]);
                uQlustTreeSimple tree;
                if(set.mode==INPUTMODE.USER_DEFINED)
                    tree = new uQlustTreeSimple(this, set, results, userDefinedPath+profiles["uQlustTree"]);
                else
                    tree = new uQlustTreeSimple(this, set, results, genomePath+ profiles["uQlustTree"],dataFileName);
                tree.processName = GetProcessName(tree);
                tree.Show();
            //tree.Show();
            this.Hide();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Jury1DSimple jury = new Jury1DSimple();
                Jury1DSimple hash;
                if(set.mode==INPUTMODE.USER_DEFINED)
                    hash =new Jury1DSimple(this, set, results, userDefinedPath+profiles["1DJury"]);
                else
                    hash = new Jury1DSimple(this, set, results, genomePath + profiles["1DJury"], dataFileName);
                hash.processName = GetProcessName(hash);
                hash.Show();
            //hash.Show();
            this.Hide();

        }

    }
}
