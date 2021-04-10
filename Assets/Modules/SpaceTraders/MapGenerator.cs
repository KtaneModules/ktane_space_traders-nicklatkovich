using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class MapGenerator {
	public readonly static Vector2Int[] ADJACENT_CELLS = new Vector2Int[] {
		new Vector2Int(1, 0),
		new Vector2Int(1, -1),
		new Vector2Int(0, -1),
		new Vector2Int(-1, -1),
		new Vector2Int(-1, 0),
		new Vector2Int(-1, 1),
		new Vector2Int(0, 1),
		new Vector2Int(1, 1),
	};

	public readonly static Vector2Int[] HYPERCORRIDOR_CELLS = new Vector2Int[] {
		new Vector2Int(2, 0),
		new Vector2Int(2, -1),
		new Vector2Int(1, -2),
		new Vector2Int(0, -2),
		new Vector2Int(-1, -2),
		new Vector2Int(-2, -1),
		new Vector2Int(-2, 0),
		new Vector2Int(-2, 1),
		new Vector2Int(-1, 2),
		new Vector2Int(0, 2),
		new Vector2Int(1, 2),
		new Vector2Int(2, 1),
	};

	public const int GRID_WIDHT = 10;
	public const int GRID_HEIGHT = 10;
	public const string SUN_NAME = "Sun";

	public static bool IsOnTheGrid(Vector2Int pos) {
		return pos.x >= 0 && pos.y >= 0 && pos.x < GRID_WIDHT && pos.y < GRID_HEIGHT;
	}

	private enum CellStatus { EMPTY, ADJACENT, STAR, CANDIDATE }

	public class CellStar {
		public bool edge = false;
		public int tax = 0;
		public string name;
		public string race;
		public string regime;
		public Vector2Int position;
		public List<CellStar> adjacentStars = new List<CellStar>();
		public List<CellStar> path = new List<CellStar>();
		public CellStar(Vector2Int position, string name) {
			this.position = position;
			this.name = name;
			this.race = StarData.raceNames.PickRandom();
			this.regime = StarData.regimeNames.PickRandom();
		}
	}

	private class Cell {
		public CellStatus status = CellStatus.EMPTY;
		public CellStar star = null;
	}

	public static HashSet<CellStar> Generate(SpaceTradersModule module) {
		HashSet<string> unusedStarNames = new HashSet<string>(StarData.starNames);
		HashSet<CellStar> result = new HashSet<CellStar>();
		Cell[][] grid = new Cell[GRID_WIDHT][];
		for (int x = 0; x < GRID_WIDHT; x++) grid[x] = new int[GRID_HEIGHT].Select((_) => new Cell()).ToArray();
		for (int x = 1; x <= 2; x++) {
			for (int y = 1; y <= 2; y++) grid[GRID_WIDHT - x][GRID_HEIGHT - y].status = CellStatus.ADJACENT;
		}
		for (int x = 0; x < 3; x++) for (int y = 0; y < 3; y++) grid[x][y].status = CellStatus.ADJACENT;
		Vector2Int startPosition = new Vector2Int(Random.Range(3, GRID_WIDHT - 3), Random.Range(3, GRID_HEIGHT - 3));
		grid[startPosition.x][startPosition.y].status = CellStatus.CANDIDATE;
		Vector<Vector2Int> queue = new Vector<Vector2Int>(startPosition);
		while (queue.length > 0) {
			Vector2Int pos = queue.RemoveRandom();
			Cell cell = grid[pos.x][pos.y];
			if (cell.status != CellStatus.CANDIDATE) continue;
			cell.status = CellStatus.STAR;
			foreach (Vector2Int adjacentCell in ADJACENT_CELLS.Select((diff) => pos + diff)) {
				if (!IsOnTheGrid(adjacentCell)) continue;
				grid[adjacentCell.x][adjacentCell.y].status = CellStatus.ADJACENT;
			}
			Vector<CellStar> adjacentStars = new Vector<CellStar>();
			foreach (Vector2Int hypercorridorCell in HYPERCORRIDOR_CELLS.Select((diff) => pos + diff)) {
				if (!IsOnTheGrid(hypercorridorCell)) continue;
				Cell hyperCell = grid[hypercorridorCell.x][hypercorridorCell.y];
				if (hyperCell.status == CellStatus.STAR) adjacentStars.Append(hyperCell.star);
				if (hyperCell.status != CellStatus.EMPTY) continue;
				hyperCell.status = CellStatus.CANDIDATE;
				queue.Append(hypercorridorCell);
			}
			cell.star = new CellStar(pos, unusedStarNames.PickRandom());
			result.Add(cell.star);
			unusedStarNames.Remove(cell.star.name);
			if (adjacentStars.length == 0) continue;
			CellStar adjacentStar = adjacentStars.RemoveRandom();
			cell.star.adjacentStars.Add(adjacentStar);
			adjacentStar.adjacentStars.Add(cell.star);
			cell.star.path = new List<CellStar>(adjacentStar.path);
			cell.star.path.Add(cell.star);
		}
		CellStar sun = grid[startPosition.x][startPosition.y].star;
		sun.name = SUN_NAME;
		sun.race = "Humans";
		sun.regime = "Democracy";
		MarkEdges(result);
		GenerateTaxes(result);
		return result;
	}

	public static void GenerateMaxTaxAndGoodsToSoldCount(
		HashSet<CellStar> starsSet, SpaceTradersModule module, out int maxTax, out int goodsToBeSoldCount
	) {
		CellStar[] stars = starsSet.Where((cell) => cell.edge).ToArray();
		int[] maxPathsTaxes = stars.Select((s) => (
			s.path.Where((p) => StarData.HasTaxOnGenerationAt(p, module)).Select((p) => p.tax).Sum())
		).ToArray();
		Debug.Log(maxPathsTaxes);
		System.Array.Sort(maxPathsTaxes);
		int i = maxPathsTaxes.Length / 2;
		int _maxTax = maxPathsTaxes[i];
		while (_maxTax == maxPathsTaxes[maxPathsTaxes.Length - 1] && i > 0) _maxTax = maxPathsTaxes[i--];
		maxTax = _maxTax;
		goodsToBeSoldCount = maxPathsTaxes.Where((tax) => tax <= _maxTax).Count();
		if (goodsToBeSoldCount == stars.Count()) goodsToBeSoldCount -= 1;
	}

	private static void MarkEdges(HashSet<CellStar> starsSet) {
		foreach (CellStar cell in starsSet) {
			if (cell.adjacentStars.Count() < 2 && cell.name != SUN_NAME) cell.edge = true;
		}
	}

	private static void GenerateTaxes(HashSet<CellStar> starsSet) {
		CellStar[] stars = starsSet.Where((cell) => cell.edge).ToArray();
		System.Array.Sort(stars, (a, b) => b.path.Count() - a.path.Count());
		int expectedTaxPerPath = stars[0].path.Count();
		foreach (CellStar star in stars) {
			int currentTax = star.path.Select((s) => s.tax).Sum();
			CellStar[] untaxedStars = star.path.Where((s) => s.tax == 0).ToArray();
			int untaxedStarsCount = untaxedStars.Length;
			int minTax = (expectedTaxPerPath - currentTax) / untaxedStarsCount;
			int increasedTax = (expectedTaxPerPath - currentTax) % untaxedStarsCount;
			foreach (CellStar untaxed in untaxedStars) untaxed.tax = minTax + (increasedTax-- > 0 ? 1 : 0);
		}
	}
}
