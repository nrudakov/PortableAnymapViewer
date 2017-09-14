using Portable_Anymap_Viewer.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Services.Store;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// Шаблон элемента пустой страницы задокументирован по адресу http://go.microsoft.com/fwlink/?LinkId=234238

namespace Portable_Anymap_Viewer
{
    /// <summary>
    /// Пустая страница, которую можно использовать саму по себе или для перехода внутри фрейма.
    /// </summary>
    public sealed partial class DonatePage : Page
    {
        public DonatePage()
        {
            this.InitializeComponent();
            this.LoadStoreItems();
        }

        private StoreContext storeContext = StoreContext.GetDefault();

        public async void LoadStoreItems()
        {
            // Create a filtered list of the product AddOns I care about
            string[] filterList = new string[] { "Consumable", "Durable", "UnmanagedConsumable" };

            // Get list of Add Ons this app can sell, filtering for the types we know about
            StoreProductQueryResult addOns = await storeContext.GetAssociatedStoreProductsAsync(filterList);

            ProductsListView.ItemsSource = await CreateProductListFromQueryResult(addOns, "Add-Ons");
        }

        public async Task<ObservableCollection<ItemDetails>> CreateProductListFromQueryResult(StoreProductQueryResult addOns, string description)
        {
            var productList = new ObservableCollection<ItemDetails>();

            if (addOns.ExtendedError != null)
            {
                var loader = new ResourceLoader();
                var warningTitle = loader.GetString("StoreFailureTitle");
                var ok = loader.GetString("Ok");
                MessageDialog decodeFailedDialog = new MessageDialog(addOns.ExtendedError.Message, warningTitle);
                decodeFailedDialog.Commands.Add(new UICommand(ok));
                decodeFailedDialog.DefaultCommandIndex = 0;
                await decodeFailedDialog.ShowAsync();
            }
            else
            {
                foreach (StoreProduct product in addOns.Products.Values)
                {
                    productList.Add(new ItemDetails(product));
                }
            }
            this.Purchase.Visibility = Visibility.Visible;
            return productList;
        }

        private async void Purchase_Click(object sender, RoutedEventArgs e)
        {
            var item = (ItemDetails)ProductsListView.SelectedItem;
            StorePurchaseResult result = await storeContext.RequestPurchaseAsync(item.StoreId);
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            switch (result.Status)
            {
                case StorePurchaseStatus.AlreadyPurchased:
                    Result.Text = loader.GetString("PurchaseStatusAlreadyPurchased");
                    break;

                case StorePurchaseStatus.Succeeded:
                    Result.Text = loader.GetString("PurchaseStatusSucceeded") + " " + item.Title + " !";
                    break;

                case StorePurchaseStatus.NotPurchased:
                    Result.Text = loader.GetString("PurchaseStatusNotPurchased");
                    break;

                case StorePurchaseStatus.NetworkError:
                    Result.Text = loader.GetString("PurchaseStatusNetworkError");
                    break;

                case StorePurchaseStatus.ServerError:
                    Result.Text = loader.GetString("PurchaseStatusServerError");
                    break;

                default:
                    Result.Text = loader.GetString("PurchaseStatusUnknownError");
                    break;
            }
        }
    }
}
