using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpaceTradersModule : MonoBehaviour {
	public const int GRID_WIDHT = 10;
	public const int GRID_HEIGHT = 10;
	public const float CELL_SIZE = .015f;
	public const float STAR_MIN_HEIGHT = .015f;
	public const float STAR_MAX_HEIGHT = .055f;

	private static int _moduleIdCounter = 1;

	public GameObject HypercorridorPrefab;
	public GameObject StarsContainer;
	public KMAudio Audio;
	public Material HypercorridorMaterial;
	public Material UsedHypercorridorMaterial;
	public TextMesh ShipPowerTextMesh;
	public TextMesh GoodsTextMesh;
	public KMBombInfo BombInfo;
	public KMBombModule BombModule;
	public StarObject StarPrefab;

	public Dictionary<string, StarObject> starByName = new Dictionary<string, StarObject>();

	private HashSet<string> _submittedStars = new HashSet<string>();
	public HashSet<string> submittedStars { get { return new HashSet<string>(_submittedStars); } }

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
			if (soldGoodsCount > 0 && soldGoodsCount >= goodsToBeSoldCount) BombModule.HandlePass();
		}
	}

	public int remainingMinutesCount { get { return Mathf.FloorToInt(BombInfo.GetTime() / 60f); } }

	private int _moduleId;
	public int moduleId { get { return _moduleId; } }

	private List<GameObject> _hypercorridors = new List<GameObject>();

	private void Start() {
		_moduleId = _moduleIdCounter++;
		GenerateStars();
		ResetModule();
		AddHandlers();
		BombModule.OnActivate += () => Activate();
	}

	private void Activate() {
		_startingMinutes = remainingMinutesCount;
		KMSelectable selfSelectable = GetComponent<KMSelectable>();
		selfSelectable.Children = starByName.Values.Select((star) => {
			KMSelectable starSelectable = star.GetComponent<KMSelectable>();
			starSelectable.Parent = selfSelectable;
			return starSelectable;
		}).ToArray();
		selfSelectable.UpdateChildren();
	}

	private void GenerateStars() {
		HashSet<MapGenerator.CellStar> cells = MapGenerator.Generate(this);
		foreach (MapGenerator.CellStar cell in cells) {
			Debug.LogFormat("[Space Traders #{0}] Star generated: {1}-{2}-{3}-{4}:{5} -> {6}", _moduleId, cell.name,
				cell.race, cell.regime, cell.position.x, cell.position.y,
				cell.adjacentStars.Select((c) => c.name).Join(","));
			StarObject star = Instantiate(StarPrefab);
			star.cell = cell;
			star.transform.parent = StarsContainer.transform;
			float x = CELL_SIZE * (cell.position.x - (GRID_WIDHT - 1) / 2f);
			x += Random.Range(-CELL_SIZE / 2f, CELL_SIZE / 2f);
			float y = Random.Range(STAR_MIN_HEIGHT, STAR_MAX_HEIGHT);
			float z = CELL_SIZE * (cell.position.y - (GRID_HEIGHT - 1) / 2f);
			z += Random.Range(-CELL_SIZE / 2f, CELL_SIZE / 2f);
			star.transform.localPosition = new Vector3(x, y, z);
			star.transform.localScale = new Vector3(0.005f, 0.005f, 0.005f);
			star.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
			starByName[cell.name] = star;
		}
		GenerateHypercorridors();
	}

	private void GenerateHypercorridors() {
		var hypercorridorsToIgnore = new HashSet<KeyValuePair<string, string>>();
		foreach (StarObject star in starByName.Values) {
			foreach (MapGenerator.CellStar adjacentCell in star.cell.adjacentStars) {
				var pairToCheck = new KeyValuePair<string, string>(adjacentCell.name, star.cell.name);
				if (hypercorridorsToIgnore.Contains(pairToCheck)) continue;
				hypercorridorsToIgnore.Add(new KeyValuePair<string, string>(star.cell.name, adjacentCell.name));
				GameObject hypercorridor = Instantiate(HypercorridorPrefab);
				_hypercorridors.Add(hypercorridor);
				hypercorridor.transform.parent = StarsContainer.transform;
				StarObject adjacentStar = starByName[adjacentCell.name];
				if (star.cell.path.Count > adjacentCell.path.Count) star.HypercorridorToSun = hypercorridor;
				else adjacentStar.HypercorridorToSun = hypercorridor;
				Vector3 from = adjacentStar.transform.localPosition;
				Vector3 to = star.transform.localPosition;
				hypercorridor.transform.localPosition = (to + from) / 2f;
				hypercorridor.transform.localRotation = Quaternion.LookRotation(from - to, Vector3.up);
				hypercorridor.transform.localScale = new Vector3(0.001f, 0.001f, (to - from).magnitude);
			}
		}
	}

	private void AddHandlers() {
		foreach (StarObject star in starByName.Values) {
			KMSelectable selectable = star.GetComponent<KMSelectable>();
			MapGenerator.CellStar cell = star.cell;
			SpaceTradersModule self = this;
			selectable.OnInteract = () => {
				if (
					cell.adjacentStars.Count() != 1
					|| cell.name == MapGenerator.SUN_NAME
					|| _submittedStars.Contains(cell.name)
					|| soldGoodsCount == goodsToBeSoldCount
				) {
					Audio.PlaySoundAtTransform("NotOutskirts", selectable.transform);
					return false;
				}
				Debug.LogFormat("[Space Traders #{0}] Pressed star: {1}", _moduleId, cell.name);
				Debug.LogFormat("[Space Traders #{0}] Current time: {1}", _moduleId, BombInfo.GetFormattedTime());
				Debug.LogFormat("[Space Traders #{0}] Path: {1}", _moduleId, cell.path.Select((c) => c.name).Join(","));
				IEnumerable<MapGenerator.CellStar> starsWithTax = cell.path.Where((s) => StarData.HasTaxAt(s, self));
				Debug.LogFormat("[Space Traders #{0}] Stars with tax: {1}", _moduleId,
					starsWithTax.Select((c) => c.name).Join(","));
				int tax = starsWithTax.Select((s) => s.tax).Sum();
				Debug.LogFormat("[Space Traders #{0}] Required tax: {1}", _moduleId, tax);
				if (tax > maxTax) {
					Debug.LogFormat("[Space Traders #{0}] Required tax greater than maximum allowed", _moduleId);
					BombModule.HandleStrike();
					Debug.LogFormat("[Space Traders #{0}] Reseting module", _moduleId);
					ResetModule(true);
				} else {
					Audio.PlaySoundAtTransform("StarSubmitted", selectable.transform);
					soldGoodsCount += 1;
					Debug.LogFormat("[Space Traders #{0}] Sold products count: {1}/{2}", _moduleId, soldGoodsCount,
						goodsToBeSoldCount);
					_submittedStars.Add(cell.name);
					foreach (StarObject pathStar in cell.path.Select((pathCell) => starByName[pathCell.name])) {
						pathStar.HypercorridorToSun.GetComponent<Renderer>().material = UsedHypercorridorMaterial;
					}
				}
				return false;
			};
		}
	}

	public void ResetModule(bool generateNewRegimes = false) {
		foreach (GameObject hypercorridor in _hypercorridors) {
			hypercorridor.GetComponent<Renderer>().material = HypercorridorMaterial;
		}
		soldGoodsCount = 0;
		_submittedStars = new HashSet<string>();
		int _maxTax;
		int _goodsToBeSoldCount;
		List<StarObject> stars = starByName.Values.ToList();
		HashSet<MapGenerator.CellStar> cells = new HashSet<MapGenerator.CellStar>(stars.Select((s) => s.cell));
		if (generateNewRegimes) {
			foreach (MapGenerator.CellStar cell in cells) {
				string newRegime = StarData.regimeNames.PickRandom();
				if (cell.regime == newRegime) continue;
				Debug.LogFormat("[Space Traders #{0}] Star {1}: regime changed to {2}", _moduleId, cell.name,
					newRegime);
				cell.regime = newRegime;
			}
		}
		MapGenerator.GenerateMaxTaxAndGoodsToSoldCount(cells, this, out _maxTax, out _goodsToBeSoldCount);
		Debug.LogFormat("[Space Traders #{0}] New max tax per vessel: {1}", _moduleId, _maxTax);
		Debug.LogFormat("[Space Traders #{0}] New products count to be sold: {1}", _moduleId, _goodsToBeSoldCount);
		Debug.LogFormat("[Space Traders #{0}] Possible solutions for even minute: {1}", _moduleId, stars.Where((s) => (
			s.cell.edge
		)).Select((s) => (
			new {
				tax = s.cell.path.Where((p) => StarData.HasTaxOnGenerationAt(p, this)).Select((p) => p.tax).Sum(),
				name = s.cell.name
			}
		)).Where((d) => d.tax <= _maxTax).Select((d) => d.name).Join(","));
		maxTax = _maxTax;
		goodsToBeSoldCount = _goodsToBeSoldCount;
		foreach (StarObject star in stars) star.cell = star.cell;
	}
}
