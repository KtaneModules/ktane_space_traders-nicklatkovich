using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpaceTradersModule : MonoBehaviour {
	public const int GRID_WIDHT = 10;
	public const int GRID_HEIGHT = 10;
	public const float CELL_SIZE = .015f;
	public const float STAR_MIN_HEIGHT = .03f;
	public const float STAR_MAX_HEIGHT = .07f;

	private static int _moduleIdCounter = 1;

	public GameObject HypercorridorPrefab;
	public GameObject StarsContainer;
	public TextMesh ShipPowerTextMesh;
	public TextMesh GoodsTextMesh;
	public KMBombInfo BombInfo;
	public KMBombModule BombModule;
	public StarObject StarPrefab;

	public Dictionary<string, StarObject> starByName = new Dictionary<string, StarObject>();

	private int _startingMinutes;
	public int startingMinutes { get { return _startingMinutes; } }

	private int _maxTax;
	public int maxTax {
		get { return _maxTax; }
		private set {
			_maxTax = value;
			ShipPowerTextMesh.text = string.Format("{0} GCr", value.ToString());
		}
	}

	private int _goodsToBeSoldCount;
	public int goodsToBeSoldCount {
		get { return _goodsToBeSoldCount; }
		private set {
			_goodsToBeSoldCount = value;
			GoodsTextMesh.text = string.Format(@"{0}/{1}", soldGoodsCount, goodsToBeSoldCount);
		}
	}

	private int _soldGoodsCount;
	public int soldGoodsCount {
		get { return _soldGoodsCount; }
		private set {
			_soldGoodsCount = value;
			GoodsTextMesh.text = string.Format(@"{0}/{1}", soldGoodsCount, goodsToBeSoldCount);
			if (soldGoodsCount >= goodsToBeSoldCount) BombModule.HandlePass();
		}
	}

	public int remainingMinutesCount { get { return Mathf.FloorToInt(BombInfo.GetTime() / 60f); } }

	private int _moduleId;
	public int moduleId { get { return _moduleId; } }

	private void Start() {
		_moduleId = _moduleIdCounter++;
		GenerateStars();
		KMSelectable selfSelectable = GetComponent<KMSelectable>();
		selfSelectable.Children = starByName.Values.Select((star) => {
			KMSelectable starSelectable = star.GetComponent<KMSelectable>();
			starSelectable.Parent = selfSelectable;
			return starSelectable;
		}).ToArray();
		selfSelectable.UpdateChildren();
		BombModule.OnActivate += () => Activate();
	}

	private void Activate() {
		_startingMinutes = remainingMinutesCount;
	}
	private void GenerateStars() {
		int maxTax;
		int goodsToBeSoldCount;
		HashSet<MapGenerator.CellStar> cells = MapGenerator.Generate(this, out maxTax, out goodsToBeSoldCount);
		this.maxTax = maxTax;
		this.goodsToBeSoldCount = goodsToBeSoldCount;
		foreach (MapGenerator.CellStar cell in cells) {
			StarObject star = Instantiate(StarPrefab);
			star.cell = cell;
			star.transform.parent = StarsContainer.transform;
			float x = CELL_SIZE * (cell.position.x - (GRID_WIDHT - 1) / 2f);
			x += Random.Range(-CELL_SIZE / 2f, CELL_SIZE / 2f);
			float y = Random.Range(STAR_MIN_HEIGHT, STAR_MAX_HEIGHT);
			float z = CELL_SIZE * (cell.position.y - (GRID_HEIGHT - 1) / 2f);
			z += Random.Range(-CELL_SIZE / 2f, CELL_SIZE / 2f);
			star.transform.localPosition = new Vector3(x, y, z);
			starByName[cell.name] = star;
		}
		GenerateHypercorridors();
		AddHandlers();
	}

	private void GenerateHypercorridors() {
		var hypercorridorsToIgnore = new HashSet<KeyValuePair<string, string>>();
		foreach (StarObject star in starByName.Values) {
			foreach (MapGenerator.CellStar adjacentCell in star.cell.adjacentStars) {
				var pairToCheck = new KeyValuePair<string, string>(adjacentCell.name, star.cell.name);
				if (hypercorridorsToIgnore.Contains(pairToCheck)) continue;
				hypercorridorsToIgnore.Add(new KeyValuePair<string, string>(star.cell.name, adjacentCell.name));
				GameObject hypercorridor = Instantiate(HypercorridorPrefab);
				hypercorridor.transform.parent = StarsContainer.transform;
				StarObject adjacentStar = starByName[adjacentCell.name];
				Vector3 from = adjacentStar.transform.localPosition;
				Vector3 to = star.transform.localPosition;
				hypercorridor.transform.localPosition = (to + from) / 2f;
				hypercorridor.transform.localRotation = Quaternion.LookRotation(from - to, Vector3.up);
				Vector3 hyperScale = hypercorridor.transform.localScale;
				hyperScale.z = (to - from).magnitude;
				hypercorridor.transform.localScale = hyperScale;
			}
		}
	}

	private void AddHandlers() {
		foreach (StarObject star in starByName.Values) {
			KMSelectable selectable = star.GetComponent<KMSelectable>();
			MapGenerator.CellStar cell = star.cell;
			SpaceTradersModule self = this;
			selectable.OnInteract = () => {
				int tax = cell.path.Where((s) => StarData.HasTaxAt(s, self)).Select((s) => s.tax).Sum();
				if (tax > maxTax) BombModule.HandleStrike();
				else soldGoodsCount += 1;
				return false;
			};
		}
	}
}
