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

namespace WorkFlows
{
    public partial class HashSimple : RpartSimple
    {
        public HashSimple()
        {
            InitializeComponent();
        }


        public HashSimple(Form parent,Settings set,ResultWindow results,string fileName=null,string dataFileName=null): base(parent,set,results,fileName,dataFileName)
        {
            InitializeComponent();
            this.Text = "Hash";
            ShowLabels();
            checkBox1.Checked = opt.hash.useConsensusStates;
            opt.hash.combine = false;           
        }
        public override void SetProfileOptions()
        {
            
            base.SetProfileOptions();
           
        }
        public override void GetData()
        {
            base.GetData();
            opt.hash.useConsensusStates = checkBox1.Checked;
        }
        public override void button2_Click(object sender, EventArgs e)
        {
            base.button2_Click(sender, e);
            
        }
        public override string ToString()
        {
            return "Hash";
        }
    }
}
