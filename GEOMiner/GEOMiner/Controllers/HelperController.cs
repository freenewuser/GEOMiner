using System;
using Microsoft.AspNetCore.Mvc;
using System.Xml;
using System.Net;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using System.IO.Compression;

namespace GEOMiner.Controllers
{
    public class HelperController : Controller
    {

        public static string language = "de";

        private static DateTime StartZeit;

        //#################################################################################################

        public static Message GetMessage(string msgcode)
        {
            try
            {
                Controllers.LogController.Begin();
                StartZeit = DateTime.Now;

                LogController.LogMessage(string.Format("GetMessage: MsgCode: '{0}', Language:'{1}'", msgcode, language));

                Message msg = new Message();
                string strFilename = Path.Combine("Config","Messages.xml");
                XmlTextReader reader = new XmlTextReader("Messages.xml");

                XmlDocument doc = new XmlDocument();
                try
                {
                    doc.Load(strFilename);
                }
                catch
                {
                    if (System.IO.File.Exists(strFilename)) { Controllers.LogController.LogError($"Error loading {strFilename}."); }
                    else { Controllers.LogController.LogError($"Could not load {strFilename}. File does not exist"); }

                }
                XmlNode messages = doc.SelectSingleNode("messages");

                foreach (XmlNode node in messages.SelectNodes("message"))
                {
                    if (node.SelectSingleNode("msgcode") == null)
                        continue;

                    if (!node.SelectSingleNode("msgcode").InnerText.ToLower().Equals(msgcode.ToLower()))
                        continue;

                    if (node.SelectSingleNode("text") != null)
                        if (node.SelectSingleNode(string.Format("text/{0}", language)) != null)
                            msg.txt = node.SelectSingleNode("text/" + language).InnerText;


                    if (msg.txt == "" || msg.txt == null)
                    {
                        msg.txt = string.Format("{0} for lanuage '{1}' isn't available.", msgcode, language);
                        LogController.LogError(string.Format("{0} for lanuage '{1}' isn't available.", msgcode, language));
                    }

                    if (node.SelectSingleNode("extern") != null)
                        if (node.SelectSingleNode(string.Format("extern/{0}", language)) != null)
                            msg.ext = node.SelectSingleNode("extern/" + language).InnerText;

                    if (node.SelectSingleNode("type") != null)
                        msg.type = node.SelectSingleNode("type").Value;
                    else
                        msg.type = "default";

                    break;
                }



                Controllers.LogController.End(String.Format("verstrichene Zeit: {0}", DateTime.Now - StartZeit));
                return msg;
            } catch 
            { 
                Controllers.LogController.LogError("Message could not be produced");
                Message msg = new Message();
                msg.txt = "'Empty'";
                return msg;
            }
        }
        //#################################################################################################
        #nullable enable
        [HttpGet]
        public IActionResult SetLanguage(string? lang)
        {
            LogController.Begin();
            if(!String.IsNullOrEmpty(lang))
                language = lang;

            
            return RedirectToAction("Index", "Home");
        }
        
        //#################################################################################################
        public static string ConvertBase64ToUtf8(string base64)
        {
            try
            {
                if (base64.IndexOf("data:") >= 0)
                {
                    base64 = Regex.Replace(base64, "data:([^,]+)", String.Empty);
                    base64 = base64.Remove(0, 1);
                }
                var data = System.Convert.FromBase64String(base64);
                base64 = System.Text.UTF8Encoding.UTF8.GetString(data);
            }
            catch(Exception ex)
            {
                LogController.LogError(String.Format(HelperController.GetMessage("HelperController.Err.ConvertBase64ToUtf8").txt, ex.ToString()));
                throw;
            }

            return base64;
        }

    }
}