using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace TraceReader
{
    class Program
    {
        static void Main(string[] args)
        {
            var data = JsonReader.ReadJson("app.json");

            data = GetFiles(data);

            try
            {

                try
                {
                    Parallel.ForEach(data, item =>
                    {
                        try
                        {
                            GetTrace(item.Source, item.Destination, item.Keyword);
                        }
                        catch (Exception ex)
                        {
                            LogError(ex);
                        }
                    });
                }
                catch (AggregateException ex)
                {
                    foreach (var innerEx in ex.InnerExceptions)
                    {
                        LogError(innerEx);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        static void GetTrace(string SourceFile, string DestinationFile, string[] Keyword)
        {
            try
            {
                DestinationFile = GetDestinationFile(DestinationFile, SourceFile);
                long lastPosition = GetInitialFilePosition(SourceFile);
                string lastLine = ReadLastLine(SourceFile).Trim();

                while (!string.IsNullOrEmpty(lastLine))
                {
                    string[] newLines = ReadNewLines(SourceFile, ref lastPosition);

                    foreach (string line in newLines)
                    {
                        // if (!string.IsNullOrWhiteSpace(Keyword))
                        if (Keyword.Length > 0)
                        {
                            //    if (line.ToLower().Contains(Keyword.ToLower()))
                            if(IsKeywordMatching(line,Keyword))
                            {
                                Console.WriteLine($"Filename: {SourceFile}");

                                Console.WriteLine(line);
                                File.AppendAllText(DestinationFile, line + Environment.NewLine);
                            }
                        }
                        else
                        {
                            Console.WriteLine(line);
                            File.AppendAllText(DestinationFile, line + Environment.NewLine);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }
        }

        static string ReadLastLine(string filePath)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                if (fileStream.Length == 0)
                {
                    return null; // File is empty
                }

                // Start reading from the end of the file
                fileStream.Seek(-2, SeekOrigin.End);

                while (fileStream.Position > 0)
                {
                    int currentByte = fileStream.ReadByte();
                    fileStream.Seek(-2, SeekOrigin.Current);

                    if (currentByte == '\n')
                    {
                        // Found the end of the last line
                        byte[] buffer = new byte[fileStream.Length - fileStream.Position];
                        fileStream.Read(buffer, 0, buffer.Length);
                        var txt = Encoding.Default.GetString(buffer);
                        //   txt = NumberOfLines(txt) ??txt ;
                        return txt;
                    }
                }

                // If the file has only one line or no line break at all
                fileStream.Seek(0, SeekOrigin.Begin);
                byte[] finalBuffer = new byte[fileStream.Length];
                fileStream.Read(finalBuffer, 0, finalBuffer.Length);

                return Encoding.Default.GetString(finalBuffer);
            }
        }

        static string[] ReadNewLines(string filePath, ref long lastPosition)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                fileStream.Seek(lastPosition, SeekOrigin.Begin);

                using (StreamReader reader = new StreamReader(fileStream))
                {
                    string content = reader.ReadToEnd();
                    lastPosition = fileStream.Position; // Update last position

                    // Split content into lines
                    string[] lines = content.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                    return lines;
                }
            }
        }

        static long GetInitialFilePosition(string filePath)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                return fileStream.Length;
            }
        }

        static string GetDestinationFile(string destFilename, string sourceFile)
        {
            if (string.IsNullOrWhiteSpace(destFilename))
            {
                return Path.GetFileName(sourceFile);
            }
            string value = null;
            var ext = Path.GetExtension(destFilename);

            if (!string.IsNullOrWhiteSpace(ext))
            {
                return destFilename;
            }

            else
            {
                value = destFilename + "\\" + Path.GetFileName(sourceFile);
            }

            return value;
        }

        static void LogError(Exception ex)
        {
            string errorMessage = $"{DateTime.Now} An error occurred: {ex.Message}";
            Console.WriteLine(errorMessage);
            File.AppendAllText("error.txt", errorMessage + Environment.NewLine);
        }

        static List<SourceDestination> GetFiles(List<SourceDestination> path)
        {
            List<SourceDestination> Files = new List<SourceDestination>();
            foreach (var item in path)
            {
                if (item.Source.Contains("*"))
                {
                    var index = item.Source.LastIndexOf('\\');
                    var folderPath = item.Source.Remove(index);
                    var files = Directory.GetFiles(folderPath);
                    var like = item.Source.Substring(index);
                    like = like.Replace('*', ' ').Replace('\\', ' ').Trim();
                    foreach (var item3 in files)
                    {
                        if (Path.GetFileName(item3).Contains(like))
                        {
                            Files.Add(new SourceDestination() { Source = item3, Destination = item.Destination, Keyword = item.Keyword });

                        }
                    }

                }

                if (Directory.Exists(item.Source))
                {
                    var files = Directory.GetFiles(item.Source);

                    foreach (var item2 in files)
                    {
                        Files.Add(new SourceDestination() { Source = item2, Destination = item.Destination, Keyword = item.Keyword });
                    }

                }

                else if (!string.IsNullOrWhiteSpace(Path.GetExtension(item.Source)))
                {
                    Files.Add(item);
                }
            }

            return Files;
        }

        static bool IsKeywordMatching(string line, string[] keywords)
        {
            foreach (var item in keywords)
            {
                if (!line.ToLower().Contains(item.ToLower()))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
