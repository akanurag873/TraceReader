using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace TraceReader
{
    class JsonReader
    {
        internal static List<SourceDestination> ReadJson(string file)
        {
            List<SourceDestination> sourcesDestinations = null;
            try
            {
                using (StreamReader r = new StreamReader(file))
                {
                    string json = r.ReadToEnd();

                    sourcesDestinations = JsonConvert.DeserializeObject<List<SourceDestination>>(json);

                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("File not found.");
            }
            catch (JsonException)
            {
                Console.WriteLine("Invalid JSON format.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            return sourcesDestinations;
        }
    }

    class SourceDestination
    {
        public string Source { get; set; }
        public string Destination { get; set; }
        public string[] Keyword { get; set; }
    }
}



