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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenFilesLocker
{
    /// <summary>
    /// This class contains methods to parse 'openfile.dat' and generate a list of 'OpenFilesInfo' objects
    /// </summary>
    public class OpenFilesParser : OpenFilesBase
    {
        public List<OpenFilesInfo> ParseFile(string filename, string basePath)
        {
            var ofInfos = new List<OpenFilesInfo>();
            string result = File.ReadAllText(filename);
            //Parse file and obtain info
            var allData = result.Split(new[] { "\r\n" }, StringSplitOptions.None);

            foreach (var lineData in allData)
            {
                if (lineData.Contains(tokenSeparator))
                {

                    var tokens = lineData.Split(new[] { tokenSeparator }, StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length == 7)
                    {
                        int lockNumber = 0;
                        int.TryParse(tokens[3], out lockNumber);

                        OpenModeEnum openMode = OpenModeEnum.WriteAndRead;
                        switch (tokens[5])
                        {
                            case "Read":
                                openMode = OpenModeEnum.Read;
                                break;
                            case "Write":
                                openMode = OpenModeEnum.Write;
                                break;
                            default:
                                openMode = OpenModeEnum.WriteAndRead;
                                break;
                        }

                        var l = new OpenFilesInfo
                        {
                            Hostname = tokens[0],
                            ID = tokens[1],
                            AccessedBy = tokens[2],
                            LockType = tokens[3],
                            Locks = lockNumber,
                            OpenMode = openMode,
                            Filename = tokens[6],
                            Timestamp = DateTime.Now
                        };
                        l.Filename = AbsoluteFilename(l.Filename, basePath);
                        ofInfos.Add(l);
                    }
                }

            }
            return ofInfos;
        }

    }
}
