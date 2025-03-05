using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Configuration;
using System.Collections.Specialized;

internal class Program
{
    public static string[,] lista_plikow = new string[,]
    {
        {"\\Wzory1",""},
        {"\\Wzory2",""},
        {"\\Wzory3",""},
        {"\\Wzory4",""},
        {"\\Wzory5",""}
    };

    public static string folderPath = "Allegro\\";

    public static List<string> lista_zdjec = new List<string>() { };
    public static List<string> lista_ofert = new List<string>() { };
    public static string[,] pliki = new string[5,2];
    public static List<string> linki = new List<string>();
    public static List<string> zdjecia = new List<string>();
    public static log4net.ILog log = log4net.LogManager.GetLogger(typeof(Program));
    public static bool czyPomyslnie = true;

    static async Task Main(string[] args)
    {
        log4net.Config.XmlConfigurator.Configure();

        string clientId = ConfigurationManager.AppSettings.Get("clientId");
        string clientSecret = ConfigurationManager.AppSettings.Get("clientSecret");
        string redirectUri = ConfigurationManager.AppSettings.Get("redirectUri");

        string authUrl = $"https://allegro.pl.allegrosandbox.pl/auth/oauth/authorize?response_type=code&client_id={clientId}&redirect_uri={redirectUri}";


        Process.Start(new ProcessStartInfo
        {
            FileName = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
            Arguments = authUrl,
            UseShellExecute = false
        });

        string authorizationCode = await GetAuthorizationCode();
        string tokenUrl = "https://allegro.pl.allegrosandbox.pl/auth/oauth/token";
        string apiUrl = "https://api.allegro.pl.allegrosandbox.pl/sale/offers";
        string accessToken = await GetUserAccessToken(clientId, clientSecret, tokenUrl, authorizationCode, redirectUri);

        
        if (!string.IsNullOrEmpty(accessToken))
        {
            await GetOffers(accessToken, apiUrl);
        }

        //WypiszZdjecia();
        
        string temp = "";
        string folder1 = "";
        string folder2 = "";

        Console.Write("\nPodaj folder nadrzędny (domyślnie Allegro): ");
        temp = Console.ReadLine();
        if(temp != "")
        {
            folderPath = temp;
        }

        if (!Directory.Exists(folderPath))
        {
            Console.WriteLine($"\nFolder {folderPath} nie istnieje.");
            Console.ReadLine();
            return;
        }

        folderPath += "\\";

        Console.Write("\nPodaj pierwszy parametr (Folder z obecnymi zdjęciami): ");
        folder1 = Console.ReadLine();
        while (folder1 == "")
        {
            Console.WriteLine("To pole nie może być puste!");
            Console.Write("\nPodaj pierwszy parametr (Folder z obecnymi zdjęciami): ");
            folder1 = Console.ReadLine();
        }
        folder1 = folderPath + folder1;

        Console.Write("\nPodaj drugi parametr (Folder z nowymi zdjęciami): ");
        folder2 = Console.ReadLine();
        while (folder2 == "")
        {
            Console.WriteLine("To pole nie może być puste!");
            Console.Write("\nPodaj drugi parametr (Folder z nowymi zdjęciami): ");
            folder2 = Console.ReadLine();
        }
        folder2 = folderPath + folder2;

        if (folder1 == folder2)
        {
            AddToLog("Parametry nie mogą być takie same!");
            Console.ReadLine();
            return;
        }

        if (CzyPoprawnyFolder1(folder1) == "error" || CzyPoprawnyFolder2(folder2) == "error")
        {
            Console.ReadLine();
            return;
        }

        if(zdjecia.Count == 5 && linki.Count == 5)
        {
            for (int i = 0; i < 5; i++)
            {
                pliki[i, 0] = File.ReadAllText(linki.ElementAt(i));
                pliki[i, 1] = zdjecia.ElementAt(i);
            }
        }
        else
        {
            AddToLog("Zła struktura plików!");

            Console.ReadLine();
            return;
        }

        for(int j = 0; j < 5; j++)
        {
            string uploadedImageUrl = await UploadImageToAllegro(accessToken, pliki[j, 1]);
            if (!string.IsNullOrEmpty(uploadedImageUrl))
            {
                string path = folder2 + lista_plikow[j, 0] + ".txt";
                File.WriteAllText(path, uploadedImageUrl);
                Console.WriteLine($"\nPrzesłany obraz pod adresem: {uploadedImageUrl}");
                await UpdateOfferImage(accessToken, pliki[j, 0], uploadedImageUrl);
            }
            else
            {
                AddToLog("Błąd przy zamianie zdjęć!");
                Console.ReadLine();
                return;
            }
        }

        if(czyPomyslnie)
        {
            Console.WriteLine("\nPomyślnie wykonano!");
        }
        else
        {
            Console.WriteLine();
            AddToLog("Wykonano z błędami!");
        }
        Console.ReadLine();
        return;
    }

    static void AddToLog(string komunikat)
    {
        log.Error(komunikat);
        Console.WriteLine(komunikat);
    }
     
    static string CzyPoprawnyFolder1(string path)
    {
        List<string> zdjecia = new List<string>();

        if (!Directory.Exists(path))
        {
            AddToLog($"Folder {path} nie istnieje!");
            return "error";
        }

        string[] fileEntries = Directory.GetFiles(path);

        foreach (string fileName in fileEntries)
        {
            if (fileName[fileName.Length - 3] == 't' && fileName[fileName.Length - 2] == 'x' && fileName[fileName.Length - 1] == 't')
            {
                linki.Add(fileName);
            }
            else if (fileName[fileName.Length - 3] == 'j' && fileName[fileName.Length - 2] == 'p' && fileName[fileName.Length - 1] == 'g')
            {
                zdjecia.Add(fileName);
            }
        }

        foreach(string elem in zdjecia)
        {
            for (int i = 0; i < 5; i++)
            {
                if (elem == path + lista_plikow[i, 0] + ".jpg")
                {
                    lista_plikow[i, 1] = "1";
                }
            }
        }

        for (int j = 0; j < 5; j++)
        {
            if (lista_plikow[j, 1] != "1")
            {
                AddToLog("Nazwy zdjęć w pierwszym folderze są niepoprawne!");
                return "error";
            }
            else
            {
                lista_plikow[j, 1] = "";
            }
        }

        foreach (string elem in linki)
        {
            for (int i = 0; i < 5; i++)
            {
                if (elem == path + lista_plikow[i, 0] + ".txt")
                {
                    lista_plikow[i, 1] = "1";
                }
            }
        }

        for (int j = 0; j < 5; j++)
        {
            if (lista_plikow[j, 1] != "1")
            {
                AddToLog("Nazwy plików tekstowych w pierwszym folderze są niepoprawne!");
                return "error";
            }
            else
            {
                lista_plikow[j, 1] = "";
            }
        }
        return "";
    }
    static string CzyPoprawnyFolder2(string path)
    {
        if (!Directory.Exists(path))
        {
            AddToLog($"Folder {path} nie istnieje!");
            return "error";
        }
        string[] fileEntries = Directory.GetFiles(path);

        foreach (string fileName in fileEntries)
        {
            if (fileName[fileName.Length-3] == 'j' && fileName[fileName.Length-2] == 'p' && fileName[fileName.Length-1] == 'g')
            {
                zdjecia.Add(fileName);
                for (int i = 0; i < 5; i++)
                {
                    if (fileName == path + lista_plikow[i, 0] + ".jpg")
                    {
                        lista_plikow[i, 1] = "1";
                    }
                }
            }     
        }
        
        for(int j = 0; j < 5; j++)
        {
            if(lista_plikow[j, 1] != "1")
            {
                AddToLog("Nazwy plików w drugim folderze są niepoprawne!");
                return "error";
            }
            else
            {
                lista_plikow[j, 1] = "";
            }
        }
        return "";
    }


    static async Task<string> GetAuthorizationCode()
    {
        var listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:5000/callback/");
        listener.Start();

        var context = await listener.GetContextAsync();
        var response = context.Response;
        string code = context.Request.QueryString["code"];

        string responseString = @"
            <html>
                <head>
                    <meta charset=""UTF-8"">
                    <style>
                        body {
                            font-family: Arial, sans-serif;
                            background-color: #f4f7f6;
                            margin: 0;
                            padding: 0;
                            height: 100vh;
                            text-align: center;
                        }
                        .container {
                            background-color: #ffffff;
                            padding: 30px;
                            border-radius: 8px;
                            box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1);
                            max-width: 400px;
                            width: 100%;
                            margin-top: 50px; 
                            margin-left: auto;
                            margin-right: auto;
                        }
                        h1 {
                            color: #4CAF50;
                            font-size: 24px;
                        }
                        p {
                            font-size: 16px;
                            color: #555;
                            margin-top: 10px;
                        }
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <h1>Autoryzacja pomyślnie zrealizowana!</h1>
                        <p>Teraz możesz zamknąć to okno!</p>
                        <script>
                            setTimeout(() => { window.close(); }, 0);
                        </script>
                    </div>
                </body>
            </html>";

        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        var output = response.OutputStream;
        await output.WriteAsync(buffer, 0, buffer.Length);
        output.Close();
        listener.Stop();

        return code;
    }
    static void WypiszZdjecia()
    {
        foreach (string elem in lista_zdjec)
        {
            Console.WriteLine(elem);
        }
    }

    static async Task<string> GetUserAccessToken(string clientId, string clientSecret, string tokenUrl, string authorizationCode, string redirectUri)
    {
        using (var client = new HttpClient())
        {
            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

            var content = new StringContent($"grant_type=authorization_code&code={authorizationCode}&redirect_uri={redirectUri}",
                Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await client.PostAsync(tokenUrl, content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                AddToLog($"Błąd pobierania tokena użytkownika: {response.StatusCode}\nTreść odpowiedzi: {responseString}");
                return null;
            }

            var tokenJson = JObject.Parse(responseString);
            string accessToken = tokenJson["access_token"].ToString();

            return accessToken;
        }
    }

    static async Task GetOffers(string accessToken, string apiUrlOffers)
    {
        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.allegro.public.v1+json"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Add("Accept-Language", "pl-PL");

            HttpResponseMessage response = await client.GetAsync(apiUrlOffers);
            string responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                AddToLog($"Błąd przy pobieraniu ofert: {response.StatusCode}\nTekst odpowiedzi: {responseString}");
                return;
            }

            JObject jsonResponse = JObject.Parse(responseString);

            if (jsonResponse.ContainsKey("offers"))
            {
                JArray offers = (JArray)jsonResponse["offers"];
                if (offers.Count == 0)
                {
                    AddToLog("Brak ofert do pobrania!");
                    return;
                }

                foreach (var offer in offers)
                {
                    string offerId = offer["id"].ToString();
                    lista_ofert.Add(offerId);
                    lista_zdjec.Add(offerId);
                    string offerDetailsUrl = $"https://api.allegro.pl.allegrosandbox.pl/sale/product-offers/{offerId}";
                    await GetOfferDetailsAndImages(client, offerDetailsUrl);
                }
            }
            else
            {
                AddToLog("Odpowiedź nie zawiera klucza 'offers'!");
            }
        }
    }

    static async Task GetOfferDetailsAndImages(HttpClient client, string offerDetailsUrl)
    {
        HttpResponseMessage response = await client.GetAsync(offerDetailsUrl);
        string responseString = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            AddToLog($"Błąd przy pobieraniu szczegółów oferty: {response.StatusCode}\nTekst odpowiedzi: {responseString}");
            return;
        }

        JObject jsonResponse = JObject.Parse(responseString);

        if (jsonResponse.ContainsKey("images"))
        {
            JArray images = (JArray)jsonResponse["images"];
            foreach (var image in images)
            {
                Program.lista_zdjec.Add(image.ToString());
            }
        }
        else
        {
            AddToLog("Brak zdjęć w ofercie!");
        }
    }

    static string GetFileHash(string filePath)
    {
        using (var md5 = MD5.Create())
        {
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hashBytes = md5.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }
    }

    static async Task<string> UploadImageToAllegro(string accessToken, string imagePath)
    {
        if (!File.Exists(imagePath))
        {
            AddToLog($"Plik {imagePath} nie istnieje!");
            return null;
        }

        string apiUrl = "https://upload.allegro.pl.allegrosandbox.pl/sale/images";

        using (HttpClient client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.allegro.public.v1+json"));
            client.DefaultRequestHeaders.Add("Accept-Language", "pl-PL");

            string fileExtension = Path.GetExtension(imagePath).ToLower();
            string contentType = fileExtension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                _ => null
            };

            if (contentType == null)
            {
                AddToLog("Nieobsługiwany format pliku. Dozwolone: PNG, JPG!");
                return null;
            }

            byte[] imageBytes = await File.ReadAllBytesAsync(imagePath);
            ByteArrayContent byteContent = new ByteArrayContent(imageBytes);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            HttpResponseMessage response = await client.PostAsync(apiUrl, byteContent);
            string responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                czyPomyslnie = false;
                AddToLog($"Błąd podczas przesyłania obrazu: {response.StatusCode}\nTreść odpowiedzi: {responseString}");
                return null;
            }

            JObject jsonResponse = JObject.Parse(responseString);
            string imageUrl = jsonResponse["location"]?.ToString();

            if (string.IsNullOrEmpty(imageUrl))
            {
                czyPomyslnie = false;
                AddToLog("API nie zwróciło poprawnego linku do obrazu!");
                return null;
            }

            return imageUrl;
        }
    }

    static async Task UpdateOfferImage(string accessToken, string oldImageUrl, string newImageUrl)
    {
        foreach(string offerId in lista_ofert)
        {
            string error = "";

            using (HttpClient client = new HttpClient())
            {
                Console.WriteLine("\n--------------------------------");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.allegro.public.v1+json"));
                client.DefaultRequestHeaders.Add("Accept-Language", "pl-PL");

                string apiUrl = $"https://api.allegro.pl.allegrosandbox.pl/sale/offers/{offerId}";

                HttpResponseMessage getResponse = await client.GetAsync(apiUrl);
                string getResponseString = await getResponse.Content.ReadAsStringAsync();

                if (!getResponse.IsSuccessStatusCode)
                {
                    czyPomyslnie = false;
                    AddToLog($"Błąd pobierania oferty: {getResponse.StatusCode}\nTreść odpowiedzi: {getResponseString}");
                    return;
                }

                JObject offerData = JObject.Parse(getResponseString);

                JArray imagesArray = offerData["images"] as JArray;

                bool isOld = false;
                bool isNew = true;

                foreach (var elem in imagesArray)
                {
                    if ((string)elem["url"] == oldImageUrl)
                    {
                        isOld = true;
                    }
                    if ((string)elem["url"] == newImageUrl)
                    {
                        error += " Nowy obraz nie może zostać zamieniony, ponieważ istnieje już w ofercie!";
                        isNew = false;
                    }
                }

                if (!isOld)
                {
                    error += " W ofercie nie ma obrazu do zamiany!";
                }

                if (isOld && isNew)
                {
                    //Usuwanie
                    if (imagesArray != null)
                    {
                        var elementToRemove = imagesArray.FirstOrDefault(item => (string)item["url"] == oldImageUrl);

                        if (elementToRemove != null)
                        {
                            imagesArray.Remove(elementToRemove);
                            Console.WriteLine($"Usunięto zdjęcie dla {offerId}");
                        }
                        else
                        {
                            AddToLog($"Nie znaleziono zdjęcia dla {offerId}");
                        }
                    }

                    //Dodawanie
                    if (imagesArray != null)
                    {
                        JObject newImage = new JObject();
                        newImage["url"] = newImageUrl;
                        imagesArray.Add(newImage);
                        Console.WriteLine($"Dodano zdjęcie dla {offerId}");
                    }
                }


                if (offerData?["description"]?["sections"] is JArray sections)
                {
                    foreach (var section in sections)
                    {
                        if (section?["items"] is JArray items)
                        {
                            foreach (var item in items)
                            {
                                if (item?["type"]?.ToString() == "IMAGE" && item?["url"]?.ToString() == oldImageUrl)
                                {
                                    item["url"] = newImageUrl;
                                    Console.WriteLine($"Zmieniono zdjęcie w opisie dla {offerId}");
                                }
                            }
                        }
                    }
                }

                string requestBody = offerData.ToString(Formatting.Indented);

                StringContent content = new StringContent(requestBody, Encoding.UTF8, "application/vnd.allegro.public.v1+json");
                HttpResponseMessage putResponse = await client.PutAsync(apiUrl, content);
                string putResponseString = await putResponse.Content.ReadAsStringAsync();

                if (!putResponse.IsSuccessStatusCode)
                {
                    czyPomyslnie = false;
                    AddToLog($"Błąd aktualizacji oferty: {putResponse.StatusCode}\nTreść odpowiedzi: {putResponseString}");
                    return;
                }

                if (error == "")
                {
                    Console.WriteLine($"Zdjęcie w ofercie ({offerId}) zostało zaktualizowane!");
                }
                else
                {
                    czyPomyslnie = false;
                    AddToLog($"W oferie ({offerId}) nie wykonano akcji! :{error}");
                }
                Console.WriteLine("--------------------------------");
            }
        }
    }
}