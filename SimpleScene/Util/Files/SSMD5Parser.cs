using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using OpenTK;

namespace SimpleScene
{
	public abstract class SSMD5Parser
	{
		public static readonly string _quotedStrRegex = @"(?<="")[^\""]*(?="")";
		public static readonly string _uintRegex = @"(\d+)";
		public static readonly string _intRegex = @"(-*\d+)";
		public static readonly string _floatRegex = @"(-*\d*\.*\d*[Ee]*-*\d*)";
		public static readonly string _parOpen = @"\(";
		public static readonly string _parClose = @"\)";

		private static readonly char[] _wordDelimeters = {' ', '\t' };

		private Dictionary<string, Regex> m_regexCache = new Dictionary<string, Regex>();
		private int m_lineIdx = 0;
		private StreamReader m_reader;

        public SSMD5Parser(string path)
		{
            m_reader = SSAssetManager.OpenStreamReader (path);
			System.Console.WriteLine ("Reading a \"doom\" file: " + path);
		}

		public Match[] seekEntry(params string[] wordRegExStrArray)
		{
			Regex[] regexArray = new Regex[wordRegExStrArray.Length];
			for (int w = 0; w < wordRegExStrArray.Length; ++w) {
				string regExStr = wordRegExStrArray [w];
				Regex regex = null;
				if (!m_regexCache.TryGetValue(regExStr, out regex)) {
					regex = new Regex (regExStr);
					m_regexCache.Add (regExStr, regex);
				}
				regexArray [w] = regex;
			}
			return seekRegexEntry (regexArray, wordRegExStrArray);
		}

		public void seekFloats(float[] floats)
		{
			int numSoFar = 0;
			while (numSoFar < floats.Length) {
				string line = m_reader.ReadLine ();
				string[] words = line.Split (_wordDelimeters);
				for (int n = 0; n < words.Length; ++n) {
					if (words [n].Length > 0) {
						floats [numSoFar++] = (float)Convert.ToDouble (words [n], CultureInfo.InvariantCulture);
					}
				}
			}
		}

		private Match[] seekRegexEntry(Regex[] wordRegEx, string[] wordRegExStr)
		{
			string line;

			while ((line = m_reader.ReadLine ()) != null) {
				int commentsIdx = line.IndexOf ("//");
				if (commentsIdx >= 0) {
					line = line.Substring (0, commentsIdx);
				}

				string[] words = line.Split (_wordDelimeters);
				if (words.Length > 1 || (words.Length == 1 && words[0].Length > 0)) {

					// combine words when in brackets
					bool openBracket = false;
					var adjustedWords = new List<string> ();
					for (int w = 0; w < words.Length; ++w) {
						string word = words [w];
						if (word.Length > 0) {
							if (openBracket) {
								adjustedWords [adjustedWords.Count - 1] += " ";
								adjustedWords [adjustedWords.Count - 1] += word;
								if (word [word.Length - 1] == '\"') {
									openBracket = false;
								}
							} else {
								adjustedWords.Add (word);
								if (word[0] == '\"' 
									&& (word.Length == 1 || word[word.Length-1] != '\"')) {
									openBracket = true;
								}
							}
						}
					}

					if (adjustedWords.Count == 0) continue;

					if (adjustedWords.Count != wordRegEx.Length) {
						entryFailure (line, wordRegExStr);
					}

					Match[] ret = new Match[wordRegEx.Length];
					for (int w = 0; w < wordRegEx.Length; ++w) {
						ret [w] = wordRegEx [w].Match (adjustedWords [w]);
						if (!ret [w].Success) {
							entryFailure (line, wordRegExStr);
						}
					}
					m_lineIdx++;
					return ret;
				}
				m_lineIdx++;
			}
			entryFailure ("EOF", wordRegExStr);
			return null;
		}

		private void entryFailure(string line, string[] regexStr)
		{
			string expectingStr = "";
			for (int r = 0; r < regexStr.Length; ++r) {
				expectingStr += regexStr [r] + ' ';
			}

			string errorStr = String.Format (
				"Failed to read a \"doom\" file: line {0}: {1} *** Expecting: {2}",
				m_lineIdx, line, expectingStr);
			System.Console.WriteLine (errorStr);
			throw new Exception (errorStr);
		}
	}
}

