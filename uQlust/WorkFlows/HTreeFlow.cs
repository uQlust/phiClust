﻿using System;
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
    
    public partial class HTreeFlow :RpartSimple
    {
        public HTreeFlow()
        {            
            InitializeComponent();            
        }
        public HTreeFlow(Form parent, Settings set, ResultWindow results, string fileName = null, string dataFileName = null): base(parent,set,results,fileName,dataFileName)
        {
            InitializeComponent();
            label10.Visible = false;
            refPoints.Visible = false;
            this.Text = "HTree";
            ShowLabels();
            // checkBox1.Checked = opt.hash.useConsensusStates;
            opt.hash.useConsensusStates = true;
            opt.hash.combine = false;
            opt.clusterAlgorithm.Clear();
            opt.clusterAlgorithm.Add(ClusterAlgorithm.HTree);
        }
    }
}
