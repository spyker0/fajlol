using RiotSharp.Endpoints.MatchEndpoint;
using RiotSharp.Endpoints.StaticDataEndpoint.Champion;
using RiotSharp.Endpoints.StaticDataEndpoint.Item;
using RiotSharp.Endpoints.StaticDataEndpoint.SummonerSpell;
using System;
using System.Collections.Generic;
using System.Linq;
using TCC.RegraNegocio;

namespace TCC.Models
{
    public class ParticipanteViewModel
    {
        /// <summary>
        /// Constroi um participante
        /// </summary>
        /// <param name="partida">Partida atual</param>
        /// <param name="campeoes">Lista de campeoes</param>
        /// <param name="versao">Versao do jogo</param>
        /// <param name="contaId">AccoutId do participante, somente é utilizado se o participante for nulo</param>
        /// <param name="itens">Lista de itens do jogo</param>
        /// <param name="participant">Participante que vem da api</param>
        public ParticipanteViewModel(Partida partida, ChampionListStatic campeoes, string versao, string contaId, ItemListStatic itens, Participant participant, SummonerSpellListStatic feiticos, bool detalhado = false)
        {
            var participante = participant ?? partida.Participants.First(y => y.ParticipantId == partida.ParticipantIdentities.First(k => k.Player.CurrentAccountId == contaId).ParticipantId);

            var campeao =
                campeoes
                    .Champions
                    .FirstOrDefault(c => c.Value.Id == participante.ChampionId).Value;

            NomeCampeao = campeao.Name;

            UrlIconeCampeao = Util.RetornarIconeCampeao(versao, campeao);

            OuroAcumulado = participante.Stats.GoldEarned > 100 ? participante.Stats.GoldEarned.ToString("0,.#K") : participante.Stats.GoldEarned.ToString();

            KDA = $"{participante.Stats.Kills}/{participante.Stats.Deaths}/{participante.Stats.Assists}";

            Lane = Util.RetornarLaneFinalJogador(participante.Timeline.Lane, participante.Timeline.Role);

            NivelMaximoAtingido = participante.Stats.ChampLevel;

            Vitoria = participante.Stats.Winner;

            Data = partida.GameCreation.ToString("dd/MM/yyyy");

            ItensFinais = new List<Item>();

            ItensFinais.AdicionarItem(participante.Stats.Item0, itens, versao);
            ItensFinais.AdicionarItem(participante.Stats.Item1, itens, versao);
            ItensFinais.AdicionarItem(participante.Stats.Item2, itens, versao);
            ItensFinais.AdicionarItem(participante.Stats.Item3, itens, versao);
            ItensFinais.AdicionarItem(participante.Stats.Item4, itens, versao);
            ItensFinais.AdicionarItem(participante.Stats.Item5, itens, versao);
            var ward = Util.ObterItem(participante.Stats.Item6, itens, versao);
            if (ward != null)
                Ward = ward;

            GameId = partida.GameId;
            var jogador = partida.ParticipantIdentities.First(x => x.ParticipantId == participante.ParticipantId).Player;

            AccountId = jogador.CurrentAccountId;
            SummonerId = jogador.SummonerId;

            if (detalhado)
            {
                NomeJogador = jogador.SummonerName;
                double ouroPorMinuto;

                try
                {
                    ouroPorMinuto = participante.Stats.GoldEarned / partida.GameDuration.TotalMinutes;
                }
                catch (Exception)
                {
                    ouroPorMinuto = 0;
                }

                OuroPorMinuto = ouroPorMinuto.ToString("0.#");

                Farm = participante.Stats.TotalMinionsKilled + participante.Stats.NeutralMinionsKilled;

                double farmPorMinuto;

                try
                {
                    farmPorMinuto = Convert.ToDouble(Farm) / partida.GameDuration.TotalMinutes;
                }
                catch (Exception)
                {
                    farmPorMinuto = 0;
                }

                FarmPorMinuto = farmPorMinuto.ToString("0.#");

                if (feiticos != null)
                {
                    Feitico1 = AdicionarFeitico(participante.Spell1Id, feiticos, versao);
                    Feitico2 = AdicionarFeitico(participante.Spell2Id, feiticos, versao);
                }
            }

        }

        private Feitico AdicionarFeitico(int spellId, SummonerSpellListStatic spells, string versao)
        {
            if (spellId > 0)
                try
                {
                    var feitico = spells.SummonerSpells.First(x => x.Value.Id == spellId);

                    return new Feitico
                    {
                        Nome = feitico.Value.Name,
                        UrlIcone = $"//ddragon.leagueoflegends.com/cdn/{versao}/img/spell/{feitico.Value.Image.Full}"
                    };
                }
                catch
                {

                }


            return new Feitico
            {
                Nome = $"Feitiço {spellId} não encontrado",
                UrlIcone = "/images/image_not_found.png"
            };
        }
        public string NomeCampeao { get; set; }

        public string UrlIconeCampeao { get; set; }

        public string OuroAcumulado { get; set; }

        public string KDA { get; set; }

        public string Lane { get; set; }

        public long NivelMaximoAtingido { get; set; }

        public List<Item> ItensFinais { get; set; }

        public Item Ward { get; set; }

        public bool Vitoria { get; set; }

        public string Data { get; set; }

        public long GameId { get; set; }
        public string AccountId { get; set; }
        public string SummonerId { get; set; }

        #region detalhes
        public Feitico Feitico1 { get; set; }
        public Feitico Feitico2 { get; set; }
        public string Elo { get; set; }
        public string FarmPorMinuto { get; set; }
        public string OuroPorMinuto { get; set; }
        public long Farm { get; set; }
        public string NomeJogador { get; set; }
        #endregion

    }

    public class Item
    {
        public string Nome { get; set; }
        public string UrlIcone { get; set; }
    }

    public struct Feitico
    {
        public string Nome { get; set; }
        public string UrlIcone { get; set; }
    }
}

