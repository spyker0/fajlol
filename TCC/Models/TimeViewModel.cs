using System.Collections.Generic;
using RiotSharp.Endpoints.MatchEndpoint;

namespace TCC.Models
{
    public class TimeViewModel
    {
        public TimeViewModel(TeamStats time)
        {
            NumeroDragoes = time.DragonKills;
            NumeroBaroes = time.BaronKills;
            NumeroArautos = time.RiftHeraldKills;
            NumeroInibidores = time.InhibitorKills;
            NumeroTorres = time.TowerKills;
        }

        public List<ParticipanteViewModel> Participantes { get; set; }
        public int NumeroDragoes { get; set; }
        public int NumeroBaroes { get; set; }
        public int NumeroArautos { get; set; }
        public int NumeroInibidores { get; set; }
        public int NumeroTorres { get; set; }
    }
}
