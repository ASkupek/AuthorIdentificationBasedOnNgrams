using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace AuthorIdentification {
	class Program {
		static int maxNgram = 5;
		static int minNgram = 1;
		static int profileLimit = 1500;           // Author profile contains only L most frequent ngrams

		/// <summary>
		/// Usage:
		/// AuthorIdentification.exe new <file> <author> [nGrams] [profileLimit]		Create a new profile for the given author
		/// AuthorIdentification.exe id <file>											Finds who the author of the text is 
		/// AuthorIdentification.exe test <corpus_folder>								... 
		/// AuthorIdentification.exe h													Prints help
		/// </summary>
		static int Main(string[] args) {
			if(args[0].ToLower() == "new") {
				// New author profile (creates it in the /Profiles folder)
				if(!File.Exists(args[1])) {
					Console.WriteLine("File does not exist");
					return 1;
				}

				if(args.Length >= 4)
					maxNgram = int.Parse(args[3]);

				if(args.Length >= 5)
					profileLimit = int.Parse(args[4]);

				DefineNewAuthorProfile(File.ReadAllText(args[1]), args.Length >= 3 ? args[2] : null);
			} else if(args[0].ToLower() == "test") {
				// Check accuracy & time efficiency of program
				StreamWriter resultsFile = new StreamWriter("results.txt", false);
				StreamWriter timingsFile = new StreamWriter("timings.txt", false);
				Stopwatch stopWatch;
				//Random rnd = new Random();
				int[] profileSizes = { 20, 50, 100, 200, 500, 1000 };
				double[,] accuracy_sum = new double[8, profileSizes.Length];
				double[,] timings_sum = new double[8, profileSizes.Length];
				int testRuns = 25;

				for(int i = 0; i < testRuns; i++) {
					for(int n = 3; n <= 10; n++) {
						for(int profile_i = 0; profile_i < profileSizes.Length; profile_i += 1) {
							DirectoryInfo directoryProfiles = new DirectoryInfo(args[1]);
							stopWatch = new Stopwatch();
							int elapsedTime = 0;

							maxNgram = n;
							profileLimit = profileSizes[profile_i];
							foreach(var file in directoryProfiles.GetFiles("*.txt")) {
								// Create profile for every author
								DefineNewAuthorProfile(File.ReadAllText(file.FullName), file.Name.Split('.')[0]);
							}

							int correct = 0;
							int all = 0;
							foreach(var file in directoryProfiles.GetFiles("*.txt")) {
								//if(rnd.Next(0, 100) >= 5)   // 5% celotnega korpusa
								//	continue;

								all += 1;
								string text = File.ReadAllText(file.FullName);
								int chunkSize = 150;
								//text = text.Substring(rnd.Next(0, text.Length - chunkSize), text.Length > chunkSize ? chunkSize : text.Length);
								text = text.Substring(0, text.Length > chunkSize ? chunkSize : text.Length);
								stopWatch.Start();
								if(FindTheAuthor(text) == file.Name.Split('.')[0].ToLower())
									correct += 1;
								stopWatch.Stop();
								elapsedTime += stopWatch.Elapsed.Milliseconds;
							}

							double accuracy = (double)correct / (double)all;
							accuracy_sum[n - 3, profile_i] += accuracy;
							timings_sum[n - 3, profile_i] += (double)elapsedTime / (double)all;
						}

					}
				}

				for(int i = 3; i <= 10; i++) {
					for(int j = 0; j < profileSizes.Length; j += 1) {
						timingsFile.Write(string.Format("{0:0.##}\t", (double)timings_sum[i - 3, j] / (double)testRuns));
						resultsFile.Write(string.Format("{0:0.##}\t", (double)accuracy_sum[i - 3, j] / (double)testRuns));
					}

					resultsFile.Write("\n");
					timingsFile.Write("\n");
				}

				resultsFile.Close();
				timingsFile.Close();
			} else if(args[0].ToLower() == "id") {
				// Find the author of the text
				if(!File.Exists(args[1])) {
					Console.WriteLine("File does not exist");
					return 1;
				}
				if(!Directory.Exists(Environment.CurrentDirectory + $"\\Profiles")) {
					Console.WriteLine("Folder 'Profiles' does not exist");
					return 2;
				}
				FindTheAuthor(File.ReadAllText(args[1]));
			} else if(args[0].ToLower() == "h" || (args[0].ToLower() == "help")) {
				// User help
				Console.Write("\nProgram usage:\n" +
					" - new <file> <author> [nGrams] [profileLimit]\t\t(Create a new profile for the given author)\n" +
					" - id <file>\t\t\t\t\t\t(Finds who the author of the text is)\n" +
					" - h|help\t\t\t\t\t\t(This help)\n\n");
			}

			return 0;
		}



		static string FindTheAuthor(string text) {
			AuthorProfile textProfile = new AuthorProfile() { Ngrams = GenerateProfile(text), Author = "UNKNOWN" };
			string BestMatchAuthor = "UNKNOWN";
			int minDis = int.MaxValue;

			// XML serialization/reading variables
			DirectoryInfo d = new DirectoryInfo(Environment.CurrentDirectory + $"\\Profiles");
			XmlSerializer serializer = new XmlSerializer(typeof(AuthorProfile));
			FileStream fileStream;

			foreach(var file in d.GetFiles("*.xml")) {
				// Check all xml profiles in /Profile folder
				fileStream = new FileStream($"Profiles/{file}", FileMode.Open);
				AuthorProfile tmp = (AuthorProfile)serializer.Deserialize(fileStream);

				int dis = CompareTwoProfiles(tmp, textProfile);
				if(dis <= minDis) {
					minDis = dis;
					BestMatchAuthor = tmp.Author;
				}

				fileStream.Close();
			}

			Console.WriteLine(BestMatchAuthor);
			return BestMatchAuthor;
		}


		/// <summary>
		/// Compares two profiles and return their dissimilarity.
		/// </summary>
		static int CompareTwoProfiles(AuthorProfile profile1, AuthorProfile profile2) {
			int sum = 0;
			List<NgramFreq> allNgrams = new List<NgramFreq>();
			int indexOfItemInOtherP;

			foreach(KeyValuePair<string, int> item in profile1.Ngrams) {
				indexOfItemInOtherP = profile2.Ngrams.IndexOf(item);
				allNgrams.Add(new NgramFreq() {
					ngram = item.NGram,
					f1 = item.Freq,
					f2 = (indexOfItemInOtherP == -1 ? 0 : profile2.Ngrams[indexOfItemInOtherP].Freq)
				});
			}

			foreach(KeyValuePair<string, int> item in profile2.Ngrams) {
				if(!allNgrams.Exists(x => x.ngram == item.NGram)) {
					// Ngram was NOT processed yet
					indexOfItemInOtherP = profile1.Ngrams.IndexOf(item);
					allNgrams.Add(new NgramFreq() {
						ngram = item.NGram,
						f1 = (indexOfItemInOtherP == -1 ? 0 : profile1.Ngrams[indexOfItemInOtherP].Freq),
						f2 = item.Freq
					});
				}
			}

			// Calculate total dissimilarity
			foreach(var item in allNgrams) {
				sum += (int)Math.Floor(Math.Pow(2 * (item.f1 - item.f2) / (item.f1 + item.f2), 2));
			}

			return sum;
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
			List<KeyValuePair<string, int>> myList = GenerateProfile(learnData, profileLimit);

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

			authorProfile.Author = catname.ToLower();
			authorProfile.Ngrams = myList;
			Directory.CreateDirectory("Profiles");
			xmlFile = File.Create(Environment.CurrentDirectory + $"\\Profiles\\{catname}.xml");
			writer.Serialize(xmlFile, authorProfile);
			xmlFile.Close();
		}


		static List<KeyValuePair<string, int>> GenerateProfile(string data, int limit = 0) {
			List<string> tokens = TokenizeText(data);
			Dictionary<string, int> table;

			table = new Dictionary<string, int>();
			foreach(string token in tokens) {
				// Predelamo vse tokens
				for(int N = minNgram; N <= maxNgram; N++) {
					// Za vsak token tvorimo 1..N gramov
					foreach(string gram in GetNgrams(token, N)) {
						table.TryGetValue(gram, out var currentCount);
						table[gram] = currentCount + 1;
					}
				}
			}

			List<KeyValuePair<string, int>> myList = GenericKeyValuePairToMyKeyValuePair(table.ToList());
			myList.Sort((pair1, pair2) => pair2.Freq.CompareTo(pair1.Freq));    // Sort that sh!t

			if(limit > 0)
				myList = myList.Take(limit).ToList();

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

			while(i <= input.Length - maxNgram) {
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

	struct NgramFreq {
		public string ngram;
		public int f1;          // Frequency in profile 1
		public int f2;          // Frequency in profile 2
	}

}
