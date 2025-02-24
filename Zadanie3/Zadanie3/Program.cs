using System.IO;
using Zadanie3;

internal class Program
{
    private static void Main(string[] args)
    {
        Uzytkownik uzytkownik = new Uzytkownik();
        string path = @"C:\Users\kozik\Desktop\Praktyki\Zadanie3\";

        using (StreamWriter file = new StreamWriter(path + "users-" + DateTime.Now.ToString("dd_MM_yyyy_hh_mm_tt") + ".csv"))
        {
            file.WriteLine("LP  Imie    Nazwisko    Rok urodzenia");

            for (int i = 0; i < 100; i++)
            {
                uzytkownik.Wygeneruj();
                file.WriteLine(uzytkownik);
            }
        }

        Console.WriteLine("Plik został zapisany.");
    }


}