using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using RiotSharp;
using RiotSharp.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TCC.Data;
using TCC.Models;

namespace TCC.RegraNegocio
{
    public class ItensRegraNegocio
    {
        private readonly MongoDbContext _context;
        private readonly ApiConfiguration _apiConfiguration;
        private readonly Util _util;
        private readonly ILogger<ItensRegraNegocio> _logger;
        private BsonDocument[] _agrupamentosParticipantes;
        private BsonDocument[] _agrupamentosEstatisticasParticipante;
        private BsonDocument[] _retornarItemMaiorTaxaVitoria;

        public ItensRegraNegocio(MongoDbContext context, IOptions<ApiConfiguration> apiConfiguration, Util util, ILogger<ItensRegraNegocio> logger)
        {
            _context = context;
            _apiConfiguration = apiConfiguration.Value;
            _util = util;
            _logger = logger;
            InstanciarDocumentosComuns();
        }



        public async Task CalculaItensCampeoes()
        {
            var api = RiotApi.GetDevelopmentInstance(_apiConfiguration.Key);

            var ultimaVersao = await _util.RetornarUltimoPatchAsync();

            var campeoes = await api.StaticData.Champions.GetAllAsync(ultimaVersao, Language.pt_BR, fullData: false);

            var idsCampeoes = campeoes.Champions.Select(x => x.Value.Id);

            var tasks = new Task[idsCampeoes.Count()];

            for (int i = 0; i < idsCampeoes.Count(); i++)
            {
                tasks[i] = PersistirCampeao(idsCampeoes.ElementAt(i));
            }

            Task.WaitAll(tasks);
        }

        private List<BsonDocument> RetornarMatchCampeao(int idCampeao, List<ItemCampeao> itens, int posicaoItem)
        {
            var retorno = new List<BsonDocument>{
                new BsonDocument("$match",
                    new BsonDocument("Participants.ChampionId", idCampeao))
                };


            retorno.Add(new BsonDocument("$match",
                new BsonDocument($"Participants.Stats.Item{posicaoItem}",
                new BsonDocument("$not",
                new BsonDocument("$eq", 0)))));

            if (itens != null && itens.Count > 0 && posicaoItem != 6)
                retorno.Add(new BsonDocument("$match",
                    new BsonDocument($"Participants.Stats.Item{posicaoItem}",
                    new BsonDocument("$not",
                    new BsonDocument("$in", itens.Select(x => (int)x._id).ToArray().ToBsonDocumentArray())))));

            return retorno;
        }




        private async Task PersistirCampeao(int idCampeao)
        {
            var campeao = new MelhoresItensCampeao { ChampionId = idCampeao };

            var pipelineItens = new List<BsonDocument>(18);
            for (int i = 0; i <= 6; i++)
            {
                pipelineItens.AddRange(RetornarMatchCampeao(idCampeao, null, i));

                pipelineItens.AddRange(_agrupamentosParticipantes);

                pipelineItens.AddRange(RetornarMatchCampeao(idCampeao, campeao.Itens, i));

                pipelineItens.AddRange(_agrupamentosEstatisticasParticipante);

                pipelineItens.Add(RetornarGroupItem(i));

                pipelineItens.AddRange(_retornarItemMaiorTaxaVitoria);

                var itemCampeao = await _context
                   .Partidas
                   .Aggregate<ItemCampeao>(pipelineItens.ToArray())
                .FirstOrDefaultAsync();

                if (itemCampeao != null)
                {
                    if (i < 6)
                        campeao.Itens.Add(itemCampeao);
                    else
                        campeao.Ward = itemCampeao;
                }

                pipelineItens.Clear();
            }

            var filtroCampeao = Builders<MelhoresItensCampeao>.Filter.Eq(x => x.ChampionId, idCampeao);
            var campeaoBanco = await (await _context.MelhoresItensCampeao.FindAsync(x => x.ChampionId == idCampeao)).FirstOrDefaultAsync();
            if (campeaoBanco != null)
            {
                campeao.Id = campeaoBanco.Id;
                await _context.MelhoresItensCampeao.ReplaceOneAsync(filtroCampeao, campeao);
            }
            else
                await _context.MelhoresItensCampeao.InsertOneAsync(campeao);


        }

        private static BsonDocument RetornarGroupItem(int numeroItem)
        {
            return new BsonDocument("$group",
            new BsonDocument
                {
                    { "_id", "$Participants.Stats.Item" + numeroItem },
                    { "Vitorias",
                        new BsonDocument("$sum",
                        new BsonDocument("$cond",
                        new BsonArray
                                {
                                    new BsonDocument("$eq",
                                    new BsonArray
                                        {
                                            "$Participants.Stats.Winner",
                                            true
                                        }),
                                    1,
                                    0
                                })) },
                        { "Total", new BsonDocument("$sum", 1) }
            });
        }

        private void InstanciarDocumentosComuns()
        {
            _agrupamentosParticipantes = RetornarAgrupamentoParticipantes();
            _agrupamentosEstatisticasParticipante = AgruparEstatisticasParticipante();
            _retornarItemMaiorTaxaVitoria = RetonarItemComMaiorTaxaVitoria();
        }

        private BsonDocument[] RetornarAgrupamentoParticipantes()
            => new BsonDocument[]{ new BsonDocument("$unwind",
                new BsonDocument("path", "$Participants")),
                new BsonDocument("$unwind",
                new BsonDocument("path", "$ParticipantIdentities")) };

        private BsonDocument[] AgruparEstatisticasParticipante()
            => new BsonDocument[]
            {
            new BsonDocument("$redact",
            new BsonDocument("$cond",
            new BsonDocument
                    {
                            { "if",
                new BsonDocument("$eq",
                new BsonArray
                                {
                                    "$Participants.ParticipantId",
                                    "$ParticipantIdentities.ParticipantId"
                                }) },
                            { "then", "$$KEEP" },
                            { "else", "$$PRUNE" }
                    }))
            };

        private BsonDocument[] RetonarItemComMaiorTaxaVitoria()
            => new BsonDocument[]
            {
                new BsonDocument("$project",
                new BsonDocument
                    {
                        { "TaxaVitoria",
                new BsonDocument("$divide",
                new BsonArray
                            {
                                "$Vitorias",
                                "$Total"
                            }) },
                        { "Vitorias", "$Vitorias" },
                        { "Total", "$Total" }
                    }),
                new BsonDocument("$sort",
                new BsonDocument
                    {
                        { "Vitorias", -1 },
                        { "TaxaVitoria", -1 },
                        { "Total", 1 }
                    }),
                new BsonDocument("$project",
                    new BsonDocument
                        {
                            { "_id", "$_id" },
                            { "Vitorias", "$Vitorias" },
                            { "Total", "$Total" }
                        }),
                new BsonDocument("$limit", 1)
            };

        public async Task<(List<Item>, Item)> ObterSugestaoCampeao(long championId)
        {
            var campeaoItens = await (await _context.MelhoresItensCampeao.FindAsync(x => x.ChampionId == championId)).FirstOrDefaultAsync();

            if (campeaoItens == null)
                return (null, null);


            var api = RiotApi.GetDevelopmentInstance(_apiConfiguration.Key);

            var ultimaVersao = await _util.RetornarUltimoPatchAsync();

            var itens = await api.StaticData.Items.GetAllAsync(ultimaVersao, Language.pt_BR);

            var itensCampeao = new List<Item>();

            campeaoItens.Itens.ForEach(x => itensCampeao.AdicionarItem(x._id, itens, ultimaVersao));

            Item ward;

            try
            {
                ward = Util.ObterItem(campeaoItens.Ward._id, itens, ultimaVersao);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Problema ao obter ward");
                ward = null;
            }

            return (itensCampeao, ward);
        }
    }
}
