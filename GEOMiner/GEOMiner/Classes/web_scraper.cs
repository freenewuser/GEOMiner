using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Data;
using System.Text.RegularExpressions;
using System.Net.Http;

namespace GEOMiner.Classes
{
    public static class web_scraper
    {
        private static readonly HttpClient httpclient = new HttpClient();

        private static readonly string base_url = "ftp://ftp.ncbi.nlm.nih.gov/geo";

        private static readonly Dictionary<string, string> locator = new Dictionary<string, string> {
                { "GSM","samples"},
                { "GDS", "datasets"},
                { "GSE", "series"},
                { "GPL", "platforms"} };

        public static string accession_to_url(string accession)
        {
            string type = accession.Substring(0, 3);
            string folder = locator[type];
            string subrange = accession.Substring(0, accession.Length - 3) + "nnn";

            return $"{base_url}/{folder}/{subrange}/{accession}";
        }
        public static bool download_matrix_file(string accession, string destination)
        {
            string url = $"{accession_to_url(accession)}/matrix/{accession}_series_matrix.txt.gz";

            // it is probably useful to save the compressed gz file always, and then unzip it when it is actually being used
            try { download_File(url, destination);  }
            catch { Controllers.LogController.LogError($"DownloadError: {accession} not found"); return false; }
            return true;
        }
        public static bool download_soft_file(string accession, string destination)
        {
            string url = $"{accession_to_url(accession)}/soft/{accession}_full.soft.gz";

            // it is probably useful to save the compressed gz file always, and then unzip it when it is actually being used
            try { download_File(url, destination); }
            catch { Controllers.LogController.LogError($"DownloadError: {accession} not found"); return false; }
            return true;
        }
        public static void download_File(string url, string destination, string user = null, string password = null)
        {
            FtpWebResponse response = searchDownload(url, user, password);
            var dest = System.IO.File.Create(destination);
            try { response.GetResponseStream().CopyTo(dest); }
            catch
            {
                dest.Close(); System.IO.File.Delete(destination);
                Controllers.LogController.LogError($"Could not write Downloadresult from {url} to {destination}"); throw;
            }
            
            dest.Close();
        }

        public static IEnumerable<string> scrape_ftp_directory(string accession) // use sparsely, requires many requests at once
        {
            string url;
            if (!accession.StartsWith(base_url)) { url = accession_to_url(accession); }
            else url = accession;

            List<string> directories = new List<string>();
            try { directories = listFiles(url, WebRequestMethods.Ftp.ListDirectory); }
            catch { Controllers.LogController.LogError($"Error listing directories in {url}"); }

            List<string> files = new List<string>();
            foreach (string dir in directories)
            {
                if (!dir.Contains("matrix")) { continue; }
                try { files.AddRange(listFiles(dir, WebRequestMethods.Ftp.ListDirectory)); }
                catch { Controllers.LogController.LogError($"Error listing files in {dir}"); }
            }            

            foreach (string file in files) if (file.EndsWith(".txt.gz")||file.EndsWith(".csv.gz")) yield return file; 
            //alternatively: files.Remove(file); then return
        }

        private static List<string> listFiles(string url, string method, string user = null, string password = null)
        {
            FtpWebResponse response = searchFiles(url, user, password);
            StreamReader streamReader = null;
            try { streamReader = new StreamReader(response.GetResponseStream()); }
            catch { Controllers.LogController.LogError($"No response from {url}"); throw; }
            
            List<string> paths = new List<string>();

            string line = streamReader.ReadLine();
            while (!string.IsNullOrEmpty(line))
            {
                line = url + '/' + line.Split('/')[1];
                paths.Add(line);
                line = streamReader.ReadLine();
            }

            streamReader.Close();

            return paths;
        }

        private static FtpWebResponse getResponse(string url, string requestType = null, string user = null, string password = null)
        {
            FtpWebRequest ftpRequest = (FtpWebRequest)WebRequest.Create(url);
            if ((user != null) && (password != null)) ftpRequest.Credentials = new NetworkCredential(user, password);
            if (requestType != null) { ftpRequest.Method = requestType; }
            else { ftpRequest.Method = WebRequestMethods.Ftp.DownloadFile; }

            FtpWebResponse response;
            try { response = (FtpWebResponse)ftpRequest.GetResponse(); }
            catch { Controllers.LogController.LogError($"DownloadError: ftp request failed"); return null; }

            return response;
        }

        private static FtpWebResponse searchDownload(string url, string user = null, string password = null)
        { return getResponse(url, WebRequestMethods.Ftp.DownloadFile, user, password); }

        private static FtpWebResponse searchFiles(string url, string user = null, string password = null)
        { return getResponse(url, WebRequestMethods.Ftp.ListDirectory, user, password); }


        public static void downloadGEOProfilesViaPOST(string uid, string destination, string page = null)
        {
            int uid_;
            Int32.TryParse(uid, out uid_);

            try { downloadGEOProfilesViaPOST(uid_, destination, page); }
            catch { Controllers.LogController.LogError($"Could not write Downloadresult for {uid} to {destination}"); throw; }
        }
        public static void downloadGEOProfilesViaPOST(int uid, string destination, string page = null)
        {
            if (page == null) page = $"https://www.ncbi.nlm.nih.gov/geoprofiles/?term={uid}";
            HttpContent bodyContent = new StringContent($"term={uid}%5Buid%5D+&EntrezSystem2.PEntrez.Geo.Entrez_PageController.PreviousPageName=results&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_DisplayBar.sPresentation=docsum&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_DisplayBar.FFormat=docsum&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_DisplayBar.FileFormat=docsum&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_DisplayBar.LastPresentation=docsum&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_DisplayBar.Presentation=docsum&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_DisplayBar.PageSize=20&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_DisplayBar.LastPageSize=20&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_DisplayBar.Sort=AFLAG&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_DisplayBar.LastSort=AFLAG&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_DisplayBar.FileSort=AFLAG&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_DisplayBar.Format=&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_DisplayBar.LastFormat=&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_DisplayBar.PrevPageSize=20&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_DisplayBar.PrevPresentation=docsum&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_DisplayBar.PrevSort=AFLAG&CollectionStartIndex=1&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_ResultsController.ResultCount=1&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_ResultsController.RunLastQuery=&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_DisplayBar.GeoProfileData=true&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_SingleItemSupl.Geo_downloadProfileData.GeoProfileData=true&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Discovery_SearchDetails.SearchDetailsTerm=132767181%5Buid%5D&EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.HistoryDisplay.Cmd=DisplayChanged&EntrezSystem2.PEntrez.DbConnector.Db=geoprofiles&EntrezSystem2.PEntrez.DbConnector.LastDb=geoprofiles&EntrezSystem2.PEntrez.DbConnector.Term=132767181%5Buid%5D&EntrezSystem2.PEntrez.DbConnector.LastTabCmd=&EntrezSystem2.PEntrez.DbConnector.LastQueryKey=2&EntrezSystem2.PEntrez.DbConnector.IdsFromResult=&EntrezSystem2.PEntrez.DbConnector.LastIdsFromResult=&EntrezSystem2.PEntrez.DbConnector.LinkName=&EntrezSystem2.PEntrez.DbConnector.LinkReadableName=&EntrezSystem2.PEntrez.DbConnector.LinkSrcDb=&EntrezSystem2.PEntrez.DbConnector.Cmd=DisplayChanged&EntrezSystem2.PEntrez.DbConnector.TabCmd=&EntrezSystem2.PEntrez.DbConnector.QueryKey=&p%24a=EntrezSystem2.PEntrez.Geo.Geo_ResultsPanel.Geo_SingleItemSupl.Geo_downloadProfileData.bGeoProfileData&p%24l=EntrezSystem2&p%24st=geoprofiles");
            var response = httpclient.PostAsync(page, bodyContent);
            Stream responseStream = response.Result.Content.ReadAsStreamAsync().Result;

            var dest = System.IO.File.Create(destination);
            try { responseStream.CopyToAsync(dest).Wait(); }
            catch 
            {
                dest.Close(); System.IO.File.Delete(destination); 
                Controllers.LogController.LogError($"Could not write Downloadresult for {uid} to {destination}"); throw; 
            }
            dest.Close();
            
        }

    }
}
