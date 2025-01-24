using System.Configuration;

namespace ConfigurableReader;

public class BookPosition : ConfigurationSection
{
    [ConfigurationProperty("books")]
    [ConfigurationCollection(typeof(BookCollection), AddItemName = "book")]
    public BookCollection Books => (BookCollection)this["books"];
    public class BookCollection :
        ConfigurationElementCollection, IEnumerable<Book>
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new Book();
        }
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((Book)element).Name;
        }
        public void Add(Book book)
        {
            BaseAdd(book);
        }
        public new IEnumerator<Book> GetEnumerator()
        {
            foreach (var key in BaseGetAllKeys())
            {
                yield return (Book)BaseGet(key);
            }
        }
    }
    public class Book : ConfigurationElement
    {
        [ConfigurationProperty("name", IsRequired = true, IsKey = true)]
        public string Name
        {
            get
            {
                return (string)this["name"];
            }
            set
            {
                this["name"] = value;
            }
        }
        [ConfigurationProperty("scrollPosition", IsRequired = true)]
        public int ScrollPosition
        {
            get
            {
                return (int)this["scrollPosition"];
            }
            set
            {
                this["scrollPosition"] = value;
            }
        }
    }
}
