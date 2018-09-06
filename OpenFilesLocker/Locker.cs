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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenFilesLocker
{
    /// Main class for OpenFilesLocker, that implements the BL of the application as a 'Manager' object.
    /// The OpenFilesLocker uses Microsoft 'openfiles' command to implement a distributed file locking system: 
    /// Each node executes two parallel Task
    /// - The first one calls the Microsoft Windows 'openfiles' command and generates a local file (openfiles.dat) containing all the (local and remote) opened files in a specified share
    /// - The second one retrieves the 'openfiles.dat' file from all the connected hosts and generate a lock locally on every files listed on each single 'openfiles.dat'
    /// The App.config file contains all the parameters needed by the synchronization logic:
    /// 'RemoteLocations' is an ArrayOfString with all the remote locations to sync with
    /// 'GenerationDelay' indicates how frequently generates the 'openfiles.dat'
    /// 'CheckLocksDelay' indicates how frequently retrieves and analizes 'openfiles.dat'
    /// 'LocalShare' reports the local folder to share
    /// 'Exceptions' is an ArrayOfStrings with all the exceptions to not consider by the locking mechanism
    /// 'LoggerVerbosity' is the verbosity level (1..3)
    /// This object exposes method to Start/Stop the manager, generates openfiles info, analyzes openfiles from other location and locks/unlocks files 
    /// </summary>
    public class Locker
    {
        #region private variables
        private OpenFilesParser parser = new OpenFilesParser();
        private OpenFilesGenerator generator = new OpenFilesGenerator();
        private Dictionary<string, FileStream> lockedFiles = new Dictionary<string, FileStream>();
        private bool running = false;
        //Parallel Tasks to generate and check locks 
        private Task taskGenerateLocks = null;
        private Task taskCheckLocks = null;
        #endregion private variables

        public int GenerationDelay { get; set; }
        public int CheckLocksDelay { get; set; }
        public List<string> RemoteLocations { get; set; }
        public string OpenFilesFilename { get; set; }
        public string LocalShare { get; set; }
        public string WorkingFolder { get; set; }
        public List<string> Exceptions { get; set; }

        /// <summary>
        /// Create Tasks to generate and check locks
        /// </summary>
        public void Start()
        {
            running = true;
            taskGenerateLocks = Task.Factory.StartNew(GenerateLocalLocks);
            taskCheckLocks = Task.Factory.StartNew(CheckRemoteLocks);
        }

        /// <summary>
        /// Stop parallel tasks used to generate and check locks
        /// </summary>
        public void Stop()
        {
            running = false;
            taskGenerateLocks.Wait();
            Logger.Log("Stop> Task 'taskGenerateLocks' ended", LogInfo.Info, VerbosityInfoLevel.V3);
            taskCheckLocks.Wait();
            Logger.Log("Stop> Task 'taskCheckLocks' ended", LogInfo.Info, VerbosityInfoLevel.V3);
        }

        public void GenerateLocalLocks()
        {
            Logger.Log("GenerateLocalLocks> Task 'taskGenerateLocks' started", LogInfo.Info, VerbosityInfoLevel.V3);
            while (running)
            {

                string lockFilename = Path.Combine(LocalShare, OpenFilesFilename);
                generator.CreateFile(lockFilename, LocalShare, Exceptions);
                Thread.Sleep(GenerationDelay);
            }
        }
         
        public void CheckRemoteLocks()
        {
            Logger.Log("CheckRemoteLocks> Task 'taskCheckLocks' started", LogInfo.Info, VerbosityInfoLevel.V3);
            while (running)
            {
                foreach (var remoteLocation in RemoteLocations)
                {

                    //Copy file from remote host
                    var hostName = GetHostName(remoteLocation);
                    string src = Path.Combine(remoteLocation, OpenFilesFilename);
                    var tempFilename = hostName + "_" + OpenFilesFilename;
                    string dst = Path.Combine(WorkingFolder, tempFilename);
                    try
                    {
                        File.Copy(src, dst, true);
                    }
                    catch(Exception ex)
                    {
                        Logger.Log("CheckRemoteLocks> Error on file copy", LogInfo.Error, VerbosityInfoLevel.V1, ex.ToString());
                    }

                    try
                    { 
                        var locks = parser.ParseFile(dst, LocalShare);
                        if (locks != null && locks.Count > 0)
                        { 
                            //Cycle on current locks
                            foreach (var fileLock in locks)
                            {
                                if (!lockedFiles.ContainsKey(fileLock.Filename))
                                {
                                    LockFile(fileLock.Filename);
                                    Logger.Log("CheckRemoteLocks> Lock {0} added from location {1}", LogInfo.Info, VerbosityInfoLevel.V3, fileLock.Filename, remoteLocation);
                                }
                            }
                        }

                        //Cycle on locked files
                        for (int i = 0; lockedFiles != null && i < lockedFiles.Count; i++)
                        {
                            var appLock = lockedFiles.ElementAt(i);
                            var lockCheck = locks == null || locks.Where(l => l.Filename == appLock.Key).FirstOrDefault() == null;
                            if (lockCheck)
                            {
                                //lock has been removed
                                var filename = appLock.Key;
                                UnlockFile(filename);
                                Logger.Log("CheckRemoteLocks> Lock {0} removed", LogInfo.Info, VerbosityInfoLevel.V3, filename);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.Log("CheckRemoteLocks> Error on file parse", LogInfo.Error, VerbosityInfoLevel.V1, ex.ToString());
                    }
                }

                Thread.Sleep(CheckLocksDelay);
            }
        }

        private string GetHostName(string remoteLocation)
        {
            if (!remoteLocation.Contains(@"\\"))
                return null;
            var hostname = remoteLocation.Substring(remoteLocation.IndexOf(@"\\") + 2);
            if (!hostname.Contains(@"\"))
                return hostname;
            hostname = hostname.Substring(0, hostname.IndexOf(@"\"));
            return hostname;
       }

        public bool IsFileLocked(string filename)
        {
            try
            {
                if (File.Exists(filename))
                {
                    FileStream fs =
                        new FileStream(filename, FileMode.Open, FileAccess.ReadWrite);
                    fs.Close();
                }
            }
            catch (IOException)
            {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Lock a file by opening an handle (FileStream)
        /// </summary>
        /// <param name="filename"></param>
        public void LockFile(string filename)
        {
            FileStream fs = null;
            try
            {
                fs = System.IO.File.Open(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                fs.Lock(0, fs.Length);
            }
            catch
            {

            }
            lockedFiles[filename] = fs;
        }
        /// <summary>
        /// Unlock a file by closing the handle associated with
        /// </summary>
        /// <param name="filename"></param>
        public void UnlockFile(string filename)
        {
            var fs = lockedFiles[filename];
            if (fs != null)
            {
                try
                {
                    fs.Unlock(0, fs.Length);
                    fs.Dispose();
                }
                catch
                {

                }
            }
            lockedFiles.Remove(filename);
        }
    }
}
