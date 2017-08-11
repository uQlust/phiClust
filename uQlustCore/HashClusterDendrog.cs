using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using phiClustCore;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using phiClustCore.Interface;
using System.Data;
using phiClustCore.Distance;
using System.Text.RegularExpressions;

namespace phiClustCore
{
    class HashClusterDendrog:HashCluster,IProgressBar
    {
         DistanceMeasures dMeasure;
         DistanceMeasure dist=null;
         AglomerativeType linkageType;       
         bool jury1d;
         string profileName;
         string refJuryProfile;
         string dirName;
         HierarchicalCInput hier;
         hierarchicalCluster hk = null;
         public HashClusterDendrog(string dirName, string alignFile, HashCInput input, HierarchicalCInput dendrogOpt)
             : base(dirName, alignFile, input)
        {
            this.dMeasure=dendrogOpt.distance;
            this.linkageType=dendrogOpt.linkageType;        
            this.jury1d = dendrogOpt.reference1DjuryH;
            this.profileName = dendrogOpt.hammingProfile;
            this.refJuryProfile = dendrogOpt.jury1DProfileH;

            this.dirName = dirName;
            hier = dendrogOpt;
        }
         public HashClusterDendrog(string dirName, HashCInput input,HierarchicalCInput dendrogOpt,Alignment al)
             : base(al,input)
         {
             this.dMeasure = dendrogOpt.distance;
             this.linkageType = dendrogOpt.linkageType;         
             this.jury1d = dendrogOpt.reference1DjuryH;
             this.profileName = dendrogOpt.hammingProfile;
             this.refJuryProfile = dendrogOpt.jury1DProfileH;
             this.dirName = dirName;
             this.al = al;
             hier = dendrogOpt;
         }
    
        public void InitHashClusterDendrog()
         {
             base.InitHashCluster();
         }
        public new double ProgressUpdate()
        {
            double progress = 0;
            double res = 0;
            if (al != null)
                res = al.ProgressUpdate();
            progress=0.5* (res * 0.45 + 0.55 * (double)currentV / maxV);

            if (hk != null)
                progress += 0.5 * hk.ProgressUpdate() ;

            return StartProgress + (EndProgress - StartProgress) * progress;
        }
        public new Exception GetException()
        {
            return null;
        }
        public new List<KeyValuePair<string, DataTable>> GetResults()
        {
            return null;
        }

        public string UsedMeasure()
         {
             if (dist != null)
                 return dist.ToString();

             return "NONE";
         }
         public override string ToString()
         {        
             return "uQlust:Tree";
         }
         public ClusterOutput RunHashDendrogCombine()
         {
             ClusterOutput output = DendrogUsingMeasures(stateAlignKeys);
             return output;
         }

        static public Dictionary<string,KeyValuePair<List<string>,Dictionary<byte,int>[]>> StructuresToDenrogram(List<List<string>> structures,Alignment al)
         {
             Dictionary<string, KeyValuePair<List<string>,Dictionary<byte,int>[]>> translateToCluster = new Dictionary<string,KeyValuePair<List<string>,Dictionary<byte,int>[]>>(structures.Count);

             jury1D juryLocal = new jury1D();
             juryLocal.PrepareJury(al);

             foreach(var item in structures)
             {
                 if (item.Count > 2)
                 {
                     ClusterOutput output = juryLocal.JuryOptWeights(item);
                     translateToCluster.Add(output.juryLike[0].Key, new KeyValuePair<List<string>,Dictionary<byte,int>[]>(new List<string>(item.Count),juryLocal.columns));
                     
                     translateToCluster[output.juryLike[0].Key].Key.Add(output.juryLike[0].Key);
                     foreach (var i in item)
                         if (!i.Equals(output.juryLike[0].Key))
                            translateToCluster[output.juryLike[0].Key].Key.Add(i);
                     
                 }
                 else
                     translateToCluster.Add(item[0],new KeyValuePair<List<string>,Dictionary<byte,int>[]>(item,juryLocal.columns));
             }

             return translateToCluster;
         }
        public ClusterOutput DendrogUsingMicroClusters(Dictionary<string, KeyValuePair<List<string>, Dictionary<byte, int>[]>> translateToCluster)
         {
             ClusterOutput outC = null;
             Regex rgx = new Regex("_similarity|_distance|profiles|profile");
          
             string aux1 = al.r.currentProfileName;
             string aux2 = profileName;
             string aux3 = refJuryProfile;
             aux1 = rgx.Replace(aux1, "");
             aux2 = rgx.Replace(aux2, "");
             aux3 = rgx.Replace(aux3, "");
            

             switch (dMeasure)
             {
                 case DistanceMeasures.HAMMING:
                     if (refJuryProfile == null || !jury1d)
                         throw new Exception("Sorry but for jury measure you have to define 1djury profile to find reference structure");
                     //
                         this.al.r.LoadProfiles(profileName);
                         dist = new JuryDistance(this.al, true);
                     //dist.InitMeasure();
                     break;


                 case DistanceMeasures.COSINE:
                         this.al.r.LoadProfiles(profileName);
                         dist = new CosineDistance(this.al, jury1d);
                     break;
                 case DistanceMeasures.EUCLIDIAN:
                         this.al.r.LoadProfiles(profileName);
                         dist = new Euclidian(this.al, jury1d);
                     break;

                 case DistanceMeasures.PEARSON:
                         this.al.r.LoadProfiles(profileName);
                         dist = new Pearson(this.al, jury1d);
                     break;
             }

             // return new ClusterOutput();
             DebugClass.WriteMessage("Start hierarchical");
             //Console.WriteLine("Start hierarchical " + Process.GetCurrentProcess().PeakWorkingSet64);
             currentV = maxV;
             hk = new hierarchicalCluster(dist, hier, dirName);
             dist.InitMeasure();

             //Now just add strctures to the leaves   
             List<string> keys = new List<string>(translateToCluster.Keys);
             List<Dictionary<byte, int>[]> freq = new List<Dictionary<byte, int>[]>();

             foreach (var item in translateToCluster.Keys)
                 freq.Add(translateToCluster[item].Value);

             outC = hk.HierarchicalClustering(new List<string>(translateToCluster.Keys),freq);
             DebugClass.WriteMessage("Stop hierarchical");
             List<HClusterNode> hLeaves = outC.hNode.GetLeaves();

             

             foreach (var item in hLeaves)
             {
                 if (translateToCluster.ContainsKey(item.refStructure))
                 {
                     item.setStruct.Clear();
                     item.setStruct.AddRange(translateToCluster[item.refStructure].Key);

                     if (dist is JuryDistance)
                     {
                         foreach (var str in item.setStruct)
                         {
                             item.setProfiles.Add(((JuryDistance)dist).stateAlign[str]);
                         }

                     }


                     item.consistency = CalcClusterConsistency(item.setStruct);

                 }
                 else
                     throw new Exception("Cannot add structure. Something is wrong");
             }
             outC.hNode.RedoSetStructures();
             outC.runParameters = hier.GetVitalParameters();
             outC.runParameters += input.GetVitalParameters();
             return outC;

         }
         public ClusterOutput DendrogUsingMeasures(List<string> structures)
         {
             jury1D juryLocal = new jury1D();
             juryLocal.PrepareJury(al);
             
             ClusterOutput outC = null;
             Dictionary<string, List<int>> dic;
             //Console.WriteLine("Start after jury " + Process.GetCurrentProcess().PeakWorkingSet64);
             maxV = 100;
             currentV = 0;
             dic = PrepareKeys(structures,false,true);

             
             currentV+=5;
             //DebugClass.DebugOn();
           //  input.relClusters = input.reqClusters;
           //  input.perData = 90;
             if (dic.Count > input.relClusters)
             {
                 if (!input.combine)
                     dic = HashEntropyCombine(dic, structures, input.relClusters);
                 else
                     //dic = FastCombineKeys(dic, structures, false);
                     dic = FastCombineKeysNew(dic, structures, false);
             }
             //Console.WriteLine("Entropy ready after jury " + Process.GetCurrentProcess().PeakWorkingSet64);
             DebugClass.WriteMessage("Entropy ready");
             //Alternative way to start of UQclust Tree must be finished
             //input.relClusters = 10000;

             
             //dic = FastCombineKeys(dic, structures, true);
             DebugClass.WriteMessage("dic size" + dic.Count);             
             
             //Console.WriteLine("Combine ready after jury " + Process.GetCurrentProcess().PeakWorkingSet64);
             DebugClass.WriteMessage("Combine Keys ready");
             Dictionary<string, string> translateToCluster = new Dictionary<string, string>(dic.Count);
             List<string> structuresToDendrogram = new List<string>(dic.Count);
             List<string> structuresFullPath = new List<string>(dic.Count);
             DebugClass.WriteMessage("Number of clusters: "+dic.Count);
             int cc = 0;
             List<string> order = new List<string>(dic.Keys);
//             order.Sort((a,b)=>dic[b].Count.CompareTo(dic[a].Count));
             order.Sort(delegate(string a, string b)
             {

                 if (dic[b].Count == dic[a].Count)
                     for (int i = 0; i < a.Length; i++)
                         if (a[i] != b[i])
                             if (a[i] == '0')
                                 return -1;
                             else
                                 return 1;


                 return dic[b].Count.CompareTo(dic[a].Count);
             });
             foreach (var item in order)
             {
                 if (dic[item].Count > 2)
                 {
                     List<string> cluster = new List<string>(dic[item].Count);
                     foreach (var str in dic[item])
                         cluster.Add(structures[str]);

                     
                     ClusterOutput output = juryLocal.JuryOptWeights(cluster);
                     int end = output.juryLike.Count;
                     structuresToDendrogram.Add(output.juryLike[0].Key);
                     if(alignFile==null)
                        structuresFullPath.Add(dirName + Path.DirectorySeparatorChar + output.juryLike[0].Key);
                     else
                         structuresFullPath.Add(output.juryLike[0].Key);
                     translateToCluster.Add(output.juryLike[0].Key, item);
                 }
                 else
                 {
                     structuresToDendrogram.Add(structures[dic[item][0]]);
                     if(alignFile==null)
                        structuresFullPath.Add(dirName + Path.DirectorySeparatorChar + structures[dic[item][0]]);
                     else
                         structuresFullPath.Add(structures[dic[item][0]]);
                     translateToCluster.Add(structures[dic[item][0]], item);
                 }
                 cc++;
             }
             currentV+=10;
             DebugClass.WriteMessage("Jury finished");
             Regex rgx = new Regex("_similarity|_distance|profiles|profile");
              string aux1=al.r.currentProfileName;
                         string aux2=profileName;
                         string aux3=refJuryProfile;
             aux1=rgx.Replace(aux1,"");
             aux2=rgx.Replace(aux2,"");
             aux3=rgx.Replace(aux3,"");

             switch (dMeasure)
             {
                 case DistanceMeasures.HAMMING:
                     if (refJuryProfile == null || !jury1d)
                         throw new Exception("Sorry but for jury measure you have to define 1djury profile to find reference structure");
                         //
                     if (aux1.Equals(aux2) && aux1.Equals(aux3))
                     {
                         this.al.r.LoadProfiles(profileName);
                         dist = new JuryDistance(this.al, true);
                     }
                     else
                         dist = new JuryDistance(structuresFullPath, alignFile, true, profileName, refJuryProfile);
                         //dist.InitMeasure();
                         break;


                 case DistanceMeasures.COSINE:
                         if (aux1.Equals(aux2) && aux1.Equals(aux3))
                         {
                             this.al.r.LoadProfiles(profileName);
                             dist = new CosineDistance(this.al, jury1d);
                         }
                         else
                            dist = new CosineDistance(structuresFullPath, alignFile, jury1d, profileName, refJuryProfile);
                     break;
                 case DistanceMeasures.EUCLIDIAN:
                     if (aux1.Equals(aux2) && aux1.Equals(aux3))
                     {
                         this.al.r.LoadProfiles(profileName);
                         dist = new Euclidian(this.al, jury1d);
                     }
                     else
                        dist = new Euclidian(structuresFullPath, alignFile, jury1d, profileName, refJuryProfile);
                     break;

                 case DistanceMeasures.PEARSON:
                     if (aux1.Equals(aux2) && aux1.Equals(aux3))
                     {
                         this.al.r.LoadProfiles(profileName);
                         dist = new Pearson(this.al, jury1d);
                     }
                     else
                        dist=new Pearson(structuresFullPath, alignFile, jury1d, profileName, refJuryProfile);
                     break;
             }

            // return new ClusterOutput();
             DebugClass.WriteMessage("Start hierarchical");
             //Console.WriteLine("Start hierarchical " + Process.GetCurrentProcess().PeakWorkingSet64);
             currentV = maxV;
             hk = new hierarchicalCluster(dist, hier, dirName);
             dist.InitMeasure();
            
             //Now just add strctures to the leaves             
             outC = hk.HierarchicalClustering(structuresToDendrogram);
             DebugClass.WriteMessage("Stop hierarchical");
             List<HClusterNode> hLeaves = outC.hNode.GetLeaves();

             foreach(var item in hLeaves)
             {
                 if (translateToCluster.ContainsKey(item.setStruct[0]))
                 {
                     foreach (var str in dic[translateToCluster[item.setStruct[0]]])
                         if (item.setStruct[0] != structures[str])
                            item.setStruct.Add(structures[str]);

                     if (dist is JuryDistance)
                     {
                             foreach (var str in item.setStruct)
                             {
                                 item.setProfiles.Add(((JuryDistance)dist).stateAlign[str]);
                             }
                         
                     }


                     item.consistency = CalcClusterConsistency(item.setStruct);

                 }
                 else
                     throw new Exception("Cannot add structure. Something is wrong");
             }
             outC.hNode.RedoSetStructures();
             outC.runParameters = hier.GetVitalParameters();
             outC.runParameters += input.GetVitalParameters();
             return outC;
         }
         

    }
}
