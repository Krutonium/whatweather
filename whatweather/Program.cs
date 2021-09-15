using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.Json;

namespace whatweather
{
    class Program
    {
        private static config Config = new config();
        private static WebClient client = new WebClient();
        private static string oneTimeLookup = "";
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                foreach (var item in args)
                {
                    oneTimeLookup += " " + item;
                }
            }
            Config.Location = "";
            Config.showGraphic = true;
            Program prog = new Program();
            prog._Main();
            //Get us out of static land
        }

        private void _Main()
        {
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.DoNotVerify), "whatweather");
            string configFile = Path.Combine(appData, "config.json");
            if (!Directory.Exists(appData))
            {
                Directory.CreateDirectory(appData);
                Console.WriteLine("Created Config Dir");
            }
            if (File.Exists(configFile))
            {
                Config = JsonSerializer.Deserialize<config>(File.ReadAllText(configFile));
            }
            else
            {
                var JSO = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                File.WriteAllText(configFile,  JsonSerializer.Serialize(Config, JSO));
                Console.WriteLine("Wrote config file to " + configFile);
            }
            if (oneTimeLookup != "")
            {
                Config.Location = oneTimeLookup;
            }
            if (AreWeOnline() == true)
            {
                Console.WriteLine("We're offline");
                Environment.Exit(0);
            }
            if (Config.showGraphic)
            {
                //Load the first chunk of the page and display it
                var weather = getWeatherAscii();
                Console.WriteLine(weather);
            }
            else
            {
                //Load the JSON and build our display
                var weather = getWeather();
                Console.WriteLine("Current Weather in {0}", weather.nearest_area[0].areaName[0].value);
                Console.WriteLine("{0}C, Wind is blowing {1}km/h to the {2}", 
                    weather.current_condition[0].temp_C,
                    weather.current_condition[0].windspeedKmph, 
                    DegreesToCompass(int.Parse(weather.current_condition[0].winddirDegree)));
                Console.WriteLine("Humidity is currently {0}%, and cloudcover is currently {1}%", 
                    weather.current_condition[0].humidity, weather.current_condition[0].cloudcover);
                Console.WriteLine("The weather can be described as: {0} ", weather.current_condition[0].weatherDesc[0].value);
            }
            
            
        }
        
        string DegreesToCompass(int degrees)
        {
            string[] caridnals = { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW", "N" };
            var test = caridnals[(int)Math.Round(((double)degrees * 10 % 3600) / 225)];
            return test.ToString();
        }
        
        
        private weather.wttrin getWeather()
        {
            client.Headers.Add("user-agent", "github.com/krutonium");
            weather.wttrin theWeather =
                JsonSerializer.Deserialize<weather.wttrin>(client.DownloadString("https://wttr.in/" + Config.Location + "?format=j1"));
            
            return theWeather;
        }

        private string getWeatherAscii()
        {
            client.Headers.Add("user-agent", "cURL");
            var locWea = client.DownloadString("https://wttr.in/" + Config.Location);
            string theWeather = "";
            var split = locWea.Split(Environment.NewLine);
            // for (int i = 0; i != 6; i++)
            // {
            //     theWeather += split[i] + Environment.NewLine;
            // }
            bool cont = true;
            int i = 0;
            if (locWea.Contains("We were unable to find your location"))
            {
                return "Bad Location";
            }
            while (cont == true)
            {
                if (i >= split.Length)
                {
                    cont = false;
                }
                else
                {
                    if (split[i].Contains("───────") | split[i].Contains("Follow"))
                    {
                        cont = false;
                    }
                    else
                    {
                        theWeather += split[i] + Environment.NewLine;
                    }
                }

                i++;
            }
            return theWeather;
        }

        private bool AreWeOnline()
        {
            try { 
                Ping myPing = new Ping();
                String host = "google.com";
                byte[] buffer = new byte[32];
                int timeout = 500;
                PingOptions pingOptions = new PingOptions();
                PingReply reply = myPing.Send(host, timeout, buffer, pingOptions);
                Debug.Assert(reply != null, nameof(reply) + " != null");
                return (reply.Status == IPStatus.Success);
            }
            catch (Exception) {
                return false;
            }
        }

        public class config
        {
            public string Location { get; set; }
            public bool showGraphic { get; set; }
        }
    }
}
