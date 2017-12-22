﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Data;
using System.Drawing;
//using System.Data;
using phiClustCore;
using phiClustCore.Interface;
using phiClustCore.Distance;
using phiClustCore.Profiles;

namespace phiClustCore
{
    public delegate void UpdateJob(string item,bool errorFlag=false,bool finishAll=true);
    public delegate void StartJob(string name,string al,string dirName,string measure);
    public delegate void ErrorMessage(string item);
    public delegate void UpdateMessage(string m);
    public delegate void ErrorJob();

    class ThreadParam
    {
        public string name;
        public int num;
        public int start;
        public int stop;
        //public string dirName;
        //public DistanceMeasure distance;
    }
    public interface MessageUpdate
    {
        void UpdateMessage(string message);
        void ActivateUpdateting();
        void CloseUpdateting();
    }
    class OmicsData
    {
        public List<Color> colorMap;
        public List<int> index;
        public List<string> labels;
    };
    public class JobManager
    {
        Dictionary<string, Thread> runnigThreads = new Dictionary<string, Thread>();
        public Dictionary<string, ClusterOutput> clOutput = new Dictionary<string, ClusterOutput>();
        Dictionary<string, IProgressBar> progressDic = new Dictionary<string, IProgressBar>();
        public Options opt = new Options();
        string clType = "";
        public MessageUpdate mUpdate;        
        string currentProcessName="";
        Thread startProg;
       

        public UpdateJob updateJob;
        public StartJob beginJob;
        public ErrorMessage message;
        public UpdateMessage upMessage;
        public ErrorJob errorJob;
        private Object thisLock = new Object();


        public Dictionary<string,double> ProgressUpdate()
        {
            Dictionary<string, double> res = new Dictionary<string, double>();          

            if (progressDic.Count==0)
                return null;
            lock (thisLock)
            {
                foreach (var item in progressDic)
                {
                    res.Add(item.Key, item.Value.ProgressUpdate());
                }
                foreach (var item in res)
                    if (item.Value == 1)
                        progressDic.Remove(item.Key);

                return res;
            }            
            
        }
        public Exception GetException()
        {
            return null;
        }
        public List<KeyValuePair<string, DataTable>> GetResults()
        {
            return null;
        }


        private void RunHashCluster(string name, string dirName, string alignmentFile=null)
        {
            DateTime cpuPart1 = DateTime.Now;
            HashCluster hk = null;

                if(alignmentFile!=null)
                    hk = new HashCluster("", alignmentFile, opt.hash);
                else
                    hk = new HashCluster(dirName, null, opt.hash);

            
            progressDic.Add(name, hk);
            if (beginJob != null)
                beginJob(currentProcessName, hk.ToString(), dirName, "HAMMING");
            hk.InitHashCluster();


            DateTime cpuPart2 = DateTime.Now;



            ClusterOutput output;
            output = hk.RunHashCluster();
            UpdateOutput(name, dirName,alignmentFile, output, "HAMMING", cpuPart1, cpuPart2, hk);

        }
        private void RunGuidedHashCluster(string name, string dirName, string alignmentFile = null)
        {
            DateTime cpuPart1 = DateTime.Now;
            GuidedHashCluster hk = null;

                if (alignmentFile != null)
                    hk = new GuidedHashCluster("", alignmentFile, opt.hash);
                else
                    hk = new GuidedHashCluster(dirName, null, opt.hash);


            progressDic.Add(name, hk);
            if (beginJob != null)
                beginJob(currentProcessName, hk.ToString(), dirName, "HAMMING");
            hk.InitHashCluster();

            DateTime cpuPart2 = DateTime.Now;

            ClusterOutput output;
            hk.ReadClassLabels(opt.hNNLabels);
            output = hk.RunHashCluster();
            UpdateOutput(name, dirName, alignmentFile, output, "HAMMING", cpuPart1, cpuPart2, hk);

        }

        private void RunHashDendrogCombine(string name, string dirName, string alignmentFile = null)
        {
            DateTime cpuPart1 = DateTime.Now;
            HashClusterDendrog hk = null;

                if (alignmentFile != null)
                    hk = new HashClusterDendrog(null, alignmentFile, opt.hash, opt.hierarchical);
                else
                    hk = new HashClusterDendrog(dirName, null, opt.hash, opt.hierarchical);
            

            ClusterOutput output;
            if (beginJob != null)
                beginJob(currentProcessName, hk.ToString(), dirName,"NONE");
            progressDic.Add(name, hk);
            hk.InitHashCluster();
            
            DateTime cpuPart2 = DateTime.Now;
            output = hk.RunHashDendrogCombine();
            if (opt.hash.profileName.Contains("omics"))
            {
                OmicsProfile om = new OmicsProfile();
                om.LoadOmicsSettings();
                string intvName = "generatedProfiles/OmicsIntervals_" + om.processName + ".dat";
                if (File.Exists(intvName))
                {
                    OmicsData res = ReadOmicsData(intvName);
                    output.profilesColor = res.colorMap;
                    output.aux2 = res.labels;
                    
                }
            }
            UpdateOutput(name, dirName,alignmentFile, output, hk.UsedMeasure(), cpuPart1, cpuPart2, hk);

        }
        OmicsData ReadOmicsData(string fileName)
        {
            OmicsData res=new OmicsData();
            List<Color> colorMap;
            List<int> index = new List<int>();
            List<string> labels=new List<string>();
            StreamReader wr = new StreamReader(fileName);
            Dictionary<int, double[]> codingInterv = null;
            codingInterv = new Dictionary<int, double[]>();
            string line = wr.ReadLine();
            while (line != null)
            {
                string[] aux = line.Split(' ');
                if (aux[0].Contains("Label"))
                    labels.Add(aux[1]);
                if (aux.Length == 4 && aux[0].Contains("Code"))
                {
                    double[] tab = new double[2];
                    tab[0] = Convert.ToDouble(aux[2]);
                    tab[1] = Convert.ToDouble(aux[3]);
                    codingInterv.Add(Convert.ToInt32(aux[1]), tab);
                }
                line = wr.ReadLine();
            }
            wr.Close();

            colorMap = new List<Color>(codingInterv.Count);
            for (int i = 0; i < codingInterv.Count; i++)
            {
                colorMap.Add(Color.Black);
                index.Add(0);
            }
            List<int> hot = new List<int>();
            List<int> cool = new List<int>();
            foreach (var item in codingInterv.Keys)
            {
                if (codingInterv[item][0] < 0 && codingInterv[item][1] < 0)
                    cool.Add(item);
                else
                    if (codingInterv[item][0] > 0 && codingInterv[item][1] > 0)
                        hot.Add(item);
            }

            hot.Sort((x, y) => x.CompareTo(y));
            cool.Sort((x, y) => y.CompareTo(x));
            if (hot.Count > 1)
            {

                Color stepper = Color.FromArgb(
                                       (byte)((255 - 0) / (hot.Count)),
                                       (byte)((255 - 0) / (hot.Count)),
                                       (byte)((0 - 0) / (hot.Count)));
                for (int i = 0; i < hot.Count; i++)
                {
                    colorMap[hot[i]] = Color.FromArgb(
                                                0 + (stepper.R * (i + 1)),
                                                0 + (stepper.G * (i + 1)),
                                                0 + (stepper.B * (i + 1)));
                    index[hot[i]] = i + 1;
                }
            }
            else
                if (hot.Count == 1)
                {
                    colorMap[hot[0]] = Color.FromArgb(255, 255, 0);
                    index[hot[0]] = 1;
                }

            if (cool.Count > 1)
            {
                Color stepper = Color.FromArgb(
                                       (byte)((20 - 0) / (cool.Count)),
                                       (byte)((0 - 0) / (cool.Count)),
                                       (byte)((255 - 0) / (cool.Count)));
                for (int i = 0; i < cool.Count; i++)
                {
                    colorMap[cool[i]] = Color.FromArgb(
                                                0 + (stepper.R * (i + 1)),
                                                0 + (stepper.G * (i + 1)),
                                                0 + (stepper.B * (i + 1)));
                    index[cool[i]] = -(i+1);
                }
            }
            else
                if (cool.Count == 1)
                {
                    colorMap[cool[0]] = Color.FromArgb(20, 0, 255);
                    index[cool[0]] = -1;
                }



            res.colorMap = colorMap;
            res.labels = labels;
            res.index = index;
            return res;
        }

        Dictionary<string,string> ReadLabels(string fileName)
        {
            StreamReader lFile = new StreamReader(fileName);
            Dictionary<string, string> classLabels;
            classLabels = new Dictionary<string, string>();
            string line = lFile.ReadLine();
            while (line != null)
            {
                string[] aux = line.Split(' ');
                if (aux.Length == 2)
                {
                    classLabels.Add(aux[0], aux[1]);
                }
                line = lFile.ReadLine();
            }

            lFile.Close();
            return classLabels;
        }
        void RunHTree(string name, string dirName, string alignmentFile = null)
        {
            DateTime cpuPart1 = DateTime.Now;
            HashCluster hCluster;

            

            if (alignmentFile != null)
                hCluster = new HashCluster(null, alignmentFile,opt.hash);
            else
                hCluster = new HashCluster(dirName, null, opt.hash);

            HTree h = new HTree(dirName, alignmentFile, hCluster);
            beginJob(currentProcessName, h.ToString(), dirName, "HAMMING");
            progressDic.Add(name, h);
            hCluster.InitHashCluster();
   
            DateTime cpuPart2 = DateTime.Now;            
          

            ClusterOutput output = new ClusterOutput();
            output = h.RunHTree();
            UpdateOutput(name, dirName, alignmentFile, output, "NONE", cpuPart1, cpuPart2, h);


        }
        private void RunHNN(string name, string dirName, string alignmentFile = null)
        {
            DateTime cpuPart1 = DateTime.Now;
            HashCluster hCluster = null;
            OmicsProfile remOm = new OmicsProfile();
            remOm.LoadOmicsSettings();
            OmicsProfile profOm = new OmicsProfile();
            profOm.LoadOmicsSettings();
            profOm.numRow = 3;
            profOm.numCol = 2;
            profOm.labelGeneStart = new List<int>();
            profOm.labelGeneStart.Add(1);
            profOm.labelSampleStart = new List<int>();
            profOm.labelSampleStart.Add(2);
            profOm.SaveOmicsSettings();
            if (alignmentFile != null)
                hCluster = new HashCluster(null, alignmentFile, opt.hash);
            else
                hCluster = new HashCluster(dirName, null, opt.hash);

            profOm.LoadOmicsSettings();
            string hClusterName = name + "upper";
            if (beginJob != null)
                beginJob(currentProcessName, hCluster.ToString(), dirName, "HAMMING");
            progressDic.Add(name, hCluster);



            hCluster.InitHashCluster();

            DateTime cpuPart2 = DateTime.Now;
            Dictionary<string, string> classLabels = ReadLabels(opt.hNNLabels);
            ClusterOutput aux = hCluster.RunHashCluster(new List<string>(classLabels.Keys));

            HNN knn = new HNN(hCluster,classLabels);
            List<string> testList = new List<string>();

            foreach(var item in hCluster.stateAlign.Keys)
            {
                if (!classLabels.ContainsKey(item))
                    testList.Add(item);
            }


            Dictionary<string, string> resT = knn.HNNTest(testList);
            remOm.SaveOmicsSettings();
            //double res = knn.HNNValidate(knn.validateList);
            ClusterOutput output = new ClusterOutput();
            output.hNNRes = resT;
            UpdateOutput(name, dirName, alignmentFile, output, "NONE", cpuPart1, cpuPart2, knn);

        }
        private void RunOmicsHeatmap(string name, string dirName, string alignmentFile = null)
        {
            DateTime cpuPart1 = DateTime.Now;
            HashClusterDendrog upper = null;
            HashClusterDendrog left = null;
            OmicsProfile profOm = new OmicsProfile();
            HashCluster hashUpper = null;
            HashCluster hashLeft = null;
            ClusterOutput aux,output;
            //  opt.hierarchical.distance = DistanceMeasures.PEARSON;
            profOm.LoadOmicsSettings();
            int numLeftClusters, numUpperClusters;

            numLeftClusters = opt.hash.reqClusters;
            numUpperClusters = opt.hash.relClusters;
            if (alignmentFile != null)
            {
                hashUpper = new HashCluster("", alignmentFile, opt.hash);               
                hashLeft = new HashCluster("", alignmentFile, opt.hash);
            }
            else
            {
                hashUpper = new HashCluster(dirName, null, opt.hash);
                hashLeft = new HashCluster(dirName, null, opt.hash);
            }
            if (beginJob != null)
                beginJob(currentProcessName, "HeatMap", dirName, "HAMMING");

            hashUpper.StartProgress = 0;
            hashUpper.EndProgress = 0.25;
            progressDic.Add( name, hashUpper);
            hashUpper.InitHashCluster();
           
            ClusterOutput outputUpper;
            DateTime cpuPart2 = DateTime.Now;
            opt.hash.relClusters = numUpperClusters;
            outputUpper = hashUpper.RunHashCluster();
            progressDic.Remove(name);
            hashLeft.StartProgress = 0.25;
            hashLeft.EndProgress = 0.5;
            progressDic.Add(name, hashLeft);

            ClusterOutput outputLeft;
            profOm.heatmap = true;
            profOm.SaveOmicsSettings();
            opt.hash.relClusters = numLeftClusters;
           
            
            hashLeft.InitHashCluster();
            outputLeft = hashLeft.RunHashCluster();

            OmicsProfile om = new OmicsProfile();
            Settings set = new Settings();
            set.Load();


            string fileName=set.profilesDir + Path.DirectorySeparatorChar + om.processName;

            List<string> listUpper=OmicsProfile.GetOrderedProfiles(fileName);
            fileName += "_transpose";
            List<string> listLeft = OmicsProfile.GetOrderedProfiles(fileName);

            List<string> refUpper= HashClusterDendrog.ClustersReferences(outputUpper.clusters, hashUpper.al);
            List<string> refLeft = HashClusterDendrog.ClustersReferences(outputLeft.clusters, hashLeft.al);


            hashUpper.CuttAlignment(listLeft, refLeft);
            hashLeft.CuttAlignment(listUpper, refUpper);


            Dictionary<string, KeyValuePair<List<string>, Dictionary<byte, int>[]>> refStructUpper = HashClusterDendrog.StructuresToDenrogram(outputUpper.clusters, hashUpper.al);
            Dictionary<string, KeyValuePair<List<string>, Dictionary<byte, int>[]>> refStructLeft = HashClusterDendrog.StructuresToDenrogram(outputLeft.clusters, hashLeft.al);

            output = new ClusterOutput();

        


            //   hashUpper.CuttAlignment(hashLeft.structNames, refStructLeft);
            //  hashLeft.CuttAlignment(hashUpper.structNames, refStructUpper);
            //output.aux1 = hashLeft.stateAlignKeys;


         //   refStructUpper=HashCluster.CuttFreq(listLeft, new List<string>(refStructLeft.Keys), refStructUpper);
         //   refStructLeft=HashCluster.CuttFreq(listUpper, new List<string>(refStructUpper.Keys), refStructLeft);

            left = new HashClusterDendrog(dirName, opt.hash, opt.hierarchical,hashLeft.al);
           upper = new HashClusterDendrog(dirName, opt.hash, opt.hierarchical, hashUpper.al);


           output.aux1 = listLeft;
           output.aux2 = listUpper;
           output.nodes = new List<HClusterNode>();
           progressDic.Remove(name);
           upper.StartProgress = 0.5;
           upper.EndProgress = 0.75;
           progressDic.Add(name, upper);
           
            if(opt.hierarchical.uHTree)
            {
                HTree ct = new HTree(hashUpper.al, hashUpper);
                aux = ct.RunHTree();
            }
            else
                aux=upper.DendrogUsingMicroClusters(refStructUpper);

           List<HClusterNode> upperLeafs = aux.hNode.GetLeaves();

            Dictionary<string, int> toCheckLeft = new Dictionary<string, int>();
            foreach(var item in hashUpper.selectedColumnsHash)
            {
                toCheckLeft.Add(listLeft[item], 0);
            }




           output.nodes.Add(aux.hNode);
           if (opt.hash.profileName.Contains("omics"))
           {
               //OmicsProfile om = new OmicsProfile();
               om.LoadOmicsSettings();
               string intvName = "generatedProfiles/OmicsIntervals_" + om.processName + ".dat";
               if (File.Exists(intvName))
               {
                   OmicsData res = ReadOmicsData(intvName);
                   output.profilesColor = res.colorMap;
                   output.auxInt = res.index;
                   output.aux2 = res.labels;
               }
           }
           progressDic.Remove(name);
           left.StartProgress = 0.75;
           left.EndProgress = 1.0;
           progressDic.Add(name, left);

            if (opt.hierarchical.uHTree)
            {
                HTree ct = new HTree(hashLeft.al, hashLeft);
                aux = ct.RunHTree();
            }
            else
                aux = left.DendrogUsingMicroClusters(refStructLeft);

            Dictionary<string, int> toCheckUpper = new Dictionary<string, int>();
            foreach (var item in hashLeft.selectedColumnsHash)
            {
                toCheckUpper.Add(listUpper[item], 0);
            }


            
            foreach (var item in upperLeafs)
            {
                foreach (var it in item.setStruct)
                    if (toCheckUpper.ContainsKey(it))
                    {
                        item.flagSign = true;
                        break;
                    }
            }

            List<HClusterNode> leftLeafs = aux.hNode.GetLeaves();
            foreach (var item in leftLeafs)
            {
                foreach (var it in item.setStruct)
                    if (toCheckLeft.ContainsKey(it))
                    {
                        item.flagSign = true;
                        break;
                    }
            }



            output.nodes.Add(aux.hNode);
           /*List<string> str = new List<string>();
           foreach (var item in hashLeft.)
               if (refStructLeft.ContainsKey(item))
                   str.Add(item);
           output.aux1 = str;*/
         
           UpdateOutput(name, dirName, alignmentFile, output, upper.UsedMeasure(), cpuPart1, cpuPart2, left);

        }
        private void RunOmicsHeatmap2(string name, string dirName, string alignmentFile = null)
        {
            DateTime cpuPart1 = DateTime.Now;
            HashClusterDendrog upper = null;
            HashClusterDendrog left = null;
            OmicsProfile profOm = new OmicsProfile();
            //  opt.hierarchical.distance = DistanceMeasures.PEARSON;
            profOm.LoadOmicsSettings();

            if (alignmentFile != null)
            {
                upper = new HashClusterDendrog(null, alignmentFile, opt.hash, opt.hierarchical);
            }
            else
            {
                upper = new HashClusterDendrog(dirName, null, opt.hash, opt.hierarchical);
            }

            //profOm.LoadOmicsSettings();

            string nameUpper = name + "upper";
            if (beginJob != null)
                beginJob(currentProcessName, upper.ToString(), dirName, "HAMMING");

            upper.EndProgress = 0.5;
            progressDic.Add(name, upper);
            upper.InitHashCluster();
            DateTime cpuPart2 = DateTime.Now;

            ClusterOutput output, aux;
            output = new ClusterOutput();
            aux = upper.RunHashDendrogCombine();

            if (opt.hash.profileName.Contains("omics"))
            {
                OmicsProfile om = new OmicsProfile();
                om.LoadOmicsSettings();
                string intvName = "generatedProfiles/OmicsIntervals_" + om.processName + ".dat";
                if (File.Exists(intvName))
                {
                    OmicsData res = ReadOmicsData(intvName);
                    output.profilesColor = res.colorMap;
                    output.aux2 = res.labels;
                }
            }
            output.nodes = new List<HClusterNode>();
            output.nodes.Add(aux.hNode);
            //  UpdateOutput(nameUpper, dirName, alignmentFile, output, "NONE", cpuPart1, cpuPart2, upper);
            opt.hash.relClusters = opt.hash.reqClusters;
            if (alignmentFile != null)
                left = new HashClusterDendrog(null, alignmentFile, opt.hash, opt.hierarchical);
            else
                left = new HashClusterDendrog(dirName, null, opt.hash, opt.hierarchical);

            //profOm.transpose = true;
            profOm.heatmap = true;
            profOm.SaveOmicsSettings();
            string nameLeft = name + "left";
            //progressDic.Add(nameLeft, left);
            left.StartProgress = 0.5;
            left.EndProgress = 1.0;
            progressDic[name] = left;
            left.InitHashCluster();

            aux = left.RunHashDendrogCombine();
            output.nodes.Add(aux.hNode);
            output.aux1 = left.stateAlignKeys;
            UpdateOutput(name, dirName, alignmentFile, output, upper.UsedMeasure(), cpuPart1, cpuPart2, left);

        }


        private void RunHierarchicalCluster(string name, string dirName,string alignFile=null)
        {
            DateTime cpuPart1 = DateTime.Now;
            DistanceMeasure distance = null;
            //distance.CalcDistMatrix(distance.structNames);
           // opt.hierarchical.atoms = PDB.PDBMODE.ALL_ATOMS;
                distance = CreateMeasure(name,dirName,opt.hierarchical.distance, opt.hierarchical.reference1DjuryAglom,
                alignFile, opt.hierarchical.hammingProfile, opt.hierarchical.jury1DProfileAglom);

            DebugClass.WriteMessage("Measure Created");          
            hierarchicalCluster hk = new hierarchicalCluster(distance, opt.hierarchical,dirName);
            if (beginJob != null)
                beginJob(currentProcessName,hk.ToString(), dirName, distance.ToString());
            clType = hk.ToString();
            ClusterOutput output;
            progressDic.Add(name, hk);
            distance.InitMeasure();
            DateTime cpuPart2 = DateTime.Now;
            output = hk.HierarchicalClustering(new List<string>(distance.structNames.Keys));
            UpdateOutput(name, dirName, alignFile,output, distance.ToString(), cpuPart1, cpuPart2, hk);

        }
        private void RunHKMeans(string name, string dirName, string alignFile=null)
        {
            DateTime cpuPart1 = DateTime.Now;
            ClusterOutput clustOut = null;
            DistanceMeasure distance = null;
                distance = CreateMeasure(name,dirName,opt.hierarchical.distance, opt.hierarchical.reference1DjuryKmeans,
                                        alignFile, opt.hierarchical.hammingProfile, opt.hierarchical.jury1DProfileKmeans);
         
            kMeans km;

            km = new kMeans(distance,true);
            if (beginJob != null)
                beginJob(currentProcessName, km.ToString(), dirName, distance.ToString());

            progressDic.Add(name, km);
            DateTime cpuPart2 = DateTime.Now;
            distance.InitMeasure();

            

            clType = km.ToString();
            km.BMIndex = opt.hierarchical.indexDB;
            km.threshold = opt.hierarchical.numberOfStruct;
            km.maxRepeat = opt.hierarchical.repeatTime;
            km.maxK = opt.hierarchical.maxK;
            clustOut = km.HierarchicalKMeans();
            UpdateOutput(name, dirName,alignFile,clustOut, distance.ToString(), cpuPart1, cpuPart2, km);
        }
       
     
        private void Run1DJury(string name, string dirName, string alignFile=null)
        {
            DateTime cpuPart1 = DateTime.Now;
            ClusterOutput output;


            jury1D ju=new jury1D();
            if (beginJob != null)
                beginJob(currentProcessName, ju.ToString(), dirName, "NONE");

            progressDic.Add(name,ju);


            //DistanceMeasure distance = CreateMeasure();
                if (opt.other.alignGenerate)
                    opt.other.alignFileName = "";
                if (alignFile != null)
                    ju.PrepareJury(alignFile, opt.other.juryProfile);
                else
                        ju.PrepareJury(dirName, alignFile, opt.other.juryProfile);

                
            clType = ju.ToString();
            DateTime cpuPart2 = DateTime.Now;
            //jury1D ju = new jury1D(opt.weightHE,opt.weightC,(JuryDistance) distance);
            //output = ju.JuryOpt(new List<string>(ju.stateAlign.Keys));
            if (ju.alignKeys != null)
            {
              
                output = ju.JuryOptWeights(ju.alignKeys);
            }
            else
            {
                UpadateJobInfo(name, true, false);
                throw new Exception("Alignment is epmty! Check errors");
            }
            UpdateOutput(name, dirName,alignFile, output,ju.ToString(), cpuPart1,cpuPart2, ju);
        }
      
        private void UpdateOutput(string name, string dirName, string alignFile, ClusterOutput output, string distStr, DateTime cpuPart1, DateTime cpuPart2, object obj)
        {           
            output.clusterType = obj.ToString();
            output.measure = distStr.ToString();

            DateTime cc = DateTime.Now;
            
            TimeSpan preprocess=new TimeSpan();
            TimeSpan cluster=new TimeSpan();
            if(cpuPart1!=null && cpuPart2!=null)
                preprocess = cpuPart2.Subtract(cpuPart1);
            if(cpuPart2!=null)
                cluster = cc.Subtract(cpuPart2);

            output.time = "Prep="+String.Format("{0:F2}", preprocess.TotalMinutes);
            if(cpuPart2!=null)
                output.time += " Clust=" + String.Format("{0:F2}", cluster.TotalMinutes);
            output.name = name;
            output.dirName = dirName;
            output.alignFile = alignFile;
            output.peekMemory = Process.GetCurrentProcess().PeakWorkingSet64;
            Process.GetCurrentProcess().Refresh();
            progressDic.Remove(name);

            //Process.GetCurrentProcess().
            clOutput.Add(output.name, output);
            UpadateJobInfo(name, false,false);
        }
        public void UpadateJobInfo(string processName, bool errorFlag,bool finishAll)
        {
            if (updateJob != null)
                updateJob(processName, errorFlag,finishAll);

        }
        public void FinishThread(string processName,bool errorFlag)
        {
            lock (runnigThreads)
            {
                UpadateJobInfo(currentProcessName, errorFlag,true);
                runnigThreads.Remove(processName);
                if (progressDic.ContainsKey(currentProcessName))
                    progressDic.Remove(currentProcessName);
            }
        }
        public void RemoveJob(string jobName)
        {
            if (runnigThreads.ContainsKey(jobName))
            {
                lock (runnigThreads)
                {
                    runnigThreads[jobName].Abort();
                    runnigThreads.Remove(jobName);
                }
            }
        }
        private DistanceMeasure CreateMeasure(string processName, string dirName,DistanceMeasures measure,bool jury1d,string alignFileName,
                                              string profileName=null,string refJuryProfile=null)
        {
            DistanceMeasure dist=null;
            switch(measure)
            {
                case DistanceMeasures.HAMMING:
                    if (alignFileName != null)
                        dist = new JuryDistance(alignFileName, jury1d, profileName, refJuryProfile);
                    else
                        dist = new JuryDistance(dirName, alignFileName, jury1d, profileName, refJuryProfile);
                    break;
                case DistanceMeasures.COSINE:
                    if (alignFileName != null)
                        dist = new CosineDistance(alignFileName, jury1d, profileName, refJuryProfile);
                    else
                        dist = new CosineDistance(dirName, alignFileName, jury1d, profileName, refJuryProfile);
                    break;

                case DistanceMeasures.PEARSON:
                    if (dirName == null)
                        throw new Exception("RMSD and MAXSUB measures cannot be used for aligned profiles!");
                    dist = new Pearson(dirName, alignFileName, jury1d, refJuryProfile);
                    break;


            }
            return dist;
        }
        string MakeName(object processParams,ClusterAlgorithm alg,int counter)
        {
            string currentProcessName = "";
            if (((ThreadParam)processParams).name != null && ((ThreadParam)processParams).name.Length > 0)
                currentProcessName = ((ThreadParam)processParams).name + ";" + counter;
            else
                currentProcessName = alg.ToString() + ";" + counter;

            return currentProcessName;
        }
        public void StartAll(object processParams)
        {
            ErrorBase.ClearErrors();
            string orgProcessName = ((ThreadParam)processParams).name;
            currentProcessName = ((ThreadParam)processParams).name;
            int counter = 1;
            try
            {
                if (opt.profileFiles.Count == 0)
                {
                    foreach (var alg in opt.clusterAlgorithm)
                    {
                        
                        foreach (var item in opt.dataDir)
                        {
                         //   if (tTimer != null)
                         //       tTimer.Start();
                            currentProcessName = MakeName(processParams, alg, counter);
                            //if (beginJob != null)
                              //  beginJob(currentProcessName, alg.ToString("g"), item, opt.GetDistanceMeasure(alg));

                            switch (alg)
                            {
                                case ClusterAlgorithm.uQlustTree:
                                    RunHashDendrogCombine(currentProcessName, item);
                                    break;                                
                                case ClusterAlgorithm.HashCluster:
                                    RunHashCluster(currentProcessName, item);
                                    break;
                                case ClusterAlgorithm.GuidedHashCluster:
                                    RunGuidedHashCluster(currentProcessName, item);
                                    break;
                                case ClusterAlgorithm.HierarchicalCluster:
                                    RunHierarchicalCluster(currentProcessName, item);
                                    break;
                                case ClusterAlgorithm.HKmeans:
                                    RunHKMeans(currentProcessName, item);
                                    break;
                                case ClusterAlgorithm.Jury1D:
                                    Run1DJury(currentProcessName, item);
                                    break;
                                case ClusterAlgorithm.HTree:
                                    RunHTree(currentProcessName, item);
                                    break;
                            }

                            counter++;
                        }
                    }
                }
                else
                {
                    foreach(var alg in opt.clusterAlgorithm)
                    {
                    foreach (var item in opt.profileFiles)
                    {
//                        if (tTimer != null)
//                            tTimer.Start();
                        currentProcessName = MakeName(processParams, alg, counter);
                            // if (beginJob != null)
                            //   beginJob(currentProcessName, opt.clusterAlgorithm.ToString(), item, opt.GetDistanceMeasure(alg));

                            switch (alg)
                            {
                                case ClusterAlgorithm.uQlustTree:
                                    RunHashDendrogCombine(currentProcessName, item, item);
                                    //RunHashDendrog(currentProcessName, null, item);
                                    break;
                                case ClusterAlgorithm.OmicsHeatMap:
                                    RunOmicsHeatmap(currentProcessName, item, item);
                                    break;
                                case ClusterAlgorithm.HashCluster:
                                    RunHashCluster(currentProcessName, item, item);
                                    break;
                                case ClusterAlgorithm.GuidedHashCluster:
                                    RunGuidedHashCluster(currentProcessName, item, item);
                                    break;
                                case ClusterAlgorithm.HierarchicalCluster:
                                    RunHierarchicalCluster(currentProcessName, item, item);
                                    break;
                                case ClusterAlgorithm.HKmeans:
                                    RunHKMeans(currentProcessName, item, item);
                                    break;
                                case ClusterAlgorithm.Jury1D:
                                    Run1DJury(currentProcessName, item, item);
                                    break;
                                case ClusterAlgorithm.HNN:
                                    RunHNN(currentProcessName, item, item);
                                    break;
                                case ClusterAlgorithm.HTree:
                                    RunHTree(currentProcessName, item,item);
                                    break;

                            }
                            counter++;
                    }

                }
                }
                FinishThread(orgProcessName, false);
            
            }
            catch (Exception ex)
            {
                FinishThread(orgProcessName, true);
                message(ex.Message);
            }
            
        }
        public void RunJob(string processName)
        {
            ThreadParam tparam=new ThreadParam();

             startProg = new Thread(StartAll);
             tparam.name = processName;
             startProg.Start(tparam);            
            lock (runnigThreads)
            {
                runnigThreads.Add(processName, startProg);
            }
        }
        public void WaitAllNotFinished()
        {
            while (runnigThreads.Count > 0)
            {
                Thread.Sleep(1000);
            }
        }
        public void SaveOutput(string fileName)
        {            
            StreamWriter w = new StreamWriter(fileName);
            int count=0;
            foreach (var item in clOutput.Keys)
            {
                string name=fileName+count++;
                w.WriteLine(name);
                ClusterOutput.Save(name, clOutput[item]);
            }
            w.Close();

        }
        public void LoadOutput(string fileName)
        {
            ClusterOutput outP;

            if (File.Exists(fileName))
            {
                clOutput.Clear();
                string line;
                StreamReader r = new StreamReader(fileName);
                while (!r.EndOfStream)
                {
                    line = r.ReadLine();
                    if (File.Exists(line))
                    {
                        outP = ClusterOutput.Load(line);
                        clOutput.Add(outP.name, outP);
                    }
                }
                r.Close();
            }
        }

        public void LoadExternal(string fileName, Func<string, string, ClusterOutput> Load)
        {
            ClusterOutput aux;
            if (File.Exists(fileName))
            {
                string line;
                StreamReader r = new StreamReader(fileName);
                line = r.ReadLine();
                string dirNameOrg = line;
                while (!r.EndOfStream)
                {
                    line = r.ReadLine();
                    if (File.Exists(line))
                    {
                        string nn=Path.GetFileName(line);
                        if(nn.Contains("list"))
                        {
                            string[] a = nn.Split('.');
                            nn = a[0];
                        }
                        if (nn.Contains("_"))
                        {
                            string[] aa = nn.Split('_');
                            nn = aa[0];
                        }

                        string dirName = dirNameOrg+Path.DirectorySeparatorChar+nn;
                        aux = Load(line,dirName);
                        clOutput.Add(aux.name, aux);
                    }
                }
                r.Close();
            }
        }

        public void LoadExternalF(string fileName)
        {
            LoadExternal(fileName, ClusterOutput.LoadExternal);
        }
        public void LoadExternalPleiades(string fileName)
        {
            LoadExternal(fileName,ClusterOutput.LoadExternalPleiades);
        }
        public void LoadExternalPconsD(string fileName)
        {
            LoadExternal(fileName, ClusterOutput.LoadExternalPconsD);
        }

    }

}
