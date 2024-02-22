using System.Configuration;

namespace United.Common.Helper.PageProduct
{
    public interface IProductElementCollection
    {
        ProductElement this[string id] { get; }
        ProductElement this[int index] { get; set; }

        ConfigurationElementCollectionType CollectionType { get; }
    }
}