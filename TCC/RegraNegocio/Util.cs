using Microsoft.Extensions.Options;
using MongoDB.Bson;
using RiotSharp;
using RiotSharp.Endpoints.StaticDataEndpoint.Champion;
using RiotSharp.Endpoints.StaticDataEndpoint.Item;
using RiotSharp.Misc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TCC.Models;

namespace TCC.RegraNegocio
{
    public class Util
    {
        private readonly ApiConfiguration _apiConfiguration;

        public Util(IOptions<ApiConfiguration> apiConfiguration)
        {
            _apiConfiguration = apiConfiguration.Value;
        }

        public async Task<string> RetornarUltimoPatchAsync()
        {
            var api = RiotApi.GetDevelopmentInstance(_apiConfiguration.Key);

            return (await api.StaticData.Versions.GetAllAsync())[0];
        }

        public static string RetornarIconeCampeao(string versao, ChampionStatic campeao)
        {
            return $"//ddragon.leagueoflegends.com/cdn/{versao}/img/champion/{campeao.Image.Full}";
        }

        public static List<RiotSharp.Endpoints.MatchEndpoint.Enums.Season> RetornaUltimasSeason(int numeroSeasons)
        {
            var seasons = Enum.GetValues(typeof(RiotSharp.Endpoints.MatchEndpoint.Enums.Season));
            return seasons.Cast<RiotSharp.Endpoints.MatchEndpoint.Enums.Season>().OrderByDescending(x => (int)x).Take(numeroSeasons).ToList();
        }

        public static string RetornarLaneFinalJogador(string lane, string role)
        {
            if (lane == "TOP")
                return "Top";
            if (lane == "MIDDLE" || lane == "MID")
                return "Mid";
            if (lane == "BOTTOM" || lane == "BOT")
                return role == "DUO_SUPPORT" ? "Support" : "Adc";
            if (lane == "JUNGLE")
                return "Jungle";

            return "Não definida";

        }

        public static Item ObterItem(long itemID, ItemListStatic itens, string versao)
        {
            if (itemID > 0)
            {
                var conversao = itens.Items.TryGetValue((int)itemID, out var item);

                if (conversao)
                {
                    return new Item
                    {
                        Nome = item.Name,
                        UrlIcone = $"//ddragon.leagueoflegends.com/cdn/{versao}/img/item/{item.Image.Full}"
                    };
                }
                else
                {
                    return new Item
                    {
                        Nome = $"Item {itemID} não encontrado",
                        UrlIcone = "/images/image_not_found.png"
                    };
                }
            }

            return null;
        }
    }
}
