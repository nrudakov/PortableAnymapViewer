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
       // public StoreImage Image { get; private set; }
        public string Title { get; private set; }
        public string Price { get; private set; }
        public string Description { get; private set; }
        public bool InCollection { get; private set; }
        public string ProductKind { get; private set; }
        public string StoreId { get; private set; }
        //public object FormattedImage => Image;
        public string FormattedTitle => $"{Title}";
        public string FormattedPrice => $"{Price}";
        public string FormattedDescription => $"{Description}";

        public ItemDetails(StoreProduct product)
        {
            //if (product.Images.Count > 0)
            //    Image = product.Images[0];
            Title = product.Title;
            Price = product.Price.FormattedPrice;
            Description = product.Description;
            InCollection = product.IsInUserCollection;
            ProductKind = product.ProductKind;
            StoreId = product.StoreId;
        }
    }
}
