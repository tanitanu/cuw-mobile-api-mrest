using System.Collections.Generic;
using System.Configuration;

namespace United.Common.Helper.PageProduct
{
    [ConfigurationCollection(typeof(ProductElement), AddItemName = "product", CollectionType = ConfigurationElementCollectionType.BasicMap)]
    public class ProductElementCollection : ConfigurationElementCollection
    {
        private static ConfigurationPropertyCollection properties;

        public ProductElementCollection()
        {
        }

        /* FOR TEST ONLY */
        public ProductElementCollection(ICollection<ProductElement> elements)
        {
            foreach (ProductElement element in elements)
            {
                base.BaseAdd(element);
            }
        }

        static ProductElementCollection()
        {
            properties = new ConfigurationPropertyCollection();
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get { return properties; }
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.BasicMap; }
        }

        protected override string ElementName
        {
            get { return "product"; }
        }

        public ProductElement this[int index]
        {
            get { return (ProductElement)base.BaseGet(index); }
            set
            {
                if (base.BaseGet(index) != null)
                {
                    base.BaseRemoveAt(index);
                }
                base.BaseAdd(index, value);
            }
        }

        public ProductElement this[string id]
        {
            get { return (ProductElement)base.BaseGet(id); }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new ProductElement();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as ProductElement).Id;
        }
    }
}
