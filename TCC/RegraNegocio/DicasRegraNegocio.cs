using RiotSharp.Endpoints.StaticDataEndpoint.SummonerSpell;
using System.Linq;
using System.Threading.Tasks;
using TCC.Models;

namespace TCC.RegraNegocio
{
    public static class DicasRegraNegocio
    {
        public static async Task PreencherDicas(this PartidaAtualViewModel partida, SummonerSpellListStatic spells)
        {
            DicaIncendiar();
            DicaBarao();
            DicaFirstBlood();

            void DicaIncendiar()
            {
                var incenciar = spells.SummonerSpells.First(k => k.Key == "SummonerDot");

                var inimigosComIncendiar = partida.Inimigos.Where(x => x.Participante.SummonerSpell1 == incenciar.Value.Id || x.Participante.SummonerSpell2 == incenciar.Value.Id).ToArray();

                if (inimigosComIncendiar.Count() == 1)
                    partida.Dicas.Add(new Dica($"Matenha distância do jogador {inimigosComIncendiar.First().Nome} até os 15 minutos, pois ele possui o feitiço incendiar, que pode ser mortal caso você esteja com pouca vida."));
               if (inimigosComIncendiar.Count() > 1)
                    partida.Dicas.Add(new Dica($"Tenha cuidado com os jogadores {string.Join(", ", inimigosComIncendiar.Select(x => x.Nome).ToArray(), 0, inimigosComIncendiar.Count() - 1)} e {inimigosComIncendiar.Last().Nome} até os 15 minutos, pois eles possuem o feitiço incendiar, que pode ser mortal caso você esteja com pouca vida."));

            }

            void DicaBarao()
            {
                if (partida.Inimigos.Select(x => x.TaxaPrimeiroBarao).RetornarMedia() > partida.Aliados.Select(x => x.TaxaPrimeiroBarao).RetornarMedia())
                    partida.Dicas.Add(new Dica("Mantenha visão do barão a partir dos 20 minutos de partida. O time inimigo costuma matar ele mais cedo que o seu!"));
            }

            void DicaFirstBlood()
            {
                if (partida.Inimigos.Select(x => x.TaxaFirstBlood).RetornarMedia() > partida.Aliados.Select(x => x.TaxaFirstBlood).RetornarMedia())
                    partida.Dicas.Add(new Dica("Cuidado com confrontos no início da partida, o time inimigo costuma matar o primeiro oponente antes do seu!"));
            }
        }


    }
}
