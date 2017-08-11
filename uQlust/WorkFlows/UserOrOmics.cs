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

namespace WorkFlows
{
    public partial class UserOrOmics : Form
    {
        Form parent;
        bool previous = false;
        OMICS_CHOOSE prevWindow;
        public UserOrOmics(Form parent,OMICS_CHOOSE prevWindow)
        {
            this.parent = parent;
            this.prevWindow = prevWindow;
            InitializeComponent();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            Omics g = new Omics(this, prevWindow);
            g.Show();
            this.Hide();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Settings set;
            set=new Settings();
            Form c=null;
            set.mode = INPUTMODE.USER_DEFINED;
            switch(prevWindow)
            { 
                case OMICS_CHOOSE.HNN:
                    c = new HNN(this, set, Rna_Protein_UserDef.results, OMICS_CHOOSE.HNN,"workFlows" + Path.DirectorySeparatorChar + "userDefined" + Path.DirectorySeparatorChar + "uQlust_config_file_Rpart.txt");
                    break;
                case OMICS_CHOOSE.GUIDED_HASH:
                    c = new HNN(this, set, Rna_Protein_UserDef.results, OMICS_CHOOSE.GUIDED_HASH,"workFlows" + Path.DirectorySeparatorChar + "userDefined" + Path.DirectorySeparatorChar + "uQlust_config_file_GuidedHash.txt");
                    break;
                case OMICS_CHOOSE.NONE:
                    c = new ClusteringChoose(set, this);
                    break;
                case OMICS_CHOOSE.HEATMAP:
                    c = new OmicsHeatMap(this, Rna_Protein_UserDef.results);
                    break;              
            }
            c.Show();
            this.Hide();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            previous = true;
            parent.Show();
            this.Close();
        }

        private void UserOrOmics_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (!previous)
                parent.Close();
        }
    }
}
