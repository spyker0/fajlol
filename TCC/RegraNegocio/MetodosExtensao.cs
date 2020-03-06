using RiotSharp.Endpoints.StaticDataEndpoint.Item;
using System;
using System.Collections.Generic;
using System.Linq;
using TCC.Models;

namespace TCC.RegraNegocio
{
    public static class MetodosExtensao
    {

        public static decimal RetornarMedia(this IEnumerable<decimal> taxa)
        {
            try
            {
                if (taxa.Count() > 0)
                    return taxa.Sum(x => x) / (decimal)taxa.Count();
            }
            catch (Exception ex)
            {

            }
            return 0;
        }

        public static void AdicionarItem(this List<Item> itensTratados, long itemID, ItemListStatic itens, string versao)
        {
            var item = Util.ObterItem(itemID, itens, versao);
            if (item != null)
                itensTratados.Add(item);
        }
    }
}
