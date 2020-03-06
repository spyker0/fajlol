using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TCC.RegraNegocio;

namespace TCC.Models
{
    public class Lane
    {
        public Lane(string descricao, int partidasGanhas, int partidasPerdidas, int totalPartidasTodasLanes)
        {
            Descricao = descricao;
            PartidasGanhas = partidasGanhas;
            PartidasPerdidas = partidasPerdidas;
            PercentualUtilizacao = Math.Round((decimal)PartidasTotais / (decimal)totalPartidasTodasLanes, 2);
        }

        public string Descricao { get; set; }
        public int PartidasGanhas { get; set; }
        public int PartidasPerdidas { get; set; }
        public int PartidasTotais
        {
            get
            {
                return PartidasGanhas + PartidasPerdidas;
            }
        }
        public decimal PercentualUtilizacao { get; set; }
    }
}
