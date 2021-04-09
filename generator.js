function shuffled(arr) {
  const result = [...arr];
  for (let i = 0; i < result.length; i++) {
    const swapIndex = Math.floor(Math.random() * result.length);
    [result[i], result[swapIndex]] = [result[swapIndex], result[i]];
  }
  return result;
}

const starNames = [
  "Achernar", "Acrux", "Adhara", "Aldebaran", "Alhena",
  "Alioth", "Alkaid", "Alnair", "Alnilam", "Alnitak",
  "Alphard", "Alpheratz", "Alsephina", "Altair", "Antares",
  "Arcturus", "Atria", "Avior", "Bellatrix", "Betelgeuse",
  "Canopus", "Capella", "Castor", "Deneb", "Diphda",
  "Dubhe", "Elnath", "Fomalhaut", "Gacrux", "Hadar",
  "Hamal", "Menkalinan", "Menkent", "Miaplacidus", "Mimosa",
  "Mirfak", "Mirzam", "Nunki", "Peacock", "Pollux",
  "Procyon", "Regulus", "Rigel", "Rigil Kentaurus", "Sargas",
  "Sirius", "Spica", "Toliman", "Vega", "Wezen",
];

const maxStarNameLength = starNames.reduce((acc, name) => Math.max(acc, name.length), 0);

const starIndex = new Map(starNames.map((starName, i) => [starName, i]));

const properties = [
  "Humans", "Maloqs", "Pelengs", "Faeyans", "Gaals",
  "Democracy", "Aristocracy", "Monarchy", "Dictatorship", "Anarchy",
];

const propertyIndex = new Map(properties.map((property, i) => [property, i]));

const sortedProperties = [
  "Democracy", "Faeyans", "Gaals", "Maloqs", "Aristocracy",
  "Humans", "Monarchy", "Dictatorship", "Pelengs", "Anarchy",
];

const dependencies = "BPISTMRCEF";

const grid = new Array(starNames.length).fill(null).map(() => new Array(properties.length).fill("-"));
const partSize = (starNames.length + 1) / properties.length;

const dynamics = new Set();

for (let i = 0; i < properties.length; i++) {
  const shuffledStars = shuffled(starNames);
  const taxedStarsCount = Math.floor(partSize * (Math.random() + i));
  for (let j = 0; j < taxedStarsCount; j++) {
    const value = "X";
	const x = starIndex.get(shuffledStars[j]);
	const y = propertyIndex.get(sortedProperties[i]);
	if (Math.random() < .5) dynamics.add({ x, y });
    grid[x][y] = value;
  }
}

const shuffledDependencies = shuffled(dependencies.split("").join(""));

let depIndex = 0;
for (const { x, y } of shuffled(dynamics)) {
	grid[x][y] = shuffledDependencies[depIndex];
	depIndex = (depIndex + 1) % shuffledDependencies.length;
}

for (let i = 0; i < starNames.length; i++) {
  console.log(starNames[i].padStart(maxStarNameLength + 1, " "), grid[i].join(""));
}
