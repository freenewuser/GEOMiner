using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

namespace GEOMiner.Classes
{
    public static class PythonProcess
    {
        private static readonly string PythonPath = GetPythonPath();
        private static readonly string PythonScriptPath = GetPythonScriptPath();
        public static readonly Dictionary<string, string> PythonScriptLocation
            = new Dictionary<string, string>
            {
                {"confirm_csv_dialect", Path.Combine(PythonScriptPath, "confirm_csv_dialect.py") },
                {"convert_csv_dialect", Path.Combine(PythonScriptPath, "convert_csv_dialect.py") },
                {"filter_GEODatasets_matrix", Path.Combine(PythonScriptPath, "filter_GEODatasets_matrix.py") },
                {"filter_GEOProfiles_soft", Path.Combine(PythonScriptPath, "filter_GEOProfiles_soft.py") },
                {"matrix_file_imputation", Path.Combine(PythonScriptPath, "matrix_file_imputation.py") },
                {"transpose_matrix", Path.Combine(PythonScriptPath, "transpose_matrix.py") }
            };

        private static string GetPythonScriptPath()
        {
            string result = Path.Combine(AppContext.BaseDirectory, "Config", "PythonScripts");
            if (System.IO.Directory.Exists(result)) return result;
            else result = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Config", "PythonScripts");
            if (System.IO.Directory.Exists(result)) return result;
            else
            {
                Controllers.LogController.LogError("Could not find Python script directory");
                return null;
            }
        }

        private static IEnumerable<T> SkipExceptions<T>(IEnumerable<T> values)
        {
            using (var enumerator = values.GetEnumerator())
            {
                bool next = true;
                while (next)
                {
                    try { next = enumerator.MoveNext(); }
                    catch { continue; }
                    if (next) yield return enumerator.Current;
                }
            }
        }

        private static IEnumerable<T> print<T>(this IEnumerable<T> values, string name, StreamWriter dest)
        {
            #if _DEBUG
            using (var enumerator = values.GetEnumerator())
            {
                bool next = true;
                while (next)
                {
                    try
                    {
                        next = enumerator.MoveNext();
                        if (enumerator.Current != null) { dest.WriteLine($"{name}: {enumerator.Current}"); }
                        else { dest.WriteLine($"{name}: NullElement"); }
                    }
                    catch (System.Exception e){ dest.WriteLine($"{name}: Exception {e.GetType()}:{e.Message}"); throw (e); } 
                    if (next) yield return enumerator.Current;
                }
            }
            #else
            return values;
            #endif
        }

        private static (string, string) GetRegistryValues(RegistryKey InstallKey)
        {
            if (InstallKey == null) return (null, null);
            string execPath;
            string pythonVersion;

            try
            {
                execPath = InstallKey.GetValue("ExecutablePath").ToString();
                pythonVersion = InstallKey.OpenSubKey("InstallGroup").GetValue(null).ToString();
            }
            catch { }

            try
            {
                execPath = InstallKey.GetValue(null).ToString();
                pythonVersion = InstallKey.OpenSubKey("InstallGroup").GetValue(null).ToString();
            }
            catch { return (null, null); }

            if (execPath.Contains(".exe")) return (execPath, pythonVersion);
            else
            {
                string alternatePath = Path.Combine(execPath, "python.exe");
                if (System.IO.File.Exists(alternatePath)) return (alternatePath, pythonVersion);
                else return (null, null);
            }

        }

        private static string GetPythonPath(string requiredVersion = "3.5", string maxVersion = "9.9") // needs to be specified once incompatibility known
        {

            string result = "";
            if (System.IO.File.Exists(Path.Combine(AppContext.BaseDirectory,"appsettings.json")))
            {
                using (StreamReader reader = new StreamReader(Path.Combine(AppContext.BaseDirectory, "appsettings.json")))
                {
                    result = reader.ReadToEnd();
                }
            }
            dynamic settings_obj = JObject.Parse(result);
            result = settings_obj.PythonPath;
            if (result != "" && result != null) return result;

            System.Diagnostics.ProcessStartInfo start = new System.Diagnostics.ProcessStartInfo();
            start.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;

            start.FileName = "/bin/bash";
            start.Arguments = "which python";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                start.FileName = "cmd.exe";
                start.Arguments = "/C python -c \"import sys; print(sys.executable)\"";
            }

            try
            {
                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(start))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        result = reader.ReadToEnd();
                    }
                }
            }
            catch
            {
                Controllers.LogController.LogMessage($"Python installation not found via shell command");
            }

            if (result != "" && result != null) { return result.Replace("\n",string.Empty).Trim().Replace(@"\", @"\\"); } 

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return null;
            // following code is windows-specific

            string[] basePythonLocations = new string[] {
                @"HKLM\SOFTWARE\Python\",
                @"HKCU\SOFTWARE\Python\", 
                @"HKLM\SOFTWARE\Wow6432Node\Python\" 
            };


            #if _DEBUG
            string logfile = @"..\registrylog.txt";
            if (System.IO.File.Exists(logfile)) { System.IO.File.Delete(logfile); }
            StreamWriter testwriter = new StreamWriter(logfile);

            #else
            StreamWriter testwriter = null;
            #endif

            (string, Version)[] ppythonLocations;

            try
            {

            var key_pipeline = basePythonLocations
                   .Select(str => (str == "HKLM" ? Registry.LocalMachine : Registry.CurrentUser).OpenSubKey(@"Software\Python") )
                   .print("SelectRegistry", testwriter)
                   .SelectMany(registryKey => registryKey.GetSubKeyNames().Select(subkey => registryKey.OpenSubKey(subkey))) // enters PythonCore or ContinuumAnalytics
                   .print("Search First Subkey", testwriter)
                   .SelectMany(registryKey => registryKey.GetSubKeyNames().Select(subkey => registryKey.OpenSubKey(subkey))) // enters individual python or anaconda installations
                   .print("Search Second Subkey", testwriter)
                   .Select(productKey => productKey.OpenSubKey("InstallPath"))
                   .print("Search InstallPath", testwriter)
                   .Select(Installkey => GetRegistryValues(Installkey)) //Registry entry does not always contain named values or the actual python.exe
                   .print("Get final Registry values", testwriter)
                   .Where(items => items.Item1 != null && items.Item1.Contains(".exe") &&
                                   items.Item2 != null && items.Item2 != "")
                   .print("Select good values", testwriter)
                   .Select(items => (items.Item1, new Version(items.Item2.Substring(6))))
                   .print("convert into version", testwriter);
                key_pipeline = SkipExceptions(key_pipeline);

                Version minVersion = new Version(requiredVersion), supportedVersion = new Version(maxVersion);
                ppythonLocations = key_pipeline.Where(items => items.Item2.CompareTo(minVersion) >= 0 &&
                                                                          items.Item2.CompareTo(supportedVersion) <= 0).ToArray();
 
            }
            catch (System.Exception e){ testwriter.WriteLine($"{e.GetType()}: {e.Message}");  throw; }
            #if _DEBUG
            finally { testwriter.Close(); testwriter.Dispose(); }
            #endif

            if (ppythonLocations == null || ppythonLocations.Length < 1)
            {
                Controllers.LogController.LogError("Error: No Python installation could be found");
                return "Error: no Python Installation could be found";
            }
            else
            {
                (string tmploc, Version tmpver) = ppythonLocations[0];
                foreach ( (string elemloc, Version elemver) in ppythonLocations.Skip(1))
                {
                    if (elemver.CompareTo(tmpver)>0) { tmploc = elemloc; tmpver = elemver; }
                }
                return tmploc;
            }
        }

        private static string run_file(string FilePath, string args)
        {
            if (!System.IO.File.Exists(PythonPath))
            {
                Controllers.LogController.LogError($"FileNotFoundError: Python Installation at {PythonPath} not found");
                throw new FileNotFoundException($"Python Executable not found at {PythonPath}");
            }

            if (!System.IO.File.Exists(FilePath))
            {
                Controllers.LogController.LogError($"FileNotFoundError: Python script at {FilePath} not found");
                throw new FileNotFoundException($"Python Script not found at {FilePath}");
            }

            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = PythonPath;
            start.Arguments = FilePath + " " + args;
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            string result = "";
            try
            {
                using (System.Diagnostics.Process process = System.Diagnostics.Process.Start(start))
                {
                    using (StreamReader reader = process.StandardOutput)
                    {
                        result = reader.ReadToEnd();
                    }
                }
            }
            catch 
            { 
                Controllers.LogController.LogError($"FileNotFoundError: Python script at {FilePath} not found");
                return $"Error: FileNotFoundError: Python script at {FilePath} not found";
            }

            if (result=="")
            {
                Controllers.LogController.LogError($"FileNotFoundError: Python script at {FilePath} threw an unhandled Exception");
                return $"Error: Python script at {FilePath} threw an unhandled Exception";
            }

            return result;
        }

        public static string[] confirm_csv_dialect(string CSVPath)
        {
            // if no dialect is recognized, returns null

            string ret;
            try     { ret = run_file(PythonScriptLocation["confirm_csv_dialect"], $"\"{CSVPath}\""); }
            catch   { Controllers.LogController.LogError($"FileNotFoundError: Python script at " +
                        $"{PythonScriptLocation["confirm_csv_dialect"]} not found"); return null; }
            
            Regex rx = new Regex(@"\'(\W+?)\' \'(\W+?)\'",
                                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var matches = rx.Match(ret);
            if  (!matches.Success) { return null; }
            else
            {
                string[] result = new string[] { matches.Groups[1].Value, matches.Groups[2].Value };

                if (ret.StartsWith("Error")) 
                { 
                    Controllers.LogController.LogError($"Erratic Python response from {PythonScriptLocation["confirm_csv_dialect"]}: {ret}\n"
                        +$"Arguments: {CSVPath}"); 
                    return null; 
                }

                return result;
            }
        }

        public static bool convert_csv_dialect( string CSVPath, string old_item_delimiter, string DestinationPath=null)
        {
            // sets line delimiter to '; ', does not delete old file, except when old and new path are equal
            // if script fails, returns false
            if (DestinationPath == null) { DestinationPath = CSVPath; }
            string args = $"\"{CSVPath}\" \"{old_item_delimiter}\" \"{DestinationPath}\"";

            string ret;
            try { ret = run_file(PythonScriptLocation["convert_csv_dialect"], args); }
            catch 
            { 
                Controllers.LogController.LogError($"FileNotFoundError: Python script at " +
                        $"{PythonScriptLocation["convert_csv_dialect"]} not found"); return false; 
            }

            if (ret.StartsWith("Error"))
            {
                Controllers.LogController.LogError($"Erratic Python response from {PythonScriptLocation["convert_csv_dialect"]}: {ret}\n"
                    +$"Arguments: {CSVPath}, '{old_item_delimiter}', {DestinationPath}");
                return false;
            }

            return true;
        }

        public static string filter_GEOProfiles_soft(string CSVPath, string IdentPath, string tablePath)
        {
            // splits file at CSVPath into header (contains additional information) and
            // table (contains actual data, including headers)
            // if script fails, returns null
            string ret;
            try { ret = run_file(PythonScriptLocation["filter_GEOProfiles_soft"], $"\"{CSVPath}\" \"{IdentPath}\" \"{tablePath}\""); }
            catch
            {
                Controllers.LogController.LogError($"FileNotFoundError: Python script at " +
                    $"{PythonScriptLocation["filter_GEOProfiles_soft"]} not found"); return null;
            }

            if (ret.StartsWith("Error"))
            {
                Controllers.LogController.LogError($"Erratic Python response from {PythonScriptLocation["filter_GEOProfiles_soft"]}: {ret}\n"
                    + $"Arguments: {CSVPath}, {IdentPath} {tablePath}");
                return null;
            }

            return ret;
        }
        public static string filter_GEODatasets_matrix(string CSVPath, string DestinationPath)
        {
            // if original matrix part is too short (only "ID_REF" line) returns ','-separated GSM accessions
            // then it is probably useful to try another search and do data imputation
            // if matrix part of the original file is large enough, returns destination path
            // if script fails, returns null
            string ret;
            try { ret = run_file(PythonScriptLocation["filter_GEODatasets_matrix"], $"\"{CSVPath}\" \"{DestinationPath}\""); }
            catch
            {
                Controllers.LogController.LogError($"FileNotFoundError: Python script at " +
                    $"{PythonScriptLocation["filter_GEODatasets_matrix"]} not found"); return null;
            }

            if (ret.StartsWith("Error"))
            {
                Controllers.LogController.LogError($"Erratic Python response from {PythonScriptLocation["filter_GEODatasets_matrix"]}: {ret}\n"
                    + $"Arguments: {CSVPath}, {DestinationPath}");
                return null;
            }

            return ret;
        }

        public static bool matrix_file_imputation(string matrixPath, string destinationPath, string imputation_Paths)
        {
            // fills a GEODatasets matrix.txt file with references to GEO Samples matrices in imputation_Paths
            // file names in imputation_Paths should be {accession}.txt
            // returns false if unsuccessfull

            string ret;
            try { ret = run_file(PythonScriptLocation["matrix_file_imputation"], $"\"{matrixPath}\" \"{destinationPath}\" \"{imputation_Paths}\""); }
            catch
            {
                Controllers.LogController.LogError($"FileNotFoundError: Python script at " +
                    $"{PythonScriptLocation["matrix_file_imputation"]} not found"); return false;
            }

            if (ret.StartsWith("Error"))
            {
                Controllers.LogController.LogError($"Erratic Python response from {PythonScriptLocation["matrix_file_imputation"]}: {ret}\n"
                    +$"Arguments: {matrixPath}, {destinationPath}, {imputation_Paths}");
                return false;
            }

            return true;
        }

        public static bool transpose_matrix(string matrixPath)
        {
            // replaces file with one where rows and columns have been swapped
            // file has to be ';'-separated values
            // return false if script fails
            string ret;
            try { ret = run_file(PythonScriptLocation["transpose_matrix"], $"\"{matrixPath}\""); }
            catch
            {
                Controllers.LogController.LogError($"FileNotFoundError: Python script at " +
                    $"{PythonScriptLocation["transpose_matrix"]} not found"); return false;
            }

            if (ret.StartsWith("Error"))
            {
                Controllers.LogController.LogError($"Erratic Python response from {PythonScriptLocation["transpose_matrix"]}: {ret}\n"
                    +$"arguments: {matrixPath}");
                Controllers.LogController.LogError($"");
                return false;
            }

            return true;
        }

    }
}
