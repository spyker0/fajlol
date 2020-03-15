using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using RiotSharp;
using RiotSharp.Endpoints.MatchEndpoint;
using RiotSharp.Endpoints.SpectatorEndpoint;
using RiotSharp.Endpoints.SummonerEndpoint;
using RiotSharp.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TCC.Data;
using TCC.Models;
using LaneModel = TCC.Models.Lane;

namespace TCC.RegraNegocio
{
    public class JogadorRegraNegocio
    {
        private const int _rankedSolo = 420;
        private readonly ApiConfiguration _apiConfiguration;
        private readonly MongoDbContext _context;
        private readonly ILogger<JogadorRegraNegocio> _logger;
        private readonly Util _util;
        private readonly ItensRegraNegocio _itensRegraNegocio;

        public JogadorRegraNegocio(IOptions<ApiConfiguration> apiConfiguration, MongoDbContext context, ILogger<JogadorRegraNegocio> logger, Util util, ItensRegraNegocio itensRegraNegocio)
        {
            _apiConfiguration = apiConfiguration.Value;
            _context = context;
            _logger = logger;
            _util = util;
            _itensRegraNegocio = itensRegraNegocio;
        }

        public async Task<EstatisticasJogador> PersistirDadosJogadorAsync(int regiao, string jogador, List<int> championsIds = null)
        {
            var api = RiotApi.GetDevelopmentInstance(_apiConfiguration.Key);

            Summoner summoner = null;
            try
            {
                summoner = await api.Summoner.GetSummonerByNameAsync((Region)regiao, jogador);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Não foi possível contatar a api da RIOT games");
                var retorno = await _context.EstatisticasJogador.Find(x => x.NomeNormalizado == jogador.ToUpperInvariant().Trim().Replace(" ", "") && x.Regiao == (Region)regiao).FirstOrDefaultAsync();
                retorno.Desatualizado = true;

                return retorno;
            }

            EstatisticasJogador estatisticasJogadorBanco = null;

            estatisticasJogadorBanco = await _context.EstatisticasJogador.Find(x => x.PUDID == summoner.Puuid).FirstOrDefaultAsync();

            if (estatisticasJogadorBanco == null)
            {
                _logger.LogInformation($"{summoner.Name} não existe no banco");

                //var estatisticasLiga = await api.League.GetLeaguePositionsAsync((Region)regiao, jogador);

                var estatisticasJogador = new EstatisticasJogador(summoner, await RetornarEloJogador(regiao, summoner.Id));

                await SincronizaUltimasPartidasJogador((Region)regiao, summoner.AccountId, championIds: championsIds);

                await _context.EstatisticasJogador.InsertOneAsync(estatisticasJogador);

                return await PersistirEstatisticasJogadorAsync(summoner.Puuid);
            }
            else
            {
                if (summoner.RevisionDate > estatisticasJogadorBanco.DataUltimaModificacao || (championsIds != null && championsIds.Count > 0))
                {
                    _logger.LogInformation($"{summoner.Name} existe mas desatualizado");
                    await SincronizaUltimasPartidasJogador((Region)regiao, summoner.AccountId, estatisticasJogadorBanco.DataUltimaModificacao, championIds: championsIds);

                    estatisticasJogadorBanco.AtualizaComSummoner(summoner, await RetornarEloJogador(regiao, summoner.Id));

                    await _context.EstatisticasJogador.ReplaceOneAsync(Builders<EstatisticasJogador>.Filter.Eq(x => x.PUDID, estatisticasJogadorBanco.PUDID), estatisticasJogadorBanco);

                    return await PersistirEstatisticasJogadorAsync(summoner.Puuid);
                }
                else
                {

                    _logger.LogInformation($"{summoner.Name} existe e já atualizado");
                    //Somente para fins de teste
                    //return await PersistirEstatisticasJogadorAsync(summoner.Puuid);

                }
            }
            return estatisticasJogadorBanco;
        }

        internal async Task<bool> GravarResultadoPrevisaoPartidasAsync()
        {
            var api = RiotApi.GetDevelopmentInstance(_apiConfiguration.Key);

            var partidasAtuais = await _context.PartidaAtual.Find(x => !x.PrevisaoAcertada.HasValue).Limit(50).ToListAsync();
            foreach (var item in partidasAtuais)
            {
                var timeAliadoCampeao = item.ChanceVitoriaAliados > (decimal)0.5;
                var jogadorAliado = item.Aliados.First().Nome;
                var partida = await PersistirPartida(Region.Br, api, item.GameId);
                var participante = partida.Participants.First(y => y.ParticipantId == partida.ParticipantIdentities.First(k => k.Player.SummonerName == jogadorAliado).ParticipantId);
                item.PrevisaoAcertada = participante.Stats.Winner == timeAliadoCampeao;
                await _context.PartidaAtual.ReplaceOneAsync(Builders<PartidaAtualViewModel>.Filter.Eq(x => x.Id, item.Id), item);
            }

            return partidasAtuais.Count < 50;
        }

        /// <summary>
        /// Sincroniza partidas de todos os jogadores de todas as partidas o jogador fornecido
        /// </summary>
        /// <param name="regiao"></param>
        /// <param name="jogador"></param>
        /// <returns></returns>
        internal async Task PopularBancoAsync(int regiao, string jogador)
        {

            var estatisticas = await PersistirDadosJogadorAsync(regiao, jogador);
            var partidas = await RetornarPartidasJogador(estatisticas.ContaId, estatisticas.Regiao);
            foreach (var item in partidas)
            {
                var partidaDetalhada = await RetornarPartidaDetalhada((Region)regiao, item.GameId, item.AccountId);
                foreach (var summonner in partidaDetalhada.TimeAliado.Participantes.Where(x => x.AccountId != item.AccountId))
                {
                    await PersistirDadosJogadorAsync(regiao, summonner.NomeJogador);
                    //await PopularBancoAsync(regiao, summonner.NomeJogador);
                }
                foreach (var summonner in partidaDetalhada.TimeInimigo.Participantes)
                {
                    await PersistirDadosJogadorAsync(regiao, summonner.NomeJogador);
                    //await PopularBancoAsync(regiao, summonner.NomeJogador);
                }
            }
        }

        private async Task<string> RetornarEloJogador(int regiao, string id)
        {
            var api = RiotApi.GetDevelopmentInstance(_apiConfiguration.Key);

            //var estatisticasLiga = await api.League.GetLeaguePositionsAsync((Region)regiao, id);
            var estatisticasLiga = await api.League.GetLeagueEntriesBySummonerAsync((Region)regiao, id);

            string elo = "";
            var ligaRanqueada = estatisticasLiga.FirstOrDefault(x => x.QueueType == "RANKED_SOLO_5x5");

            if (ligaRanqueada != null)
            {
                elo = $"{ligaRanqueada.Tier} {ligaRanqueada.Rank}";
            }
            return elo;
        }

        public async Task<List<ParticipanteViewModel>> RetornarPartidasJogador(string contaId, Region regiao)
        {
            var api = RiotApi.GetDevelopmentInstance(_apiConfiguration.Key);

            var ultimaVersao = await _util.RetornarUltimoPatchAsync();

            var campeoes = await api.StaticData.Champions.GetAllAsync(ultimaVersao, Language.pt_BR, fullData: false);

            var itens = await api.StaticData.Items.GetAllAsync(ultimaVersao, Language.pt_BR);

            var seasons = Util.RetornaUltimasSeason(4).Select(x => (int)x).ToArray();

            return await _context
                .Partidas
                .Find(x => x.PlatformId == regiao && x.ParticipantIdentities.Any(y => y.Player.CurrentAccountId == contaId) && seasons.Contains(x.SeasonId))
                .SortByDescending(x => x.GameCreation)
                .Project(x => new ParticipanteViewModel(x, campeoes, ultimaVersao, contaId, itens, null, null, false)
                    )
                .ToListAsync();
        }

        public async Task<PartidaViewModel> RetornarPartidaDetalhada(Region regiao, long gameId, string idJogadorPrincipal)
        {
            var api = RiotApi.GetDevelopmentInstance(_apiConfiguration.Key);

            var ultimaVersao = await _util.RetornarUltimoPatchAsync();

            var campeoes = await api.StaticData.Champions.GetAllAsync(ultimaVersao, Language.pt_BR, fullData: false);

            var itens = await api.StaticData.Items.GetAllAsync(ultimaVersao, Language.pt_BR);

            var seasons = Util.RetornaUltimasSeason(4).Select(x => (int)x).ToArray();

            var spells = await api.StaticData.SummonerSpells.GetAllAsync(ultimaVersao, Language.pt_BR);


            var retorno = await _context
                .Partidas
                .Find(x => x.PlatformId == regiao && x.GameId == gameId)
                .SortByDescending(x => x.GameCreation)
                .Project(x => new PartidaViewModel(x, campeoes, ultimaVersao, idJogadorPrincipal, itens, spells))
                .FirstOrDefaultAsync();

            var listaTask = new List<Task<string>>();

            retorno.TimeAliado.Participantes.ForEach(x => listaTask.Add(RetornarEloJogador((int)regiao, x.SummonerId)));
            retorno.TimeInimigo.Participantes.ForEach(x => listaTask.Add(RetornarEloJogador((int)regiao, x.SummonerId)));
            var res = Task.WhenAll(listaTask);
            res.Wait();

            var i = 0;
            foreach (var elo in res.Result)
            {
                if (i < 5)
                    retorno.TimeAliado.Participantes[i].Elo = elo;
                else
                    retorno.TimeInimigo.Participantes[i - 5].Elo = elo;
                i++;
            }
            return retorno;
        }

        public async Task SincronizaUltimasPartidasJogador(Region regiao, string accountId, DateTime? dataUltimaAtualizacao = null, int quantidadePartidas = 2, List<int> championIds = null)
        {
            if (championIds != null && championIds.Count > 0)
                dataUltimaAtualizacao = null;
            var api = RiotApi.GetDevelopmentInstance(_apiConfiguration.Key);


            if (championIds != null && championIds.Count > 0)
            {
                try
                {
                    var partidasComCampeao = await api.Match.GetMatchListAsync(regiao, accountId, queues: new List<int> { _rankedSolo }, endIndex: quantidadePartidas, beginTime: dataUltimaAtualizacao, seasons: Util.RetornaUltimasSeason(4), championIds: championIds);

                    foreach (var item in partidasComCampeao.Matches)
                    {
                        await PersistirPartida(regiao, api, item.GameId);
                    }
                }
                catch (RiotSharpException ex)
                {
                    if (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogInformation($"Não foram encontradas novas partidas para os campeões {string.Join(",", championIds)}");
                    }
                }
            }
            try
            {
                var partidasNoGeral = await api.Match.GetMatchListAsync(regiao, accountId, queues: new List<int> { _rankedSolo }, endIndex: quantidadePartidas, beginTime: dataUltimaAtualizacao, seasons: Util.RetornaUltimasSeason(4));

                foreach (var item in partidasNoGeral.Matches)
                {
                    await PersistirPartida(regiao, api, item.GameId);
                }
            }
            catch (RiotSharpException ex)
            {
                _logger.LogError(ex, "Erro ao sincronizar as ultimas partidas do jogador");
                if (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation("Não foram encontradas novas partidas");
                }
            }
        }

        private async Task<Partida> PersistirPartida(Region regiao, RiotApi api, long gameId)
        {
            var partida = await _context.Partidas.Find(x => x.PlatformId == regiao && x.GameId == gameId).FirstOrDefaultAsync();
            if (partida == null)
            {
                _logger.LogInformation($"gravando partida {gameId} - {regiao}");

                var partidaCompleta = await api.Match.GetMatchAsync(regiao, gameId);
                partida = new Partida(partidaCompleta, regiao);
                await _context.Partidas.InsertOneAsync(partida);
            }
            else
            {
                _logger.LogInformation($"partida {gameId} - {regiao} existente, pulando");
            }
            return partida;
        }

        public async Task<string> RetornarUrlIconeJogadorAsync(int iconeId)
        {
            try
            {
                var api = RiotApi.GetDevelopmentInstance(_apiConfiguration.Key);

                var ultimaVersao = await _util.RetornarUltimoPatchAsync();

                var icone = (await api.StaticData.ProfileIcons.GetAllAsync(ultimaVersao, Language.pt_BR)).ProfileIcons.FirstOrDefault(x => x.Value.Id == iconeId);

                return $"//ddragon.leagueoflegends.com/cdn/{ultimaVersao}/img/profileicon/{icone.Value.Image.Full}";

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Não foi possível contatar a api estática de ícones de perfil");
                return "";
            }
        }

        /// <summary>
        /// Pega partida ranqueada acontecendo
        /// Somente para propósitos de demonstração e teste
        /// </summary>
        /// <returns></returns>
        public async Task<FeaturedGame> RetornarPartidaAleatoriaAsync()
        {
            var api = RiotApi.GetDevelopmentInstance(_apiConfiguration.Key);

            FeaturedGame retorno = null;

            while (retorno == null)
            {
                retorno = (await api.Spectator.GetFeaturedGamesAsync(Region.Br)).GameList.FirstOrDefault(x => x.Platform == Platform.BR1 && x.GameQueueType == _rankedSolo.ToString() && _context.PartidaAtual.Find(k => k.GameId == x.GameId && x.Platform == x.Platform).FirstOrDefault() == null);

                if (retorno == null)
                {
                    _logger.LogInformation("Partida brasileira nao encontrada, tentando novamente em alguns instantes");
                    await Task.Delay(TimeSpan.FromSeconds(10));
                }
            }

            return retorno;
        }

        public async Task<PartidaAtualViewModel> PersistirDadosPartidaAtualAsync(int regiao, string jogador)
        {
            _logger.LogInformation($"Gravando partida do jogador {jogador}");

            var api = RiotApi.GetDevelopmentInstance(_apiConfiguration.Key);

            var estatisticasPrincipalJogador = await PersistirDadosJogadorAsync(regiao, jogador);

            if (!estatisticasPrincipalJogador.Desatualizado)
            {
                try
                {
                    var retorno = new PartidaAtualViewModel();

                    var partidaAtual = await api.Spectator.GetCurrentGameAsync((Region)regiao, estatisticasPrincipalJogador.InvocadorId);

                    var partidaAtualBanco = 
                        await _context
                            .PartidaAtual
                            .Find(x => 
                                x.GameId == partidaAtual.GameId 
                                && x.Platform == partidaAtual.Platform 
                                && x.Aliados.Any(k => k.JogadorPrincipal && k.Participante.SummonerId == estatisticasPrincipalJogador.InvocadorId))
                            .FirstOrDefaultAsync();

                    if (partidaAtualBanco == null)
                    {
                        _logger.LogInformation("Gravando nova partida");

                        retorno.GameId = partidaAtual.GameId;
                        retorno.Platform = partidaAtual.Platform;

                        estatisticasPrincipalJogador =
                            await PersistirDadosJogadorAsync(regiao,
                                partidaAtual.Participants.First(x => x.SummonerId == estatisticasPrincipalJogador.InvocadorId).SummonerName,
                                new List<int> { (int)partidaAtual.Participants.First(x => x.SummonerId == estatisticasPrincipalJogador.InvocadorId).ChampionId });

                        retorno.Aliados = new List<JogadorPartidaAtual>();
                        retorno.Inimigos = new List<JogadorPartidaAtual>();

                        var ultimaVersao = await _util.RetornarUltimoPatchAsync();

                        var campeoes = await api.StaticData.Champions.GetAllAsync(ultimaVersao, Language.pt_BR, fullData: false);

                        var participantePrincipal = partidaAtual.Participants.First(x => x.SummonerId == estatisticasPrincipalJogador.InvocadorId);

                        var idTimeAliado = participantePrincipal.TeamId;

                        retorno.Aliados.Add(new JogadorPartidaAtual(estatisticasPrincipalJogador, participantePrincipal, true, campeoes, ultimaVersao, true));

                        //adicionar jogadorprincipal a lista de aliados
                        foreach (var item in partidaAtual.Participants.Where(x => x.SummonerId != estatisticasPrincipalJogador.InvocadorId).ToList())
                        {
                            var estatisticasJogador = await PersistirDadosJogadorAsync(regiao, item.SummonerName, new List<int> { (int)item.ChampionId });
                            if (item.TeamId == idTimeAliado)
                            {
                                retorno.Aliados.Add(new JogadorPartidaAtual(estatisticasJogador, item, true, campeoes, ultimaVersao));
                            }
                            else
                            {
                                retorno.Inimigos.Add(new JogadorPartidaAtual(estatisticasJogador, item, false, campeoes, ultimaVersao));
                            }
                        }

                        retorno.ChanceVitoriaAliados = ((retorno.Aliados.Sum(x => x.ChanceVitoria) / (decimal)retorno.Aliados.Count) + (retorno.Inimigos.Sum(x => 1 - x.ChanceVitoria) / (decimal)retorno.Inimigos.Count)) / 2;

                        retorno.ChanceVitoriaInimigos = 1 - retorno.ChanceVitoriaAliados;

                        var spells = await api.StaticData.SummonerSpells.GetAllAsync(ultimaVersao, Language.pt_BR);

                        await retorno.PreencherDicas(spells);

                        (retorno.ItensSugeridos, retorno.WardSugerida) = await _itensRegraNegocio.ObterSugestaoCampeao(participantePrincipal.ChampionId);

                        await _context.PartidaAtual.InsertOneAsync(retorno);

                        return retorno;
                    }
                    else
                    {
                        _logger.LogInformation("Partida já existente no banco, pulando");
                        return partidaAtualBanco;
                    }
                }
                catch (RiotSharpException ex)
                {

                    if (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        _logger.LogInformation(ex, "Partida em andamento não encontrada");

                        return null;
                    }
                    else
                    {
                        _logger.LogError(ex, "Ocorreu um erro ao obter partida existente");

                        return null;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ocorreu um erro ao obter partida existente");

                    return null;

                }
            }
            return null;
        }


        public async Task<EstatisticasJogador> PersistirEstatisticasJogadorAsync(string puuid)
        {
            #region Instancia api
            var api = RiotApi.GetDevelopmentInstance(_apiConfiguration.Key);

            var ultimaVersao = await _util.RetornarUltimoPatchAsync();

            var campeoes = await api.StaticData.Champions.GetAllAsync(ultimaVersao, Language.pt_BR, fullData: false);
            #endregion

            //Pega estatisticas do banco
            var estatisticasJogador = await _context.EstatisticasJogador.Find(x => x.PUDID == puuid).FirstOrDefaultAsync();


            //Pega últimas 4 seasons
            BsonArray arraySeasons = Util.RetornaUltimasSeason(4).Select(x => (int)x).ToArray().ToBsonDocumentArray();


            #region Retorna taxa de vitória
            var match = new BsonDocument
                {
                    {
                        "$match",
                        new BsonDocument
                            {
                                {"PlatformId", estatisticasJogador.Regiao},
                                {"ParticipantIdentities.Player.CurrentAccountId", estatisticasJogador.ContaId },
                                {"SeasonId", new BsonDocument
                                    {
                                        {
                                            "$in", arraySeasons
                                        }
                                    }
                                }
                            }
                    }
                };

            var unwind1 = new BsonDocument
            {
                {
                    "$unwind",
                    "$Participants"
                }
            };

            var unwind2 = new BsonDocument
            {
                {
                    "$unwind",
                    "$ParticipantIdentities"
                }
            };

            var match2 = new BsonDocument
                {
                    {
                        "$match",
                        new BsonDocument
                            {
                                {"ParticipantIdentities.Player.CurrentAccountId", estatisticasJogador.ContaId }
                            }
                    }
                };


            var redact = BsonDocument.Parse(@"{'$redact': {
                     '$cond': {
                         if: {$eq: ['$Participants.ParticipantId', '$ParticipantIdentities.ParticipantId']},
                         then : '$$KEEP',
                         else: '$$PRUNE'
                 }}}");

            var groupTaxaVitoria = BsonDocument.Parse(@"{$group: {
                _id : { $toString: '$Participants.ChampionId'}, 
                'PartidasGanhas' : { 
                            '$sum' : {$cond: [{$eq : ['$Participants.Stats.Winner', true]},1, 0 ]}
                            },
                        'PartidasPerdidas' : { 
                            '$sum' : {$cond: [{$eq : ['$Participants.Stats.Winner', false]},1, 0 ]}
                            },
            }}");

            //var projectTaxaVitoria = BsonDocument.Parse("{$project: {PercentualVitoria:{ $divide: [ '$PartidasGanhas', '$TotalPartidas' ]}}}");

            var pipelineTaxaVitoria = new[] { match, unwind1, unwind2, match2, redact, groupTaxaVitoria, /*projectTaxaVitoria */};

            var lista = await _context
               .Partidas
               .Aggregate<VitoriaDerrota>(pipelineTaxaVitoria)
            .ToListAsync();

            estatisticasJogador.CampeoesXTaxaVitoria = lista.Select(x => new Campeao
            {
                ID = Convert.ToInt32(x._id),
                PartidasGanhas = x.PartidasGanhas ?? 0,
                PartidasPerdidas = x.PartidasPerdidas ?? 0,
                TaxaVitoria = Math.Round((decimal)((decimal)x.PartidasGanhas / ((decimal)x.PartidasPerdidas + (decimal)x.PartidasGanhas)), 2),
                Nome = campeoes
                    .Champions
                    .FirstOrDefault(c => c.Value.Id == Convert.ToInt32(x._id)).Value.Name
            })
            .ToList();

            #endregion

            #region calculo de quantas partidas ganhas e quantas perdidas
            var groupVitoriaDerrota = BsonDocument.Parse(@"{$group: {
                _id : '$ParticipantIdentities.Player.CurrentAccountId', 
                'PartidasGanhas' : { 
                    '$sum' : {$cond: [{$eq : ['$Participants.Stats.Winner', true]},1, 0 ]}
                    },
                'PartidasPerdidas' : { 
                    '$sum' : {$cond: [{$eq : ['$Participants.Stats.Winner', false]},1, 0 ]}
                    },
                }}");

            var pipelineVitoriaDerrota = new[] { match, unwind1, unwind2, match2, redact, groupVitoriaDerrota };

            var vitoriaDerrota = await _context
               .Partidas
               .Aggregate<VitoriaDerrota>(pipelineVitoriaDerrota)
            .FirstOrDefaultAsync();

            estatisticasJogador.PartidasGanhas = vitoriaDerrota?.PartidasGanhas ?? 0;
            estatisticasJogador.PartidasPerdidas = vitoriaDerrota?.PartidasPerdidas ?? 0;

            #endregion

            #region lanes mais jogadas

            var projectLanesMaisJogadas = BsonDocument.Parse(@"{$project: {
               Lane:{ 
                   '$switch' : {
                        'branches':[
                            {case: { $eq : ['$Participants.Timeline.Lane', 'TOP']}, then: 'Top'},
                            {case: { $in: ['$Participants.Timeline.Lane',  ['MIDDLE','MID']]}, then: 'Mid'},
                            {case: { $and: [ { $in: ['$Participants.Timeline.Lane',  ['BOTTOM','BOT']] }, {$eq: ['$Participants.Timeline.Role', 'DUO_SUPPORT']}]}, then: 'Support'},
                            {case: { $and: [ { $in: ['$Participants.Timeline.Lane',  ['BOTTOM','BOT']] }, {$ne: ['$Participants.Timeline.Role', 'DUO_SUPPORT']}]}, then: 'Adc'},
                            {case: { $eq : ['$Participants.Timeline.Lane', 'JUNGLE']}, then: 'JUNGLE'},
                       ],
                        default: 'Não definida'
                       }
                   },
               Vitoria: '$Participants.Stats.Winner'
               }
           }");

            var groupLanesMaisJogadas = BsonDocument.Parse(@"{$group: {
                _id : '$Lane', 
                'PartidasGanhas' : { 
                    '$sum' : {$cond: [{$eq : ['$Vitoria', true]},1, 0 ]}
                    },
                'PartidasPerdidas' : { 
                    '$sum' : {$cond: [{$eq : ['$Vitoria', false]},1, 0 ]}
                    },
            }}");

            var pipelineLanesMaisJogadas = new[] { match, unwind1, unwind2, match2, redact, projectLanesMaisJogadas, groupLanesMaisJogadas };

            var lanesMaisJogadas = await _context
               .Partidas
               .Aggregate<VitoriaDerrota>(pipelineLanesMaisJogadas)
            .ToListAsync();

            var totalPartidasJogadas = lanesMaisJogadas?.Sum(l => (l.PartidasGanhas ?? 0) + (l.PartidasPerdidas ?? 0)) ?? 0;

            if (totalPartidasJogadas > 0)
                estatisticasJogador.Lanes = lanesMaisJogadas?.Select(x => new LaneModel(x._id, x.PartidasGanhas ?? 0, x.PartidasPerdidas ?? 0, totalPartidasJogadas)).ToList();

            #endregion


            #region taxaPrimeiroBarao
            var unwindTeams = new BsonDocument
            {
                {
                    "$unwind",
                    "$Teams"
                }
            };

            var redactTeams = BsonDocument.Parse(@"{'$redact': {
                     '$cond': {
                         if: {
                            $and: [
                                { $eq: ['$Participants.ParticipantId', '$ParticipantIdentities.ParticipantId'] },
                                { $eq: ['$Participants.TeamId', '$Teams.TeamId'] }
                            ]},
                         then : '$$KEEP',
                         else: '$$PRUNE'
                 }}}");


            var groupBaron = BsonDocument.Parse(@"{$group: {
              _id : '$ParticipantIdentities.Player.CurrentAccountId', 
              'PrimeiroBaronSim' : { 
                  '$sum' : {$cond: [{$eq : ['$Teams.FirstBaron', true]},1, 0 ]}
                  },
              'PrimeiroBaronNao' : { 
                  '$sum' : {$cond: [{$eq : ['$Teams.FirstBaron', false]},1, 0 ]}
                  },
            }}");

            var pipelineBaron = new[] { match, unwind1, unwind2, unwindTeams, match2, redactTeams, groupBaron };

            var baronEstatisticas = await _context
              .Partidas
              .Aggregate<BaronEstatisticas>(pipelineBaron)
           .FirstOrDefaultAsync();

            if (totalPartidasJogadas > 0)
                estatisticasJogador.TaxaPrimeiroBarao = Math.Round((decimal)((decimal)(baronEstatisticas?.PrimeiroBaronSim ?? 0) / (decimal)totalPartidasJogadas), 2);

            #endregion

            #region taxaFirstBlood


            var groupFirstBlood = BsonDocument.Parse(@"{$group: {
              _id : '$ParticipantIdentities.Player.CurrentAccountId', 
              'FirstBloodSim' : { 
                  '$sum' : {$cond: [{$eq : ['$Teams.FirstBlood', true]},1, 0 ]}
                  },
              'FirstBloodNao' : { 
                  '$sum' : {$cond: [{$eq : ['$Teams.FirstBlood', false]},1, 0 ]}
                  },
            }}");

            var pipelineFirstBlood = new[] { match, unwind1, unwind2, unwindTeams, match2, redactTeams, groupFirstBlood };

            var firstBloodEstatisticas = await _context
              .Partidas
              .Aggregate<FirstBloodEstatisticas>(pipelineFirstBlood)
           .FirstOrDefaultAsync();

            if (totalPartidasJogadas > 0)
                estatisticasJogador.TaxaFirstBlood = Math.Round((decimal)((decimal)firstBloodEstatisticas?.FirstBloodSim / (decimal)totalPartidasJogadas), 2);

            #endregion

            //Faz o update do documento
            await _context.EstatisticasJogador.ReplaceOneAsync(Builders<EstatisticasJogador>.Filter.Eq(x => x.PUDID, estatisticasJogador.PUDID), estatisticasJogador);
            return estatisticasJogador;
        }

        private class VitoriaDerrota
        {
            public string _id { get; set; }
            public int? PartidasGanhas { get; set; }
            public int? PartidasPerdidas { get; set; }
        }

        private class BaronEstatisticas
        {
            public string _id { get; set; }
            public int? PrimeiroBaronSim { get; set; }
            public int? PrimeiroBaronNao { get; set; }
        }

        private class FirstBloodEstatisticas
        {
            public string _id { get; set; }
            public int? FirstBloodSim { get; set; }
            public int? FirstBloodNao { get; set; }
        }
    }
}
