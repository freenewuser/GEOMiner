using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Xml;
using System.Net;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;
using System.Net.Http;
using System.IO.Compression;

namespace GEOMiner.Controllers
{
    public class UploadController : Controller
    {
        private static DateTime StartZeit;
        public static string _FlatMode = "FLAT";
        public static string _RowMode = "ROW";

        //#################################################################################################
        public IActionResult Upload()
        {
            if (Program.uploadModel.ViewMode == null) Program.uploadModel.ViewMode = FlatMode();

            ViewData["ChangePreview"] = GEOMiner.Controllers.HelperController.GetMessage("Upload.Inf.ChangePreview").txt;
            ViewData["ChangeView"] = GEOMiner.Controllers.HelperController.GetMessage("Upload.Inf.ChangeView").txt;
            ViewData["Save"] = GEOMiner.Controllers.HelperController.GetMessage("Upload.Inf.Save").txt;
            ViewData["Load"] = GEOMiner.Controllers.HelperController.GetMessage("Upload.Inf.Load").txt;

            return View("~/Views/Upload.cshtml", Program.uploadModel);
        }

        //#################################################################################################
        public IActionResult UploadFile(Models.UploadModel uploadModel)
        {
            try 
            {
                Controllers.LogController.Begin();
                StartZeit = DateTime.Now;

                Program.uploadModel.ExceptionMessage = null;
                Program.uploadModel.Step = 1;

                string tmpPath = @"C:\temp\tmp_upload_" + Program.GuidString;
                string dataPath = Path.Combine(tmpPath, "data");
                string csvPath = Path.Combine(tmpPath, "csv");
                string processedPath = Path.Combine(tmpPath, "processed");

                Directory.CreateDirectory(tmpPath);
                Directory.CreateDirectory(dataPath);
                Directory.CreateDirectory(csvPath);
                Directory.CreateDirectory(processedPath);

                if (uploadModel.file != null)
                {
                    try
                    {
                        using (var stream = System.IO.File.Create(Path.Combine(dataPath, uploadModel.file.FileName)))
                        {
                            uploadModel.file.CopyTo(stream);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogController.LogError(String.Format(HelperController.GetMessage("UploadController.Err.UploadFile1").txt, ex.ToString()));
                        Program.uploadModel.ExceptionMessage = HelperController.GetMessage("UploadController.Err.UploadFile1").ext;
                        return Upload();
                    }
                }
                else
                {
                    try
                    {
                        if (uploadModel.fileString.IndexOf("data:") >= 0)
                        {
                            uploadModel.fileString = Regex.Replace(uploadModel.fileString, "data:([^,]+)", String.Empty);
                            uploadModel.fileString = uploadModel.fileString.Remove(0, 1);
                        }
                        System.IO.File.WriteAllBytesAsync(Path.Combine(dataPath, uploadModel.fileName), Convert.FromBase64String(uploadModel.fileString));
                    }
                    catch (Exception ex)
                    {
                        LogController.LogError(String.Format(HelperController.GetMessage("UploadController.Err.UploadFile2").txt, ex.ToString()));
                        Program.uploadModel.ExceptionMessage = HelperController.GetMessage("UploadController.Err.UploadFile2").ext;
                        return Upload();
                    }
                }

                Program.uploadModel.FileList = new List<Classes.File>();
                FileController.MoveAndExtractFiles(dataPath, csvPath);

                

                FileController.MoveAndExtractFiles(csvPath, processedPath);
                LogController.End(String.Format("verstrichene Zeit: {0}", DateTime.Now - StartZeit));
                return Preview(processedPath);
            }
            catch (Exception ex)
            {
                LogController.LogError(String.Format(HelperController.GetMessage("UploadController.Err.Total").txt, ex.ToString()));
                Program.uploadModel.ExceptionMessage = HelperController.GetMessage("UploadController.Err.Total").ext;
                FileController.ClearDirectory(@"C:\temp\tmp_upload_" + Program.GuidString);
                Program.uploadModel.FileList = null;
                return Upload();
            }


        }

        //#################################################################################################

        public void _Preview(string processedPath, bool UseUpload = false, int? ID = null)
        {
            bool HasData = false;
            foreach (FileInfo fi in new DirectoryInfo(processedPath).GetFiles())
            {
                HasData = true;
                // in release mode, Files get deleted, in _DEBUG mode this statement ignores duplicates
                #if _DEBUG
                if (Program.uploadModel.FileList.Select(file => file.Name).Contains(fi.Name)) continue;
                #endif

                Classes.File file = new Classes.File();
                file.ID = ID;
                file.Name = fi.Name;
                file.Length = fi.Length;
                file.content = new Classes.Content();

                

                if (fi.Extension == ".csv")
                {
                    string[] lines = System.IO.File.ReadAllLines(fi.FullName);

                    string[][] parts = new string[lines.Length][];

                    for (int i = 0; i < lines.Length; i++)
                    {
                        lines[i].Replace("\"", "");
                        parts[i] = lines[i].Split(';');
                        if (parts[i].Length != parts[0].Length)
                        {
                            file.ExceptionMessage = HelperController.GetMessage("UploadController.Err.ColumnMismatch").txt;
                        }
                    }

                    List<int> li = new List<int>();
                    for (int i = 0; parts.Length > 0 && i < parts[0].GetLength(0); i++)
                    {
                        li.Add(0);
                    }
                    Program.uploadModel.FileList.Add(file);
                    Program.uploadModel.ArrayList.Add(parts);
                    Program.uploadModel.Offset.Add(0);
                    Program.uploadModel.IntegerList.Add(li);
                    continue;
                }

                Program.uploadModel.FileList.Add(file);
                Program.uploadModel.ArrayList.Add(null);
                Program.uploadModel.Offset.Add(0);
                Program.uploadModel.IntegerList.Add(null);
            }
            if(!UseUpload && !HasData)
            {
                Classes.File file = new Classes.File();
                file.ID = ID;
                file.ExceptionMessage = HelperController.GetMessage("UploadController.Err.NoDataFound").txt;
                Program.uploadModel.FileList.Add(file);
                Program.uploadModel.ArrayList.Add(null);
                Program.uploadModel.Offset.Add(0);
                Program.uploadModel.IntegerList.Add(null);
            }
            #if !_DEBUG
            FileController.ClearDirectory(processedPath, false, false);
            #endif
        }

        //#################################################################################################

        public IActionResult Preview(string processedPath)
        {
            Program.uploadModel.FileList = new List<Classes.File>();
            Program.uploadModel.IntegerList = new List<List<int>>();
            Program.uploadModel.ArrayList = new List<string[][]>();
            Program.uploadModel.Offset = new List<int>();
            _Preview(processedPath, true);
            FileController.ClearDirectory(processedPath,deleteFolder:false);
            return Upload();
        }

        //#################################################################################################

        public static string FlatMode()
        {
            return _FlatMode;
        }

        //#################################################################################################

        public static string RowMode()
        {
            return _RowMode;
        }

        //#################################################################################################
        public IActionResult CreateNewView(Models.UploadModel model, int save)
        {
            if (save == 1) return SaveView();

            List<Tuple<int, string[]>> tuples = new List<Tuple<int, string[]>>();

            for(int i = 0; i < Program.uploadModel.ArrayList.Count; i++)
            {
                for(int j = 0; j < model.IntegerList[i].Count; j++)
                {
                    if (model.IntegerList[i][j] != 0)
                    {
                        var t = GetCol(Program.uploadModel.ArrayList[i], j);
                        Program.uploadModel.ArrayList[i][0][j] = Program.uploadModel.FileList[i].content.accession + "_" + Program.uploadModel.ArrayList[i][0][j];
                        tuples.Add(new Tuple<int, string[]>(model.IntegerList[i][j], GetCol(Program.uploadModel.ArrayList[i], j)));
                    }
                }
                Program.uploadModel.Offset[i] = 0;
            }

            string[][] ret = new string[0][];
            int max = 0;
            tuples.Sort((t1, t2) => t1.Item1.CompareTo(t2.Item1));
            for(int i = 0; i < tuples.Count; i++)
            {
                if (max < tuples[i].Item2.Length)
                    max = tuples[i].Item2.Length;
            }

            ret = new string[max][];
            int[] offsets = new int[max];
            for(int i = 0; i < max; i++)
            {
                string line = "";
                for(int j = 0; j < tuples.Count; j++)
                {
                    line += ";" + (tuples[j].Item2.Length > i ? tuples[j].Item2[i] : null);
                }

                line = line.Remove(0, 1);
                ret[i] = line.Split(';');
                offsets[i] = 0;
            }

            Program.uploadModel.Step = 2;
            Program.uploadModel.IntegerList.Clear();
            Program.uploadModel.FileList.Clear();
            Program.uploadModel.FileList.Add(new Classes.File());
            Program.uploadModel.ArrayList.Clear();
            Program.uploadModel.ArrayList.Add(ret);
            Program.uploadModel.Offset.Clear();
            Program.uploadModel.Offset.AddRange(offsets);
            return Upload();
        }

        //#################################################################################################
        private static string[] GetCol(string[][] array, int col)
        {
            var colLength = array.GetLength(0);
            var colVector = new string[colLength];

            for (var i = 0; i < colLength; i++)
                colVector[i] = array[i][col];

            return colVector;
        }


        //#################################################################################################
        public IActionResult ChangeViewMode(Models.UploadModel model, string Mode)
        {
            if(Program.uploadModel.ViewMode == FlatMode() && Mode == Program.uploadModel.ViewMode)
            {
                Program.uploadModel.ViewMode = RowMode();
            }
            if (Program.uploadModel.ViewMode == RowMode() && Mode == Program.uploadModel.ViewMode)
            {
                Program.uploadModel.ViewMode = FlatMode();
            }
            Program.uploadModel.ExceptionMessage = null;
            return Upload();
        }

        //#################################################################################################
        public IActionResult SaveView()
        {

            var csv = new StringBuilder();

            for (int i = 0; i < Program.uploadModel.ArrayList[0].Length; i++)
            {
                string line = null;
                for (int j = 0; j < Program.uploadModel.ArrayList[0][i].Length; j++)
                {
                    if(line == null)
                    {
                        line = Program.uploadModel.ArrayList[0][i][j];
                    }
                    else
                    {
                        line = line + ";" + Program.uploadModel.ArrayList[0][i][j];
                    }

                }
                csv.AppendLine(line);
                
            }

            var bArray = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bArray, "application/octet-stream", "export.csv");
        }
    }
}