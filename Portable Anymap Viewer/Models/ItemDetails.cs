using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Services.Store;

namespace Portable_Anymap_Viewer.Models
{
    public class ItemDetails
    {
        public string Title { get; private set; }
        public string Price { get; private set; }
        public bool InCollection { get; private set; }
        public string ProductKind { get; private set; }
        public string StoreId { get; private set; }
        public string FormattedTitle => $"{Title} ({ProductKind}) {Price}, InUserCollection:{InCollection}";

        public ItemDetails(StoreProduct product)
        {
            Title = product.Title;
            Price = product.Price.FormattedPrice;
            InCollection = product.IsInUserCollection;
            ProductKind = product.ProductKind;
            StoreId = product.StoreId;
        }
    }
}
