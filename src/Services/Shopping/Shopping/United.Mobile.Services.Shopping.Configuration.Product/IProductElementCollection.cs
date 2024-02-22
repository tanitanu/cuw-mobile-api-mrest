using System.Configuration;

namespace United.Mobile.Services.Shopping.Configuration.Product
{
    public interface IProductElementCollection
    {
        ProductElement this[string id] { get; }
        ProductElement this[int index] { get; set; }

        ConfigurationElementCollectionType CollectionType { get; }
    }
}