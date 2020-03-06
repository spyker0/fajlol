using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TCC.Models
{
    public class EstatisticasJogadorViewModel
    {
        public EstatisticasJogador EstatisticasJogador { get; set; }
        public string LinkIcone { get; set; }

        public List<ParticipanteViewModel> Partidas { get; set; }
    }
}
