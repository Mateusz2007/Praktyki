using System.IO;
internal class Program
{
    private static void Main(string[] args)
    {
        string path = @"C:\test\test_mat_koz.txt";
        int wynik = 0;
        if(File.Exists(path))
        {
            string tekst = File.ReadAllText(path);
            for(int i = 0; i < tekst.Length; i++){
                if (tekst[i] == 'a')
                {
                    wynik++;
                }
            }
            Console.WriteLine("W tym pliku litera 'a' występuje " + wynik + " razy.");
        }
        else
        {
            Console.WriteLine("Plik nie istnieje");
        }    
    }
}