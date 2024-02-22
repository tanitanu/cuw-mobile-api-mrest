using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace United.Common.Helper.PageProduct
{
    public class ProductSection : ConfigurationSection, IProductSection
    {
        private static ConfigurationPropertyCollection properties;
        private static ConfigurationProperty productElementCollection;

        static ProductSection()
        {
            properties = new ConfigurationPropertyCollection();

            productElementCollection = new ConfigurationProperty(
                "products",
                typeof(ProductElementCollection),
                null,
                ConfigurationPropertyOptions.IsRequired
            );

            properties.Add(productElementCollection);
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get { return properties; }
        }

        [ConfigurationProperty("products", IsRequired = true)]
        public ProductElementCollection ProductElementCollection
        {
            get { return (ProductElementCollection)base[productElementCollection]; }
        }
    }
}
