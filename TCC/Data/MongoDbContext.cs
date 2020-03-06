using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TCC.Models;

namespace TCC.Data
{
    public class MongoDbContext
    {
        public static string ConnectionString { get; set; }
        public static string DatabaseName { get; set; }
        public static bool IsSSL { get; set; }
        private IMongoDatabase _database { get; }
        public MongoDbContext()
        {
            try
            {
                MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(ConnectionString));
                if (IsSSL)
                {
                    settings.SslSettings = new SslSettings { EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 };
                }
                var mongoClient = new MongoClient(settings);
                _database = mongoClient.GetDatabase(DatabaseName);

                var partidasBuilder = Builders<Partida>.IndexKeys;
                var indexModel = new CreateIndexModel<Partida>(
                    partidasBuilder
                        .Combine(
                            Builders<Partida>.IndexKeys.Ascending(x => x.PlatformId), 
                            Builders<Partida>.IndexKeys.Ascending(x => x.GameId))
                        .Ascending("ParticipantIdentities.Player.CurrentAccountId"));
                _database.GetCollection<Partida>("Partidas").Indexes.CreateOne(indexModel);

                var estatisticasBuilder = Builders<EstatisticasJogador>.IndexKeys;
                var indexEstatisticas = new CreateIndexModel<EstatisticasJogador>(
                    estatisticasBuilder
                    .Combine(
                            Builders<EstatisticasJogador>.IndexKeys.Ascending(x => x.NomeNormalizado),
                            Builders<EstatisticasJogador>.IndexKeys.Ascending(x => x.Regiao))
                    .Ascending(x => x.PUDID));
                _database.GetCollection<EstatisticasJogador>("EstatisticasJogador").Indexes.CreateOne(indexEstatisticas);
            }
            catch (Exception ex)
            {
                throw new Exception("Não foi possível se conectar com o servidor.", ex);
            }
        }

        public IMongoCollection<Partida> Partidas
        {
            get
            {
                return _database.GetCollection<Partida>("Partidas");
            }
        }

        public IMongoCollection<EstatisticasJogador> EstatisticasJogador
        {
            get
            {
                return _database.GetCollection<EstatisticasJogador>("EstatisticasJogador");
            }
        }

        public IMongoCollection<PartidaAtualViewModel> PartidaAtual
        {
            get
            {
                return _database.GetCollection<PartidaAtualViewModel>("PartidaAtual");
            }
        }

        public IMongoCollection<MelhoresItensCampeao> MelhoresItensCampeao
        {
            get
            {
                return _database.GetCollection<MelhoresItensCampeao>("MelhoresItensCampeao");
            }
        }
    }
}
