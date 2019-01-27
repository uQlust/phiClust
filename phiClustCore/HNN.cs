using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using phiClustCore.Profiles;
using System.IO;
using phiClustCore.Interface;
namespace phiClustCore
{
    [Serializable]
    class HNN:ISerialize
    {
        Dictionary<string, string> classLabels;
        Dictionary<string, string> caseBase = new Dictionary<string,string>();
        Dictionary<string, string> labelToBaseKey = new Dictionary<string, string>();
        public List<string> validateList = new List<string>();
        public List<string> testList = new List<string>();
        HashCluster hk=null;
        ClusterOutput outP=null;
        public ClusterOutput outCl { get { return outP; } set { outP = value; } }
        Settings set;
        HNNCInput opt;

        public HNN(HashCluster hk,ClusterOutput outp,HNNCInput opt)        
        {
            this.hk=hk;
            this.opt = opt;
            set = new Settings();
            set.Load();
            PrepareCaseBaseLabels(outp);
        }
        public void ISaveBinary(string fileName)
        {
            GeneralFunctionality.SaveBinary(fileName, this);
        }
        public ISerialize ILoadBinary(string fileName)
        {
            return GeneralFunctionality.LoadBinary(fileName);
        }
        static Dictionary<string,string> ReadClassLabels(string fileName)
        {
            Dictionary<string, string> labels = new Dictionary<string, string>();
            StreamReader wr = new StreamReader(fileName);
            string line = wr.ReadLine();
            while(line!=null)
            {
                string[] aux = line.Split(' ');
                if (aux.Length == 2)
                    labels.Add(aux[0], aux[1]);

                line = wr.ReadLine();
            }
            wr.Close();

            return labels;
        }
        void PrepareCaseBaseLabels(ClusterOutput outp)
        {
            classLabels = null;
            if(opt.labelsFile.Length>0)
                classLabels = ReadClassLabels(opt.labelsFile);

            if (classLabels == null || classLabels.Count == 0)
            {
                int num = 1;
                this.classLabels = new Dictionary<string, string>();
                foreach (var cluster in outp.clusters)
                {
                    string label = "cluster_" + num++;
                    foreach(var item in cluster)                    
                        this.classLabels.Add(item, label);                    
                }
            }
            Dictionary<string, int> classDic = new Dictionary<string, int>();
            foreach (var item in hk.dicFinal)
            {
                if (item.Value.Count > 0)
                {
                    classDic.Clear();
                    foreach (var index in item.Value)
                    {

                        string structV = hk.structNames[index];
                        if (this.classLabels.ContainsKey(structV))
                        {
                            if (classDic.ContainsKey(this.classLabels[structV]))
                                classDic[this.classLabels[structV]]++;
                            else
                                classDic.Add(this.classLabels[structV], 1);
                        }
                    }
                    foreach (var index in item.Value)
                        if (labelToBaseKey.ContainsKey(hk.structNames[index]))
                            //throw new Exception("Key " + hk.structNames[index] + " already exists");
                            Console.Write("Key " + hk.structNames[index] + " already exists");
                        else
                            labelToBaseKey.Add(hk.structNames[index], item.Key);
                    if (classDic.Count == 0)
                        continue;
                    List<string> classLab = new List<string>(classDic.Keys);

                    classLab.Sort((x, y) => classDic[x].CompareTo(classDic[y]));
                    caseBase.Add(item.Key, classLab[0]);

                }
                else
                    if (labelToBaseKey.ContainsKey(hk.structNames[item.Value[0]]))
                    throw new Exception("Key " + hk.structNames[item.Value[0]] + " already exists");
                else
                    labelToBaseKey.Add(hk.structNames[item.Value[0]], item.Key);
            }


        }
        public double HNNValidate(List<string> validList)
        {
            int good = 0;
            int all = 0;
            double acc=0;
            Dictionary<string, int> classDic = new Dictionary<string, int>();
            foreach (var vItem in validList)
            {
                if(labelToBaseKey.ContainsKey(vItem) && classLabels.ContainsKey(vItem))
                {
                    if (classLabels[vItem] == caseBase[labelToBaseKey[vItem]])
                        good++;
                    all++;
                }
                
            }
            acc = (double)good / all;
            return acc;
        }
        public Dictionary<string, string> ITest(string fileName)
        {
            Dictionary<string, protInfo> test = new Dictionary<string, protInfo>();
            if (set.mode == INPUTMODE.USER_DEFINED)
            {
                UserDefinedProfile p = new UserDefinedProfile();
                List<string> nodes = new List<string>(hk.al.r.masterNode.Keys);
                test = p.GetProfile(hk.al.r.masterNode[nodes[0]], fileName);
            }
            
            foreach (var item in test)
            {
                List<string> aux = new List<string>();
                foreach (var state in item.Value.profile)
                    aux.Add(state.ToString());
                
                hk.al.r.AddItemsCombineStates(item.Key, aux);
            }
            Dictionary<string, string> res = HNNTest(new List<string>(test.Keys));

            return res;
        }
        public Dictionary<string, string> HNNTest(List<string> testList)
        {
            Dictionary<string, string> aux = new Dictionary<string, string>();
            Dictionary<string, string> res = new Dictionary<string, string>();
            Dictionary<string,List<int>> kk=hk.PrepareKeys(testList, true, false);
            
            if(hk.validIndexes.Count>0)
            {
                List<string> keyList = new List<string>(kk.Keys);
                for(int j=0;j<keyList.Count;j++)
                {
                    StringBuilder newOrder = new StringBuilder();
                    for (int i = 0; i < hk.validIndexes.Count; i++)
                        newOrder.Append(keyList[j][hk.validIndexes[i]]);
                    
                    string newKey = newOrder.ToString();
                    if (kk.ContainsKey(newKey))
                        kk[newKey].Add(j);
                    else
                    {
                        List<int> indx = new List<int>();
                        indx.AddRange(kk[keyList[j]]);
                        kk.Add(newKey, indx);
                    }
                    kk.Remove(keyList[j]);
                }
            }
            List<string> caseKeys = new List<string>(kk.Keys);
            foreach (var item in caseKeys)
            {
                if (caseBase.ContainsKey(item))
                {
                    foreach (var v in kk[item])
                        if (!res.ContainsKey(testList[v]))
                            res.Add(testList[v], caseBase[item]);
                }
                else
                {
                    foreach (var v in kk[item])
                        if (!aux.ContainsKey(item))
                            aux.Add(item, testList[v]);
                }
            }
            if (aux.Count > 0)
            {
                Dictionary<string, List<string>> keys = hk.AddToClusters(new List<string>(caseBase.Keys), new List<string>(aux.Keys));
                foreach (var item in keys.Keys)
                {
                    string final = "";
                    if (keys[item].Count > 0)
                    {
                        foreach (var it in keys[item])
                            for (int i = 0; i < keys[item].Count - 1; i++)
                                final += caseBase[keys[item][i]] + ":";
                        final += caseBase[keys[item][keys[item].Count - 1]];
                    }
                    if (final.Length == 0)
                        res.Add(aux[item], "NOT CLASSIFIED");
                    else
                        res.Add(aux[item], final);
                }
            }

            return res;
        }

       
       

    }
}
