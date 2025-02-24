using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Write("Podaj kwotę w PLN: ");
        double kwotaPLN = double.Parse(Console.ReadLine());

        double kursUSD = await PobierzKursUSDAsync();
        if (kursUSD > 0)
        {
            double kwotaUSD = kwotaPLN / kursUSD;
            Console.WriteLine($"{kwotaPLN} PLN to {kwotaUSD:F2} USD");
        }
        else
        {
            Console.WriteLine("Nie udało się pobrać kursu USD.");
        }

    }

    static async Task<double> PobierzKursUSDAsync()
    {
        string url = "https://api.nbp.pl/api/exchangerates/rates/a/usd/?format=json";
        using (HttpClient client = new HttpClient())
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                JObject dane = JObject.Parse(responseBody);
                return dane["rates"][0]["mid"].Value<double>();
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Błąd podczas pobierania danych: {e.Message}");
                return 0;
            }
        }
    }
}
