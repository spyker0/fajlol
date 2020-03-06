using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace TCC.Models
{
    public class MelhoresItensCampeao
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }
        public int ChampionId { get; set; }
        public List<ItemCampeao> Itens { get; set; } = new List<ItemCampeao>();
        public ItemCampeao Ward { get; set; }
    }

    public class ItemCampeao
    {
        public long _id { get; set; }
        public int? Vitorias { get; set; }
        public int? Total { get; set; }
    }
}
