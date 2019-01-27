using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Data;
using phiClustCore.Interface;

namespace phiClustCore
{
    [Serializable]
    public class Alignment : IProgressBar
	{
        Settings dirSettings;
        public Options opt;
		Dictionary <string,string> align=new Dictionary<string,string>();
        string refSeq = null;
        int maxV;
		public ProfileTree r;

        double startProgress = 0;
        double endProgress = 1;

        public double StartProgress { set { startProgress = value; } get { return startProgress; } }
        public double EndProgress { set { endProgress = value; } get { return endProgress; } }


        public Alignment(Options opt)
        {
            this.opt = opt;
            r = new ProfileTree();
        }

        public double ProgressUpdate()
        {
            return StartProgress+(EndProgress - StartProgress) * r.ProgressUpdate();
        }

        public Exception GetException()
        {
            return null;
        }
        public List<KeyValuePair<string, DataTable>> GetResults()
        {
            return null;
        }


		public void Prepare(string pathName,Settings dirSettings,string profName)
		{
            StartAlignment(dirSettings, profName, pathName, null);
        }
        public void Prepare(List<string>fileNames, Settings dirSettings, string profName)
        {
            StartAlignment(dirSettings, profName, null, fileNames);
        }
        public void Prepare(List<string> names,string alignFile,string profName)
        {

        }
        public void Clean()
       {
           align.Clear();
           r.protCombineStates.Clear();
           r.profiles.Clear();          
       }
        public void Prepare(List<KeyValuePair<string,string>> profilesStr,string profName,string profFile)
        {
            r = new ProfileTree();
            r.LoadProfiles(profFile);            
            
            foreach(var item in profilesStr)
            {
                List<string> aux = new List<string>(item.Value.Length);
                for (int i = 0; i < item.Value.Length; i++)
                    aux.Add(item.Value[i].ToString());

               r.AddItemsCombineStates(item.Key,aux);
                                
            }
        }
        public void Prepare(string profilesFile, string profName)
        {
            r.LoadProfiles(profName);
            r.listFile = profilesFile;
            DebugClass.WriteMessage("profiles gen started "+profName);
            r.MakeProfiles(opt);
            DebugClass.WriteMessage("Prfofiles end");
        }
        private void StartAlignment(Settings dirSettings, string profName, string dirName, List<string> fileNames )
        {

            DebugClass.WriteMessage("Start align");
            string refFile = null;
            this.dirSettings = dirSettings;            
           // r = new ProfileTree();
            r.LoadProfiles(profName);
                if (dirName != null)
                {
                    refFile = dirName + ".ref";
                    DebugClass.WriteMessage("profiles gen started");
                    r.PrepareProfiles(opt,dirName);
                    DebugClass.WriteMessage("finished");
                    refSeq=ReadRefSeq(refFile);
                }
                else
                {
                    maxV = fileNames.Count;
                    string name = fileNames[0];
                    if(fileNames[0].Contains("|"))
                    {
                        string[] aux = fileNames[0].Split('|');
                        name = aux[0];
                    }
                    refFile = Directory.GetParent(name).ToString() + ".ref";
                    DebugClass.WriteMessage("profiles gen started");
                    r.PrepareProfiles(opt,fileNames);
                    DebugClass.WriteMessage("finished");
                    refSeq = ReadRefSeq(refFile);
                }
            DebugClass.WriteMessage("Prfofiles end"); 


        }
		public Dictionary<string,string> GetAlignment()
		{
			return align;
		}
		public Dictionary<string, List<byte>> GetStateAlign()
		{
			return r.protCombineStates;
		}
        public static string ReadRefSeq(string fileName)
        {
            string rSeq;
            if (!File.Exists(fileName))
                return null;

            StreamReader stR = new StreamReader(fileName);
            rSeq = stR.ReadLine();
            stR.Close();
            return rSeq;
        }
        public static Dictionary<string, string> ReadAlignment(string fileName)
        {
            Dictionary<string, string> alignLoc = new Dictionary<string, string>();
            StreamReader file_in = null;
            string line, remName = "";

            try
            {
                file_in = new StreamReader(fileName);
                line = file_in.ReadLine();
                while (line != null)
                {
                    if (line.Contains(">"))
                    {
                        string name = line.Substring(1, line.Length-1);
                        string profile = "";
                        line = file_in.ReadLine();
                        while (line!=null && !(line.Contains(">")))
                        {
                            profile+=line;
                            line = file_in.ReadLine();                                                        
                        }
                        if (alignLoc.Count == 0)
                            remName = name;
                        else
                        {
                            profile.Replace("\n", "");
                            profile.Replace(" ", "");
                            if (profile.Length != alignLoc[remName].Length)
                                throw new Exception("Alignment incorrect for " + remName + " and "+ name + "\nDifferent number of symbols in the alignment!");
                        }
                        alignLoc.Add(name, profile);
                    }
                    else
                        line = file_in.ReadLine();
                }
            }
            finally
            {
                if (file_in != null)
                    file_in.Close();
            }

            return alignLoc;

        }

        public void AddStructureToAlignment(string protName,string profileName,ref protInfo profile)
        {
           
                MAlignment al = new MAlignment(refSeq.Length);
                string resSeq = (al.Align(refSeq, profile.sequence)).seq2;

                if (!align.ContainsKey(protName))
                    align.Add(protName, resSeq);
                if(!r.profiles[profileName].ContainsKey(protName))
                    r.profiles[profileName].Add(protName,profile);
                
            
            AlignProfile(protName, profileName, profile.profile);
            profile.alignment = r.profiles[profileName][protName].alignment;
          
        }
		public void MyAlign(string alignFile)
		{
            try
            {
                AlignProfiles();
            }
            catch (Exception)
            {
                throw new Exception("Combining alignments went wrong!");
            }
			
		}
		public void NoBlast()
		{
 			foreach (var item in r.profiles)
            {
                    foreach (var pp in item.Value)
                    {
                        align[pp.Key]=pp.Value.sequence;
                    }
                    break;                
            }		
			
			try
			{
				AlignProfiles();
			}
			catch(Exception ex)
			{
				Console.WriteLine("Something went wrong :"+ex.Message);
			}
		}
        private void AlignProfile(string protName,string profileName,List<byte>prof)
        {
            int m = 0;
            string alignProfile = align[protName];
            List<byte> ll = new List<byte>(alignProfile.Length);
            for (int i = 0; i < alignProfile.Length; i++)
            {
                if (alignProfile[i] == '-')
                {
                    ll.Add(0);
                    continue;
                }
                if (m < prof.Count)
                    ll.Add(prof[m++]);
                else
                    ErrorBase.AddErrors("Profile " + profileName + " for " + protName + " seems to be incorect");

            }
            protInfo tmp = new protInfo();
            if (r.profiles[profileName].ContainsKey(protName))
            {
                tmp = r.profiles[profileName][protName];
                tmp.alignment = ll;
                r.profiles[profileName][protName] = tmp;
            }
           

        }
		public void AlignProfiles()
		{
            r.CombineProfiles();                    
            GC.Collect();           
		}
        public int ProfileLength()
        {
            return r.GetProfileLength();
        }
        public Dictionary<byte, double> StatesStat()
        {
            return r.StatesStat();
        }
       
       
		public void PrintAlign()
		{
			StreamWriter file=null;
			try
			{
				file=new StreamWriter("aaa");
			
			
				file.WriteLine("Alignment:");
				foreach(string i in align.Keys)
				{
				//	file.WriteLine(i);
					file.WriteLine(align[i]);
				}
			}
			finally
			{
				if(file!=null)
					file.Close();
				
			}
				
		}
	}
}

