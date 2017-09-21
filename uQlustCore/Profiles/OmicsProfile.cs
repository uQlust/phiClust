using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Globalization;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
//using phiClustCore;

namespace phiClustCore.Profiles
{
    public enum CodingAlg
    {
        PERCENTILE,
        Z_SCORE,
        EQUAL_DIST
    };
    public class OmicsProfile : UserDefinedProfile
    {
        public static string omicsSettings = "omicsSettings.dat";
        public int numCol=10;
        public int numRow=18;
        public bool genePosition;
        public int selectGenes = 0;
        public bool zScore;
        public bool quantile;
        public int numStates = 6;
        public List<int> labelGeneStart = null;
        public List<int> labelSampleStart = null;       
        public bool uLabelGene = false;
        public bool uLabelSample = false;
        public int labelNumRows = 1;
        public string processName;
        public bool heatmap = false;

        profileNode localNode = null;
        profileNode localNodeDist = null;
        List<string> labels = new List<string>();
        public List<string> []labelGenes =null;
        public List<string>[] labelSamples = null;
        public string[] labId = null;
        public string fileSelectedGenes = "";
        List<string> labelsData=new List<string>();
        Dictionary<string,int> selectedGenes = null;
        Dictionary<int, string> indexSelectedGenes = null;
        public bool transpose = false;
        public CodingAlg coding = CodingAlg.EQUAL_DIST;
        static int profSize = 0;

        double []dev = null;
      
        double []avr=null;

        private static string ProfileName ="Ömics profile";
        Settings set=new Settings();
        public OmicsProfile()
        {
                ProfileName = "Omics profile";
                profileName = ProfileName;
                AddInternalProfiles();
                destination = new List<INPUTMODE>();
                destination.Add(INPUTMODE.OMICS);
                maxV = 100;
                currentProgress = 0;
                set.Load();
                LoadOmicsSettings();

        }
        public void SaveOmicsSettings()
        {
            StreamWriter wr = new StreamWriter(omicsSettings);

            wr.WriteLine("Column " + numCol);
            wr.WriteLine("Rows " + numRow);
            wr.WriteLine("Use gene labels " + uLabelGene);
            wr.WriteLine("Use sample labels " + uLabelSample);
            string s = "";
            for (int i = 0; i < labelGeneStart.Count - 1; i++)
                s += labelGeneStart[i] + ";";
            s += labelGeneStart[labelGeneStart.Count - 1];
            wr.WriteLine("Label Genes " + s);
            s = "";
            for (int i = 0; i < labelSampleStart.Count - 1; i++)
                s += labelSampleStart[i] + ";";
            s += labelSampleStart[labelSampleStart.Count-1];
            wr.WriteLine("Label Samples " +s);
            wr.WriteLine("Label Number of rows " + labelNumRows);
            wr.WriteLine("States " + numStates);
            wr.WriteLine("transposition " + transpose);
            wr.WriteLine("Coding Algorithm " + coding);
            wr.WriteLine("Heatmap " + heatmap);
            if(processName.Length>0)
                wr.WriteLine("OutputName " + processName);
            wr.WriteLine("Gene Position Rows " + genePosition);
            wr.WriteLine("Z-score " + zScore);
            wr.WriteLine("Quantile " + quantile);
            wr.WriteLine("Selected genes " + fileSelectedGenes);
            if (selectGenes > 0)
                wr.Write("Select genes " + selectGenes);
            wr.Close();
        }
        public override void AddInternalProfiles()
        {
            profileNode node = new profileNode();

            node.profName = ProfileName;
            node.internalName = ProfileName;
            InternalProfilesManager.AddNodeToList(node, this.GetType().FullName);
        }
        public void LoadOmicsSettings()
        {
            if (!File.Exists(omicsSettings))
                return;

            StreamReader r = new StreamReader(omicsSettings);
            string line = r.ReadLine();

            while (line != null)
            {
                string[] aux = line.Split(' ');
                if (line.Contains("Column "))
                    numCol = Convert.ToInt32(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                if (aux[0].Equals("Rows"))
                    numRow = Convert.ToInt32(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                if (line.Contains("Label"))
                    if (line.Contains("Genes"))
                    {
                        labelGeneStart = new List<int>();
                        labelGeneStart.Add(Convert.ToInt32(aux[aux.Length - 1], CultureInfo.InvariantCulture));
                    }
                    else
                        if (line.Contains("Samples"))
                        {
                            labelSampleStart = new List<int>();
                            if (aux[aux.Length - 1].Contains(";"))
                            {
                                string[] s = aux[aux.Length - 1].Split(';');
                                foreach (var item in s)
                                    labelSampleStart.Add(Convert.ToInt32(item, CultureInfo.InvariantCulture));
                            }
                            else
                                labelSampleStart.Add(Convert.ToInt32(aux[aux.Length - 1], CultureInfo.InvariantCulture));
                        }
                        else
                            if (line.Contains("Number"))
                                labelNumRows = Convert.ToInt32(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                if (line.Contains("Use"))
                    if (line.Contains("gene"))
                        uLabelGene = Convert.ToBoolean(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                    else
                        if (line.Contains("sample"))
                            uLabelSample = Convert.ToBoolean(aux[aux.Length - 1], CultureInfo.InvariantCulture);


                if (line.Contains("States"))
                    numStates = Convert.ToInt32(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                if (line.Contains("trans"))
                    transpose = Convert.ToBoolean(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                if (line.Contains("Algo"))
                    coding = (CodingAlg)Enum.Parse(typeof(CodingAlg), aux[aux.Length - 1]);
                if (line.Contains("Heatmap"))
                    heatmap = Convert.ToBoolean(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                if (line.Contains("OutputName"))
                    processName = aux[aux.Length - 1];
                if (line.Contains("Gene Position Rows"))
                    genePosition = Convert.ToBoolean(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                if (line.Contains("Z-score"))
                    zScore = Convert.ToBoolean(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                if (line.Contains("Quantile"))
                    quantile = Convert.ToBoolean(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                if(line.Contains("Selected"))
                {

                    fileSelectedGenes = aux[aux.Length - 1];
                    if(File.Exists(fileSelectedGenes))
                    {
                        selectedGenes = new Dictionary<string, int>();
                        StreamReader gR = new StreamReader(fileSelectedGenes);
                        string rLine = gR.ReadLine();
                        while(rLine!=null)
                        {
                            selectedGenes.Add(rLine, 0);
                            rLine = gR.ReadLine();
                        }

                        gR.Close();

                    }
                }
                if(line.Contains("Select "))
                {
                    if (aux[aux.Length - 1].Length > 0)
                        selectGenes = Convert.ToInt32(aux[aux.Length - 1], CultureInfo.InvariantCulture);
                    else
                        selectGenes = 0;
                }
                line = r.ReadLine();
            }
            r.Close();

            labelGenes = new List<string>[labelGeneStart.Count];
            for (int i = 0; i < labelGenes.Length; i++)
                labelGenes[i] = new List<string>();

            labelSamples = new List<string>[labelSampleStart.Count];
            for (int i = 0; i < labelSamples.Length; i++)
                labelSamples[i] = new List<string>();

            labId = new string[labelSamples.Length];

        }
        public void CombineTrainigTest(string fileName, string fileNameTrain, string fileNameTest)
        {
            StreamWriter wF = new StreamWriter(fileName);
            string line = "";
            string remLine = "";
            List<List<double>> dat = ReadOmicsFile(fileNameTest);
            List<string> auxRowTest = null;
            List<string> auxColTest = null;
            if (genePosition)
            {
                auxRowTest =new List<string>( labelGenes[0]);
                auxColTest = new List <string>(labelSamples[0]);
            }
            else
            {
                auxRowTest = new List<string>(labelSamples[0]);
                auxColTest = new List<string>(labelGenes[0]);
            }

            List<List<double>> trainDat = ReadOmicsFile(fileNameTrain);

            List<string> auxRow = null;
            List<string> auxCol = null;
            if (genePosition)
            {
                auxRow = labelGenes[0];
                auxCol = labelSamples[0];
            }
            else
            {
                auxRow = labelSamples[0];
                auxCol = labelGenes[0];
            }
            wF.WriteLine();
            wF.Write("EMPTY ");
            for (int i = 0; i < auxCol.Count; i++)
                wF.Write(auxCol[i] +" ");
            for (int i = 0; i < auxColTest.Count - 1; i++)
                wF.Write(auxColTest[i] + " ");
            wF.WriteLine(auxColTest[auxColTest.Count - 1]);


                for (int i = 0; i < trainDat.Count; i++)
                {
                    wF.Write(auxRow[i] + " ");
                    for (int j = 0; j < trainDat[i].Count; j++)
                        wF.Write(trainDat[i][j] + " ");
                    if (transpose)
                    {
                        for (int j = 0; j < dat[i].Count - 1; j++)
                            wF.Write(dat[i][j] + " ");
                        wF.WriteLine(dat[i][dat[i].Count - 1]);
                    }
                    else
                        wF.WriteLine();

                }
            if (!transpose)
                for (int i = 0; i < dat.Count; i++)
                {
                    wF.Write(auxRowTest[i] + " ");
                    for (int j = 0; j < dat[i].Count - 1; j++)
                        wF.Write(dat[i][j] + " ");
                    wF.WriteLine(dat[i][dat[i].Count - 1]);
                }
            wF.Close();
        }
        public static List<KeyValuePair< string,List<byte>>> ReadOmicsProfile(string fileName)
        {
            List<KeyValuePair<string, List<byte>>> dic = new List<KeyValuePair< string,List<byte>>>();
            Settings set = new Settings();
            set.Load();
            string key="";
            string value;

            StreamReader r = new StreamReader(set.profilesDir+Path.DirectorySeparatorChar+fileName);

            string line = r.ReadLine();
            while(line!=null)
            {
                if(line.Contains(">"))                
                    key = line.Remove(0, 1);
                else
                {
                    if(line.Contains(ProfileName))
                    {
                        value = line.Remove(0, ProfileName.Length+1);
                        string[] aux = value.Split(' ');
                        List<byte> v = new List<byte>(aux.Length);
                        foreach (var item in aux)
                            v.Add(Convert.ToByte(item, CultureInfo.InvariantCulture));
                        KeyValuePair<string, List<byte>> xx = new KeyValuePair<string, List<byte>>(key, v);
                        dic.Add(xx);
                    }
                }
                line = r.ReadLine();
            }
            r.Close();
            return dic;
        }
        public override void RunThreads(string fileName)
        {
            ThreadFiles ff = new ThreadFiles();

            //StreamReader str = new StreamReader(fileName);
            ff.fileName = fileName ;     
            Run((object)ff);
        }
        string GetProfileName(string fileName)
        {
            return set.profilesDir + Path.DirectorySeparatorChar +  processName;
        }
        public double [,] QuantileNorm(double[,] data)
        {
            int[][] copyData;
            double[,] rankData;
            Dictionary<double, int> dic = new Dictionary<double, int>();
            double[] avr;

            avr = new double[data.GetLength(0)];
            rankData = new double[data.GetLength(0), data.GetLength(1)];
            copyData = new int[data.GetLength(1)][];
            for (int i = 0; i < data.GetLength(1); i++)
                copyData[i] = new int[data.GetLength(0)];

            for (int i = 0; i < data.GetLength(1); i++)
                for (int j = 0; j < data.GetLength(0); j++)
                    copyData[i][j] = j;

            for (int i = 0; i < data.GetLength(1); i++)
            {
                Array.Sort<int>(copyData[i], (a, b) => data[a, i].CompareTo(data[b, i]));
                dic.Clear();
                for (int j = 0; j < data.GetLength(0); j++)
                {
                    if (data[copyData[i][j], i] != double.NaN)
                    {
                        if (!dic.ContainsKey(data[copyData[i][j], i]))
                            dic.Add(data[copyData[i][j], i], j + 1);
                        rankData[copyData[i][j], i] = dic[data[copyData[i][j], i]];
                    }
                    else
                        rankData[copyData[i][j], i] = double.NaN;
                }
            }


            for (int i = 0; i < avr.Length; i++)
            {
                double sum = 0;
                for (int j = 0; j < data.GetLength(1); j++)
                    if (data[copyData[j][i], j]!=double.NaN)
                        sum += data[copyData[j][i], j];
                avr[i] = sum / data.GetLength(1);
            }

            for (int i = 0; i < data.GetLength(0); i++)
                for (int j = 0; j < data.GetLength(1); j++)
                    if (rankData[i, j]!=double.NaN)
                        rankData[i, j] = avr[(int)rankData[i, j] - 1];


            return rankData;
        }
        void AddLabelsToGeneORSample(List <string> labels,int k)
        {
            
            if (!genePosition)
                labelGenes[k]=labels;
            else
                labelSamples[k]=labels;

        }
        List<int>  GetLabelPosition()
        {
            if (!genePosition)
                return labelGeneStart;
            else
                return labelSampleStart;

        }
        int ReadLabels(StreamReader r,string line,char delimiter=' ')
        {
            int i=0;
            int countPos=0;
            List<int> rowPositions;
            List<string> tmpAux;
           // line = r.ReadLine();

            rowPositions = GetLabelPosition();
            for (i=1;i<rowPositions[rowPositions.Count-1]; i++)
            {
                line = r.ReadLine();
                if (i== rowPositions[countPos]-1)
                {
                    line = line.Replace('\t', ' ');
                    if (line.Contains("\""))
                        line = Regex.Replace(line, "\"", "");
                    tmpAux = new List<string>();
                    string[] aux = line.Split(delimiter);
                    labId[countPos]=aux[0];
                    for (int j = numCol, n = 0; j < aux.Length; j++, n++)
                        tmpAux.Add(aux[j]);

                    AddLabelsToGeneORSample(tmpAux,countPos);
                    countPos++;
                }

            }

            return i;
        }
        public List<string> ReadClassLabels(string fileName,bool column,int pos)
        {
            List<string> labels = new List<string>();
            string line="";
            char tab = '\t';
            StreamReader r = new StreamReader(fileName);

            if (r == null)
                throw new Exception("Cannot open file: " + fileName);

            if(!column)
            {
                for (int i=0; i < pos; i++)
                    line = r.ReadLine();
                line = line.Replace(tab.ToString(), " ");
                string[] aux = line.Split(' ');
                for (int i = numCol; i < aux.Length; i++)
                    labels.Add(aux[i]);

            }
            else
            {
                for (int i=0; i < numRow; i++)
                    line = r.ReadLine();

                while (line != null)
                {
                    line = line.Replace(tab.ToString(), " ");
                    string[] aux = line.Split(' ');
                    labels.Add(aux[pos - 1]);
                    line = r.ReadLine();
                }
            }
            r.Close();

            return labels;
        }
        public List<List<double>> ReadOmicsExcelFile(string fileName)
        {
            List<List<double>> localData = new List<List<double>>();

            for (int i = 0; i < labelGenes.Length;i++ )
                labelGenes[i].Clear();
            for (int i = 0; i < labelSamples.Length; i++)
                labelSamples[i].Clear();
            List<int> labelPosition=GetLabelPosition();
            StringBuilder text = new StringBuilder();
            List<string> labels = new List<string>();
            int remProgress = currentProgress;
            int numLabel = 0;
            using (SpreadsheetDocument spr = SpreadsheetDocument.Open(fileName, false))
            {
                WorkbookPart wp = spr.WorkbookPart;
                WorksheetPart wsp = wp.WorksheetParts.First();

                
              //  IEnumerable<SheetData> sheetData = wsp.Worksheet.Elements<SheetData>();
//                IEnumerable<Row> row = sheetData.First().Elements<Row>();
                
                int maxRows = 0;
                 OpenXmlReader reader = OpenXmlReader.Create(wsp);
                 while (reader.Read())
                 {
                     if (reader.ElementType == typeof(Row) && reader.IsStartElement)
                     {
                         do
                         {
                             maxRows++;
                         } while (reader.ReadNextSibling()); // Skip to the next row
                         break;
                     }
                 }
                 reader = OpenXmlReader.Create(wsp);
                 double step = 20.0 /maxRows;
                 //currentProgress += 5;
                Cell c=null;
                int rem=0;
                int rowCounter = 0;
                
                while (reader.Read())
                {
                    if (reader.ElementType == typeof(Row) && reader.IsStartElement)
                    {
                        rowCounter++;
                        if (rowCounter < numRow - 1 && rowCounter != labelPosition[0])                                                 
                            continue;
               
                        if((int)(rowCounter*step)>rem)
                        {
                            if(currentProgress<20)
                                currentProgress++;
                            rem = (int)(rowCounter * step);
                        }
         
                        text.Clear();
                        reader.ReadFirstChild();                        
                        if (reader.ElementType == typeof(Cell))
                        {
                            int cellCounter = 0;
                            do
                            {
                                if (text.Length > 0)
                                    text.Append(";");
                                c = (Cell)reader.LoadCurrentElement();
                                for (numLabel = 0; numLabel < labelPosition.Count;numLabel++)
                                    if (rowCounter == labelPosition[numLabel])
                                    {
                                        if (cellCounter >= numCol)
                                            labels.Add(GetCellValue(c, wp));
                                        cellCounter++;
                                    }
                                    else
                                        text.Append(GetCellValue(c, wp));
                            }
                            while (reader.ReadNextSibling());
                            if(rowCounter>=numRow)
                                ProcessRow(text.ToString(), localData,';');
                        }                        
                    }
                }                
                AddLabelsToGeneORSample(labels,numLabel);

            }
            currentProgress += 20-(currentProgress-remProgress);
            GenerateDefaultLabels(localData.Count, localData[0].Count);
            
            return localData;
        }

        static string GetCellValue(Cell c, WorkbookPart workbookPart)
        {
            string cellValue = string.Empty;
            if (c.DataType != null && c.DataType == CellValues.SharedString)
            {
                SharedStringItem ssi =
                    workbookPart.SharedStringTablePart.SharedStringTable
                        .Elements<SharedStringItem>()
                        .ElementAt(int.Parse(c.CellValue.InnerText));
                if (ssi.Text != null)
                {
                    cellValue = ssi.Text.Text;
                }
            }
            else
            {
                if (c.CellValue != null)
                {
                    cellValue = c.CellValue.InnerText;
                }
            }
            return cellValue;
        }


        static string GetCellValue(SpreadsheetDocument document, Cell cell)
        {
            SharedStringTablePart stringTablePart = document.WorkbookPart.SharedStringTablePart;
            string value = cell.CellValue.InnerXml;

            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                return stringTablePart.SharedStringTable.ChildElements[Int32.Parse(value)].InnerText;
            }
            else
            {
                return value;
            }
        }
        private void GenerateDefaultLabels(int dataCount,int rowCount)
        {
            int i;
            if (!genePosition)
            {
                if (labelSamples[0] != null && labelSamples[0].Count == 0)
                {
                    labelSamples[0] = new List<string>();
                    for (i = 0; i < dataCount; i++)
                        labelSamples[0].Add("Sample_" + (i + 1));
                }
                if (labelGenes[0] == null || labelGenes[0].Count == 0)
                {
                    labelGenes[0] = new List<string>();
                    for (i = 0; i < rowCount; i++)
                        labelGenes[0].Add("Gene_" + (i + 1));
                }
            }
            else
            {
                if (labelGenes[0] == null || labelGenes[0].Count == 0)
                {
                    labelGenes[0] = new List<string>();
                    for (i = 0; i < dataCount; i++)
                        labelGenes[0].Add("Gene_" + (i + 1));
                }
                if (labelSamples[0] != null && labelSamples[0].Count == 0)
                {
                    labelSamples[0] = new List<string>();
                    for (i = 0; i < rowCount; i++)
                        labelSamples[0].Add("Sample_" + (i + 1));

                }
            }

        }
        private void ProcessRow(string line,List<List<double>> data,char delimiter=' ')
        {
            List<double> row = new List<double>();
            line = Regex.Replace(line, @"\s+", " ");
            line = line.TrimEnd();
            string[] aux = line.Split(delimiter);
            if (aux.Length == 0)
                return;
            try
            {
                for (int i = numCol; i < aux.Length; i++)
                {
                    double tmp = 0;
                    tmp = Convert.ToDouble(aux[i], CultureInfo.InvariantCulture);
                    row.Add(tmp);
                }
                if (genePosition && uLabelGene)
                {
                    int count=0;

                    if(selectedGenes!=null)
                        if(!selectedGenes.ContainsKey(aux[labelGeneStart[0]-1]))
                            return;

                    foreach (var item in labelGeneStart)
                        labelGenes[count++].Add(aux[item - 1]);
                }
                else
                    if (!genePosition && uLabelSample)
                    {
                        int count = 0;
                        foreach (var item in labelSampleStart)
                            labelSamples[count++].Add(aux[item - 1]);
                        //Remove genes from row

                        if (indexSelectedGenes != null)
                            for (int i = 0; i < row.Count; i++)
                                if (!indexSelectedGenes.ContainsKey(i))
                                    row.RemoveAt(i);

                    }
                if (row.Count > 0)
                    data.Add(row);

            }
            catch (FormatException)
            {

            }

        }
        public List<List<double>> ReadOmicsFile(string fileName)
        {
            List<List<double>> localData = new List<List<double>>();

            if (Path.GetExtension(fileName).Contains("xlsx"))
                return ReadOmicsExcelFile(fileName);

            StreamReader r = new StreamReader(fileName);
            char delimiter=' ';
            if (Path.GetExtension(fileName).Contains("csv"))
                delimiter = ',';

            if (r == null)
                throw new Exception("Cannot open file: " + fileName);
            int i=0;
            
            string line= r.ReadLine();
            List<List<double>> data = new List<List<double>>();
            for (int  v= 0; v < labelGenes.Length;v++ )
               labelGenes[v].Clear();
            for (int v = 0; v < labelSamples.Length; v++)
                labelSamples[v].Clear();
            if(genePosition && uLabelSample || !genePosition && uLabelGene)
                i = ReadLabels(r,line,delimiter);

            for (; i < numRow-1; i++)
                line = r.ReadLine();

            currentProgress += 5;
            while (line != null)
            {
                if(line.Contains("\""))
                    line = Regex.Replace(line, "\"", "");
                ProcessRow(line, localData,delimiter);
                line = r.ReadLine();
            }
            r.Close();
            currentProgress += 15;
            GenerateDefaultLabels(localData.Count, localData[0].Count);
            
            return localData;
        }
        static double [,] TransposeData(double [,] data)
        {
            double [,]dataFinal = new double[data.GetLength(1), data.GetLength(0)];

            for (int i = 0; i < data.GetLength(0); i++)
                for (int j = 0; j < data.GetLength(1); j++)
                    dataFinal[j, i] = data[i, j];


                    return dataFinal;
        }
        void Check(double [,] data)
        {
            /*Console.WriteLine("Wiersze");
            for(int i=0;i<data.GetLength(0);i++)
            {
                double sum=0;                
                for (int j = 0; j < data.GetLength(1); j++)
                    sum += data[i, j];

                Console.WriteLine("sum="+sum);
            }*/
            Console.WriteLine("Columny");
            List<double> median = new List<double>();
            for (int j = 0; j < data.GetLength(1); j++)
            {
                double sum = 0;
                median.Clear();
                for (int i = 0; i < data.GetLength(0); i++)
                    median.Add(data[i, j]);
                    //sum += data[i, j];
                median.Sort((x, y) => x.CompareTo(y));
                sum = median[median.Count / 2];
                Console.WriteLine("sum=" + sum);
            }
        }
        double [,] SelectNMostDiff(double [,]data,int n)
        {
            double [,] resData=new double[n,data.GetLength(1)];
            double sumX;
            double sumX2;

            dev = new double[data.GetLength(0)];

            for (int i = 0; i < data.GetLength(0); i++)
            {
                sumX = 0;
                sumX2 = 0;
                for (int j = 0; j < data.GetLength(1); j++)
                    if (data[i, j] != double.NaN)
                    {
                        sumX += data[i, j];
                        sumX2 += data[i, j] * data[i, j];
                    }

                sumX /= data.GetLength(1);
                sumX2 /= data.GetLength(1);
                dev[i] = Math.Sqrt(sumX2 - avr[i] * avr[i]);

            }
        


            return resData;
        }
        public override int Run(object processParams)
        {
            string fileName = ((ThreadFiles)(processParams)).fileName;
            StreamReader r = new StreamReader(fileName);
            int i;

            if (heatmap)
                return 0;

            List<List<double>> data = ReadOmicsFile(fileName);

            dev = new double[data.Count];
            avr = new double[data.Count];


            double [,]dataFinal;
            dataFinal=new double [data.Count,data[0].Count];
            for (i = 0; i < data.Count; i++)
                for (int j = 0; j < data[i].Count; j++)
                        dataFinal[i, j] = data[i][j];

            if (genePosition)
            {
                List<string>[] temp;
                if (labelGenes[0].Count > 0)
                    temp = labelGenes;
                else
                    temp = labelSamples;

                labelsData = new List<string>();
                string s = "";
                for (int n = 0; n < temp[0].Count; n++)
                {
                    s = "";
                    for (int k = 0; k < temp.Length - 1; k++)
                        s += temp[k][n] + ";";
                    s += temp[temp.Length - 1][n];
                    labelsData.Add(s);
                }

            }

            if (!genePosition)
                dataFinal = TransposeData(dataFinal);
            //Check(dataFinal);
            if (zScore)
                dataFinal=StandardData(dataFinal,false,selectGenes);
            currentProgress += 20;
            if (quantile)
            {
                // dataFinal = TransposeData(dataFinal);
                dataFinal = QuantileNorm(dataFinal);
            //    dataFinal = TransposeData(dataFinal);
            }

            //if (zScore)
              //  StandardData(dataFinal);

            

            //if (!transpose)
           //     dataFinal = TransposeData(dataFinal);
            
            StreamWriter wr;
            string profFile = GetProfileName(fileName);

            wr = new StreamWriter(profFile);
           // dataFinal = TransposeData(dataFinal);
            double [,] outData=IntervalCoding(dataFinal);
            if (!genePosition)
                outData = TransposeData(outData);
            
            int l=0;
            for (i = 0; i < outData.GetLength(0); i++)
            {
                wr.WriteLine(">" + labelsData[i]);
                wr.Write(ProfileName + " ");
                for (l = 0; l < outData.GetLength(1) - 1; l++)
                    wr.Write((int)outData[i, l] + " ");
                wr.WriteLine((int)outData[i, l]);
            }
            wr.Close();
            currentProgress += 20;
            List<string>[] tx;
            if (!genePosition)            
                tx = labelGenes;
            else
                tx=labelSamples;
                

                labelsData = new List<string>();
                string st = "";
                for (int n = 0; n < tx[0].Count; n++)
                {
                    st = "";
                    for (int k = 0; k < tx.Length - 1; k++)
                        st += tx[k][n] + ";";
                    st += tx[tx.Length - 1][n];
                    labelsData.Add(st);
                }
            
/*            if (!genePosition)
                    labelsData = labelGenes[0];
            else
                    labelsData = labelSamples[0];*/

            wr.Close();
            string profFileTransp = profFile + "_transpose";
            wr = new StreamWriter(profFileTransp);
            for (i = 0; i < outData.GetLength(1); i++)
            {
                wr.WriteLine(">" + labelsData[i]);
                wr.Write(ProfileName + " ");
                for (l = 0; l < outData.GetLength(0) - 1; l++)
                    wr.Write((int)outData[l,i] + " ");
                wr.WriteLine((int)outData[l,i]);
            }
            wr.Close();
            currentProgress += 20;
            ProfileTree td=ProfileAutomatic.AnalyseProfileFile(profFile, SIMDIST.DISTANCE, ProfileName);
            List<string> keys=new List<string>(td.masterNode.Keys);

            ProfileTree ts = ProfileAutomatic.AnalyseProfileFile(profFile, SIMDIST.SIMILARITY, ProfileName);
                string locPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "profiles" + Path.DirectorySeparatorChar;
                td.SaveProfiles(locPath + Path.GetFileNameWithoutExtension(fileName) + "_distance.profile");
                ts.SaveProfiles(locPath + Path.GetFileNameWithoutExtension(fileName) + ".profiles");
                profSize = td.masterNode[keys[0]].codingToByte.Count;

                List<string> m = new List<string>(ts.masterNode.Keys);
                localNode = ts.masterNode[m[0]];
                localNodeDist = td.masterNode[m[0]];

                currentProgress = maxV;
            return 0;

        }
        void PrepareStandardParams(double [,]data)
        {
            double sumX = 0;
            double sumX2 = 0;

            dev = new double[data.GetLength(0)];
            avr = new double[data.GetLength(0)];

            for (int i = 0; i < data.GetLength(0); i++)
            {
                sumX = 0;
                sumX2 = 0;
                for (int j = 0; j < data.GetLength(1); j++)
                    if (data[i, j] != double.NaN)
                    {
                        sumX += data[i, j];
                        sumX2 += data[i, j] * data[i, j];
                    }

                sumX /= data.GetLength(1);
                sumX2 /= data.GetLength(1);
                avr[i] = sumX;
                dev[i] = Math.Sqrt(sumX2 - avr[i] * avr[i]);
            }

        }
        double [,] StandardData(double [,]data,bool externalDataFlag=false,int nData=0)
        {
            if(!externalDataFlag)
                PrepareStandardParams(data);

            if (nData > data.GetLength(0))
                nData = 0;

            for (int i = 0; i < data.GetLength(0); i++)
            {

                    for (int j = 0; j < data.GetLength(1); j++)
                        if (data[i, j] != double.NaN && dev[i] > 0)
                            data[i, j] = (data[i, j] - avr[i]) / dev[i];
            }

            if(nData==0)            
                return data;           
            else
            {
                int[] index = new int[dev.Length];
                for (int i = 0; i < dev.Length; i++)
                    index[i] = i;

                Array.Sort(dev, index);
                Array.Reverse(index);
                double[,] locData = new double[nData, data.GetLength(1)];
                for(int i=0;i<nData;i++)
                {
                    for(int j=0;j<data.GetLength(1);j++)                    
                        locData[i, j] = data[index[i], j];                    
                }
                return locData;
            }
            
        }

        static double[,] LoadIntervals(string fileName)
        {
            double[,] intervals=null;
            StreamReader r = new StreamReader(fileName);
            string line = r.ReadLine();
            int num=0;
            while(line!=null)
            {
                if (line.Contains("Code"))
                    num++;
                line = r.ReadLine();
            }

            intervals = new double[num, 2];
            r.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
            num=0;
            line = r.ReadLine();
            while (line != null)
            {
                if (line.Contains("Code"))
                {
                    string[] aux = line.Split(' ');
                    if (aux.Length == 4)
                    {
                        intervals[num, 0] = Convert.ToDouble(aux[2]);
                        intervals[num++, 0] = Convert.ToDouble(aux[3]);
                    }
                }
                line = r.ReadLine();
            }            
            r.Close();

            return intervals;
        }
        static double [,] IntervalCodig(double[,] data,double [,] intervals)
        {
            double[,] newData = new double[data.GetLength(0), data.GetLength(1)];
            int[] codedRow = new int[newData.GetLength(1)];
            for (int i = 0; i < data.GetLength(0); i++)
            {
                for (int j = 0; j < data.GetLength(1); j++)
                {
                    int code = intervals.GetLength(0);

                    if (data[i, j] < intervals[0, 0])
                    {
                        code = 1;
                        continue;
                    }
                    if(data[i,j]>intervals[intervals.GetLength(0)-1,1])
                    {
                        code = intervals.Length - 1;
                        continue;
                    }
                    for (int k = 0; k < intervals.GetLength(0); k++)
                    {                        
                        if (data[i, j] == double.NaN)
                        {
                            code = 0;
                            break;
                        }
                        if (data[i, j] >= intervals[k, 0] && data[i, j] < intervals[k, 1])
                        {
                            code = k + 1;
                            break;
                        }
                    }
                    codedRow[j] = code;
                }
                for (int n = 0; n < newData.GetLength(1); n++)
                    newData[i, n] = codedRow[n];

            }

            return newData;
        }

        

        double [,] IntervalCoding(double [,] data)
        {
            double[,] newData = data;
            HashSet<double> hashValues = new HashSet<double>();
            double []colValues = new double [data.GetLength(1)];
            double[,] intervals;
            double[,] outData;
            if (coding == CodingAlg.Z_SCORE)
                newData=ZscoreCoding(data);
            
            for (int j = 0; j < newData.GetLength(0); j++)
            {
                for (int i = 0; i < newData.GetLength(1); i++)
                {
                    colValues[i] = data[j, i];

                    if (!hashValues.Contains(data[j, i]))
                        hashValues.Add(data[j,i]);
                }

                Array.Sort(colValues);
            }

            int nn=0;
            colValues = new double[hashValues.Count];
            foreach (var item in hashValues)
                colValues[nn++] = item;
            Array.Sort(colValues);
            intervals = SetupIntervals(colValues);

            
            StreamWriter ir;
            if(processName.Length>0)
                ir = new StreamWriter("generatedProfiles/OmicsIntervals_"+processName+".dat");
            else
                ir = new StreamWriter("generatedProfiles/OmicsIntervals_.dat");
            foreach (var item in labId)
                ir.WriteLine("Label " + item);
            for (int i = 0; i < intervals.GetLength(0); i++)
                ir.WriteLine("Code " + i + " " + intervals[i,0] + " " + intervals[i,1]);
            ir.Close();

            outData = IntervalCodig(data, intervals);

            return outData;
        }
        double[, ] SetupIntervals(double []dataValues)
        {
            double [,] intervals=new double [numStates,2];
            double max, min;
            max = double.MinValue;
            min = double.MaxValue;
            for (int i = 0; i < dataValues.Length; i++)
            {
                if (max < dataValues[i])
                    max = dataValues[i];
                if (min > dataValues[i])
                    min = dataValues[i];
            }

            switch(coding)
            {
                case CodingAlg.EQUAL_DIST:
                case CodingAlg.Z_SCORE:
                    double step = (max - min) / numStates;
                    for (int i = 0; i < numStates; i++)
                    {
                        intervals[i, 0] = dataValues[0] + i * step;
                        intervals[i, 1] = dataValues[0] + (i + 1) * step;
                    }
                    break;
                case CodingAlg.PERCENTILE:                
                    double size = (double)dataValues.Length / numStates;
                    int num=0;
                    int rem = 0;
                    int n = 0;
                    for(n=0;n<dataValues.Length;n++)
                    {
                        if(n>=size*(num+1))
                        {
                            while (n + 1 < dataValues.Length && dataValues[n] == dataValues[n+1] || dataValues[n]==double.NaN) n++;
                            
                            if (n < dataValues.Length)
                            {
                                intervals[num, 1] = dataValues[n];
                                intervals[num, 0] = dataValues[rem];
                            }
                      
                            rem = n;
                            num++;
                        }
                    }
                    if (num < numStates)
                    {
                        intervals[num, 0] = dataValues[rem];
                        intervals[num, 1] = max;
                    }

                    break;
            }
            return intervals;

        }

        static double [,] ZscoreCoding(double [,] data)
        {
            double[,] newData = new double[data.GetLength(0), data.GetLength(1)];
            double []colValues = new double [data.GetLength(0)];
            double[] stdDev = new double[data.GetLength(1)];
            double[] avr = new double[data.GetLength(1)];
            for (int j = 0; j < data.GetLength(1); j++)
            {
                for (int i = 0; i < data.GetLength(0); i++)                
                    colValues[i]=data[i, j];                

                double sumX2=0, sumX=0;

                foreach(var item in colValues)
                {
                    if (item != double.NaN)
                    {
                        sumX2 += item * item;
                        sumX += item;
                    }
                }
                avr[j] = sumX / colValues.Length;

                stdDev[j] = Math.Sqrt(sumX2 / colValues.Length - avr[j]  * avr[j]);
                
                for(int i=0;i<data.GetLength(0);i++)                
                    if(data[i,j]!=double.NaN)
                        newData[i,j]= (data[i, j] - avr[j]) / stdDev[j];
                               
            }
            return newData;
        }
        public static List<string> GetOrderedProfiles(string fileName)
        {
            List<string> order = new List<string>();
            StreamReader st = new StreamReader(fileName);
            string line = st.ReadLine();

            while(line!=null)
            {
                if(line.Contains(">"))
                {
                    string name = line.Replace(">", "");
                    order.Add(name);
                }
                line = st.ReadLine();
            }

            return order;
        }
        public override Dictionary<string, protInfo> GetProfile(profileNode node, string fileNameProf)
        {
            Dictionary<string, protInfo> data;
            string fileName = GetProfileName(fileNameProf);

            if (heatmap || transpose)
                fileName = fileName + "_transpose";

            ProfileTree ts = new ProfileTree();
            string locPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "profiles" + Path.DirectorySeparatorChar;
            ts.LoadProfiles(locPath + Path.GetFileNameWithoutExtension(fileNameProf) + ".profiles");
            List<string> k = new List<string>(ts.masterNode.Keys);
            localNode = ts.masterNode[k[0]];
            if (node.profWeights.Count == localNode.profWeights.Count)
            {
                data = base.GetProfile(localNode, fileName);
            }
            else
            {
                ts.LoadProfiles(locPath + Path.GetFileNameWithoutExtension(fileNameProf) + "_distance.profile");
                k = new List<string>(ts.masterNode.Keys);
                localNode = ts.masterNode[k[0]];
                data = base.GetProfile(localNode, fileName);
            }
         
            return data;
        }
    }
}
