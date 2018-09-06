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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenFilesLocker
{
    /// <summary>
    /// This class is used to generate the 'openfiles.dat' file
    /// </summary>
    public class OpenFilesGenerator : OpenFilesBase
    {
    
        public void CreateFile(string filename, string path, List<string> exceptions = null)
        {
            //Execute command and generate file
            var psi = new ProcessStartInfo(@"openfiles.exe", "/query /FO CSV /NH /V"); //{ CreateNoWindow = true, WorkingDirectory = path }
            psi.RedirectStandardOutput = true;
            psi.UseShellExecute = false;
            var proc = Process.Start(psi);
            string result = "";

            using (StreamReader reader = proc.StandardOutput)
            {
                result = reader.ReadToEnd();
            }
            proc.WaitForExit();


            try
            {
                var writer = new StreamWriter(filename);
                writer.AutoFlush = true;

                //Parse file and obtain info
                //var allData = File.ReadAllLines(Path.Combine(path, filename));
                var allData = result.Split(new[] { "\r\n" }, StringSplitOptions.None);
                bool headerSkipped = false;

                for (int i = 0; i < allData.Length; i++)
                {
                    var lineData = allData[i];
                    if (lineData.Contains(headerSeparator))
                    {
                        lineData = Regex.Replace(lineData, "-{5,}", "");
                        i += 1; //skip header
                        lineData = allData[i];
                        headerSkipped = true;
                    }

                    if (headerSkipped)
                    {
                        lineData = lineData.Trim().Replace("\"", "");
                        if (lineData.Contains(tokenSeparator))
                        {

                            var tokens = lineData.Split(new[] { tokenSeparator }, StringSplitOptions.RemoveEmptyEntries);
                            if (tokens.Length == 7)
                            {
                                string lockFilename = tokens[6];
                                if (lockFilename != null && File.Exists(lockFilename) && lockFilename.ToLower().StartsWith(path.ToLower())) //add only FILE at specified path
                                {
                                    lockFilename = RelativeFilename(lockFilename, path);
                                    if (exceptions != null)
                                    {
                                        var checkException = exceptions.Where(exc => lockFilename.EndsWith(exc)).FirstOrDefault();
                                        if (checkException != null)
                                            continue;
                                    }
                                    tokens[6] = lockFilename;
                                    writer.WriteLine(string.Join(",", tokens));

                                }
                            }
                        }
                    }
                }

                writer.Close();

            }
            catch (Exception ex)
            {

                
            }


        }


    }
}
