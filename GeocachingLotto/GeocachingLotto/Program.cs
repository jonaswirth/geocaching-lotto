using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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

        static async Task Main(string[] args)
        {
            bool run = false;
            Console.WriteLine("Execute? y/n");
            if (Console.ReadKey().KeyChar == 'y')
            {
                Console.WriteLine("Executing");
                Console.WriteLine("This might take a while :)");

                run = true;
                int iteration = 0;
                while (run)
                {
                    iteration++;

                    if(iteration % 100 == 0)
                    {
                        Console.WriteLine($"{iteration} interations...");
                    }

                    CallUrl();
                    Thread.Sleep(240);
                }
            }
            Console.ReadKey();
        }

        private static async Task CallUrl()
        {
            using (var cli = new HttpClient())
            {

                var formContent = new FormUrlEncodedContent(new[]
                    {
                    new KeyValuePair<string, string>("time", "1"),
                    new KeyValuePair<string, string>("action","kukuk"),
                    new KeyValuePair<string, string>("n1", "1"),
                    new KeyValuePair<string, string>("n2", "2"),
                    new KeyValuePair<string, string>("n3", "3"),
                    new KeyValuePair<string, string>("n4", "4"),
                    new KeyValuePair<string, string>("n5", "5"),
                    new KeyValuePair<string, string>("n6", "6")
                });

                var response = await cli.PostAsync(lottoUrl.Replace("@time", "1"), formContent);

                var content = await response.Content.ReadAsStringAsync();
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


    }
}
