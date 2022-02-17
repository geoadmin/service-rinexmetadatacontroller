using Rinex3Parser.Common;
using Rinex3Parser.Obs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml;

namespace RinexMetaDataController
{
    class Program
    {
        private static List<String> completeList = new List<String>();
        private static LogWriter lwSingleton = LogWriter.GetInstance;
        private static Stopwatch stopwatchStep;
        private static Stopwatch stopwatchTotal;
        private static DateTime dateTime;
        private static DBHelper dbH;
        private static bool isParallel = false;
        private static bool isShop = false;
        private static bool isWriteXML = false;
        private static int deleteInputData = 0;
        private static string dateArchivPath = "";
        private static string dateRinexInputPath = "";
        private static string dateShopPath = "";
        private static string dateXMLPath = "";
        private static string alreadyRecordedObservations;
        private static string existingName;
        private static string existingFirstObservation;
        private static readonly string rinexInputPath = ConfigurationManager.AppSettings["RinexPath"];
        private static readonly string dbPath = ConfigurationManager.AppSettings["DBPath"];
        private static readonly string dbFile = ConfigurationManager.AppSettings["DBFile"];
        private static readonly string shopPath = ConfigurationManager.AppSettings["ShopPath"];
        private static readonly string rinexFileExtension = ConfigurationManager.AppSettings["RinexFileExtension"];
        private static readonly string XMLPath = ConfigurationManager.AppSettings["XMLPath"];
        private static readonly string workPath = ConfigurationManager.AppSettings["WorkPath"];
        private static readonly string archivPath = ConfigurationManager.AppSettings["ArchivPath"];
        private static readonly string rinexSuffix = ConfigurationManager.AppSettings["RinexSuffix"];

        static void Main(string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    Usage(args);
                }
                else
                {
                    LogWriter.WriteToLog(string.Format("===============================Start=============================="));
                    Process processes = Process.GetCurrentProcess();
                    processes.PriorityClass = ProcessPriorityClass.Normal;
                    stopwatchStep = new Stopwatch();
                    stopwatchTotal = new Stopwatch();
                    Processing();
                    LogWriter.WriteToLog(string.Format("=================================End=============================="));
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
                LogWriter.WriteToLog(e);
                LogWriter.WriteToLog(string.Format("=================================End=============================="));
            }
            finally
            {
                if (Directory.Exists(workPath))
                {
                    Directory.Delete(workPath, true);
                }
                LogWriter.ForceFlush();
            }
        }

        private static void Processing()
        {
            stopwatchTotal.Start();

            bool.TryParse(ConfigurationManager.AppSettings["Parallel"], out isParallel);
            int.TryParse(ConfigurationManager.AppSettings["Delete"], out deleteInputData);
            bool.TryParse(ConfigurationManager.AppSettings["Shop"], out isShop);
            bool.TryParse(ConfigurationManager.AppSettings["WriteXML"], out isWriteXML);

            if ((ConfigurationManager.AppSettings["SpecificDateYear"].ToString().Length > 0) &&
                (ConfigurationManager.AppSettings["SpecificDateMonth"].ToString().Length > 0) &&
                (ConfigurationManager.AppSettings["SpecificDateDay"].ToString().Length > 0))
            {
                int year = 0;
                int month = 0;
                int day = 0;
                if (int.TryParse(ConfigurationManager.AppSettings["SpecificDateYear"], out year) &&
                    int.TryParse(ConfigurationManager.AppSettings["SpecificDateMonth"], out month) &&
                    int.TryParse(ConfigurationManager.AppSettings["SpecificDateDay"], out day))
                {
                    dateTime = new DateTime(year, month, day);
                    LogWriter.WriteToLog(string.Format("Specific Day set, setting 'DaysBack' will be ignored!"));
                    ProcessingDay();
                }
                else
                {
                    throw new Exception("DateTime Parse error!");
                }
            }
            else
            {
                dateTime = DateTime.Now;
                int numDaysBack = 0;
                int.TryParse(ConfigurationManager.AppSettings["DaysBack"], out numDaysBack);
                for (int days = 0; days <= numDaysBack; days++)
                {
                    dateTime = DateTime.Now.AddDays(-days);
                    ProcessingDay();
                }
            }
            LogWriter.WriteToLog(string.Format("Totaltime to process : {0:hh\\:mm\\:ss}", stopwatchTotal.Elapsed));
            stopwatchTotal.Stop();
        }

        private static void Usage(string[] args)
        {

            if (args[0].Length > 0)
            {
                Console.WriteLine("Processes Rinex 3 files in order to push the right files to the Rinex Shop and Rinex Archiv.");
                Console.WriteLine("During the processing the Rinex files are complet read and analysed.");
                Console.WriteLine("With the read data a DB and XML is written.");
                Console.WriteLine("");
                Console.WriteLine("RinexMetaDataController [/?] [/help]");
                Console.WriteLine("");
                Console.WriteLine("/? /help     Shows this help.");
                Console.WriteLine("");
                Console.WriteLine("No Parameter are needed to start the processing.");
                Console.WriteLine("All configurations are read from the .config file.");
                Console.WriteLine("The configuration for RinexMetaDataController can be found at the install folder (.config files).");
            }
        }

        private static void InitDay()
        {
            string dateTimePath = String.Format("RefData.{0}\\Month.{1}\\Day.{2:D2}", dateTime.Year % 100, CultureInfo.CreateSpecificCulture("en").DateTimeFormat.GetAbbreviatedMonthName(dateTime.Month), dateTime.Day);
            dateRinexInputPath = Path.Combine(rinexInputPath, dateTimePath);
            dateShopPath = Path.Combine(shopPath, dateTimePath);
            dateArchivPath = Path.Combine(archivPath, dateTimePath);
            dateXMLPath = Path.Combine(XMLPath, dateTimePath);
            if (!Directory.Exists(dateArchivPath))
            {
                Directory.CreateDirectory(dateArchivPath);
            }
            if (!Directory.Exists(workPath))
            {
                Directory.CreateDirectory(workPath);
            }
            if (!Directory.Exists(Path.Combine(workPath, "RNX")))
            {
                Directory.CreateDirectory(Path.Combine(workPath, "RNX"));
            }
            if (!Directory.Exists(dateShopPath))
            {
                Directory.CreateDirectory(dateShopPath);
            }
            dbH = new DBHelper(Path.Combine(dbPath, String.Format("{0}_{1}", DateTime.Now.ToString("yyyy"), dbFile)));
        }

        private static void ProcessingDay()
        {
            InitDay();
            List<FileInfo> rinexInputFiles = new List<FileInfo>();
            List<FileInfo> inputOBSRNXFiles = new List<FileInfo>();
            List<FileInfo> inputNAVFiles = new List<FileInfo>();

            stopwatchStep.Start();
            LogWriter.WriteToLog(string.Format("Processing Day:{0}", dateTime.ToShortDateString()));

            // UNZIP Process
            DirectoryInfo dateRinexInputDIR = new DirectoryInfo(dateRinexInputPath);
            if (!dateRinexInputDIR.Exists)
            {
                LogWriter.WriteToLog(string.Format("Input directory does not exist:{0}", dateRinexInputPath));
            }
            else
            {
                FileInfo[] rinexInputZIPFiles = dateRinexInputDIR.GetFiles(ConfigurationManager.AppSettings["ZipFileExtension"]);
                rinexInputFiles = new List<FileInfo>(rinexInputZIPFiles);
                if (rinexInputZIPFiles.Count() > 0)
                {
                    if (isParallel)
                    {
                        Parallel.For(0, rinexInputZIPFiles.Length, i =>
                        {
                            Unzip(rinexInputZIPFiles[i]);
                        });
                    }
                    else
                    {
                        foreach (FileInfo file in rinexInputZIPFiles)
                        {
                            Unzip(file);
                        }
                    }
                    LogWriter.WriteToLog(string.Format("Time to uncompress ZIP : {0:hh\\:mm\\:ss}", stopwatchStep.Elapsed));
                    stopwatchStep.Restart();

                    // Uncompress HATANAKA
                    DirectoryInfo[] workPathDIRS = new DirectoryInfo(workPath).GetDirectories();
                    foreach (DirectoryInfo workDIR in workPathDIRS)
                    {
                        FileInfo[] crxFiles = workDIR.GetFiles(ConfigurationManager.AppSettings["CompactRinexFileExtension"]);
                        foreach (FileInfo crxFile in crxFiles)
                        {
                            string filenameExtension = Path.GetExtension(crxFile.FullName);
                            string crxFileFullName = crxFile.FullName;
                            string rnxFile = UncompressHatanaka(crxFileFullName);
                            if (rnxFile.Length > 0)
                            {
                                File.Copy(rnxFile, Path.Combine(Path.Combine(workPath, "RNX"), Path.GetFileName(rnxFile)), true);
                                File.Delete(crxFileFullName);
                            }
                        }
                    }
                    LogWriter.WriteToLog(string.Format("Time to uncompress Hatanaka: {0:hh\\:mm\\:ss}", stopwatchStep.Elapsed));
                    stopwatchStep.Restart();
                }
                else
                {
                    //Files not compressed
                    FileInfo[] inputRNXFiles = dateRinexInputDIR.GetFiles(ConfigurationManager.AppSettings["RinexFileExtension"]);
                    rinexInputFiles = new List<FileInfo>(inputRNXFiles);
                    foreach (FileInfo inputRNXFile in inputRNXFiles)
                    {
                        if (!IsFileInUse(inputRNXFile.FullName))
                        {
                            inputRNXFile.CopyTo(Path.Combine(Path.Combine(workPath, "RNX"), inputRNXFile.Name),true);
                            string rnxShortName = ConvertToRinexShortName(inputRNXFile.Name);
                            Directory.CreateDirectory(Path.Combine(workPath, rnxShortName + ".daf"));
                            inputOBSRNXFiles.Add(inputRNXFile);
                        }
                        else
                        {
                            //Current Rinex File
                            rinexInputFiles.Remove(inputRNXFile);
                            LogWriter.WriteToLog("File in use: " + inputRNXFile.FullName);
                        }
                    }
                    foreach (FileInfo inputOBSRNXFile in inputOBSRNXFiles)
                    {
                        DirectoryInfo dafiRNXDIR = new DirectoryInfo(dateRinexInputPath);
                        FileInfo[] dafiRNXFiles = dafiRNXDIR.GetFiles(Path.GetFileNameWithoutExtension(inputOBSRNXFile.Name).Substring(0, inputOBSRNXFile.Name.Length - 11) + "*");
                        string rnxShortName = ConvertToRinexShortName(inputOBSRNXFile.Name);
                        foreach (FileInfo dafiRNXFile in dafiRNXFiles)
                        {
                            if (!dafiRNXFile.Name.Contains(ConfigurationManager.AppSettings["rinexFileExtension"].ToString().Substring(1)))
                            {
                                inputNAVFiles.Add(dafiRNXFile);
                            }
                            dafiRNXFile.CopyTo(Path.Combine(Path.Combine(workPath, rnxShortName + ".daf"), dafiRNXFile.Name),true);
                        }
                    }
                }

                // Analyse RINEX
                DirectoryInfo workRNXDIR = new DirectoryInfo(Path.Combine(workPath, "RNX"));
                FileInfo[] workRNXFiles = workRNXDIR.GetFiles(ConfigurationManager.AppSettings["rinexFileExtension"]);
                foreach (FileInfo workRNXFile in workRNXFiles)
                {
                    ParseRinex(workRNXFile.FullName);
                    workRNXFile.Delete();
                }
                Directory.Delete(Path.Combine(workPath, "RNX"));
                LogWriter.WriteToLog(string.Format("Time to analyse Rinex: {0:hh\\:mm\\:ss}", stopwatchStep.Elapsed));
                stopwatchStep.Restart();

                // ZIP for ARCHIV
                DirectoryInfo[] workActDIRS = new DirectoryInfo(workPath).GetDirectories();
                UncompletedHours(workActDIRS);
                workActDIRS = new DirectoryInfo(workPath).GetDirectories();
                if (isParallel)
                {
                    Parallel.For(0, workActDIRS.Length, i =>
                    {
                        Zip(workActDIRS[i]);
                    });
                }
                else
                {
                    foreach (DirectoryInfo workActDIR in workActDIRS)
                    {
                        Zip(workActDIR);
                    }
                }
                LogWriter.WriteToLog(string.Format("Time to compress ZIP: {0:hh\\:mm\\:ss}", stopwatchStep.Elapsed));
                stopwatchStep.Restart();

                // Delete processed input Files
                foreach (FileInfo rinexInputFile in rinexInputFiles)
                {
                    if (deleteInputData == 1)
                    {
                        File.Delete(rinexInputFile.FullName);
                    }
                    if (deleteInputData == 2)
                    {
                        if (CleanUpFile(rinexInputFile.FullName))
                        {
                            File.Delete(rinexInputFile.FullName);
                        }
                    }
                }
                // Delete navigation input Files if inputfiles were not compressed
                foreach (FileInfo inputNAVFile in inputNAVFiles)
                {
                    if (deleteInputData == 1 || deleteInputData == 2)
                    {
                        File.Delete(inputNAVFile.FullName);
                    }
                }

                LogWriter.WriteToLog(string.Format("Time to cleanup: {0:hh\\:mm\\:ss}", stopwatchStep.Elapsed));
                stopwatchStep.Restart();
                Directory.Delete(workPath, true);
                LogWriter.WriteToLog(string.Format("Total {0} Files processed.", rinexInputFiles.Count));
            }
            stopwatchStep.Reset();
        }

        private static void CheckName(string workzipfolder)
        {
            DirectoryInfo workZipFolder = new DirectoryInfo(workzipfolder);
            FileInfo[] workRNXFiles = workZipFolder.GetFiles();
            foreach (FileInfo workRNXFile in workRNXFiles)
            {
                string rnxShortName = ConvertToRinexShortName(workRNXFile.Name);
                string restOfName = workRNXFile.Name.Substring(9, workRNXFile.Name.Length - 9);
                if (!workZipFolder.Name.Substring(0, workZipFolder.Name.Length - 7).Equals(workRNXFile.Name.Substring(0, workRNXFile.Name.Length - 7)))
                {
                    if ((!String.Format("{0}{1}", rnxShortName, rinexSuffix).Equals(workZipFolder.Name)) && (!workRNXFile.Name.Equals(string.Format("{0}00CHE{1}", workZipFolder.Name.Substring(0, 4), restOfName))))
                    {
                        LogWriter.WriteToLog(string.Format("Warning: Renamed Rinexlongname from: {0} to {1}", workRNXFile.Name, string.Format("{0}00CHE{1}", workZipFolder.Name.Substring(0, 4), restOfName)));
                        workRNXFile.MoveTo(Path.Combine(workRNXFile.DirectoryName, string.Format("{0}00CHE{1}", workZipFolder.Name.Substring(0, 4), restOfName)));
                    }
                }
            }
        }

        private static bool IsFileInUse(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("'path' cannot be null or empty.", "path");

            try
            {
                using (StreamReader sreee = new StreamReader(new FileStream(path, FileMode.Open, FileAccess.Read), System.Text.Encoding.UTF8))
                {
                    string line = String.Empty;
                    while ((line = sreee.ReadLine()) != null)
                    {
                        if (line.StartsWith(">"))
                        {
                            if (int.TryParse(line.Substring(13, 2), out int rinexhour))
                            {
                                if (rinexhour == DateTime.Now.Hour)
                                {
                                    sreee.Close();
                                    return true;
                                }
                                else
                                {
                                    sreee.Close();
                                    return false;
                                }
                            }
                            else
                            {
                                LogWriter.WriteToLog("Could not parse hour in Line: " + line);
                                sreee.Close();
                                return true;
                            }
                        }
                    }
                }
            }
            catch (IOException)
            {
                return true;
            }
            return false;
        }

        private static void ParseRinex(string file)
        {
            try
            {

                RinexObsParser rinexObsParser = new RinexObsParser(file, ParseType.StoreData);
                rinexObsParser.Parse();
                GnssObservation gpsObs;
                GnssObservation gloObs;
                GnssObservation galObs;
                GnssObservation bdsObs;
                Dictionary<SatelliteSystem, GnssObservation> ObsMetaData = rinexObsParser.ObsHeader.ObsHeaderData.ObsMetaData;
                ObsMetaData.TryGetValue(SatelliteSystem.Gps, out gpsObs);
                ObsMetaData.TryGetValue(SatelliteSystem.Glo, out gloObs);
                ObsMetaData.TryGetValue(SatelliteSystem.Gal, out galObs);
                ObsMetaData.TryGetValue(SatelliteSystem.Bds, out bdsObs);
                HeaderInfo headerInfoRecord = new HeaderInfo(rinexObsParser.ObsHeader.ObsHeaderData.Version,
                    ConvertToRinexShortName(Path.GetFileNameWithoutExtension(file)),
                    Path.GetFileNameWithoutExtension(file),
                    rinexObsParser.ObsHeader.ObsHeaderData.MarkerName,
                    rinexObsParser.ObsHeader.ObsHeaderData.X,
                    rinexObsParser.ObsHeader.ObsHeaderData.Y,
                    rinexObsParser.ObsHeader.ObsHeaderData.Z,
                    rinexObsParser.ObsHeader.ObsHeaderData.Interval.GetValueOrDefault(0),
                    rinexObsParser.ObsHeader.ObsHeaderData.FirstObs.ToString(),
                    rinexObsParser.ObservationRecords.Last().Key.ApproximateDateTime.ToString(),
                    rinexObsParser.ObservationRecords.Count,
                    rinexObsParser.ObsHeader.ObsHeaderData.NumberOfSatellites,
                    rinexObsParser.ObsHeader.ObsHeaderData.Observer.ToString(),
                    rinexObsParser.ObsHeader.ObsHeaderData.RcvInfo.RcvType.ToString(),
                    rinexObsParser.ObsHeader.ObsHeaderData.RcvInfo.Number.ToString(),
                    rinexObsParser.ObsHeader.ObsHeaderData.RcvInfo.Version.ToString(),
                    rinexObsParser.ObsHeader.ObsHeaderData.GnssTimeSystem.ToString(),
                    rinexObsParser.ObsHeader.ObsHeaderData.AntInfo.Type.ToString(),
                    rinexObsParser.ObsHeader.ObsHeaderData.AntInfo.Number.ToString(),
                    rinexObsParser.ObsHeader.ObsHeaderData.AntInfo.DeltaE,
                    rinexObsParser.ObsHeader.ObsHeaderData.AntInfo.DeltaN,
                    rinexObsParser.ObsHeader.ObsHeaderData.AntInfo.DeltaH,
                    rinexObsParser.ObsHeader.ObsHeaderData.Agency.ToString(),
                    rinexObsParser.ObsHeader.ObsHeaderData.SatelliteSystem.ToString(),
                    rinexObsParser.ObsHeader.ObsHeaderData.HeaderProgramInfo.AgencyInfo.ToString(),
                    rinexObsParser.ObsHeader.ObsHeaderData.HeaderProgramInfo.Name.ToString(),
                    rinexObsParser.ObsHeader.ObsHeaderData.HeaderProgramInfo.FileCreationDateTime.ToString(),
                    gpsObs.Observations.Count,
                    gloObs.Observations.Count,
                    galObs.Observations.Count,
                    bdsObs.Observations.Count,
                    GetChecksum(file),
                    GetChecksumMD5(file),
                    isShop);
                dbH.loadDBRecord(headerInfoRecord);
                if (isWriteXML)
                {
                    string name = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(file));
                    string shortfilename = string.Format("{0}.xml", ConvertToRinexShortName(name));
                    XMLHelper xml = new XMLHelper(dateXMLPath, shortfilename);
                    if (File.Exists(Path.Combine(dateXMLPath, shortfilename)))
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(Path.Combine(dateXMLPath, shortfilename));
                        XmlNode nodeName = doc.DocumentElement.SelectSingleNode("/VectorObject/Name");
                        XmlNode nodeNumberOfObservations = doc.DocumentElement.SelectSingleNode("/VectorObject/NumberOfObservations");
                        XmlNode nodeFirstObservation = doc.DocumentElement.SelectSingleNode("/VectorObject/FirstObservation");
                        existingName = nodeName.InnerText;
                        alreadyRecordedObservations = nodeNumberOfObservations.InnerText;
                        existingFirstObservation = nodeFirstObservation.InnerText;
                        if (existingName == headerInfoRecord.Name && existingFirstObservation.Substring(11, 2) == headerInfoRecord.FirstObservation.Substring(11, 2))
                        {
                            int nrObs = 0;
                            int exFristObs;
                            int curFristObs;
                            int.TryParse(alreadyRecordedObservations, out nrObs);
                            int.TryParse(existingFirstObservation.Substring(14, 2), out exFristObs);
                            int.TryParse(headerInfoRecord.FirstObservation.Substring(14, 2), out curFristObs);
                            if (exFristObs < curFristObs)
                            {
                                headerInfoRecord.NumberOfObservations += nrObs;
                                headerInfoRecord.FirstObservation = existingFirstObservation;
                                xml.Commentary = "Merged XML";
                                LogWriter.WriteToLog(String.Format("Warning: Merge XML {0}", shortfilename));
                            }
                        }
                    }
                    xml.writeToXML(headerInfoRecord);
                }
                if (rinexObsParser.ObservationRecords.Count == 3600)
                {
                    completeList.Add(Path.GetFileNameWithoutExtension(file));
                }
                else
                {
                    LogWriter.WriteToLog(String.Format("Warning: Observations for {0} not complete.", Path.GetFileNameWithoutExtension(file)));
                }
            }
            catch (Exception e)
            {
                LogWriter.WriteToLog(String.Format("Error: ParseRinex did not work -> {0}!.", e.Message));
            }
        }

        private static void Unzip(FileInfo zipfile)
        {
            try
            {
                ZipFile.ExtractToDirectory(zipfile.FullName, Path.Combine(workPath, Path.GetFileNameWithoutExtension(zipfile.Name)));
                CheckName(Path.Combine(workPath, Path.GetFileNameWithoutExtension(zipfile.Name)));
                if (isShop)
                {
                    DirectoryCopy(Path.Combine(workPath, Path.GetFileNameWithoutExtension(zipfile.Name)), dateShopPath, false);
                }
            }
            catch (Exception e)
            {
                if (e.HResult == -2146233087)
                {
                    Process proc = new Process();
                    proc.StartInfo.CreateNoWindow = true;
                    proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    proc.StartInfo.FileName = "cmd.exe";
                    proc.StartInfo.Arguments = String.Format("/C unzip.exe -o {0} -d {1}", zipfile.FullName, Path.Combine(workPath, Path.GetFileNameWithoutExtension(zipfile.Name)));
                    proc.Start();
                    proc.WaitForExit();
                    proc.StartInfo.Arguments = String.Format("/C unzip.exe -o {0} -d {1}", zipfile.FullName, dateShopPath);
                    proc.Start();
                    proc.WaitForExit();
                }
                LogWriter.WriteToLog(String.Format("Warning: Unzip Problem {0}. Message = {1}", zipfile.FullName, e.Message));
            }
        }

        private static string ConvertToRinexShortName(string name)
        {
            string stationName = name.Substring(0, 4);
            string dayofYear = name.Substring(16, 3);
            string hour = name.Substring(19, 2);
            if (!name.Substring(21, 2).Equals("00"))
            {
                LogWriter.WriteToLog(String.Format("Warning: Hour in Rinexlongname not complete {0}.", name));
            }
            return string.Format("{0}{1}{2}", stationName, dayofYear, convertHourNumberToLetter(hour));
        }


        private static void UncompletedHours(DirectoryInfo[] dirInfo)
        {
            List<String> uncompleteHourList = new List<String>();
            foreach (DirectoryInfo dir in dirInfo)
            {
                if (dir.Name.Length > 12)
                {
                    if (!dir.Name.Substring(21, 2).Equals("00"))
                    {
                        string fullHour = string.Format("{0}00", dir.Name.Substring(19, 2));
                        string preName = dir.Name.Substring(0, 19);
                        string postName = dir.Name.Substring(23, dir.Name.Length - 23);
                        MoveFiles(dir.FullName, Path.Combine(workPath, String.Format("{0}{1}{2}", preName, fullHour, postName)));
                        uncompleteHourList.Add(dir.FullName);
                        LogWriter.WriteToLog(String.Format("Warning: Uncompleted Hour: {0}", dir.FullName));
                    }
                }
            }
            foreach (string uncompleteDir in uncompleteHourList)
            {
                Directory.Delete(uncompleteDir);
            }
        }

        private static void Zip(DirectoryInfo workPathNow)
        {
            try
            {
                if (Path.GetExtension(workPathNow.Name).Equals(".crx"))
                {
                    string name = Path.GetFileNameWithoutExtension(workPathNow.Name);
                    string shortfilename = string.Format("{0}.zip", ConvertToRinexShortName(name));
                    if (File.Exists(Path.Combine(dateArchivPath, shortfilename)))
                    {
                        File.Delete(Path.Combine(dateArchivPath, shortfilename));
                    }
                    ZipFile.CreateFromDirectory(workPathNow.FullName, Path.Combine(dateArchivPath, shortfilename));
                }
                else if (Path.GetExtension(workPathNow.Name).Equals(".daf"))
                {
                    string name = Path.GetFileNameWithoutExtension(workPathNow.Name);
                    string filename = string.Format("{0}.zip", name);
                    if (File.Exists(Path.Combine(dateArchivPath, filename)))
                    {
                        File.Delete(Path.Combine(dateArchivPath, filename));
                    }
                    ZipFile.CreateFromDirectory(workPathNow.FullName, Path.Combine(dateArchivPath, filename));
                }
                else
                {
                    string zipfilename = "";
                    if (workPathNow.FullName.EndsWith(rinexSuffix))
                    {
                        zipfilename = workPathNow.Name.Substring(0, workPathNow.Name.Length - 1);
                        if (File.Exists(Path.Combine(dateArchivPath, string.Format("{0}.zip", zipfilename))))
                        {
                            File.Delete(Path.Combine(dateArchivPath, string.Format("{0}.zip", zipfilename)));
                        }
                        ZipFile.CreateFromDirectory(workPathNow.FullName, Path.Combine(dateArchivPath, string.Format("{0}.zip", zipfilename)));
                    }
                    else
                    {
                        throw new Exception("Error: RinexSuffix not OK!");
                    }
                }
            }
            catch (Exception e)
            {
                LogWriter.WriteToLog(String.Format("Problem with ZIP: {0}. WorkPath:{1}", e, workPathNow.FullName));
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }
            DirectoryInfo[] dirs = dir.GetDirectories();
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                file.LastWriteTimeUtc = DateTime.Now;
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }

        private static void MoveFiles(string sourceDirName, string destDirName)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }
            DirectoryInfo[] dirs = dir.GetDirectories();
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                file.LastWriteTimeUtc = DateTime.Now;
                string temppath = Path.Combine(destDirName, file.Name);
                file.MoveTo(temppath);
            }
        }

        private static string convertHourNumberToLetter(string hour)
        {
            switch (hour)
            {
                case "00":
                    return "a";
                case "01":
                    return "b";
                case "02":
                    return "c";
                case "03":
                    return "d";
                case "04":
                    return "e";
                case "05":
                    return "f";
                case "06":
                    return "g";
                case "07":
                    return "h";
                case "08":
                    return "i";
                case "09":
                    return "j";
                case "10":
                    return "k";
                case "11":
                    return "l";
                case "12":
                    return "m";
                case "13":
                    return "n";
                case "14":
                    return "o";
                case "15":
                    return "p";
                case "16":
                    return "q";
                case "17":
                    return "r";
                case "18":
                    return "s";
                case "19":
                    return "t";
                case "20":
                    return "u";
                case "21":
                    return "v";
                case "22":
                    return "w";
                case "23":
                    return "x";
                default:
                    LogWriter.WriteToLog(string.Format("Waring:{0} convertHourNumberToLetter NOK!", hour));
                    return "z";
            }
        }

        private static bool CleanUpFile(string fileName)
        {
            ZipArchive archiv = null;
            try
            {
                archiv = ZipFile.OpenRead(fileName);
                ReadOnlyCollection<ZipArchiveEntry> list = archiv.Entries;
                foreach (ZipArchiveEntry entry in list)
                {
                    if (completeList.Contains(Path.GetFileNameWithoutExtension(entry.FullName)))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                LogWriter.WriteToLog(String.Format("Warning: Problem while Reading ZIPArchiv={0}. Message = {1}", fileName, e.Message));
                return false;
            }
            finally
            {
                if (archiv != null)
                {
                    archiv.Dispose();
                }
            }
        }

        private static string UncompressHatanaka(string file)
        {
            try
            {
                Process proc = new Process();
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.Arguments = String.Format("/C crx2rnx.exe -f {0}", file);
                proc.Start();
                proc.WaitForExit();
                if (proc.ExitCode == 0)
                {
                    return Path.ChangeExtension(file, "rnx");
                }
                else
                {
                    LogWriter.WriteToLog(String.Format("Error: crx2rnx for file {0} did not work!", file));
                    return "";
                }
            }
            catch (Exception e)
            {
                LogWriter.WriteToLog(e);
                return "";
            }
        }

        private static string GetChecksum(string file)
        {
            using (FileStream stream = File.OpenRead(file))
            {
                SHA256Managed sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }

        private static string GetChecksumMD5(string file)
        {
            using (FileStream stream = File.OpenRead(file))
            {
                HMACMD5 md5 = new HMACMD5();
                byte[] checksummd5 = md5.ComputeHash(stream);
                return BitConverter.ToString(checksummd5).Replace("-", String.Empty);
            }
        }

    }
}