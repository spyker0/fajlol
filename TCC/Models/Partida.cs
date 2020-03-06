using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using RiotSharp.Endpoints.MatchEndpoint;
using RiotSharp.Misc;

namespace TCC.Models
{
    public class Partida : Match
    {
        public Partida(Match partida, Region region)
        {
            PlatformId = region;
            SeasonId = partida.SeasonId;
            QueueId = partida.QueueId;
            GameId = partida.GameId;
            ParticipantIdentities = partida.ParticipantIdentities;
            GameVersion = partida.GameVersion;
            GameMode = partida.GameMode;
            MapId = partida.MapId;
            GameType = partida.GameType;
            Teams = partida.Teams;
            Participants = partida.Participants;
            GameDuration = partida.GameDuration;
            GameCreation = partida.GameCreation;
        }

        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public Region PlatformId { get; set; }
    }
}
