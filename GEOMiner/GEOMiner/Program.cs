using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Xml;
using System.Reflection;

#pragma warning disable NU1701
#pragma warning disable NETSDK1064

namespace GEOMiner
{
    public class Program
    {
        public static Models.IndexModel indexModel;
        public static Models.SessionModel sessionModel;
        public static Models.UploadModel uploadModel;
        public static string GuidString;

        public static void Main(string[] args)
        {
            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.Load(File.OpenRead("log4net.config"));
            var repo = log4net.LogManager.CreateRepository(Assembly.GetEntryAssembly(),
                       typeof(log4net.Repository.Hierarchy.Hierarchy));
            log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);

            indexModel = new Models.IndexModel();
            indexModel.Init();
            sessionModel = new Models.SessionModel();
            uploadModel = new Models.UploadModel();

            Guid g = Guid.NewGuid();
            GuidString = Convert.ToBase64String(g.ToByteArray());
            GuidString = GuidString.Replace("=", "").Replace("+", "").Replace("/", "").Replace("\\", "");

            CreateHostBuilder(args).UseConsoleLifetime().Build().Run();

            
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
               .UseConsoleLifetime();
    }
}
