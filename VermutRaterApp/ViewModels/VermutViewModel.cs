using System.Collections.ObjectModel;
using System.ComponentModel;

namespace VermutRaterApp.ViewModels
{
    public class Vermut
    {
        public string Nom { get; set; }
        public int Puntuacio { get; set; }
    }

    public class VermutViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<Vermut> Vermuts { get; set; }

        public VermutViewModel()
        {
            Vermuts = new ObservableCollection<Vermut>
            {
                new Vermut { Nom = "Bandarra Negre", Puntuacio = 4 },
                new Vermut { Nom = "Casa Mariol Blanc", Puntuacio = 2 },
                new Vermut { Nom = "Olave Reserva", Puntuacio = 5 }
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
