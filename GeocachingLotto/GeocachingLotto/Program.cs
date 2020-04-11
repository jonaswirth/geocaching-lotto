using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace GeocachingLotto
{
    class Program
    {
        //Constants
        private const string baseUrl = "https://www.navikatzen.com/lotto/";
        private const string lottoUrl = baseUrl + "index.php?page=lotto&time=@time&@time";
        private const string winnerUrl = baseUrl + "winner.php?passwort=@password";

        private const string searchTime = "<INPUT TYPE=hidden NAME=time VALUE=\"";
        private const string searchHits = "<B>Du hast";
        private const string searchPassword = "<script type=\"text/javascript\">popup(\'";

        private static Dictionary<int, string> RegisterdHits = new Dictionary<int, string>();
        private static int[] hitStatistics = new int[7];
        private static string ExecutionId;

        private static int[] luckyNumbers = new int[6];
        private static int delay = 175;
        private static string basePath = "C:\\lotto";
        private static string filePath;

        static void Main(string[] args)
        {
            Setup();
            EnterLuckyNumbers();
            Confirm();

            Console.WriteLine("Continue? y/n");
            if (Console.ReadKey().KeyChar == 'y')
            {
                InitializeFile();
                Console.WriteLine("\n************* Execute *************");
                Console.WriteLine("This might take a while :)\n");

                uint iteration = 0;
                while (true)
                {
                    iteration++;

                    if(iteration % 100 == 0)
                    {
                        Console.WriteLine($"{iteration} attempts");
                    }
                    if(iteration % 1000 == 0)
                    {
                        Console.WriteLine("************* Current Stats *************");
                        Console.WriteLine($"Total attempts: {iteration}\nHits:\n0: {hitStatistics[0]}\n1: {hitStatistics[1]}\n2: {hitStatistics[2]}\n3: {hitStatistics[3]}\n4: {hitStatistics[4]}\n5: {hitStatistics[5]}\n6: {hitStatistics[6]}\n");
                    }

                    //Call is deliberately not awaited
                    CallUrl();
                    Thread.Sleep(delay);
                }
            }
            Console.ReadKey();
        }

        public static void Setup()
        {
            ExecutionId = Regex.Replace(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), "[/+=]", "").Substring(10);

            Console.WriteLine($"New execution with Id: {ExecutionId}");
            Console.WriteLine("************* Config *************");

            Console.WriteLine("Use default delay? (175ms) y/n");
            if(Console.ReadKey().KeyChar != 'y')
            {
                Console.Write("\nEnter delay (ms): ");
                delay = int.Parse(Console.ReadLine());
            }
            Console.WriteLine("\nUse default file location? (C:\\lotto\\...) y/n");
            if (Console.ReadKey().KeyChar != 'y')
            {
                Console.Write("\nEnter path: ");
                basePath = Console.ReadLine();
            }
        }

        public static void EnterLuckyNumbers()
        {
            Console.WriteLine("\nEnter your lucky numbers:");
            for(int i = 0; i < 6; i++)
            {
                Console.Write($"{i + 1}: ");
                var next = int.Parse(Console.ReadLine());
                if(next < 1 || next > 45)
                {
                    throw new ArgumentException("Number must be between 1 and 45");
                }
                luckyNumbers[i] = next;
            }
        }

        public static void Confirm()
        {
            Console.WriteLine("\nDo you want to proceed with following config?:");
            Console.WriteLine($"Delay: {delay} ms");
            Console.WriteLine($"File: {Path.Combine(basePath, $"{ExecutionId}.txt")}");
            Console.WriteLine($"Lucky Numbers: {luckyNumbers[0]}, {luckyNumbers[1]}, {luckyNumbers[2]}, {luckyNumbers[3]}, {luckyNumbers[4]}, {luckyNumbers[5]}");
        }

        private static async Task CallUrl()
        {
            using (var cli = new HttpClient())
            {

                var formContent = new FormUrlEncodedContent(new[]
                    {
                    new KeyValuePair<string, string>("time", "1"),
                    new KeyValuePair<string, string>("action","kukuk"),
                    new KeyValuePair<string, string>("n1", luckyNumbers[0].ToString()),
                    new KeyValuePair<string, string>("n2", luckyNumbers[1].ToString()),
                    new KeyValuePair<string, string>("n3", luckyNumbers[2].ToString()),
                    new KeyValuePair<string, string>("n4", luckyNumbers[3].ToString()),
                    new KeyValuePair<string, string>("n5", luckyNumbers[4].ToString()),
                    new KeyValuePair<string, string>("n6", luckyNumbers[5].ToString())
                });

                var response = await cli.PostAsync(lottoUrl.Replace("@time", "1"), formContent);

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Server responded with Code {response.StatusCode}");
                    return;
                }

                ParseRequest(content);
            }       
        }

        private static async void ParseRequest(string content)
        {
            string time;
            string hits;
            string password = string.Empty;

            var timeStart = content.IndexOf(searchTime) + searchTime.Length;
            var timeEnd = content.IndexOf("\"", timeStart);

            var hitsStart = content.IndexOf(searchHits) + searchHits.Length;

            hits = content.Substring(hitsStart + 1, 1);

            var h = int.Parse(hits);

            hitStatistics[h]++;

            if (h < 3)
            {
                return;
            }
                
            var passwordStart = content.IndexOf(searchPassword) + searchPassword.Length;
            var passwordEnd = content.IndexOf("');", passwordStart);
            password = content.Substring(passwordStart, passwordEnd - passwordStart);

            time = content.Substring(timeStart, timeEnd - timeStart);

            await HandleHit(h, password);        
        }

        public static async Task HandleHit(int hits, string password)
        {
            if (RegisterdHits.ContainsKey(hits))
                return;

            var coordinates = await GetCoordinates(password);

            string msg = $"{hits} hits! Password: {password} Coordinates: {coordinates}";

            RegisterdHits.Add(hits, msg);
            WriteToFile(msg);
            Console.WriteLine($"{hits} hits! Password: {password} Coordinates: {coordinates}");
        }

        public static async Task<string> GetCoordinates(string password)
        {
            using(var client = new HttpClient())
            {
                var result = await client.GetAsync(winnerUrl.Replace("@password", password));
                var content = await result.Content.ReadAsStringAsync();

                var coordSearch = "Du findest deine Belohnung an folgender Stelle:</font><br>\n  ";
                var coordStart = content.IndexOf(coordSearch) + coordSearch.Length;
                var coordEnd = content.IndexOf("<br>", coordStart);

                var coord = content.Substring(coordStart, coordEnd - coordStart);

                return coord.Replace("&deg;", "°");
            }
        }

        public static void WriteToFile(string msg)
        {
            var path = "C:\\lotto\\lotto.txt";

            if (!File.Exists(path))
            {
                using (var file = File.CreateText(path))
                {
                    file.WriteLine("Initialized");
                }
            }

            using (StreamWriter sw = File.AppendText(Path.Combine(basePath, $"{ExecutionId}.txt")))
            {
                sw.WriteLine(DateTime.Now.ToString());
                sw.WriteLine(msg);
            }
        }

        public static void InitializeFile()
        {
            using (var file = File.CreateText(Path.Combine(basePath, $"{ExecutionId}.txt")))
            {
                file.WriteLine(DateTime.Now.ToString());
                file.WriteLine($"ExecutionId: {ExecutionId}");
                file.WriteLine("***********************");
            }
        }
    }
}
