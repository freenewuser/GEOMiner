using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using log4net;
using System.Diagnostics;

namespace GEOMiner.Controllers
{
    public class LogController : Controller
    {
        private static ILog _logger;

        private static ILog logger
        {
            get
            {
                if (_logger == null)
                {
                    _logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
                }
                return _logger;
            }
        }

        //#################################################################################################
        public static void LogMessage(string logmessage)
        {
            #if _DEBUG
            logger.Info(logmessage);
            #endif
        }

        //#################################################################################################
        public static void LogError(string errormessage)
        {
            logger.Error(errormessage);
        }

        //#################################################################################################
        public static void Begin()
        {
            #if _DEBUG
            StackTrace stackTrace = new StackTrace();
            logger.Info(String.Format("{0}.{1} [B]", stackTrace.GetFrame(1).GetMethod().DeclaringType.FullName, stackTrace.GetFrame(1).GetMethod().Name));
            #endif
        }

        //#################################################################################################
        public static void Begin(String str)
        {
            #if _DEBUG
            StackTrace stackTrace = new StackTrace();
            logger.Info(String.Format("{0}.{1} [B]: {2}", stackTrace.GetFrame(1).GetMethod().DeclaringType.FullName, stackTrace.GetFrame(1).GetMethod().Name, str));
            #endif

        }

        //#################################################################################################
        public static void End()
        {
            #if _DEBUG
            StackTrace stackTrace = new StackTrace();
            logger.Info(String.Format("{0}.{1} [E]", stackTrace.GetFrame(1).GetMethod().DeclaringType.FullName, stackTrace.GetFrame(1).GetMethod().Name));
            #endif

        }

        //#################################################################################################
        public static void End(String str)
        {
            #if _DEBUG
            StackTrace stackTrace = new StackTrace();
            logger.Info(String.Format("{0}.{1} [E]: {2}", stackTrace.GetFrame(1).GetMethod().DeclaringType.FullName, stackTrace.GetFrame(1).GetMethod().Name, str));
            #endif

        }
    }
}