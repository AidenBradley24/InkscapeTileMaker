using System.Xml.Linq;
using System.Xml.Serialization;

namespace InkscapeTileMaker.Models
{
    public class TileWrapper : IDisposable
    {
        private readonly Tile _tile;
        private readonly XElement _element;
        private readonly XElement _collection;

		public Tile Value => _tile;

		public TileWrapper(XElement tileElement, XElement collectionElement)
        {
            var serializer = new XmlSerializer(typeof(Tile));
            using var reader = tileElement.CreateReader();
            _tile = (Tile)serializer.Deserialize(reader)!;
            _collection = collectionElement;
            _element = tileElement;
		}

        public void Dispose()
        {
            GC.SuppressFinalize(this);
			Sync();
		}

        public void Sync()
        {
			_element.Remove();

			var serializer = new XmlSerializer(typeof(Tile));
			XElement tileElement;

			using (var writer = new StringWriter())
			{
				serializer.Serialize(writer, _tile);
				tileElement = XElement.Parse(writer.ToString());
			}

			_collection.Add(tileElement);
		}
    }
}
