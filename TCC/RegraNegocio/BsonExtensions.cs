using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TCC.RegraNegocio
{
    public static class BsonExtensions
    {
        public static BsonArray ToBsonDocumentArray(this int[] list)
        {
            var array = new BsonArray();
            foreach (var item in list)
            {
                array.Add(item);
            }
            return array;
        }
    }
}
