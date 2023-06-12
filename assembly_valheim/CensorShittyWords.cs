using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public static class CensorShittyWords
{

	public static bool Filter(string input, out string output)
	{
		bool flag;
		return CensorShittyWords.Filter(input, out output, out flag);
	}

	public static bool Filter(string input, out string output, out bool cacheMiss)
	{
		string text;
		if (CensorShittyWords.cachedCensored.TryGetValue(input, out text))
		{
			output = text;
			cacheMiss = false;
			return true;
		}
		if (CensorShittyWords.cachedNotCensored.Contains(input))
		{
			output = input;
			cacheMiss = false;
			return false;
		}
		if (!CensorShittyWords.normalizedListsGenerated)
		{
			CensorShittyWords.<Filter>g__GenerateNormalizedLists|1_0();
		}
		bool flag = CensorShittyWords.<Filter>g__FilterInternal|1_1(input, out output);
		if (flag)
		{
			CensorShittyWords.cachedCensored.Add(input, output);
		}
		else
		{
			CensorShittyWords.cachedNotCensored.Add(input);
		}
		cacheMiss = true;
		return flag;
	}

	public static bool Filter(string input, out string output, List<string>[] blacklists, List<string>[] whitelists)
	{
		string text = CensorShittyWords.Normalize(input);
		string text2 = CensorShittyWords.NormalizeStrict(input);
		Dictionary<string, List<int>> dictionary = new Dictionary<string, List<int>>();
		foreach (List<string> list in blacklists)
		{
			for (int j = 0; j < list.Count; j++)
			{
				string text3 = CensorShittyWords.NormalizeStrict(list[j]);
				int[] array2 = text2.AllIndicesOf(text3);
				if (array2.Length != 0)
				{
					if (dictionary.ContainsKey(list[j]))
					{
						for (int k = 0; k < array2.Length; k++)
						{
							if (!dictionary[list[j]].Contains(array2[k]))
							{
								dictionary[list[j]].Add(array2[k]);
							}
						}
					}
					else
					{
						dictionary.Add(list[j], new List<int>(array2));
					}
				}
			}
		}
		if (dictionary.Count <= 0)
		{
			output = input;
			return false;
		}
		foreach (List<string> list2 in whitelists)
		{
			for (int l = 0; l < list2.Count; l++)
			{
				string text4 = CensorShittyWords.Normalize(list2[l]);
				int[] array3 = text.AllIndicesOf(text4);
				if (array3.Length != 0)
				{
					string text5 = CensorShittyWords.NormalizeStrict(list2[l]);
					Dictionary<string, int[]> dictionary2 = new Dictionary<string, int[]>();
					foreach (KeyValuePair<string, List<int>> keyValuePair in dictionary)
					{
						int[] array4 = text5.AllIndicesOf(CensorShittyWords.NormalizeStrict(keyValuePair.Key));
						if (array4.Length != 0)
						{
							dictionary2.Add(keyValuePair.Key, array4);
						}
					}
					for (int m = 0; m < array3.Length; m++)
					{
						foreach (KeyValuePair<string, int[]> keyValuePair2 in dictionary2)
						{
							for (int n = 0; n < keyValuePair2.Value.Length; n++)
							{
								int num = array3[m] + keyValuePair2.Value[n];
								if (dictionary[keyValuePair2.Key].Contains(num))
								{
									dictionary[keyValuePair2.Key].Remove(num);
									if (dictionary[keyValuePair2.Key].Count <= 0)
									{
										dictionary.Remove(keyValuePair2.Key);
									}
								}
							}
						}
					}
				}
			}
		}
		if (dictionary.Count <= 0)
		{
			output = input;
			return false;
		}
		bool[] array5 = new bool[input.Length];
		foreach (KeyValuePair<string, List<int>> keyValuePair3 in dictionary)
		{
			for (int num2 = 0; num2 < keyValuePair3.Value.Count; num2++)
			{
				for (int num3 = 0; num3 < keyValuePair3.Key.Length; num3++)
				{
					array5[keyValuePair3.Value[num2] + num3] = true;
				}
			}
		}
		char[] array6 = new char[input.Length];
		bool flag = false;
		for (int num4 = 0; num4 < input.Length; num4++)
		{
			if (array5[num4])
			{
				array6[num4] = '*';
				flag = true;
			}
			else
			{
				array6[num4] = input[num4];
			}
		}
		output = new string(array6);
		return flag;
	}

	public static void ClearCache()
	{
		CensorShittyWords.cachedNotCensored.Clear();
		CensorShittyWords.cachedCensored.Clear();
	}

	public static string FilterUGC(string text, UGCType ugcType = UGCType.Other)
	{
		if (!PrivilegeManager.CanViewUserGeneratedContentAll)
		{
			CensorShittyWords.TryShowUGCNotification();
			switch (ugcType)
			{
			case UGCType.ServerName:
				return "[UGC server name]";
			case UGCType.CharacterName:
				return "[UGC character name]";
			case UGCType.WorldName:
				return "[UGC world name]";
			}
			return "[UGC text]";
		}
		string text2;
		bool flag;
		if (CensorShittyWords.Filter(text, out text2, out flag) && flag)
		{
			switch (ugcType)
			{
			case UGCType.ServerName:
				ZLog.LogWarning(string.Concat(new string[] { "Censored server name '", text, "' -> '", text2, "'" }));
				return text2;
			case UGCType.CharacterName:
				ZLog.LogWarning(string.Concat(new string[] { "Censored character name '", text, "' -> '", text2, "'" }));
				return text2;
			case UGCType.WorldName:
				ZLog.LogWarning(string.Concat(new string[] { "Censored world name '", text, "' -> '", text2, "'" }));
				return text2;
			}
			ZLog.LogWarning(string.Concat(new string[] { "Censored text '", text, "' -> '", text2, "'" }));
		}
		return text2;
	}

	private static void TryShowUGCNotification()
	{
		if (CensorShittyWords.ugcNotificationShown)
		{
			return;
		}
		if (UnifiedPopup.IsAvailable())
		{
			UnifiedPopup.Push(new WarningPopup("$menu_ugcwarningheader", "$menu_ugcwarningtext", delegate
			{
				UnifiedPopup.Pop();
			}, true));
			CensorShittyWords.ugcNotificationShown = true;
		}
	}

	private static string Normalize(string text)
	{
		return text.ToLowerInvariant();
	}

	private static string NormalizeStrict(string text)
	{
		text = text.ToLowerInvariant();
		char[] array = new char[text.Length];
		for (int i = 0; i < text.Length; i++)
		{
			char c;
			if (CensorShittyWords.equivalentLetterPairs.TryGetValue(text[i], out c))
			{
				array[i] = c;
			}
			else
			{
				array[i] = text[i];
			}
		}
		return new string(array);
	}

	[CompilerGenerated]
	internal static void <Filter>g__GenerateNormalizedLists|1_0()
	{
		CensorShittyWords.blacklistDefault = new List<string>();
		CensorShittyWords.blacklistDefault.AddRange(CensorShittyWords.m_censoredWords);
		CensorShittyWords.blacklistDefault.AddRange(CensorShittyWords.m_censoredWordsAdditional);
		CensorShittyWords.blacklistDefault.AddRange(CensorShittyWords.m_censoredWordsXbox);
		CensorShittyWords.whitelistDefault = new List<string>();
		CensorShittyWords.whitelistDefault.AddRange(CensorShittyWords.m_exemptWords);
		CensorShittyWords.whitelistDefault.AddRange(CensorShittyWords.m_exemptNames);
		CensorShittyWords.whitelistDefault.AddRange(CensorShittyWords.m_exemptPlaces);
		CensorShittyWords.blacklistDefaultNormalizedStrict = new List<string>();
		for (int i = 0; i < CensorShittyWords.blacklistDefault.Count; i++)
		{
			CensorShittyWords.blacklistDefaultNormalizedStrict.Add(CensorShittyWords.NormalizeStrict(CensorShittyWords.blacklistDefault[i]));
		}
		CensorShittyWords.whitelistDefaultNormalized = new List<string>();
		for (int j = 0; j < CensorShittyWords.whitelistDefault.Count; j++)
		{
			CensorShittyWords.whitelistDefaultNormalized.Add(CensorShittyWords.Normalize(CensorShittyWords.whitelistDefault[j]));
		}
		CensorShittyWords.whitelistDefaultNormalizedStrict = new List<string>();
		for (int k = 0; k < CensorShittyWords.whitelistDefault.Count; k++)
		{
			CensorShittyWords.whitelistDefaultNormalizedStrict.Add(CensorShittyWords.NormalizeStrict(CensorShittyWords.whitelistDefault[k]));
		}
		CensorShittyWords.normalizedListsGenerated = true;
	}

	[CompilerGenerated]
	internal static bool <Filter>g__FilterInternal|1_1(string input, out string output)
	{
		string text = CensorShittyWords.Normalize(input);
		string text2 = CensorShittyWords.NormalizeStrict(input);
		Dictionary<string, List<int>> dictionary = new Dictionary<string, List<int>>();
		for (int i = 0; i < CensorShittyWords.blacklistDefault.Count; i++)
		{
			int[] array = text2.AllIndicesOf(CensorShittyWords.blacklistDefaultNormalizedStrict[i]);
			if (array.Length != 0)
			{
				if (dictionary.ContainsKey(CensorShittyWords.blacklistDefault[i]))
				{
					for (int j = 0; j < array.Length; j++)
					{
						if (!dictionary[CensorShittyWords.blacklistDefault[i]].Contains(array[j]))
						{
							dictionary[CensorShittyWords.blacklistDefault[i]].Add(array[j]);
						}
					}
				}
				else
				{
					dictionary.Add(CensorShittyWords.blacklistDefault[i], new List<int>(array));
				}
			}
		}
		if (dictionary.Count <= 0)
		{
			output = input;
			return false;
		}
		for (int k = 0; k < CensorShittyWords.whitelistDefault.Count; k++)
		{
			int[] array2 = text.AllIndicesOf(CensorShittyWords.whitelistDefaultNormalized[k]);
			if (array2.Length != 0)
			{
				Dictionary<string, int[]> dictionary2 = new Dictionary<string, int[]>();
				foreach (KeyValuePair<string, List<int>> keyValuePair in dictionary)
				{
					int[] array3 = CensorShittyWords.whitelistDefaultNormalizedStrict[k].AllIndicesOf(CensorShittyWords.NormalizeStrict(keyValuePair.Key));
					if (array3.Length != 0)
					{
						dictionary2.Add(keyValuePair.Key, array3);
					}
				}
				for (int l = 0; l < array2.Length; l++)
				{
					foreach (KeyValuePair<string, int[]> keyValuePair2 in dictionary2)
					{
						for (int m = 0; m < keyValuePair2.Value.Length; m++)
						{
							int num = array2[l] + keyValuePair2.Value[m];
							if (dictionary[keyValuePair2.Key].Contains(num))
							{
								dictionary[keyValuePair2.Key].Remove(num);
								if (dictionary[keyValuePair2.Key].Count <= 0)
								{
									dictionary.Remove(keyValuePair2.Key);
								}
							}
						}
					}
				}
			}
		}
		if (dictionary.Count <= 0)
		{
			output = input;
			return false;
		}
		bool[] array4 = new bool[input.Length];
		foreach (KeyValuePair<string, List<int>> keyValuePair3 in dictionary)
		{
			for (int n = 0; n < keyValuePair3.Value.Count; n++)
			{
				for (int num2 = 0; num2 < keyValuePair3.Key.Length; num2++)
				{
					array4[keyValuePair3.Value[n] + num2] = true;
				}
			}
		}
		char[] array5 = new char[input.Length];
		bool flag = false;
		for (int num3 = 0; num3 < input.Length; num3++)
		{
			if (array4[num3] && input[num3] != ' ')
			{
				array5[num3] = '*';
				flag = true;
			}
			else
			{
				array5[num3] = input[num3];
			}
		}
		output = new string(array5);
		return flag;
	}

	private static Dictionary<string, string> cachedCensored = new Dictionary<string, string>();

	private static HashSet<string> cachedNotCensored = new HashSet<string>();

	private static bool normalizedListsGenerated = false;

	private static List<string> blacklistDefault;

	private static List<string> whitelistDefault;

	private static List<string> blacklistDefaultNormalizedStrict;

	private static List<string> whitelistDefaultNormalized;

	private static List<string> whitelistDefaultNormalizedStrict;

	private static bool ugcNotificationShown = false;

	private static Dictionary<char, char> equivalentLetterPairs = new Dictionary<char, char>
	{
		{ '0', 'o' },
		{ '1', 'i' },
		{ '2', 'z' },
		{ '3', 'e' },
		{ '4', 'a' },
		{ '5', 's' },
		{ '6', 'g' },
		{ '7', 't' },
		{ '8', 'b' },
		{ '9', 'g' },
		{ 'l', 'i' },
		{ 'z', 's' },
		{ 'å', 'a' },
		{ 'ä', 'a' },
		{ 'ö', 'a' },
		{ 'á', 'a' },
		{ 'à', 'a' },
		{ 'é', 'e' },
		{ 'è', 'e' },
		{ 'ë', 'e' },
		{ 'í', 'i' },
		{ 'ì', 'i' },
		{ 'ï', 'i' },
		{ 'ó', 'o' },
		{ 'ò', 'o' },
		{ 'ü', 'u' },
		{ 'ú', 'u' },
		{ 'ù', 'u' },
		{ 'ÿ', 'y' },
		{ 'ý', 'y' }
	};

	public static readonly List<string> m_censoredWords = new List<string>
	{
		"shit", "fuck", "tits", "piss", "nigger", "kike", "nazi", "cock", "cunt", "asshole",
		"fukk", "nigga", "nigr", "niggr", "penis", "vagina", "sex", "bitch", "slut", "whore",
		"arse", "balls", "bloody", "snatch", "twat", "pussy", "wank", "butthole", "erotic", "bdsm",
		"ass", "masturbate", "douche", "kuk", "fitta", "hora", "balle", "snopp", "knull", "erotik",
		"tattare", "runk", "onani", "onanera"
	};

	public static readonly List<string> m_censoredWordsXbox = new List<string>
	{
		"1488", "8=D", "A55hole", "abortion", "ahole", "AIDs", "ainujin", "ainuzin", "akimekura", "Anal",
		"anus", "anuses", "Anushead", "anuslick", "anuss", "aokan", "Arsch", "Arschloch", "arse", "arsed",
		"arsehole", "arseholed", "arseholes", "arseholing", "arselicker", "arses", "Ass", "asshat", "asshole", "Auschwitz",
		"b00bz", "b1tc", "Baise", "bakachon", "bakatyon", "Ballsack", "BAMF", "Bastard", "Beaner", "Beeatch",
		"beeeyotch", "beefwhistle", "beeotch", "Beetch", "beeyotch", "Bellend", "bestiality", "beyitch", "beyotch", "Biach",
		"bin laden", "binladen", "biotch", "bitch", "Bitching", "blad", "bladt", "blowjob", "blowme", "blyad",
		"blyadt", "bon3r", "boner", "boobs", "Btch", "Bukakke", "Bullshit", "bung", "butagorosi", "butthead",
		"Butthole", "Buttplug", "c0ck", "Cabron", "Cacca", "Cadela", "Cagada", "Cameljockey", "Caralho", "castrate",
		"Cazzo", "ceemen", "ch1nk", "chankoro", "chieokure", "chikusatsu", "Ching chong", "Chinga", "Chingada Madre", "Chingado",
		"Chingate", "chink", "chinpo", "Chlamydia", "choad", "chode", "chonga", "chonko", "chonkoro", "chourimbo",
		"chourinbo", "chourippo", "chuurembo", "chuurenbo", "circlejerk", "cl1t", "cli7", "clit", "clitoris", "cocain",
		"Cocaine", "cock", "Cocksucker", "Coglione", "Coglioni", "coitus", "coituss", "cojelon", "cojones", "condom",
		"coon", "coon hunt", "coon kill", "coonhunt", "coonkill", "Cooter", "cotton pic", "cotton pik", "cottonpic", "cottonpik",
		"Crackhead", "CSAM", "Culear", "Culero", "Culo", "Cum", "cun7", "cunt", "cvn7", "cvnt",
		"cyka", "d1kc", "d4go", "dago", "Darkie", "Deez Nuts", "deeznut", "deeznuts", "Dickhead", "dikc",
		"dildo", "Dio Bestia", "dong", "dongs", "douche", "Downie", "Downy", "Dumbass", "Durka durka", "Dyke",
		"Ejaculate", "Encule", "enjokousai", "enzyokousai", "etahinin", "etambo", "etanbo", "f0ck", "f0kc", "f3lch",
		"facking", "fag", "faggot", "Fanculo", "Fanny", "fatass", "fck", "Fckn", "fcuk", "fcuuk",
		"felch", "Fetish", "Fgt", "Fick", "FiCKDiCH", "Figlio di Puttana", "fku", "fock", "fokc", "foreskin",
		"Fotze", "Foutre", "fucc", "fuck", "fucker", "Fucking", "fuct", "fujinoyamai", "fukashokumin", "Fupa",
		"fuuck", "fuuuck", "fuuuuck", "fuuuuuck", "fuuuuuuck", "fuuuuuuuck", "fuuuuuuuuck", "fuuuuuuuuuck", "fuuuuuuuuuu", "fvck",
		"fxck", "fxuxcxk", "g000k", "g00k", "g0ok", "gestapo", "go0k", "god damn", "goldenshowers", "golliwogg",
		"gollywog", "Gooch", "gook", "goook", "Gyp", "h0m0", "h0mo", "h1tl3", "h1tle", "hairpie",
		"hakujakusha", "hakuroubyo", "hakuzyakusya", "hantoujin", "hantouzin", "Herpes", "hitl3r", "hitler", "hitlr", "holocaust",
		"hom0", "homo", "honky", "Hooker", "hor3", "hukasyokumin", "Hure", "Hurensohn", "huzinoyamai", "hymen",
		"inc3st", "incest", "Inculato", "Injun", "intercourse", "inugoroshi", "inugorosi", "j1g4b0", "j1g4bo", "j1gab0",
		"j1gabo", "Jack Off", "jackass", "jap", "JerkOff", "jig4b0", "jig4bo", "jigabo", "Jigaboo", "jiggaboo",
		"jizz", "Joder", "Joto", "Jungle Bunny", "junglebunny", "k k k", "k1k3", "kichigai", "kik3", "Kike",
		"kikeiji", "kikeizi", "Kilurself", "kitigai", "kkk", "klu klux", "Klu Klux Klan", "kluklux", "knobhead", "koon hunt",
		"koon kill", "koonhunt", "koonkill", "koroshiteyaru", "koumoujin", "koumouzin", "ku klux klan", "kun7", "kurombo", "Kurva",
		"Kurwa", "kxkxk", "l3sb0", "lezbo", "lezzie", "m07th3rfukr", "m0th3rfvk3r", "m0th3rfvker", "Madonna Puttana", "manberries",
		"manko", "manshaft", "Maricon", "Masterbat", "masterbate", "Masturbacion", "masturbait", "Masturbare", "Masturbate", "Masturbazione",
		"Merda", "Merde", "Meth", "Mierda", "milf", "Minge", "Miststück", "mitsukuchi", "mitukuti", "Molest",
		"molester", "molestor", "Mong", "Moon Cricket", "moth3rfucer", "moth3rfvk3r", "moth3rfvker", "motherfucker", "Mulatto", "n1663r",
		"n1664", "n166a", "n166er", "n1g3r", "n1German", "n1gg3r", "n1gGerman", "n3gro", "n4g3r", "n4gg3r",
		"n4gGerman", "n4z1", "nag3r", "nagg3r", "nagGerman", "natzi", "naz1", "nazi", "nazl", "neGerman",
		"ngGerman", "nggr", "NhigGerman", "ni666", "ni66a", "ni66er", "ni66g", "ni6g", "ni6g6", "ni6gg",
		"Nig", "nig66", "nig6g", "nigar", "niGerman", "nigg3", "nigg6", "nigga", "niggaz", "nigger",
		"nigGerman", "nigglet", "niggr", "nigguh", "niggur", "niggy", "niglet", "Nignog", "nimpinin", "ninpinin",
		"Nipples", "niqqa", "niqqer", "Nonce", "nugga", "Nutsack", "Nutted", "nygGerman", "omeko", "Orgy",
		"p3n15", "p3n1s", "p3ni5", "p3nis", "p3nl5", "p3nls", "Paki", "Panties", "Pedo", "pedoph",
		"pedophile", "pen15", "pen1s", "Pendejo", "peni5", "penile", "penis", "Penis", "penl5", "penls",
		"penus", "Perra", "phaggot", "phagot", "phuck", "Pikey", "Pinche", "Pizda", "Polla", "Porca Madonna",
		"Porch monkey", "Porn", "Porra", "pr1ck", "preteen", "prick", "pu555y", "pu55y", "pub1c", "Pube",
		"pubic", "pun4ni", "pun4nl", "Punal", "punan1", "punani", "punanl", "puss1", "puss3", "puss5",
		"pusse", "pussi", "Pussies", "pusss1", "pussse", "pusssi", "pusssl", "pusssy", "Pussy", "Puta",
		"Putain", "Pute", "Puto", "Puttana", "Puttane", "Puttaniere", "puzzy", "pvssy", "queef", "r3c7um",
		"r4p15t", "r4p1st", "r4p3", "r4pi5t", "r4pist", "raape", "raghead", "raibyo", "Raip", "rap15t",
		"rap1st", "Rapage", "rape", "Raped", "rapi5t", "Raping", "rapist", "rectum", "Red Tube", "Reggin",
		"reipu", "retard", "Ricchione", "rimjob", "rizzape", "rompari", "Salaud", "Salope", "sangokujin", "sangokuzin",
		"santorum", "Scheiße", "Schlampe", "Schlampe", "schlong", "Schwuchtel", "Scrote", "secks", "seishinhakujaku", "seishinijo",
		"seisinhakuzyaku", "seisinizyo", "Semen", "semushiotoko", "semusiotoko", "sh\tt", "sh17", "sh1t", "Shat", "Shemale",
		"shi7", "shinajin", "shinheimin", "shirakko", "shit", "Shitty", "shl7", "shlt", "shokubutsuningen", "sinazin",
		"sinheimin", "Skank", "slut", "SMD", "Sodom", "sofa king", "sofaking", "Spanishick", "Spanishook", "Spanishunk",
		"STD", "STDs", "Succhia Cazzi", "suck my", "suckmy", "syokubutuningen", "Taint", "Tampon", "Tapatte", "Tapette",
		"Tard", "Tarlouse", "tea bag", "teabag", "teebag", "teensex", "teino", "Testa di Cazzo", "Testicles", "Thot",
		"tieokure", "tinpo", "Tits", "tokushugakkyu", "tokusyugakkyu", "torukoburo", "torukojo", "torukozyo", "tosatsu", "tosatu",
		"towelhead", "Tranny", "tunbo", "tw47", "tw4t", "twat", "tyankoro", "tyonga", "tyonko", "tyonkoro",
		"tyourinbo", "tyourippo", "tyurenbo", "ushigoroshi", "usigorosi", "v461n4", "v461na", "v46in4", "v46ina", "v4g1n4",
		"v4g1na", "v4gin4", "v4gina", "va61n4", "va61na", "va6in4", "va6ina", "Vaccagare", "Vaffanculo", "Vag",
		"vag1n4", "vag1na", "vagin4", "vagina", "VateFaire", "vvhitepower", "w3tb4ck", "w3tback", "Wank", "wanker",
		"wetb4ck", "wetback", "wh0r3", "wh0re", "white power", "whitepower", "whor3", "whore", "Wog", "Wop",
		"x8lp3t", "xbl pet", "XBLPET", "XBLRewards", "Xl3LPET", "yabunirami", "Zipperhead", "Блядь", "сука", "アオカン",
		"あおかん", "イヌゴロシ", "いぬごろし", "インバイ", "いんばい", "オナニー", "おなにー", "オメコ", "カワラコジキ", "かわらこじき",
		"カワラモノ", "かわらもの", "キケイジ", "きけいじ", "キチガイ", "きちがい", "キンタマ", "きんたま", "クロンボ", "くろんぼ",
		"コロシテヤル", "ころしてやる", "シナジン", "しなじん", "タチンボ", "たちんぼ", "チョンコウ", "ちょんこう", "チョンコロ", "ちょんころ",
		"ちょん公", "チンポ", "ちんぽ", "ツンボ", "つんぼ", "とるこじょう", "とるこぶろ", "トルコ嬢", "トルコ風呂", "ニガー",
		"ニグロ", "にんぴにん", "はんとうじん", "マンコ", "まんこ", "レイプ", "れいぷ", "低能", "屠殺", "強姦",
		"援交", "支那人", "精薄", "精薄者", "輪姦"
	};

	public static readonly List<string> m_censoredWordsAdditional = new List<string>
	{
		"asscrack", "crackheads", "fucked", "fuckers", "dicks", "big dick", "bigdick", "small dick", "smalldick", "suck dick",
		"suckdick", "dick suck", "dicksuck", "dick sucked", "dicksucked", "dickus", "retards", "retarded", "swampass", "thots",
		"niggers"
	};

	public static readonly List<string> m_exemptWords = new List<string>
	{
		"amass", "ambassad", "ambassade", "ambassador", "ambassiate", "ambassy", "ampassy", "assail", "assassin", "assault",
		"assemble", "assemblage", "assembly", "assemblies", "assembling", "assert", "assess", "asset", "assign", "assimilate",
		"assimilating", "assimilation", "assimilatior", "assist", "associate", "associating", "association", "associator", "assort", "assume",
		"assumption", "assumable", "assumably", "assumptive", "assure", "assurance", "assurant", "baller", "bass", "blade",
		"brass", "bunga", "bypass", "canal", "canvassed", "carcass", "cassette", "circumference", "circumstance", "circumstancial",
		"chassi", "chassis", "class", "classic", "compass", "compassion", "crass", "crevass", "cum laude", "embarrass",
		"embassade", "embassador", "embassy", "encompass", "enigma", "extravaganza", "gassy", "gasses", "glass", "grass",
		"harass", "honig", "horsemen", "jazz", "jurassic", "kanal", "kass", "knight", "krass", "krasse",
		"kvass", "lass", "lasso", "mass", "massive", "morass", "night", "palazzo", "pass", "passenger",
		"passion", "passive", "password", "petits", "potassium", "quintenassien", "rassel", "rasselbande", "reiniger", "tassel",
		"tassen", "teldrassil", "transpenisular", "trespass", "sass", "sassy", "sassier", "sassiest", "sassily", "sauvage",
		"sauvages", "shenanigans", "skibladner", "strasse", "surpass", "wassap", "wasser"
	};

	public static readonly List<string> m_exemptNames = new List<string>
	{
		"assoz", "anastassia", "baltassar", "butts", "cass", "cassian", "cockburn", "cummings", "dickman", "hasse",
		"janus", "jorgy", "kanigma", "krasson", "lasse", "medick", "nigel", "prometheus", "sporn", "thora",
		"wankum", "weiner"
	};

	public static readonly List<string> m_exemptPlaces = new List<string> { "bumpass", "clitheroe", "dassel", "penistone", "toppenish", "twatt", "scunthorpe", "sussex", "vaggeryd" };
}
