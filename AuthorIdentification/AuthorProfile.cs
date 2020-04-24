using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AuthorIdentification {
	public class AuthorProfile {
		public string Author { get; set; }
		public List<KeyValuePair<string, int>> Ngrams { get; set; }
	}

	[Serializable]
	[XmlType(TypeName = "Ngram")]
	public struct KeyValuePair<K, V> {
		public K NGram { get; set; }

		public V Freq { get; set; }
	}

}
