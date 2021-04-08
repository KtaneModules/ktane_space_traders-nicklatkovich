using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public static class StarData {
	private static readonly Dictionary<string, string> _data = new Dictionary<string, string> {
		{ "Achernar", "---------X" },
		{ "Acrux", "-------X-X" },
		{ "Adhara", "--------M-" },
		{ "Aldebaran", "----X--XC-" },
		{ "Alhena", "-------X--" },
		{ "Alioth", "-------XX-" },
		{ "Alkaid", "X-T----X--" },
		{ "Alnair", "--C-------" },
		{ "Alnilam", "X-X-------" },
		{ "Alnitak", "--X------X" },
		{ "Alphard", "X-X----XFX" },
		{ "Alpheratz", "--X---X-X-" },
		{ "Alsephina", "----I-----" },
		{ "Altair", "X-S--X--C-" },
		{ "Antares", "--------X-" },
		{ "Arcturus", "----------" },
		{ "Atria", "-XT-----RX" },
		{ "Avior", "X-----XX-X" },
		{ "Bellatrix", "M-X-------" },
		{ "Betelgeuse", "--F-----XX" },
		{ "Canopus", "X--X-----X" },
		{ "Capella", "-XX-X-FX-X" },
		{ "Castor", "------X-XI" },
		{ "Deneb", "X-X---XX--" },
		{ "Diphda", "--X-X--XX-" },
		{ "Dubhe", "---X---X-X" },
		{ "Elnath", "X-------E-" },
		{ "Fomalhaut", "-X------XX" },
		{ "Gacrux", "--X---X---" },
		{ "Hadar", "-XX---X-XI" },
		{ "Hamal", "X-XI--M---" },
		{ "Menkalinan", "------I-XX" },
		{ "Menkent", "--X------X" },
		{ "Miaplacidus", "-------XXX" },
		{ "Mimosa", "-X--X----X" },
		{ "Mirfak", "XX-------X" },
		{ "Mirzam", "--IF---X-B" },
		{ "Nunki", "--X-------" },
		{ "Peacock", "X---------" },
		{ "Pollux", "--X-------" },
		{ "Procyon", "---------X" },
		{ "Regulus", "--X-----XF" },
		{ "Rigel", "-X--------" },
		{ "Rigil Kentaurus", "X-----XX-X" },
		{ "Sargas", "----------" },
		{ "Sirius", "------XX--" },
		{ "Spica", "----X-----" },
		{ "Toliman", "--------X-" },
		{ "Vega", "XX-------X" },
		{ "Wezen", "--T------I" },
	};

	public static readonly HashSet<char> potentialTax = new HashSet<char>(new char[] { 'R', 'C', 'E', 'F' });

	public static string[] starNames {
		get { return _data.Keys.ToArray(); }
	}

	public static readonly string[] raceNames = new string[] { "Humans", "Maloqs", "Pelengs", "Faeyans", "Gaals" };
	public static readonly Dictionary<string, int> raceId = new Dictionary<string, int>();

	public static readonly string[] regimeNames = new string[] {
		"Democracy", "Aristocracy", "Monarchy", "Dictatorship", "Anarchy"
	};

	public static readonly Dictionary<string, int> regimeId = new Dictionary<string, int>();

	static StarData() {
		for (int i = 0; i < raceNames.Length; i++) raceId[raceNames[i]] = i;
		for (int i = 0; i < regimeNames.Length; i++) regimeId[regimeNames[i]] = i + raceNames.Length;
	}

	public static bool HasTaxAt(MapGenerator.CellStar star, SpaceTradersModule module) {
		char raceValue = _data[star.name][raceId[star.race]];
		if (raceValue == 'X') return true;
		char regimeValue = _data[star.name][regimeId[star.regime]];
		if (regimeValue == 'X') return true;
		if (raceValue != '-' && HasTax(raceValue, module)) return true;
		return regimeValue != '-' && HasTax(regimeValue, module);
	}

	public static bool HasTax(char value, SpaceTradersModule module) {
		KMBombInfo bombInfo = module.BombInfo;
		switch (value) {
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
			default: throw new UnityException(string.Format("Unknown system code {0}", (int)value));
		}
	}

	public static bool CanHasTax(MapGenerator.CellStar star, SpaceTradersModule module) {
		if (HasTaxAt(star, module)) return true;
		return new char[] {
			_data[star.name][raceId[star.race]],
			_data[star.name][regimeId[star.regime]],
		}.Any((c) => potentialTax.Contains(c));
	}
}
