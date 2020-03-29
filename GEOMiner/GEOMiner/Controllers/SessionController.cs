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

namespace GEOMiner.Controllers
{
    public class SessionController : Controller
    {
        // GET: Session

        //#################################################################################################

        public ActionResult Session()
        {
            return View("~/Views/Session.cshtml", Program.sessionModel);
        }

        //#################################################################################################
        public IActionResult LoadSessionFromFile(Models.SessionModel sessionModel)
        {
            Program.sessionModel.validationMessage = String.Empty;
            Program.sessionModel.valError = false;

            if((sessionModel.file != null && !sessionModel.file.FileName.EndsWith(".xml"))
                || (!String.IsNullOrEmpty(sessionModel.fileName) && !sessionModel.fileName.EndsWith(".xml")))
            {
                Program.sessionModel.validationMessage = HelperController.GetMessage("Session.Err.WrongType").txt;
                Program.sessionModel.valError = true;
                return Session();
            }
                

            if(sessionModel.file != null)
            {
                try
                {
                    var result = new StringBuilder();
                    using (var reader = new StreamReader(sessionModel.file.OpenReadStream()))
                    {
                        while (reader.Peek() >= 0)
                            result.AppendLine(reader.ReadLine());
                    }
                    
                    var filestring = result.ToString();
                    Program.indexModel.FilterList = GetFiltersFromXml(Controllers.XmlController.GetXml(filestring));
                }
                catch(Exception ex)
                {
                    LogController.LogError(String.Format(HelperController.GetMessage("SessionController.Err.LoadSessionFromFile(Model)1").txt, ex.ToString()));
                    Program.sessionModel.validationMessage = HelperController.GetMessage("SessionController.Err.LoadSessionFromFile(Model)1").ext;
                    Program.sessionModel.valError = true;
                    return Session();
                }
            }
            else
            {
                try
                {
                    
                    Program.indexModel.FilterList = GetFiltersFromXml(Controllers.XmlController.GetXml(HelperController.ConvertBase64ToUtf8(sessionModel.fileString)));
                }
                catch(Exception ex)
                {
                    LogController.LogError(String.Format(HelperController.GetMessage("SessionController.Err.LoadSessionFromFile(Model)2").txt, ex.ToString()));
                    Program.sessionModel.validationMessage = HelperController.GetMessage("SessionController.Err.LoadSessionFromFile(Model)2").ext;
                    Program.sessionModel.valError = true;
                    return Session();
                }
            }

            Program.sessionModel.validationMessage = HelperController.GetMessage("SessionController.Inf.LoadSessionFromFile(Model)").txt;

            return Session();
        }

        //#################################################################################################
        public static Classes.FList LoadSessionFromFile(string file, bool usedef)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
                doc = Controllers.FileController.LoadXmlFromFile(file);
                if (doc.ChildNodes.Count == 0 && usedef)
                {
                    return LoadSessionFromFile("DefaultFilters.xml", false);
                }
            }
            catch(Exception ex)
            {
                LogController.LogError(String.Format(HelperController.GetMessage("SessionController.Err.LoadSessionFromFile(string,bool)").txt, ex.ToString()));
                throw;
            }
            

            

            
            return GetFiltersFromXml(doc);
        }

        //#################################################################################################
        public static Classes.FList LoadSession()
        {
            return LoadSessionFromFile(@"..\Sessions\LastSession.xml", true);
        }

        //#################################################################################################
        public static void SaveSession(Classes.FList flist)
        {
            Controllers.FileController.WriteToXml(flist);
        }

        //#################################################################################################
        private static Classes.FList GetFiltersFromXml(XmlDocument doc)
        {
            var flist = new Classes.FList();
            XmlNode filters = doc.SelectSingleNode("filters");
            foreach (XmlNode node in filters.SelectNodes("filter"))
            {
                if (node.SelectSingleNode("name") == null)
                    continue;

                var filter = new Classes.Filter();
                filter.name = node.SelectSingleNode("name").InnerText;

                if (node.SelectSingleNode("value") != null)
                    filter.value = node.SelectSingleNode("value").InnerText;

                flist.Add(filter);
            }

            return flist;
        }
    }
}