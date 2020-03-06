using RiotSharp.Endpoints.StaticDataEndpoint.Champion;
using RiotSharp.Endpoints.StaticDataEndpoint.Item;
using RiotSharp.Endpoints.StaticDataEndpoint.SummonerSpell;
using System.Linq;

namespace TCC.Models
{
    public class PartidaViewModel
    {

        public PartidaViewModel(Partida partida, ChampionListStatic campeoes, string ultimaVersao, string contaJogadorPrincipal, ItemListStatic itens, SummonerSpellListStatic spells)
        {
            var idTimeAliado = partida.Participants.First(y => y.ParticipantId == partida.ParticipantIdentities.First(k => k.Player.CurrentAccountId == contaJogadorPrincipal).ParticipantId).TeamId;

            var timeAliado = partida.Teams.First(x => x.TeamId == idTimeAliado);

            TimeAliado = new TimeViewModel(timeAliado);

            TimeAliado.Participantes = partida.Participants.Where(x => x.TeamId == idTimeAliado).Select(participante => new ParticipanteViewModel(partida, campeoes, ultimaVersao, "", itens, participante, spells, true)).ToList();


            var timeInimigo = partida.Teams.First(x => x.TeamId != idTimeAliado);

            TimeInimigo = new TimeViewModel(timeInimigo);

            TimeInimigo.Participantes = partida.Participants.Where(x => x.TeamId != idTimeAliado).Select(participante => new ParticipanteViewModel(partida, campeoes, ultimaVersao, "", itens, participante, spells, true)).ToList();

        }

        public TimeViewModel TimeAliado { get; set; }
        public TimeViewModel TimeInimigo { get; set; }
    }
}
