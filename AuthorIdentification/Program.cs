using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace AuthorIdentification {
	class Program {
		const int maxNgram = 5;
		const int minNgram = 1;

		/// <summary>
		/// Usage:
		/// AuthorIdentification.exe c <file> [author]		Create a new ngram profile from <file>
		/// </summary>
		static int Main(string[] args) {
			if(args[0].ToLower() == "c") {
				if(!File.Exists(args[1])) {
					Console.WriteLine("File does not exist");
					return 1;
				}

				DefineNewAuthorProfile(File.ReadAllText(args[1]), args.Length == 3 ? args[2] : null);
			}

			return 0;
		}


		/// <summary>
		/// Create a new author profile and save it to a file
		/// </summary>
		static void DefineNewAuthorProfile(string learnData, string author = null) {
			//StreamWriter file = File.AppendText("profiles.txt");
			AuthorProfile authorProfile = new AuthorProfile();
			FileStream xmlFile;
			XmlSerializer writer = new XmlSerializer(typeof(AuthorProfile));

			// Extract only words from text
			List<KeyValuePair<string, int>> myList = GenerateProfile(learnData);

			// Author's name
			string catname;
			if(author == null) {
				// Manual author's name input
				Console.Write($"Enter authors name:");
				catname = Console.ReadLine();
			} else {
				catname = author;
			}

			if(catname.Length == 0)
				catname = "UNDEFINED";

			authorProfile.Author = catname;
			authorProfile.Ngrams = myList;
			xmlFile = File.Create(Environment.CurrentDirectory + $"\\{catname}.xml");
			writer.Serialize(xmlFile, authorProfile);
			xmlFile.Close();




			//file.Write(catname.Replace(" ", "") + " ");
			//for(int i = 0; i < myList.Count; i++) {
			//	if(i >= 300)
			//		break;
			//	file.Write(myList[i].Key);
			//	file.Write(" ");
			//}

			//file.Write("\n");
			//file.Close();
		}


		static List<KeyValuePair<string, int>> GenerateProfile(string data) {
			List<string> tokens = TokenizeText(data);
			Dictionary<string, int> table;

			table = new Dictionary<string, int>();
			foreach(string token in tokens) {
				// Predelamo vse tokens
				for(int N = minNgram; N <= maxNgram; N++) {
					// Za vsak token tvorimo 1..5 N gramov
					foreach(string gram in GetNgrams(token, N)) {
						table.TryGetValue(gram, out var currentCount);
						table[gram] = currentCount + 1;
					}
				}
			}

			List<KeyValuePair<string, int>> myList = GenericKeyValuePairToMyKeyValuePair(table.ToList());
			myList.Sort((pair1, pair2) => pair2.Freq.CompareTo(pair1.Freq));	// Sort that sh!t

			return myList;
		}


		/// <summary>
		/// Transforms List<System.Collections.Generic.KeyValuePair> to List<AuthorIdentification.KeyValuePair>
		/// </summary>
		static List<KeyValuePair<string, int>> GenericKeyValuePairToMyKeyValuePair(List<System.Collections.Generic.KeyValuePair<string, int>> input) {
			List<KeyValuePair<string, int>> output = new List<KeyValuePair<string, int>>();
			foreach(var item in input) {
				output.Add(new KeyValuePair<string, int> { NGram = item.Key, Freq = item.Value });
			}

			return output;
		}


		/// <summary>
		/// Get all N-grams
		/// </summary>
		static List<string> GetNgrams(string input, int N) {
			List<string> output = new List<string>();
			int i = 0;
			string tmp;

			while(i <= input.Length - 5) {
				tmp = input.Substring(i, N).Replace(" ", "");
				if(tmp != "")
					output.Add(tmp);
				i += 1;
			}

			return output;
		}


		static List<string> TokenizeText(string text) {
			List<string> tokenTmp;
			string tmp = SanitizeString(text);

			tokenTmp = tmp.Split(' ').ToList().Where(x => x != " " && x != "").ToList();

			for(int j = 0; j < tokenTmp.Count; j++) {
				// Padding 
				tokenTmp[j] = tokenTmp[j].Insert(0, " ");
				tokenTmp[j] += "    ";
			}

			return tokenTmp;
		}


		/// <summary>
		/// Extract only words from text
		/// </summary>
		static string SanitizeString(string text) {
			string tmp;
			string replacer = "";

			tmp = Regex.Replace(text, @"[\d-]", " ");
			tmp = Regex.Replace(tmp, @"\d", "");        // Get rid of numbers
			tmp = Regex.Replace(tmp, @"\t|\n|\r", "");  // Get rid of tabs, newlines, ...
			tmp = tmp.Replace(@"!", replacer);
			tmp = tmp.Replace(@"?", replacer);
			tmp = tmp.Replace(@".", replacer);
			tmp = tmp.Replace(@",", replacer);
			tmp = tmp.Replace(@"-", replacer);
			tmp = tmp.Replace(@"–", replacer);
			tmp = tmp.Replace(@":", replacer);
			tmp = tmp.Replace(@";", replacer);
			tmp = tmp.Replace("\"", replacer);
			tmp = tmp.Replace("\'", replacer);
			tmp = tmp.Replace("[", replacer);
			tmp = tmp.Replace("]", replacer);
			tmp = tmp.Replace("(", replacer);
			tmp = tmp.Replace(")", replacer);
			tmp = tmp.Replace("•", replacer);
			tmp = tmp.Replace("\\", replacer);
			tmp = tmp.Replace("/", replacer);
			tmp = tmp.Replace("%", replacer);
			tmp = tmp.Replace("#", replacer);
			tmp = tmp.Replace("*", replacer);
			tmp = tmp.Replace("&", replacer);
			tmp = tmp.Replace("<", replacer);
			tmp = tmp.Replace(">", replacer);
			tmp = tmp.Replace("»", replacer);
			tmp = tmp.Replace("«", replacer);
			tmp = tmp.ToLower();

			return tmp;
		}
	}

	struct Paragraf {
		public List<string> Tokens;
		public string TextAll;
	}
}
