using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using log4net;
using Microsoft.AspNetCore.Http.Features;
using System.Runtime.InteropServices;
using System.IO;

namespace GEOMiner
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddControllersWithViews();
            services.Configure<FormOptions>(x => x.ValueCountLimit = int.MaxValue);
            services.AddMvc(option => option.EnableEndpointRouting = false);
           //ervices.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, Microsoft.AspNetCore.Hosting.IApplicationLifetime applicationLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            //app.UseRouting();

            app.UseAuthorization();

            /*app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });*/

            app.UseMvc(routes =>
            {
                routes
                    .MapRoute(name: "default", template: "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseStaticFiles();

            loggerFactory.AddLog4Net();

            applicationLifetime.ApplicationStarted.Register(OnStart);
            applicationLifetime.ApplicationStopping.Register(OnStopping);
            applicationLifetime.ApplicationStopped.Register(OnStopped);
        }

        private void OnStopping()
        {
            Console.WriteLine("GEOMiner is stopping...");
            string tmpPath = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) ? @"C:\temp\tmp_upload_" + Program.GuidString : Path.Combine("/tmp", "tmp_upload_" + Program.GuidString);
            Controllers.FileController.ClearDirectory(tmpPath, deleteFolder: true);
        }

        private void OnStart()
        {
            Console.WriteLine("GEOMiner started...");
        }

        private void OnStopped()
        {
           Console.WriteLine("GEOMiner stopped...");
        }
    }
}
