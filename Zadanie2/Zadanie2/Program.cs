internal class Program
{
    private static void Main(string[] args)
    {
        string path = @"C:\Users\kozik\Desktop\Praktyki\Zadanie2\zadaniePraca.txt";
        if(File.Exists(path))
        {
            string wynik = "";
            string[] lines = File.ReadAllLines(path);
            foreach(string ln in lines)
            {
                string[] words = ln.Split(' ');
                for (int i = 0; i < words.Length; i++)
                {
                    if (words[i] == "praca")
                    {
                        wynik += "job";
                    }
                    else
                    {
                        wynik += words[i];
                    }
                    wynik += " ";
                }

            }
            string new_path = "";
            for(int i = 0; i < path.Length; i++)
            {
                if (path[i] == '.')
                {
                    break;
                }
                else
                {
                    new_path += path[i];
                }
            }
            new_path += "_changed";
            new_path += DateTime.Now.ToString("ddMMyyyy");
            new_path += ".txt";
            using (StreamWriter outputFile = new StreamWriter(new_path))
            {
                foreach (string line in lines)
                    outputFile.WriteLine(line);
            };
        }
        else
        {
            Console.WriteLine("Plik nie istnieje");
        }

    }
}