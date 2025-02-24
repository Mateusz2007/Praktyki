using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zadanie3
{
    internal class Uzytkownik
    {
        int LP = 0;
        string Imie;
        string Nazwisko;
        int Rok_urodzenia;
        public Uzytkownik() { }
        public void Wygeneruj()
        {
            Random rand = new Random();
            LP++;

            switch (rand.Next(0, 4))
            {
                case 0: Imie = "Ania"; break;
                case 1: Imie = "Kasia"; break;
                case 2: Imie = "Basia"; break;
                case 3: Imie = "Zosia"; break;
            }

            switch (rand.Next(0, 2))
            {
                case 0: Nazwisko = "Kowalska"; break;
                case 1: Nazwisko = "Nowak"; break;
            }

            Rok_urodzenia = rand.Next(1990, 2001);
        }

        public override string ToString()
        {
            return LP + " " + Imie + " " + Nazwisko + " " + Rok_urodzenia;
        }
    }
}
