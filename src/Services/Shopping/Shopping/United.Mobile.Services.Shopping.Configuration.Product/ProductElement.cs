using System.Configuration;

namespace United.Mobile.Services.Shopping.Configuration.Product
{
    public class ProductElement : ConfigurationElement
    {
        private static ConfigurationPropertyCollection properties;
        private static ConfigurationProperty id;
        private static ConfigurationProperty key;
        private static ConfigurationProperty title;
        private static ConfigurationProperty description;
        private static ConfigurationProperty cabinCount;
        private static ConfigurationProperty header;
        private static ConfigurationProperty body;
        private static ConfigurationProperty details;
        private static ConfigurationProperty shouldShowShortCabinName;

        static ProductElement()
        {
            properties = new ConfigurationPropertyCollection();

            id = new ConfigurationProperty(
                "id",
                typeof(string),
                null,
                ConfigurationPropertyOptions.IsRequired
            );

            key = new ConfigurationProperty(
                "key",
                typeof(string),
                null,
                ConfigurationPropertyOptions.IsRequired
            );

            title = new ConfigurationProperty(
                "title",
                typeof(string),
                null,
                ConfigurationPropertyOptions.IsRequired
            );

            description = new ConfigurationProperty(
                "description",
                typeof(string),
                null,
                ConfigurationPropertyOptions.IsRequired
            );

            cabinCount = new ConfigurationProperty(
                "cabinCount",
                typeof(string),
                null,
                ConfigurationPropertyOptions.IsRequired
            );

            header = new ConfigurationProperty(
                "header",
                typeof(string),
                null,
                ConfigurationPropertyOptions.IsRequired
            );

            body = new ConfigurationProperty(
                "body",
                typeof(string),
                null,
                ConfigurationPropertyOptions.IsRequired
            );

            details = new ConfigurationProperty(
                "details",
                typeof(string),
                null,
                ConfigurationPropertyOptions.IsRequired
            );

            shouldShowShortCabinName = new ConfigurationProperty(
                "shouldShowShortCabinName",
                typeof(bool),
                false);

            properties.Add(id);
            properties.Add(key);
            properties.Add(title);
            properties.Add(description);
            properties.Add(cabinCount);
            properties.Add(header);
            properties.Add(body);
            properties.Add(details);
            properties.Add(shouldShowShortCabinName);
        }

        public ProductElement() { }

        /* FOR TEST ONLY */
        public ProductElement(string id, string key, string title, string description, string cabinCount, string header, string body, string details, bool shouldShowShortCabinName = false)
        {
            base[ProductElement.id] = id;
            base[ProductElement.key] = key;
            base[ProductElement.title] = title;
            base[ProductElement.description] = description;
            base[ProductElement.cabinCount] = cabinCount;
            base[ProductElement.header] = header;
            base[ProductElement.details] = details;
            base[ProductElement.body] = body;
            base[ProductElement.shouldShowShortCabinName] = shouldShowShortCabinName;
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get { return properties; }
        }

        [ConfigurationProperty("id", IsRequired = true)]
        public string Id
        {
            get { return (string)base[id]; }
        }

        [ConfigurationProperty("key", IsRequired = true)]
        public string Key
        {
            get { return (string)base[key]; }
        }

        [ConfigurationProperty("title", IsRequired = true)]
        public string Title
        {
            get { return (string)base[title]; }
        }

        [ConfigurationProperty("description", IsRequired = true)]
        public string Description
        {
            get { return (string)base[description]; }
        }

        [ConfigurationProperty("cabinCount", IsRequired = true)]
        public string CabinCount
        {
            get { return (string)base[cabinCount]; }
        }

        [ConfigurationProperty("header", IsRequired = true)]
        public string Header
        {
            get { return (string)base[header]; }
        }

        [ConfigurationProperty("body", IsRequired = true)]
        public string Body
        {
            get { return (string)base[body]; }
        }

        [ConfigurationProperty("details", IsRequired = true)]
        public string Details
        {
            get { return (string)base[details]; }
        }

        [ConfigurationProperty("shouldShowShortCabinName", DefaultValue = false)]
        public bool ShouldShowShortCabinName
        {
            get { return (bool)base[shouldShowShortCabinName]; }
        }
    }
}
