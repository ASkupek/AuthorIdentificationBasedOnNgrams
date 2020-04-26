using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Parser {
	class Program {
		static void Main(string[] args) {
			var fileName = args[0];
			XDocument xdoc = XDocument.Load(fileName);
			XNamespace ns = "http://www.tei-c.org/ns/1.0";
			//GREMO DO NASLOVA IN FOUNDERJA
			var temp = xdoc.Element(ns + "TEI");
			var temp2 = temp.Element(ns + "teiHeader");
			var temp3 = temp2.Element(ns + "fileDesc");
			var temp4 = temp3.Element(ns + "titleStmt");
			var title = temp4.Element(ns + "title").Value;
			var founder = temp4.Element(ns + "funder").Value;
			//PRAVICE
			var publicationStmt = temp3.Element(ns + "publicationStmt");
			var availability = publicationStmt.Element(ns + "availability");
			var p = availability.Element(ns + "p"); //zenkrat imamo 2 p-ja first pa last daj, ker je v slo in ang

			//bibliografija
			var sourceDesc = temp3.Element(ns + "sourceDesc");
			var bibl = sourceDesc.Element(ns + "bibl");
			var titleBibl = bibl.Element(ns + "title").Value;
			var author = bibl.Element(ns + "author").Value;
			var date = bibl.Element(ns + "date").Value;
			var publisher = bibl.Element(ns + "publisher").Value;

			//povzetki
			var encodingDesc = temp2.Element(ns + "encodingDesc");
			var samplingDecl = encodingDesc.Element(ns + "samplingDecl");
			var povzetki = samplingDecl.Element(ns + "p").Value;

			//text
			var text = temp.Element(ns + "text");
			var body = text.Element(ns + "body");
			List<string> paragraph = body.Elements(ns + "p").Elements(ns + "s").Elements(ns + "w").Select(x => (string)x).ToList();
			//Console.WriteLine(paragraph[paragraph.Count-1]);
			//var s = paragraph.Element(ns + "s");
			//var w = s.Element(ns + "w");
			using(StreamWriter outputFile = new StreamWriter($"{author.Replace(" ", "")}.txt", true)) {
				//outputFile.WriteLine(title);
				//outputFile.WriteLine(founder);
				//outputFile.WriteLine(titleBibl);
				//outputFile.WriteLine(author);
				//outputFile.WriteLine(date);
				//outputFile.WriteLine(publisher);
				//outputFile.WriteLine(povzetki);
				foreach(var line in paragraph) {
					//Console.WriteLine(line);
					outputFile.Write(line + " ");
				}
				outputFile.Write("\n");
				outputFile.Close();
			}
		}
	}
}
