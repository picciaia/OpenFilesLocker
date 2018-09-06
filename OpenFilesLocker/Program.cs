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
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace OpenFilesLocker
{
    /// <summary>
    /// Starting object with main entry point
    /// This process can be started either as a standard windows console app or as a windows service
    /// </summary>
    public class Program
    {
        private static bool consoleMode;        // Operating mode flag: TRUE when running in console mode, FALSE otherwise
        private static Locker locker;

        static void Main(string[] args)
        {
            consoleMode = Environment.UserInteractive;
            Logger.Enabled = true;
            Logger.LogOnFile = true;
            Logger.CurrentVerbosityInfo = (VerbosityInfoLevel)AppSettings.Default.LogVerbosity;

            Logger.Log("OpenFilesLocker - ver. {0} started",LogInfo.Info, VerbosityInfoLevel.V1,
                Assembly.GetExecutingAssembly().GetName().Version.ToString());

            if (!consoleMode)
            {
                using (var service = new OFLService())
                    ServiceBase.Run(service);
            }
            else
            {
                if (args.Contains("install"))
                {
                    Program.InstallService();
                    return;
                }
                if (args.Contains("uninstall"))
                {
                    Program.UnistallService();
                    return;
                }

                var prog = new Program();
                prog.Start(args);
                if (consoleMode)
                {
                    Console.WriteLine("Press Q to exit");
                    while (Console.ReadKey().Key != ConsoleKey.Q) { }
                    prog.Stop();
                }
            }
        }

        public static void InstallService()
        {
            var installUtil = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\Microsoft.NET\Framework\v4.0.30319\installutil.exe";
            var exeName = System.Reflection.Assembly.GetEntryAssembly().Location;
            var prefix = "file:///";
            if (exeName.StartsWith(prefix))
                exeName = exeName.Substring(prefix.Length + 1);
            exeName = System.IO.Path.GetFileName(exeName);

            var psi = new System.Diagnostics.ProcessStartInfo("cmd", "/c " + installUtil + " " + exeName);
            System.Diagnostics.Process.Start(psi).WaitForExit();
        }
        public static void UnistallService()
        {
            var installUtil = Environment.GetFolderPath(Environment.SpecialFolder.Windows) + @"\Microsoft.NET\Framework\v4.0.30319\installutil.exe";
            var exeName = System.Reflection.Assembly.GetEntryAssembly().Location;
            var prefix = "file:///";
            if (exeName.StartsWith(prefix))
                exeName = exeName.Substring(prefix.Length + 1);
            exeName = System.IO.Path.GetFileName(exeName);

            var psi = new System.Diagnostics.ProcessStartInfo("cmd", "/c " + installUtil + " /u " + exeName);
            System.Diagnostics.Process.Start(psi).WaitForExit();
        }


        public void Start(string[] args)
        {
            locker = new Locker
            {
                CheckLocksDelay = AppSettings.Default.CheckLocksDelay,
                GenerationDelay = AppSettings.Default.GenerationDelay,
                LocalShare = AppSettings.Default.LocalShare,
                OpenFilesFilename = AppSettings.Default.OpenFilesFilename,
                RemoteLocations = AppSettings.Default.RemoteLocations.Cast<string>().ToList(),
                WorkingFolder = AppSettings.Default.WorkingFolder,
                Exceptions = AppSettings.Default.Exceptions.Cast<string>().ToList()
            };
            locker.Start();
        }

        public void Stop()
        {
            locker.Stop();
        }

        #region Nested classes to support running as service
        public const string ServiceName = "OpenfilesLockerService";
        public class OFLService : ServiceBase
        {
            Program prog = new Program();
            public OFLService()
            {
                ServiceName = Program.ServiceName;
            }
            protected override void OnStart(string[] args)
            {
                prog.Start(args);
            }
            protected override void OnStop()
            {
                prog.Stop();
            }
        }
        #endregion
    }


    #region Service installer, required by installutil to install as a windows service
    //Install the service using:
    //  installutil /i openfileslocker.exe

    //Uninstall using:
    //installutil /u openfileslocker.exe

    [RunInstaller(true)]
    public class OFLServiceInstaller : System.Configuration.Install.Installer
    {
        ServiceProcessInstaller process = new ServiceProcessInstaller();
        ServiceInstaller serviceAdmin = new ServiceInstaller();
        public OFLServiceInstaller()
        {
            process.Account = ServiceAccount.LocalSystem;
            serviceAdmin.StartType = ServiceStartMode.Automatic;
            serviceAdmin.ServiceName = Program.ServiceName;
            serviceAdmin.DisplayName = "Openfiles Lock Manager";
            serviceAdmin.Description = "Remote file-lock manager based on openfiles command";
            Installers.Add(process);
            Installers.Add(serviceAdmin);
        }
    }

    #endregion
}
