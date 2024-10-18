using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TraceReader
{
    class Class1
    {
        public static Dictionary<string, string> AddDataToDictionary(string file)
        {
            Dictionary<string, string> keyValuePairs = null;
            if (File.Exists(file))
            {
                keyValuePairs = new Dictionary<string, string>();

                var data = File.ReadAllLines(file);

                try
                {
                    foreach (var item in data)
                    {
                        if (item.Contains('\t'))
                        {
                            var text = item.Split('\t');

                            var key = text[0].Trim();
                            var val = text[1].Trim();

                            if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(val) && !keyValuePairs.ContainsKey(key))
                            {
                                keyValuePairs.Add(key, val);
                            }
                        }
                    }
                }

                catch (Exception e)
                {
                    //Trace(e.Message);
                }
            }

            else
            {
               // Trace($"File {file} does not exist");
            }

            return keyValuePairs;
        }

    }
}
