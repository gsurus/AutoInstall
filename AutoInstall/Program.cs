using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.Net;
using System.Diagnostics;
using System.Threading;

namespace AutoInstall
{
    class Program
    {
        public static string dataFolder = AppDomain.CurrentDomain.BaseDirectory + "Data\\";
        public static string installerFolder = $"{dataFolder}\\Installers\\";
        public static int totalFiles { get; set; }
        public static int currentFile { get; set; } = 0;
        public static Dictionary<string, string> fileData = new Dictionary<string, string>();
        public static string currentFileName { get; set; }

        static void Main(string[] args)
        {
            
            Console.Title = "AutoInstaller";
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            GetFiles();

            var mre = new ManualResetEvent(false);
            ThreadPool.QueueUserWorkItem((state) =>
            {
                Console.WriteLine("\n>> Press Any Key to Exit");
                while (true)
                {
                    var key = Console.ReadKey().ToString();
                    if (key.Length >= 1)
                    {
                        mre.Set();
                        break;
                    }
                }
            });
            mre.WaitOne();

        }

        public static Tuple<List<string>, List<string>> CheckForInstallers(List<string> urls, List<string> fileNames, Dictionary<string, string> _fileData)
        {
            Console.WriteLine(">> Checking for Pre-existing Files...");
            int numOfUrls = File.ReadAllLines($"{dataFolder}urls.txt").Count();
            List<string> requiredFileUrls = urls;
            List<string> requiredFileNames = fileNames;
            Dictionary<string, string> requiredFiles = _fileData;

            string[] files = Directory.GetFiles(installerFolder);

            foreach (string file in files)
            {
                string _file = file.Replace(installerFolder, "");
                if (_fileData.ContainsKey(_file))
                {
                    requiredFileUrls.Remove(fileData[_file]);
                    requiredFileNames.Remove(_file);
                }
            }
            var allData = new Tuple<List<string>, List<string>>(requiredFileUrls, requiredFileNames);

            if (requiredFileUrls.Count <= 0)
            {
                return allData;
            } 
            else
            {
                Console.WriteLine($">> Need {requiredFileUrls.Count()} of {numOfUrls} file(s)");
                return allData;
            }
                



            
        }

        public static void GetFiles()
        {
            string[] urls = File.ReadAllLines($"{dataFolder}urls.txt");
            List<string> fileUrls = new List<string>();
            List<string> fileNames = new List<string>();
            List<string> installNames = new List<string>();

            foreach (string str in urls)
            {
                string[] urlAndName = str.Split(null);
                fileData.Add(urlAndName[1], urlAndName[0]);
                fileNames.Add(urlAndName[1]);
                installNames.Add(urlAndName[1]);
                fileUrls.Add(urlAndName[0]);
                
            }
            

            var _fileData = CheckForInstallers(fileUrls, fileNames, fileData);
            
            totalFiles = _fileData.Item1.Count();
            
            for (int i = 0; i < _fileData.Item1.Count(); i++)
            {
                int currentFile = i + 1;
                string url = _fileData.Item1[i];
                string filename = _fileData.Item2[i];
                
                DownloadFile(url, filename, currentFile);
                Console.WriteLine(">> OK");
            }
            ProcessFolder(installNames);
        }

        private static void DownloadFile(string url, string filename, int fileNumber)
        {
            WebClient wc = new WebClient();
            Console.WriteLine($">> Downloading file {currentFile} of {totalFiles}...");
            wc.DownloadFile(new Uri(url), $"{installerFolder}{filename}");
        }

        private static void ProcessFolder(List<string> files)
        {
            string SOURCEFOLDERPATH = installerFolder;

            if (Directory.Exists(SOURCEFOLDERPATH))
            {
                Console.WriteLine(">> Directory Check...");
                Console.WriteLine(">> OK", SOURCEFOLDERPATH);
                Console.WriteLine("\n-----------------------------------------------\n   Press Any Key to Begin Installing Files\n-----------------------------------------------");
                Console.ReadKey();
                Console.Clear();
                if (Directory.GetFiles(SOURCEFOLDERPATH, "*.exe").Length > 0)
                {
                    int count = Directory.GetFiles(SOURCEFOLDERPATH, "*.exe").Length;

                    foreach (var file in files)
                    {
                        var fileName = System.IO.Path.GetFileName(file);
                        var fileNameWithPath = SOURCEFOLDERPATH + "\\" + fileName;
                        Console.WriteLine(">> Executing File: {0}", fileName);

                        DeployApplications(fileNameWithPath);
                    }
                }
            }
            else
                Console.WriteLine("Directory not Found: {0}", SOURCEFOLDERPATH);
        }

        public static void DeployApplications(string executableFilePath)
        {
            PowerShell powerShell = null;
            try
            {
                using (powerShell = PowerShell.Create())
                { 
                    powerShell.AddScript($"$setup=Start-Process '{executableFilePath} ' -ArgumentList ' / S ' -Wait -PassThru");  
                    Collection < PSObject > PSOutput = powerShell.Invoke(); foreach (PSObject outputItem in PSOutput)
                    {
                        if (outputItem != null)
                        {
                            Console.WriteLine($">> OK");
                        }
                    }
                    if (powerShell.Streams.Error.Count > 0)
                    {
                        string temp = powerShell.Streams.Error.First().ToString();
                        Console.WriteLine(">> Error: {0}", temp);
                    }
                    else
                        Console.WriteLine(">> Installation Completed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: {0}", ex.InnerException);
            }
            finally
            {
                if (powerShell != null)
                    powerShell.Dispose();
            }
        }
        
    }
}
