using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DirDiff
{
    /// <summary>
    /// Utility app to purge source input files that already exist somewhere in 
    /// target directory(s).  Created to reduce time to organize files being added 
    /// to a categorized collective.
    /// </summary>
    class Program
    {
        private readonly string _usage =
/*
          1         2         3         4         5         6         7         8
012345678901234567890123456789012345678901234567890123456789012345678901234567890
*/
@"Dir Diff 2023

-mode dedupe    Deduplication mode
-mode renmob    Rename mobile files

Rename mobile file settings:
  -src            Source directory

Dedupe settings:
  -src            Source directory 
  -dest           Destination directory
  -src_only       Search source directory, including subdirs, for dupes.
  -f              File filter expression, defaults to *
  -del_dupe_src   Optional flag to trigger deleting duplicate source files.
  -match_prefix   Match files with same file name prefix
  -match_content  Match files on file content.  Use to detect dupe files, even if renamed.
  -min <num>      Ignore files smaller than <num> Mb


Example Usage:
    - Scenario, copied Phone pics to PC, now want to delete dupes before categorizing into \Pics subfolders
        dirdiff -mode dedupe -src C:\scratch\Phone_2020 -dest C:\scratch\Pics

    - Scenario, delete duplicate files that were partially renamed, find dupe 
      (renamed) files with matching content.

        dirdiff -mode dedupe  -f * -src C:\scratch\Phone_2020 -dest C:\scratch\Pics -match_prefix -match_content -del_dupe_src

    - Scenario, rename

    dirdiff -mode renmob -src C:\Projects(NAS)\CNC_LowRider\assets\drag-race\in -dest C:\Projects(NAS)\CNC_LowRider\assets\drag-race\out
    dirdiff -mode dedupe -src_only F:\ -match_content -minMb 10
";

        private const int Mb = 1024 * 1024;
                                   
        #region Common Cmd line app stuff

        Dictionary<string, string> _args = new Dictionary<string, string>();

        static void Main(string[] args)
        {
            Program p = new Program();
            try
            {
                p.Run(args);
            }
            catch (Exception e)
            {
                Log.Error("Unhandled Error, ex={0}", e);
            }
        }

        private void ParseArgs(string[] args)
        {
            int i = 0;
            while (i < args.Length)
            {
                if (args[i][0] == '-' && i + 1 < args.Length && args[i + 1][0] != '-')
                {
                    _args[args[i++]] = args[i];
                }
                else
                {
                    _args[args[i]] = "";
                }
                i++;
            }

            //Log.Info(_args.Count + " argument(s)");
            //foreach (string key in _args.Keys)
            //{
            //    Log.Info(key + "=" + _args[key]);
            //}

            if (_args.ContainsKey("-h")
                || _args.ContainsKey("-?")
                || _args.ContainsKey("/?"))
            {
                Log.Warn(_usage);
                Environment.Exit(0);
            }
        }

        private string GetArgValue(string name, string defaultValue)
        {
            if (_args.ContainsKey(name))
            {
                return _args[name];
            }
            return defaultValue;
        }

        private string GetArgValue(string[] names, string defaultValue)
        {
            foreach (string name in names)
            {
                if (_args.ContainsKey(name))
                {
                    return _args[name];
                }
            }
            return defaultValue;
        }

        private string GetArgValue(string name)
        {
            if (!_args.ContainsKey(name))
            {
                string msg = "Missing Argument '" + name + "'";
                Log.Error(msg);
                throw new MissingFieldException(msg);
            }
            return _args[name];
        }

        private int GetArgValueInt(string name, int defaultValue)
        {
            if (_args.ContainsKey(name))
            {
                return int.Parse(_args[name]);
            }

            return defaultValue;
        }

        private DateTime GetArgValueDateTime(string name, DateTime defaultValue)
        {
            if (_args.ContainsKey(name))
            {
                return DateTime.Parse(_args[name]);
            }
            return defaultValue;
        }

        #endregion

        private void Run(string[] args)
        {
            DateTime start = DateTime.Now;
            Log.LogTime = false;
            Log.LogMessageType = false;
            Log.HtmlOutput = false;

            ParseArgs(args);

            string mode = GetArgValue("-mode");


            if ("dedupe".Equals(mode, StringComparison.OrdinalIgnoreCase))
            {
                DeduplicateFiles();
            }
            else if ("renmob".Equals(mode, StringComparison.OrdinalIgnoreCase))
            {
                RenameMobFiles();
            }

            DateTime end = DateTime.Now;
            Log.Info($"Duration {end.Subtract(start).TotalSeconds}");
        }

        private void RenameMobFiles()
        {
            string srcDir = GetArgValue("-src");
            //string destDir = GetArgValue("-dest");
            string filter = GetArgValue("-f", "*");


            if (string.IsNullOrWhiteSpace(srcDir))
            {
                Log.Error("Expected non empty -src");
            }

            //if (string.IsNullOrWhiteSpace(destDir))
            //{
            //    Log.Error("Expected non empty -dest");
            //}

            string[] srcFilePaths = Directory.GetFiles(srcDir, filter, SearchOption.AllDirectories);

            // Match based on exact filename
            foreach (string srcFilePath in srcFilePaths)
            {
                string srcFileName = Path.GetFileName(srcFilePath);

                if (IsMobileFileName(srcFileName))
                {
                    string destFileName = null;
                    if (srcFileName.StartsWith("VID_"))
                    {
                        destFileName = Regex.Replace(srcFileName, "VID_([0-9]{4})([0-9]{2})([0-9]{2})_([0-9]{2})([0-9]{2})([0-9]{2})(.*)", "$1-$2-$3 $4-$5-$6_(mob)_$7");
                    }
                    else
                    {
                        // Samsung Galaxy Active 8+
                        destFileName = Regex.Replace(srcFileName, "([0-9]{4})([0-9]{2})([0-9]{2})_([0-9]{2})([0-9]{2})([0-9]{2})(.*)", "$1-$2-$3 $4-$5-$6_(mob)_$7");
                    }

                    Log.Info("Is mobile :" + srcFileName + " -> " + destFileName + " " + srcFilePath + " " + Path.GetDirectoryName(srcFilePath));

                    string destFilePath = Path.Combine(Path.GetDirectoryName(srcFilePath), destFileName);
                    //string destFilePath = Path.Combine(Path.GetDirectoryName(srcFilePath), destFileName);
                    if (File.Exists(destFilePath))
                    {
                        Log.Warn($"Dest File already exists, srcFilePath={srcFilePath}");
                        continue;
                    }

                    File.Move(srcFilePath, destFilePath);
                }
            }
        }

        private bool IsMobileFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;

            if (name.IndexOf("-") != -1)
            {
                return false;
            }

            if (name.IndexOf("_") == -1)
            {
                return false;
            }

            if (name.IndexOf("VID_") == 0 && name.EndsWith(".mp4"))
            {
                return true;
            }

            if (name.IndexOf("_") != name.LastIndexOf("_"))
            {
                return false;
            }

            return true;
        }

        private void DeduplicateFiles()
        {
            string srcDir = GetArgValue("-src", "");
            string destDir = GetArgValue("-dest", "");
            bool isSrcDirOnly = false;
            if (string.IsNullOrWhiteSpace(srcDir) && string.IsNullOrEmpty(destDir))
            {
                srcDir = destDir = GetArgValue("-src_only");
                isSrcDirOnly = true;
            }
            string filter = GetArgValue("-f", "*");
            bool isDeletingSrcFiles = (_args.ContainsKey("-del_dupe_src"));
            bool isMatchingPrefix = (_args.ContainsKey("-match_prefix"));
            bool isMatchingContent = (_args.ContainsKey("-match_content"));
            int minSizeMb = GetArgValueInt("-minMb", 0);

            if (string.IsNullOrWhiteSpace(srcDir))
            {
                Log.Error("Expected non empty -src");
            }

            if (string.IsNullOrWhiteSpace(destDir))
            {
                Log.Error("Expected non empty -dest");
            }

            string[] srcFilePaths = Directory.GetFiles(srcDir, filter, SearchOption.AllDirectories);
            string[] destFilepaths = Directory.GetFiles(destDir, filter, SearchOption.AllDirectories);
            var destFileNames = destFilepaths.Select(f => Path.GetFileName(f));

            var dupeFilePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Match based on exact filename
            foreach(string srcFilePath in srcFilePaths)
            {
                string srcFileName = Path.GetFileName(srcFilePath);

                if (destFileNames.Contains(srcFileName))
                {

                    var matchingDestFilePaths =
                        destFilepaths.Where(f => f.EndsWith("\\" + srcFileName)).ToList();

                    foreach(var destFilePath in matchingDestFilePaths)
                    {

                        if (string.Equals(srcFilePath, destFilePath, StringComparison.OrdinalIgnoreCase))
                        {
                            // Skip without warning if we're just searching a src directory
                            if (isSrcDirOnly)
                            {
                                continue;
                            }

                            Log.Warn($"1 Check args!  Ignoring to avoid deleting only copy.  Source and Dest path match {srcFilePath}");
                            continue;
                        }

                        // Skip files with different content
                        if (isMatchingContent)
                        {
                            if (!FileCompare2(destFilePath, srcFilePath))
                            {
                                continue;
                            }
                        }

                        // Skip small files
                        if (minSizeMb > 0)
                        {
                            FileInfo srcFileInfo = new FileInfo(srcFilePath);
                            if (srcFileInfo.Length < minSizeMb * Mb)
                            {
                                continue;
                            }
                        }

                        // Skip existing match that was already found but with src and dest swapped
                        string keyVal;
                        if (dupeFilePaths.TryGetValue(destFilePath, out keyVal) && keyVal == srcFilePath)
                        {
                            continue;
                        }

                        dupeFilePaths[srcFilePath] = destFilePath;
                    }
                }
            }

            // Match based on prefix
            if (isMatchingPrefix)
            {
                foreach (string srcFilePath in srcFilePaths)
                {
                    string srcFileName = Path.GetFileName(srcFilePath);
                    string srcFileNameNoExt = Path.GetFileNameWithoutExtension(srcFilePath);

                    foreach(var destFileName in destFileNames)
                    {
                        // Skip mismatch
                        if (!destFileName.StartsWith(srcFileNameNoExt))
                        {
                            continue;
                        }

                        string destFilePath =
                            destFilepaths.Where(f => f.EndsWith("\\" + destFileName)).FirstOrDefault();

                        if (string.Equals(srcFilePath, destFilePath, StringComparison.OrdinalIgnoreCase))
                        {
                            Log.Warn($"2 Check args!  Ignoring to avoid deleting only copy.  Source and Dest path match {srcFilePath}");
                            continue;
                        }

                        long srcFileLength = (new FileInfo(srcFilePath)).Length;
                        long destFileLength = (new FileInfo(destFilePath)).Length;

                        if (srcFileLength != destFileLength)
                        {
                            Log.Warn($"Skipping prefix matched files with diff content lengths, {srcFilePath}");
                            Log.Info($"- {srcFilePath} => {destFilePath}");
                            continue;
                        }

                        // Skip existing match that was already found but with src and dest swapped
                        string keyVal;
                        if (dupeFilePaths.TryGetValue(destFilePath, out keyVal) && keyVal == srcFilePath)
                        {
                            continue;
                        }

                        dupeFilePaths[srcFilePath] = destFilePath;
                    }
                }
            }

            // Match based on content
            if (isMatchingContent)
            {
                // Build dict of dest file paths and their lengths for length based lookups.
                var destFileInfos = destFilepaths.Select(
                    path => new {
                        path = path,
                        fileName = Path.GetFileName(path),
                        length = (new FileInfo(path)).Length 
                    }).ToList();

                foreach (string srcFilePath in srcFilePaths)
                {
                    string srcFileName = Path.GetFileName(srcFilePath);
                    string srcFileNameNoExt = Path.GetFileNameWithoutExtension(srcFilePath);
                    long srcFileLength = (new FileInfo(srcFilePath)).Length;

                    // Skip files already marked as dup by previous checks
                    if (dupeFilePaths.ContainsKey(srcFilePath))
                    {
                        Log.Debug($"Skip content check for known dupe {srcFilePath} => {dupeFilePaths[srcFilePath]}");
                        continue;
                    }

                    // Skip/ignore zero length files.  Assume they're placeholders and exist for a reason.
                    if (srcFileLength == 0)
                    {
                        continue;
                    }

                    var matchingByLengthFileInfos = destFileInfos.Where(
                        info =>
                            (minSizeMb == 0 || info.length > minSizeMb * Mb) && // Ignore small
                            info.length == srcFileLength &&                     // Files with matching lengths
                            info.path != srcFilePath);                          // Ignore self

                    foreach (var matchingByLengthFileInfo in matchingByLengthFileInfos)
                    {
                        string destFilePath = matchingByLengthFileInfo.path;

                        // Log.Verbose($"Comparing content {srcFileLength / (1024 * 1024)} MB {srcFilePath} => {destFilePath}");
                        if (!FileCompare2(srcFilePath, destFilePath))
                        {
                            continue;
                        }

                        Log.Debug($"Content Match, {srcFileLength / Mb} MB {srcFilePath} => {destFilePath}");

                        if (string.Equals(srcFilePath, destFilePath, StringComparison.OrdinalIgnoreCase))
                        {
                            // Skip without warning if we're just searching a src directory
                            if (isSrcDirOnly)
                            {
                                continue;
                            }
                            
                            Log.Warn($"3 Check args!  Ignoring to avoid deleting only copy.  Source and Dest path match {srcFilePath}");
                            
                            continue;
                        }

                        // Skip existing match that was already found but with src and dest swapped
                        string keyVal;
                        if (dupeFilePaths.TryGetValue(destFilePath, out keyVal) && keyVal == srcFilePath)
                        {
                            continue;
                        }

                        dupeFilePaths[srcFilePath] = destFilePath;
                    }
                }
            }

            long totalDupeBytes = 0;
            foreach(string dupeFile in dupeFilePaths.Keys)
            {
                FileInfo fileInfo = new FileInfo(dupeFile);
                totalDupeBytes += fileInfo.Length;
                long dupeFileLengthMb = fileInfo.Length / Mb;

                if (isDeletingSrcFiles)
                {
                    Log.Info($"Deleting {dupeFile}");
                    File.Delete(dupeFile);
                }
                else
                {

                    Log.Info($"{dupeFileLengthMb} MB \"{dupeFile}\" => \"{dupeFilePaths[dupeFile]}\"");
                }
            }

            Log.Info($"Duplicate data summary:");
            Log.Info($"- Dupe files... {dupeFilePaths.Count}/{srcFilePaths.Length}");
            Log.Info($"- Dupe data...  {totalDupeBytes / Mb} MB");

        }


        // This method accepts two strings the represent two files to
        // compare. A return value of 0 indicates that the contents of the files
        // are the same. A return value of any other value indicates that the
        // files are not the same.
        private bool FileCompare(string file1, string file2)
        {
            int file1byte;
            int file2byte;
            FileStream fs1;
            FileStream fs2;

            // Determine if the same file was referenced two times.
            if (file1 == file2)
            {
                // Return true to indicate that the files are the same.
                return true;
            }

            // Open the two files.
            fs1 = new FileStream(file1, FileMode.Open);
            fs2 = new FileStream(file2, FileMode.Open);

            // Check the file sizes. If they are not the same, the files
            // are not the same.
            if (fs1.Length != fs2.Length)
            {
                // Close the file
                fs1.Close();
                fs2.Close();

                // Return false to indicate files are different
                return false;
            }

            // Read and compare a byte from each file until either a
            // non-matching set of bytes is found or until the end of
            // file1 is reached.
            do
            {
                // Read one byte from each file.
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            // Close the files.
            fs1.Close();
            fs2.Close();

            // Return the success of the comparison. "file1byte" is
            // equal to "file2byte" at this point only if the files are
            // the same.
            return ((file1byte - file2byte) == 0);
        }

        const int BYTES_TO_READ = sizeof(Int64);

        private bool FileCompare2(string file1, string file2)
        {
            FileInfo first = new FileInfo(file1);
            FileInfo second = new FileInfo(file2);

            if (first.Length != second.Length)
                return false;

            if (string.Equals(first.FullName, second.FullName, StringComparison.OrdinalIgnoreCase))
                return true;

            int iterations = (int)Math.Ceiling((double)first.Length / BYTES_TO_READ);

            using (FileStream fs1 = first.OpenRead())
            using (FileStream fs2 = second.OpenRead())
            {
                byte[] one = new byte[BYTES_TO_READ];
                byte[] two = new byte[BYTES_TO_READ];

                Array.Clear(one, 0, BYTES_TO_READ);

                for (int i = 0; i < iterations; i++)
                {
                    fs1.Read(one, 0, BYTES_TO_READ);
                    fs2.Read(two, 0, BYTES_TO_READ);
                    

                    if (BitConverter.ToInt64(one, 0) != BitConverter.ToInt64(two, 0))
                        return false;
                }
            }

            return true;
        }
    }
}
