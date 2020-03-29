using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Xml;

namespace GEOMiner.Controllers
{
    public class XmlController : Controller
    {
        public static bool IsValid(string str)
        {
            if(!String.IsNullOrEmpty(str))
            {
                try
                {
                    XmlDocument xDoc = GetXml(str);
                }
                catch(Exception ex)
                {
                    LogController.LogError(String.Format(HelperController.GetMessage("XmlController.Err.IsValid").txt, ex.ToString()));
                    throw;
                }
            }
                
            return true;
        }

        //#################################################################################################
        public static XmlDocument GetXml(string str)
        {
            XmlDocument xDoc = new XmlDocument();
            try
            {
                xDoc.LoadXml(str);
            }
            catch(Exception ex)
            {
                LogController.LogError(String.Format(HelperController.GetMessage("XmlController.Err.GetXml").txt, ex.ToString()));
                throw;
            }
            return xDoc;
        }

        
    }
}