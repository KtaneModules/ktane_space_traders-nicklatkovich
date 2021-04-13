using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using KModkit;

public static class StarData {
	private static readonly Dictionary<string, string> _data = new Dictionary<string, string> {
		{ "Achernar", "---PX-X---" },
		{ "Acrux", "XX---XXB--" },
		{ "Adhara", "CXX-X-BP-S" },
		{ "Aldebaran", "FSS----MEP" },
		{ "Alhena", "XXX-BXX-TX" },
		{ "Alioth", "BXX--R----" },
		{ "Alkaid", "------XX-M" },
		{ "Alnair", "--C----S-X" },
		{ "Alnilam", "-X------XE" },
		{ "Alnitak", "XXXPXMCIXX" },
		{ "Alphard", "XXX-X---R-" },
		{ "Alpheratz", "-XX--IX--F" },
		{ "Alsephina", "RSX-X--X--" },
		{ "Altair", "X-----B---" },
		{ "Antares", "-TB-XXB-XX" },
		{ "Arcturus", "-X---P---X" },
		{ "Atria", "X-XXXIXXFE" },
		{ "Avior", "XB-X-XXE-X" },
		{ "Bellatrix", "-X--X----T" },
		{ "Betelgeuse", "RX---XX--T" },
		{ "Canopus", "X-XC-PFXTX" },
		{ "Capella", "--XSX--I--" },
		{ "Castor", "EXFXB-XIPC" },
		{ "Deneb", "T-XI-MF--P" },
		{ "Diphda", "-I-MX-PE-R" },
		{ "Dubhe", "XX-EXBXX-X" },
		{ "Elnath", "FX--XX-XXM" },
		{ "Fomalhaut", "X-S-XXM---" },
		{ "Gacrux", "FRCX-IXRXX" },
		{ "Hadar", "M--TC-E--X" },
		{ "Hamal", "IF-X-R-X--" },
		{ "Menkalinan", "--X---I-XC" },
		{ "Menkent", "FXR--XXT--" },
		{ "Miaplacidus", "XX----TTBX" },
		{ "Mimosa", "XCSXR-EC-X" },
		{ "Mirfak", "-XT-MB-X--" },
		{ "Mirzam", "PXE--FM-XS" },
		{ "Nunki", "----CXPX--" },
		{ "Peacock", "-----RPXR-" },
		{ "Pollux", "XT-----XX-" },
		{ "Procyon", "---XF----X" },
		{ "Regulus", "X-X--XXX-M" },
		{ "Rigel", "--RI-X-F--" },
		{ "Rigil Kentaurus", "-XX--M-I-X" },
		{ "Sargas", "X--ECX-PX-" },
		{ "Sirius", "XE-XB---XM" },
		{ "Spica", "IX-X---S--" },
		{ "Toliman", "--C-X---S-" },
		{ "Vega", "S---SXXX-X" },
		{ "Wezen", "-X-TXE----" },
	};

	public static string[] starNames {
		get { return _data.Keys.ToArray(); }
	}

	public static readonly string[] raceNames = new string[] { "Humans", "Maloqs", "Pelengs", "Faeyans", "Gaals" };
	public static readonly Dictionary<string, int> raceId = new Dictionary<string, int>();

	public static readonly string[] regimeNames = new string[] {
		"Democracy", "Aristocracy", "Monarchy", "Dictatorship", "Anarchy"
	};

	public static readonly Dictionary<string, int> regimeId = new Dictionary<string, int>();

	private static readonly Dictionary<string, string> _lowerToActual = new Dictionary<string, string>();

	static StarData() {
		for (int i = 0; i < raceNames.Length; i++) raceId[raceNames[i]] = i;
		for (int i = 0; i < regimeNames.Length; i++) regimeId[regimeNames[i]] = i + raceNames.Length;
		foreach (string starName in _data.Keys) _lowerToActual.Add(starName.ToLower().Split(' ').Join(""), starName);
	}

	public static bool HasLowerCasedStarName(string lowercased) {
		return _lowerToActual.ContainsKey(lowercased);
	}

	public static string LowerCasedStarNameToActual(string lowercased) {
		return _lowerToActual[lowercased];
	}

	public static char GetRaceType(MapGenerator.CellStar star) {
		return _data[star.name][raceId[star.race]];
	}

	public static char GetRegimeType(MapGenerator.CellStar star) {
		return _data[star.name][regimeId[star.regime]];
	}

	public static bool HasTaxAt(MapGenerator.CellStar star, SpaceTradersModule module) {
		return HasTax(GetRaceType(star), module) || HasTax(GetRegimeType(star), module);
	}

	public static bool HasTax(char type, SpaceTradersModule module) {
		KMBombInfo bombInfo = module.BombInfo;
		switch (type) {
			case '-': return false;
			case 'X': return true;
			case 'B': return bombInfo.GetBatteryCount() < 4;
			case 'P': return !bombInfo.IsPortPresent(Port.Serial) && !bombInfo.IsPortPresent(Port.Parallel);
			case 'I': return bombInfo.GetIndicators().Count() % 2 == 0;
			case 'S': return bombInfo.GetSerialNumberNumbers().Count() == 2;
			case 'T': return module.startingMinutes % 2 == 1;
			case 'M': return bombInfo.GetModuleIDs().Count() % 2 == 1;
			case 'R': return module.remainingMinutesCount % 2 == 1;
			case 'C': return bombInfo.GetSolvedModuleIDs().Count() % 2 == 0;
			case 'E': return bombInfo.GetStrikes() == 0;
			case 'F': return bombInfo.GetTwoFactorCodes().Any((code) => code % 2 == 0);
			default: throw new UnityException(string.Format("Unknown system code {0}", (int)type));
		}
	}

	public static bool HasTaxOnGenerationAt(MapGenerator.CellStar star, SpaceTradersModule module) {
		return HasTaxOnGeneration(star, GetRaceType(star), module)
			|| HasTaxOnGeneration(star, GetRegimeType(star), module);
	}

	public static bool HasTaxOnGeneration(MapGenerator.CellStar star, char type, SpaceTradersModule module) {
		if (type == 'R') return false;
		if (type == 'F') return module.BombInfo.IsTwoFactorPresent();
		if (type == 'C' || type == 'E') return true;
		return HasTaxAt(star, module);
	}
}
