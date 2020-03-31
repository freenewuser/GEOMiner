using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Xml;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace GEOMiner.Controllers
{
    
    public class FileController
    {
        public static readonly string tmpPath = 
            (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? @"C:\temp\tmp_" + Program.GuidString : Path.Combine("/tmp", "tmp_" + Program.GuidString);
        public static readonly string dataPath = Path.Combine(tmpPath, "data");
        public static readonly string preprocessedPath = Path.Combine(tmpPath, "preprocessed");
        public static readonly string csvPath = Path.Combine(tmpPath, "csv");
        public static readonly string imputationPath = Path.Combine(tmpPath, "imputation");
        public static readonly string processedPath = Path.Combine(tmpPath, "processed");
        public static readonly string zipPath = Path.Combine(tmpPath, "zip");
        public static void WriteToXml(Classes.FList flist)
        {
            try
            {
                string XmlPath = @"Sessions\LastSession.xml";
                if ( !Directory.Exists(Path.Join(XmlPath,"..")) ) { Directory.CreateDirectory(Path.Join(XmlPath, "..")); }
                if (File.Exists(XmlPath)) { File.Delete(XmlPath); }
                XmlWriter xmlWriter = XmlWriter.Create(XmlPath); //@"..\Sessions\LastSession.xml"

                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("filters");

                foreach (var item in flist.flist)
                {
                    xmlWriter.WriteStartElement("filter");

                    xmlWriter.WriteStartElement("name");
                    xmlWriter.WriteString(item.name);
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteStartElement("value");
                    xmlWriter.WriteString(item.value);
                    xmlWriter.WriteEndElement();

                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteEndDocument();
                xmlWriter.Close();
            }
            catch (Exception ex)
            {
                Controllers.LogController.LogError(String.Format(Controllers.HelperController.GetMessage("File.Err.WriteToXml").txt, ex.ToString()));
                throw;
            }
        }
        //#################################################################################################

        public static XmlDocument LoadXmlFromFile(string file)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(file);
            }
            catch(Exception ex)
            {
                Controllers.LogController.LogError(String.Format(Controllers.HelperController.GetMessage("File.Err.LoadXmlFromFile").txt, file, ex.ToString()));
                throw;
            }
            return doc;
        }

        //#################################################################################################
        public static StringBuilder WriteListToCsv<T>(List<T> list)
        {
            return new StringBuilder();
        }

        //#################################################################################################
        public static StringBuilder WriteListToCsv(List<Classes.Content> clist)
        {
            var csv = new StringBuilder();
            csv.AppendLine("Title;GenDesc;GenName;ID");

            for(int i=0; i<clist.Count; i++)
            {
                if(clist[i].download)
                {
                    csv.AppendLine(String.Format("{0};{1};{2};{3}", clist[i].Title, clist[i].geneDesc, clist[i].geneName, clist[i].ID)); ;
                }
            }

            return csv;
        }

        //#################################################################################################

        public static string DownloadAndProcessGEOProfiles(List<Classes.Content> clist)
        {
            string zipName = Path.Combine(zipPath, Program.GuidString + ".zip");
            ClearDirectory(tmpPath);

            Directory.CreateDirectory(tmpPath);
            Directory.CreateDirectory(zipPath);
            Directory.CreateDirectory(dataPath);
            Directory.CreateDirectory(processedPath);

            foreach (string GDS_accession in clist.Where(cont => cont.download).Select(cont => cont.accession).Distinct())
            {
                string filename = $"{GDS_accession}_full.soft.gz";
                if (!Classes.web_scraper.download_soft_file(GDS_accession, Path.Combine(zipPath, $"{GDS_accession}_full.soft.gz")))
                { LogController.LogError($"Could not download entry for {GDS_accession}"); continue; }

                MoveAndExtractFiles(zipPath, dataPath);
                Classes.PythonProcess.filter_GEOProfiles_soft(Path.Combine(dataPath, $"{GDS_accession}_full.soft"),
                                                            Path.Combine(processedPath, $"{GDS_accession}_identifier.soft"),
                                                            Path.Combine(processedPath, $"{GDS_accession}_data.soft"));
            }

            ZipFile.CreateFromDirectory(processedPath, zipName);
            ClearDirectory(dataPath);
            ClearDirectory(processedPath);

            return zipName;
        }


        //#################################################################################################
        public static string DowloadAndProcessGDS(List<Classes.Content> clist, bool forProcessing = true, bool disableImputation = true)
        {
            string zipName = Path.Combine(zipPath, Program.GuidString + ".zip");
            ClearDirectory(tmpPath);

            Directory.CreateDirectory(tmpPath);
            Directory.CreateDirectory(zipPath);
            Directory.CreateDirectory(dataPath);
            Directory.CreateDirectory(preprocessedPath);
            Directory.CreateDirectory(csvPath);
            Directory.CreateDirectory(imputationPath);
            Directory.CreateDirectory(processedPath);

            Program.uploadModel.FileList = new List<Classes.File>();
            Program.uploadModel.ArrayList = new List<string[][]>();
            Program.uploadModel.Offset = new List<int>();
            Program.uploadModel.IntegerList = new List<List<int>>();
            Program.uploadModel.Step = 1;


            for (int i = 0; i < clist.Count; i++)
            {
                if (clist[i].download && clist[i].accession != null)
                {
                    // retrieve zip files, put them into zipPath
                    if (!Classes.web_scraper.download_matrix_file(clist[i].accession, Path.Combine(zipPath, clist[i].accession + ".txt.gz")))
                    { 
                        foreach (var file in Classes.web_scraper.scrape_ftp_directory(clist[i].accession)) 
                        {
                            Classes.web_scraper.download_File(file, Path.Combine(zipPath, System.IO.Path.GetFileName(file) ));
                        }
                    }
                    // unzip into dataPath
                    MoveAndExtractFiles(zipPath, preprocessedPath);

                    //process into csv, put results into csvPath
                    if (forProcessing)
                    {
                        string[] unzippedFiles = System.IO.Directory.GetFiles(preprocessedPath);
                        if (unzippedFiles.Count() == 0) { Controllers.LogController.LogError($"No files for accession {clist[i].accession} retrieved"); }

                        foreach (var file in unzippedFiles)
                        {
                            // convert to csv, return GSMs if imputation might be useful
                            string csvDest = System.IO.Path.Join(csvPath, System.IO.Path.GetFileNameWithoutExtension(file) + ".csv");
                            string filterResult = Classes.PythonProcess.filter_GEODatasets_matrix(file, csvDest);

                            // the following imputation section likely has to go, because there is no case when additional data is
                            // externalised into GSMs

                            // download GSMs, zipPath should be have been emptied by MoveAndExtractFiles by now
                            if (!disableImputation && filterResult != null && filterResult.StartsWith("GSM"))
                            {
                                bool fullResult = true;
                                bool singularResult = false;
                                bool currentResult;
                                string[] GSMaccessions = filterResult.Split(',');
                                // first try twice to find suitable imputation data
                                foreach (string GSMaccession in GSMaccessions.Take(2))
                                {
                                    currentResult = Classes.web_scraper.download_matrix_file(GSMaccession, Path.Combine(zipPath, GSMaccession + ".txt.gz"));
                                    fullResult = fullResult && currentResult;
                                    singularResult = singularResult || currentResult;
                                    MoveAndExtractFiles(zipPath, imputationPath);
                                }
                                // only if GSMs appear to contain suitable/downloaded data, continue with the rest, this reduces GEO's server load
                                if (singularResult)
                                {
                                    foreach (string GSMaccession in GSMaccessions.Skip(2))
                                    {
                                        currentResult = Classes.web_scraper.download_matrix_file(GSMaccession, Path.Combine(zipPath, GSMaccession + ".txt.gz"));
                                        fullResult = fullResult && currentResult;
                                        singularResult = singularResult || currentResult;
                                        MoveAndExtractFiles(zipPath, imputationPath);
                                    }
                                }

                                if (!fullResult) { Controllers.LogController.LogError($"Incomplete Imputation retrieval for {clist[i].accession}"); }
                                if (!singularResult) { Controllers.LogController.LogError($"Imputation search for {clist[i].accession} yielded no results"); }
                                // report whether imputation succeeds, check whether GSMs are suitable
                                // overwrite files in csvPath where they currently are
                                // currently, matrix_file_imputation does not do imputation because there is no known use case
                                // it simply copies the files into processedPath
                                if (!Classes.PythonProcess.matrix_file_imputation(
                                        csvDest,
                                        System.IO.Path.Join(processedPath, System.IO.Path.GetFileName(csvDest)),
                                        string.Join(',', System.IO.Directory.GetFiles(imputationPath))))
                                {
                                    Controllers.LogController.LogError($"Processing Imputation files failed for {clist[i].accession}");
                                }
                                //clear imputationPath for next file
                                foreach (var imputFile in System.IO.Directory.GetFiles(imputationPath)) { System.IO.File.Delete(imputFile); }
                            }
                        }

                        //clear preprocessedPath, simultaneously fill dataPath
                        foreach (var dataFile in System.IO.Directory.GetFiles(preprocessedPath))
                        {
                            System.IO.File.Move(dataFile, System.IO.Path.Combine(dataPath, System.IO.Path.GetFileName(dataFile)));
                        }

                        // move files from csvPath into procesedPath, if a corresponding file does not already exist
                        foreach (var csvFile in System.IO.Directory.GetFiles(csvPath))
                        {
                            string csvDest = System.IO.Path.Combine(processedPath, System.IO.Path.GetFileName(csvFile));
                            if (!System.IO.File.Exists(csvDest)) { System.IO.File.Move(csvFile, csvDest); }   
                        }

                        //at last, transpose all csv files in processed path
                        foreach (var csvFile in System.IO.Directory.GetFiles(processedPath))
                        {
                            Classes.PythonProcess.transpose_matrix(csvFile);
                            #if _DEBUG
                            string dest = Path.Combine(csvFile, "..", "..", Path.GetFileName(csvFile));
                            if (!File.Exists(dest)) { File.Copy(csvFile, dest); }
                            #endif
                        }

                        //Upload
                        UploadController up = new UploadController();
                        up._Preview(processedPath, ID: clist[i].ID);
                    }

                }
            }

            ZipFile.CreateFromDirectory(dataPath, zipName);
            //var bArray = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            
            return zipName;
        }

        //#################################################################################################
        public static void ClearDirectory(string dirPath, bool deleteParent = false, bool deleteFolder = true, bool isMaster = true)
        {
            DirectoryInfo dInfo = new DirectoryInfo(dirPath);

            if (dInfo.Exists)
            {
                foreach (DirectoryInfo d in dInfo.GetDirectories())
                {
                    ClearDirectory(d.FullName, isMaster: false);
                }

                foreach (FileInfo f in dInfo.GetFiles())
                {
                    f.Delete();
                }

                if (!isMaster || deleteFolder)
                {
                    dInfo.Delete();
                }
            }

            FileInfo fInfo = new FileInfo(dirPath);
            if (fInfo.Exists)
            {
                if (deleteParent)
                {
                    DirectoryInfo dInf = new DirectoryInfo(fInfo.DirectoryName);
                    ClearDirectory(dInf.Parent.FullName);
                    return;
                }
                ClearDirectory(fInfo.DirectoryName);

            }
        }


        //#################################################################################################
        public static void MoveAndExtractFiles(string source, string destination)
        {
            LogController.Begin();
            foreach (FileInfo fi in new DirectoryInfo(source).GetFiles())
            {
                var newName = ExtractFiles(fi.FullName);
                if (newName != null && fi.Extension == ".zip")
                {
                    var t = Path.GetFileName(newName);
                    MoveAndExtractFiles(Path.Combine(source, Path.GetFileName(newName)), destination);
                    continue;
                }

                if (newName != null)
                {
                    var t = Path.Combine(source, Path.GetFileName(newName));
                    if (new FileInfo(Path.Combine(destination, Path.GetFileName(newName))).Exists)
                    {
                        System.IO.File.Delete(Path.Combine(destination, Path.GetFileName(newName)));
                    }
                    System.IO.File.Move(Path.Combine(source, Path.GetFileName(newName)), Path.Combine(destination, Path.GetFileName(newName)));

                    continue;
                }

                if (new FileInfo(Path.Combine(destination, fi.Name)).Exists)
                {
                    System.IO.File.Delete(Path.Combine(destination, Path.GetFileName(newName)));
                }

                System.IO.File.Move(fi.FullName, Path.Combine(destination, fi.Name));
                //Program.uploadModel.FileList.Add(fi);
            }

            foreach (DirectoryInfo di in new DirectoryInfo(source).GetDirectories())
            {
                MoveAndExtractFiles(di.FullName, destination);
            }
            LogController.End();
        }

        //#################################################################################################
        public static string ExtractFiles(string file)
        {
            FileInfo fi = new FileInfo(file);
            if (fi.Extension == ".zip")
            {
                ZipFile.ExtractToDirectory(fi.FullName, Path.Combine(fi.DirectoryName, fi.Name + "_new_geominer_ghxy"));
                System.IO.File.Delete(fi.FullName);
                return fi.FullName + "_new_geominer_ghxy";
            }
            if (fi.Extension == ".gz")
            {
                string currentName = fi.FullName;
                string newName = currentName.Remove(currentName.Length - fi.Extension.Length);
                //ZipFile.ExtractToDirectory(fi.FullName, Path.Combine(source, fi.Name + "_new_geominer_ghxy"));
                using (FileStream original = fi.OpenRead())
                {

                    using (FileStream decompressed = File.Create(newName))
                    {
                        using (GZipStream gzipStream = new GZipStream(original, CompressionMode.Decompress))
                        {
                            gzipStream.CopyTo(decompressed);
                        }
                    }
                }
                System.IO.File.Delete(fi.FullName);
                return newName;
            }

            return null;
        }
    }
}
