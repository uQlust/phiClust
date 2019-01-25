using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using phiClustCore;

namespace phiClustCore.Distance
{
    public abstract class HammingBase:DistanceMeasure
    {
        string currentProfile;
        public Dictionary<string, List<byte>> stateAlign;
        protected Dictionary<byte, Dictionary<byte, double>> weights;
        Settings dirSettings = new Settings();
        public Alignment al;       

        protected Dictionary<string, int> states = new Dictionary<string, int>();
        protected List<Dictionary<string, int>> lStates = new List<Dictionary<string, int>>();

        Dictionary<byte, List<int>>[] columns = null;

        protected bool flag;
        protected string profileName;
        protected string refJuryProfile;
        protected List<string> fileNames=null;
        string profilesFile="";
        public  HammingBase(List<string> fileNames, string alignFile, bool flag,string profileName,Options opt,string refJuryProfile=null)
        {
            this.opt = opt;
            this.fileNames = fileNames;
            this.alignFile = alignFile;
            this.flag = flag;
            this.profileName = profileName;
            this.refJuryProfile = refJuryProfile;
        }
        public HammingBase(string profilesFile, bool flag, string profileName, Options opt,string refJuryProfile = null)
        {
            this.opt = opt;
            this.profilesFile = profilesFile;
            this.flag = flag;
            this.profileName = profileName;
            this.refJuryProfile = refJuryProfile;
        }
        public HammingBase(string dirName,string alignFile,bool flag,string profileName,Options opt,string refJuryProfile=null)
        {
            this.opt = opt;
            this.dirName = dirName;
            this.alignFile = alignFile;
            this.flag = flag;
            this.profileName = profileName;
            this.refJuryProfile = refJuryProfile;
        }
        public HammingBase(Alignment al, bool flag)
        {
            this.al = al;
            this.opt = al.opt;
            this.flag = flag;
        }
        public override void InitMeasure()
        {            
            if (fileNames != null)
                InitMeasure(fileNames, alignFile, flag, profileName, refJuryProfile);
            else
                if (profilesFile.Length > 0)
                InitMeasure(profilesFile, flag, profileName, refJuryProfile);
            else
                    if (dirName != null && dirName.Length > 0)
                InitMeasure(dirName, alignFile, flag, profileName, refJuryProfile);
            else
                InitMeasure(al, flag);

        }        

        public void InitMeasure(string dirName, string alignFile, bool flag,string profileName,string refJuryProfile=null)            
        {
            al = new Alignment(opt);
            if (profileName != null)
                currentProfile = profileName;

            if (alignFile == null)
                al.Prepare(dirName, dirSettings, currentProfile);
            else
                al.Prepare(alignFile, currentProfile);
            
            base.InitMeasure(dirName, alignFile, flag,refJuryProfile);
            
            dirSettings.Load();
            this.alignFile = alignFile;
         
            InitHamming();
        }
        public override void InitMeasure(Alignment al, bool flag)            
        {
            this.al = al;
            this.opt = al.opt;
            base.InitMeasure(al, flag);            
            stateAlign = al.GetStateAlign();
           
            //al.MyAlign(alignFile);
            stateAlign = al.GetStateAlign();
            //AddErrors(al.errors);
            jury = new jury1D(al.opt);
            jury.PrepareJury(al);
            structNames = new Dictionary<string, int>();
            foreach (string item in stateAlign.Keys)
            {
                string[] strTab = item.Split(Path.DirectorySeparatorChar);
                structNames.Add(strTab[strTab.Length - 1], 1);
            }
            
            order = true;
            weights = al.r.GenerateWeights(wOpertion.SUM);
        }
        public void InitMeasure(List<string> fileNames, string alignFile, bool flag,string profileName,string refJuryProfile=null)
            
        {
            base.InitMeasure(fileNames, alignFile, flag, refJuryProfile);
            if (profileName != null)
                currentProfile = profileName;

            dirSettings.Load();
            al = new Alignment(opt) ;
            if (alignFile == null)
                al.Prepare(fileNames, dirSettings, currentProfile);
            else
                al.Prepare(alignFile, currentProfile);
            InitHamming();
        }
        public override double ProgressUpdate()
        {
            if (al != null)
            {
                double v = al.ProgressUpdate();
                return v;
            }

            

            return base.ProgressUpdate();
        }
        public Alignment GetAlignment()
        {
            return al;
        }
        public override void InitMeasure(string profilesFile, bool flag, string profileName, string refJuryProfile = null)            
        {
            base.InitMeasure(profilesFile, flag, profileName, refJuryProfile);
            if (profileName != null)
                currentProfile = profileName;
            al = new Alignment(opt);
            al.Prepare(profilesFile, currentProfile);
            al.MyAlign(alignFile);
            structNames = new Dictionary<string, int>();
            foreach (var itemK in al.r.profiles.Keys)
            {
                foreach (string item in al.r.profiles[itemK].Keys)
                {
                    string[] strTab = item.Split(Path.DirectorySeparatorChar);
                    structNames.Add(strTab[strTab.Length - 1], 1);
                }
                break;
            }

            order = true;
            weights = al.r.GenerateWeights(wOpertion.SUM);
            stateAlign = new Dictionary<string, List<byte>>();
            foreach (var itemK in al.r.profiles.Keys)
            {
                foreach (string item in al.r.profiles[itemK].Keys)
                {
                    stateAlign.Add(item, al.r.profiles[itemK][item].profile);
                }
                break;
            }
        }
        private void InitHamming()
        {
            al.MyAlign(alignFile);
            stateAlign = al.GetStateAlign();

            structNames = new Dictionary<string,int>();
            foreach (string item in stateAlign.Keys)
            {
                string[] strTab = item.Split(Path.DirectorySeparatorChar);
                structNames.Add(strTab[strTab.Length - 1],1);
            }

            order = true;
            weights = al.r.GenerateWeights(wOpertion.SUM);


        }
        
        public override int[][] GetDistance(List<string> refStructure, List<string> structures)
        {
            return base.GetDistance(refStructure, structures);
        }
      

        protected Dictionary<byte, List<int>>[] MakeColumnsLists(List<string> structNames)
        {
            byte locState = 0;

            if (structNames.Count == 0)
                return null;

            columns = new Dictionary<byte, List<int>>[stateAlign[structNames[0]].Count];
           
            for (int i = 0; i < columns.Length; i++)
            {

                columns[i] = new Dictionary<byte, List<int>>(weights.Keys.Count);
                try
                {
                    for (int j = 0; j < structNames.Count; j++)
                    {
                        if (stateAlign.ContainsKey(structNames[j]) && stateAlign[structNames[j]].Count > 0)
                            locState = stateAlign[structNames[j]][i];
                        else
                            continue;
                        if (locState == 0)
                            continue;


                        if (!columns[i].ContainsKey(locState))
                        {
                            List<int> lista = new List<int>();
                            lista.Add(j);
                            columns[i].Add(locState, lista);
                        }
                        else
                            columns[i][locState].Add(j);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ups HammingBase :" + ex.Message);
                }
            }

            return columns;
        }

    }
}
