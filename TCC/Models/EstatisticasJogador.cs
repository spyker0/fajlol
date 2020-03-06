using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RiotSharp.Endpoints.SummonerEndpoint;
using RiotSharp.Misc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TCC.Models
{
    public class EstatisticasJogador
    {
        public EstatisticasJogador(Summoner summoner, string elo)
        {
            AtualizaComSummoner(summoner, elo);
        }


        public void AtualizaComSummoner(Summoner summonner, string elo)
        {
            PUDID = summonner.Puuid;
            Regiao = summonner.Region;
            Nivel = summonner.Level;
            InvocadorId = summonner.Id;
            ContaId = summonner.AccountId;
            Nome = summonner.Name;
            NomeNormalizado = summonner.Name.ToUpperInvariant().Trim().Replace(" ", "");
            IconeId = summonner.ProfileIconId;
            DataUltimaModificacao = DateTime.SpecifyKind(summonner.RevisionDate, DateTimeKind.Utc);
            Elo = elo;
        }

        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string PUDID { get; set; }

        public Region Regiao { get; set; }
        public long Nivel { get; set; }
        public string InvocadorId { get; set; }
        public string ContaId { get; set; }
        public string Nome { get; set; }

        public string NomeNormalizado { get; set; }
        public int IconeId { get; set; }

        /// <summary>
        /// Data em que houve a última alteração na conta do jogador, antes de atualizar os dados, checar esta data no servidor
        /// </summary>
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime DataUltimaModificacao { get; set; }

        public decimal TaxaVitoria
        {
            get
            {
                return (decimal)PartidasGanhas / (decimal)PartidasTotais;
            }
        }
        public int PartidasGanhas { get; set; }
        public int PartidasPerdidas { get; set; }

        public int PartidasTotais
        {
            get
            {
                return PartidasGanhas + PartidasPerdidas;
            }
        }

        public List<Campeao> CampeoesXTaxaVitoria { get; set; }

        public List<Lane> Lanes { get; set; }

        /// <summary>
        /// Informa se foi possível contatar a api para atualizar os dados
        /// </summary>
        [BsonIgnore]
        public bool Desatualizado { get; set; }

        public string Elo { get; set; }

        public decimal TaxaPrimeiroBarao { get; set; }
        public decimal TaxaFirstBlood { get; set; }
    }
}
