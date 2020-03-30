using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using GEOMiner.Models;
using System.Net;

namespace GEOMiner.Controllers
{
    public class HomeController : Controller
    {
        //#################################################################################################
        public IActionResult Index()
        {
            LogController.Begin();
            try
            {
                if(false && Program.indexModel.FilterList == null)
                    Program.indexModel.FilterList = Controllers.SessionController.LoadSession();
            }
            catch (Exception ex)
            {
                Program.indexModel.FilterList = new Classes.FList();
                LogController.LogError(ex.ToString());  
            }
            ViewData["Preview"] = GEOMiner.Controllers.HelperController.GetMessage("Index.Inf.Preview").txt;

            LogController.End();
            return View("~/Views/Home/Index.cshtml", Program.indexModel);
        }
        //#################################################################################################
        public IActionResult Privacy()
        {
            return View("~/Views/Home/Privacy.cshtml", Program.indexModel);
        }
        //#################################################################################################
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        //#################################################################################################
        [HttpPost]
        public IActionResult Start(Models.IndexModel model, string action)
        {
            LogController.Begin();
            if(model.FilterList == null | (model.FilterList != null && model.FilterList.flist == null))
            {
                var msg = HelperController.GetMessage("Index.Err.NoFilter");
                LogController.LogError(msg.txt);
                Program.indexModel.ExceptionMessage = msg.txt;
                return RedirectToAction("Index", "Home");
            }

            SessionController.SaveSession(model.FilterList);

            Program.indexModel.ExceptionMessage = model.ExceptionMessage;
            Program.indexModel.ErrorFilterList = model.ErrorFilterList;
            if (!String.IsNullOrEmpty(action) && action != "Start")
            {
                Program.indexModel.FilterList.Remove(action);
                return RedirectToAction("Index", "Home");
            }


            Program.indexModel.FilterList = new Classes.FList(model.FilterList);

            //da "GetContent" nicht statisch
            try
            {

                queryController qController = new queryController();
                Program.indexModel.ContentList = new List<Classes.Content>();
                Program.indexModel.ContentList = qController.GetContent(Program.indexModel.FilterList.flist);
            }
            catch (Exception ex)
            {
                LogController.LogError(String.Format(HelperController.GetMessage("Helper.Err.QueryController").txt, ex.ToString()));
                Program.indexModel.ExceptionMessage = HelperController.GetMessage("Helper.Err.QueryController").ext;
            }


            return RedirectToAction("Index", "Home");
        }

        //#################################################################################################
        public IActionResult AddFilter(string filtername)
        {
            LogController.Begin();
            Program.indexModel.ErrorFilterList = null;
            Program.indexModel.ExceptionMessage = null;
            if (!String.IsNullOrEmpty(filtername))
            {
                try
                {
                    if (Program.indexModel.FilterList == null)
                        Program.indexModel.FilterList = new Classes.FList();
                    Program.indexModel.FilterList.Add(new Classes.Filter(filtername, ""));
                }
                catch (Exception ex)
                {
                    Program.indexModel.ErrorFilterList = ex.Message;
                }
            }


            return RedirectToAction("Index", "Home");
        }

        //#################################################################################################
        [HttpPost]
        public IActionResult SaveToCsv(Models.IndexModel indexModel, int newSite = -1, int newCps = -1)
        {
            LogController.Begin();
            Program.indexModel.ErrorFilterList = indexModel.ErrorFilterList;
            Program.indexModel.ExceptionMessage = indexModel.ExceptionMessage;
            int num = 0;
            //change only the actual shown part of the contentlist
            for (int i = 0 + Program.indexModel.cps * Program.indexModel.actSite; i < Program.indexModel.ContentList.Count && i < Program.indexModel.cps * (Program.indexModel.actSite + 1); i++)
            {
                int j = i % Program.indexModel.cps;
                Program.indexModel.ContentList[i].download = indexModel.PartialContentList[j].download;

                if (Program.indexModel.ContentList[i].download)
                    num++;
            }
            if (num > 5)
            {
                Program.indexModel.ExceptionMessage = HelperController.GetMessage("Index.Err.ToManyFiles").txt;
                return RedirectToAction("Index", "Home");
            }

            if (newSite >= 0) return SetActSite(newSite);
            if (newCps >= 0) return SetCps(newCps);



            switch (Program.indexModel.database.KeyName)
            {
                case ("geoprofiles"):

                    var zipFile = FileController.DownloadAndProcessGEOProfiles(Program.indexModel.ContentList);
                    
                    if (zipFile != null && System.IO.File.Exists(zipFile))
                    {
                        var bArray = System.IO.File.ReadAllBytes(zipFile);
                        return File(bArray, "application/octet-stream", "export.zip");
                    }
                    else Controllers.LogController.LogError($"Zipfile could not be accessed at {zipFile}");
                    //break;
                    var csv = FileController.WriteListToCsv(Program.indexModel.ContentList);
                    if (csv != null)
                    {
                        var bArray = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
                        return File(bArray, "application/octet-stream", "export.csv");
                    }
                    break;
                case ("gds"):
                    var processedPath = FileController.DowloadAndProcessGDS(Program.indexModel.ContentList);
                    if (processedPath != null)
                    {
                        return RedirectToAction("Upload", "Upload");
                    }
                    break;
            }

            return RedirectToAction("Index", "Home");
        }

        //#################################################################################################
        public IActionResult SetActSite(int newSite)
        {
            if (newSite >= 0 && Program.indexModel.newSite == -1)
                Program.indexModel.actSite = newSite;

            if (Program.indexModel.newSite >= 0)
                Program.indexModel.actSite = Program.indexModel.newSite;

            Program.indexModel.newSite = -1;
            return RedirectToAction("Index", "Home");
        }

        //#################################################################################################
        public void SetNewSite(int newSite)
        {
            if (newSite >= 0)
                Program.indexModel.newSite = newSite;
        }

        //#################################################################################################
        public IActionResult SetCps(int newCps)
        {

            Program.indexModel.actSite = (int)Math.Ceiling((double)(Program.indexModel.actSite * Program.indexModel.cps / newCps));
            Program.indexModel.cps = newCps;

            return RedirectToAction("Index", "Home");
        }

        //#################################################################################################
        [HttpPost]
        /*public IActionResult ChangeDatabase(string newDatabase)
        {
            Program.indexModel.database = Program.indexModel.DatabaseList.Where(i => i.KeyName == newDatabase).FirstOrDefault();
            if(Program.indexModel.FilterList != null)
            {
                Program.indexModel.FilterList.flist = new List<Classes.Filter>();
            }
            return RedirectToAction("Index", "Home");
        }
        */

        //#################################################################################################
        [HttpGet]
        public JsonResult ChangeDatabase(string newDatabase)
        {
            Program.indexModel.database = Program.indexModel.DatabaseList.Where(i => i.KeyName == newDatabase).FirstOrDefault();
            if (Program.indexModel.FilterList != null)
            {
                Program.indexModel.FilterList.flist = new List<Classes.Filter>();
            }
            Program.indexModel.ErrorFilterList = null;
            return Json("Correct");
        }
    }
}
