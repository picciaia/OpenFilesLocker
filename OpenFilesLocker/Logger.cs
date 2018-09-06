//MIT License

//Copyright(c) 2018 Daniele Picciaia

//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to deal
//in the Software without restriction, including without limitation the rights
//to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//copies of the Software, and to permit persons to whom the Software is
//furnished to do so, subject to the following conditions:

//The above copyright notice and this permission notice shall be included in all
//copies or substantial portions of the Software.

//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenFilesLocker
{
    public enum LogInfo
    {
        Info,
        Warning,
        Error
    }

    public enum VerbosityInfoLevel
    {
        V1 = 1,
        V2,
        V3
    }

    /// <summary>
    /// Utility class to log information on console/file
    /// The Logger object uses some static parameters to handle all the logging activities. The principal one is 'CurretVerbosityLevel', a static field the 
    /// Indicate the current configured verbosity (in a scale 1..3)
    /// </summary>
    public class Logger
    {
        public static bool Enabled = true;
        public static VerbosityInfoLevel CurrentVerbosityInfo = VerbosityInfoLevel.V1;
        public static bool LogOnFile = false;
        public static string LogFilename = null;
        public static int MaxFileSizeKB = 100;

        public static string Timestamp
        {
            get { return DateTime.Now.ToString(); }
        }

        public static void Log(string value, LogInfo logInfo = LogInfo.Info, VerbosityInfoLevel verbosity = VerbosityInfoLevel.V1)
        {
            Log(value, logInfo, verbosity, null);
        }
        public static void Log(string format, LogInfo logInfo = LogInfo.Info, VerbosityInfoLevel verbosity = VerbosityInfoLevel.V1, params object[] arg)
        {
            if (Enabled && CurrentVerbosityInfo >= verbosity)
            {
                Console.Write("[{0} {1} {2}] ", Timestamp, logInfo, verbosity);

                string logData;

                if (arg != null)
                {
                    //Console.WriteLine(format, arg);
                    logData = string.Format(format, arg);
                }
                else
                {
                    //Console.Write("[{0} {1} {2}] ", Timestamp, logInfo, verbosity);
                    //Console.WriteLine(format);
                    logData = string.Format("[{0} {1} {2}] {3}", Timestamp, logInfo, verbosity, format);
                }
                Console.WriteLine(logData);

                if(LogOnFile)
                {
                    if (LogFilename == null || FileExceedsSize(LogFilename, MaxFileSizeKB * 1024))
                    {
                        LogFilename = Path.Combine( Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                            string.Format("{0:yyyyMMdd_HHmmss}.log", DateTime.Now));
                    }


                    try
                    {
                        File.AppendAllText(LogFilename, logData + "\n");
                    }
                    catch 
                    {
                    }
                }
            }
        }

        private static bool FileExceedsSize(string filename, long size)
        {
            if (!File.Exists(filename))
                return false;
            FileInfo fileInfo = new FileInfo(filename);
            return fileInfo.Length > size;
        }
    }
}
