using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyServer.Utils
{
    public static class FileUtils
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public static string GetDataFile(string fileName)
        {
            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeName = AppDomain.CurrentDomain.FriendlyName;
            string filePath = strExeFilePath.Replace(exeName + ".dll", string.Empty);
            string configDir = filePath + "configuration" + @"\" + fileName + ".txt";
            return configDir;
        }

        public static void CreateConfigDirectory(string fileName)
        {
            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeName = AppDomain.CurrentDomain.FriendlyName;
            string filePath = strExeFilePath.Replace(exeName + ".dll", string.Empty);
            string configDir = filePath + "configuration";
            if (!Directory.Exists(configDir))
            {
                Logger.Warn("configuration directory and file don't exist! Writing to {0}", configDir);
                Directory.CreateDirectory(configDir);
                var file = File.Create(configDir + @"\" + fileName + ".txt");
                using (StreamWriter writer = new StreamWriter(GetDataFile("config")))
                {
                    string ip = "127.0.0.1";
                    int port = 3000;
                    writer.WriteLine($"ip={ip}");
                    writer.WriteLine($"port={port}");
                }
                file.Close();
            }
        }

        public static string GetDirectory()
        {
            string strExeFilePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string exeName = System.AppDomain.CurrentDomain.FriendlyName;
            string filePath = strExeFilePath.Replace(exeName + ".dll", string.Empty);
            string configDir = filePath + "config";
            return configDir;
        }

        public static int GetPort()
        {
            string[] lines = File.ReadAllLines(GetDataFile("config"));
            foreach (string line in lines)
            {
                if (line.Contains("port"))
                {
                    int port = int.Parse(line.Replace("port=", String.Empty));
                    return port;
                }
            }
            return 0;
        }

        public static string GetIP()
        {
            string[] lines = File.ReadAllLines(GetDataFile("config"));
            foreach (string line in lines)
            {
                if (line.Contains("ip"))
                {
                    string ip = line.Replace("ip=", String.Empty);
                    return ip;
                } 
            }
            return String.Empty;
        }

    }
}
