using System;
using System.Collections.Generic;
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
        private readonly string[] numbers = { "1", "5", "9", "11", "31", "42" };

        private const string searchTime = "<INPUT TYPE=hidden NAME=time VALUE=\"";
        private const string searchHits = "<B>Du hast";
        private const string searchPassword = "<script type=\"text/javascript\">popup(\'";

        static async Task Main(string[] args)
        {
            bool run = false;
            Console.WriteLine("Execute? y/n");
            if (Console.ReadKey().KeyChar == 'y')
            {
                Console.WriteLine("");
                run = true;
                while (run)
                {
                    CallUrl();
                    Thread.Sleep(240);
                }
            }
        }

        private static async Task CallUrl()
        {
            var cli = new HttpClient();

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

        private static void ParseRequest(string content)
        {
            string time;
            string hits;
            string password = string.Empty;

            var timeStart = content.IndexOf(searchTime) + searchTime.Length;
            var timeEnd = content.IndexOf("\"", timeStart);

            var hitsStart = content.IndexOf(searchHits) + searchHits.Length;

            hits = content.Substring(hitsStart + 1, 1);

            Console.WriteLine(hits + " hits\n");

            if(hits == "3")
            {
                Console.WriteLine("HIT");
            }

            if(hits == "H")
            {
                Console.WriteLine(content);
            }

            if (int.Parse(hits) < 3)
            {
                return;
            }
                
            var passwordStart = content.IndexOf(searchPassword) + searchPassword.Length;
            var passwordEnd = content.IndexOf("');", passwordStart);
            password = content.Substring(passwordStart, passwordEnd - passwordStart);

            time = content.Substring(timeStart, timeEnd - timeStart);
            

            Console.WriteLine(time);
            Console.WriteLine(hits);
            Console.WriteLine(password);
            
            var pw3 = "jhr324ij!fh1";
        }
    }
}
