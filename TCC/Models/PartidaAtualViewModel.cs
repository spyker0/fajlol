using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RiotSharp.Endpoints.SpectatorEndpoint;
using RiotSharp.Endpoints.StaticDataEndpoint.Champion;
using RiotSharp.Endpoints.StaticDataEndpoint.SummonerSpell;
using RiotSharp.Misc;
using TCC.RegraNegocio;

namespace TCC.Models
{
    public class PartidaAtualViewModel
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public string LinkIcone { get; set; }

        public string Elo { get; set; }

        public decimal ChanceVitoriaAliados { get; set; }
        public List<JogadorPartidaAtual> Aliados { get; set; }

        public decimal ChanceVitoriaInimigos { get; set; }
        public List<JogadorPartidaAtual> Inimigos { get; set; }

        public Platform Platform { get; set; }

        public long GameId { get; set; }

        public bool? PrevisaoAcertada { get; set; }


        public List<Dica> Dicas { get; set; } = new List<Dica>();

        //[BsonIgnore]
        public List<Item> ItensSugeridos { get; set; }
        //[BsonIgnore]
        public Item WardSugerida { get; set; }
    }

    public class JogadorPartidaAtual
    {

        public JogadorPartidaAtual()
        {

        }

        public JogadorPartidaAtual(EstatisticasJogador estatisticasJogador, CurrentGameParticipant participante, bool aliado, ChampionListStatic campeoes, string versao, bool jogadorPrincipal = false)
        {
            var campeao = campeoes
                    .Champions
                    .FirstOrDefault(c => c.Value.Id == participante.ChampionId).Value;

            decimal taxaVitoria = estatisticasJogador.CampeoesXTaxaVitoria?.FirstOrDefault(x => x.ID == participante.ChampionId)?.TaxaVitoria ?? 0;

            IconeCampeao = Util.RetornarIconeCampeao(versao, campeao);
            Nome = estatisticasJogador.Nome;
            Campeao = campeao.Name;
            Divisao = estatisticasJogador.Elo;
            ChanceVitoria = taxaVitoria;
            LanePrincipal = estatisticasJogador.Lanes?.OrderByDescending(x => x.PercentualUtilizacao).FirstOrDefault()?.Descricao ?? "Sem lane principal";
            ConfiabilidadePericulosidade = CalculaConfiabilidadePericulosidade(taxaVitoria, aliado);

            var idCampeaoPrincipal = estatisticasJogador.CampeoesXTaxaVitoria.OrderByDescending(x => x.PartidasTotais).FirstOrDefault()?.ID;

            if (idCampeaoPrincipal.HasValue)
                CampeaoPrincipal = campeoes
                        .Champions
                        .FirstOrDefault(c => c.Value.Id == idCampeaoPrincipal).Value?.Name ?? "Sem campeão principal";
            else
                CampeaoPrincipal = "Sem campeão principal";

            JogadorPrincipal = jogadorPrincipal;

            Participante = participante;

            TaxaPrimeiroBarao = estatisticasJogador.TaxaPrimeiroBarao;
            TaxaFirstBlood = estatisticasJogador.TaxaFirstBlood;
        }

        private string CalculaConfiabilidadePericulosidade(decimal taxaVitoria, bool aliado)
        {
            if (aliado)
            {
                if (taxaVitoria <= 0.5M)
                {
                    return "Não confiável";
                }
                if (taxaVitoria > 0.6M)
                {
                    return "Confiável";
                }
                return "Pouco confiável";
            }
            else
            {
                if (taxaVitoria < 0.5M)
                {
                    return "Normal";
                }
                if (taxaVitoria >= 0.5M && taxaVitoria < 0.6M)
                {
                    return "Potencialmente perigoso";
                }
                if (taxaVitoria >= 0.6M && taxaVitoria <= 0.7M)
                {
                    return "Perigoso";
                }
                return "Muito perigoso";
            }
        }

        public string IconeCampeao { get; set; }
        public string Nome { get; set; }
        public string Campeao { get; set; }
        public string Divisao { get; set; }
        public decimal ChanceVitoria { get; set; }
        public string LanePrincipal { get; set; }
        public string ConfiabilidadePericulosidade { get; set; }
        public string CampeaoPrincipal { get; set; }
        public bool JogadorPrincipal { get; set; }
        public CurrentGameParticipant Participante { get; set; }
        public decimal TaxaPrimeiroBarao { get; set; }
        public decimal TaxaFirstBlood { get; set; }
    }

    public class Dica
    {
        public string Mensagem { get; set; }

        public Dica()
        {

        }

        public Dica(string mensagem)
        {
            Mensagem = mensagem;
        }
    }
}
