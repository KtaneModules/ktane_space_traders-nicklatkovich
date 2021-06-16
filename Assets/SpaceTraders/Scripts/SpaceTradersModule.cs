using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpaceTradersModule : MonoBehaviour {
	private const int GRID_WIDHT = 10;
	private const int GRID_HEIGHT = 10;
	private const float CELL_SIZE = .015f;
	private const float STAR_MIN_HEIGHT = .015f;
	private const float STAR_MAX_HEIGHT = .055f;

	private static int moduleIdCounter = 1;

	private static int[] GenerateSouvenirQuestion(int correctAnswer, int min, int max, int maxStep) {
		int minResult = correctAnswer;
		int maxResult = correctAnswer;
		int[] result = new int[5];
		int discentsCount = Random.Range(0, 5);
		for (int i = 0; i < 5; i++) {
			int diff = Random.Range(1, maxStep + 1);
			if (discentsCount > i && minResult - diff >= min) {
				minResult -= diff;
				result[i] = minResult;
			} else if (maxResult + diff <= max) {
				maxResult += diff;
				result[i] = maxResult;
			} else return result;
		}
		return result;
	}

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

	public readonly string TwitchHelpMessage = new string[] {
		"\"outskirts\" to view all outskirts",
		"\"trace Sirius\" to get information about all star systems on the path from the Sun to any other star",
		"\"send Vega\" to send a vessel to the outskirt star",
		"\"look Altair\" to view a specific star",
		"Star name with whitespace should be wroten in one word",
		"All commands and star's names are case insensitive",
		"\"trace\" and \"send\" commands can contain more than one star",
		"\"trace\", \"outskirts\" and \"look\" commands are cancelable",
	}.Join(" | ");

	public bool TwitchShouldCancelCommand = false;
	public Dictionary<string, StarObject> starByName = new Dictionary<string, StarObject>();

	private HashSet<string> _submittedStars = new HashSet<string>();
	public HashSet<string> submittedStars { get { return new HashSet<string>(_submittedStars); } }

	public int maxPossibleTaxAmount { get { return starByName.Values.Select(s => s.cell.path.Count).Max(); } }

	private int _startingMinutes;
	public int startingMinutes { get { return _startingMinutes; } }

	private bool _solved = false;
	public bool solved {
		get { return _solved; }
		private set {
			if (_solved == value) return;
			_solved = value;
			if (solved) ShipPowerTextMesh.text = GoodsTextMesh.text = "";
		}
	}

	private bool _forceSolved = true;
	public bool forceSolved { get { return _forceSolved; } }

	private int _maxTax;
	public int maxTax {
		get { return _maxTax; }
		private set {
			_maxTax = value;
			ShipPowerTextMesh.text = string.Format("{0} GCr", value.ToString());
		}
	}

	private int _productsCountToBeSold;
	public int productsCountToBeSold {
		get { return _productsCountToBeSold; }
		private set {
			_productsCountToBeSold = value;
			GoodsTextMesh.text = string.Format(@"{0}/{1}", soldProductsCount, productsCountToBeSold);
		}
	}

	private int _soldProductsCount;
	public int soldProductsCount {
		get { return _soldProductsCount; }
		private set {
			_soldProductsCount = value;
			GoodsTextMesh.text = string.Format(@"{0}/{1}", soldProductsCount, productsCountToBeSold);
			if (soldProductsCount > 0 && soldProductsCount >= productsCountToBeSold) OnSolved();
		}
	}

	public int remainingMinutesCount { get { return Mathf.FloorToInt(BombInfo.GetTime() / 60f); } }

	private int _moduleId;
	public int moduleId { get { return _moduleId; } }

	private List<GameObject> _hypercorridors = new List<GameObject>();

	private void Start() {
		_moduleId = moduleIdCounter++;
		GenerateStars();
		ResetModule();
		BombModule.OnActivate += () => Activate();
		AddHandlers();
	}

	private void Activate() {
		_startingMinutes = remainingMinutesCount;
		Debug.LogFormat("[Space Traders #{0}] Bomb starting time in minutes is {1}", _moduleId, startingMinutes);
		KMSelectable selfSelectable = GetComponent<KMSelectable>();
		selfSelectable.Children = starByName.Values.Select((star) => {
			KMSelectable starSelectable = star.GetComponent<KMSelectable>();
			starSelectable.Parent = selfSelectable;
			return starSelectable;
		}).ToArray();
		selfSelectable.UpdateChildren();
		GenerateMaxTaxAndGoodsToSoldCount();
	}

	public IEnumerator ProcessTwitchCommand(string command) {
		command = command.Trim().ToLower();
		if (command == "outskirts") {
			IEnumerable<StarObject> stars = starByName.Values.Where(s => s.cell.adjacentStars.Count == 1 && s.cell.name != MapGenerator.SUN_NAME);
			yield return null;
			if (stars.Count() > 3) yield return "waiting music";
			foreach (StarObject star in stars) {
				star.GetComponent<KMSelectable>().Highlight.transform.GetChild(0).gameObject.SetActive(true);
				star.Highlight();
				yield return new WaitForSeconds(5f);
				star.GetComponent<KMSelectable>().Highlight.transform.GetChild(0).gameObject.SetActive(false);
				star.RemoveHighlight();
				yield return null;
				if (TwitchShouldCancelCommand) {
					yield return "cancelled";
					yield break;
				}
			}
			yield break;
		}
		if (command.StartsWith("trace ")) {
			string starName = command.Skip(6).Join("").Trim();
			if (starName == MapGenerator.SUN_NAME) yield break;
			if (
				!StarData.HasLowerCasedStarName(starName)
				|| !starByName.ContainsKey(StarData.LowerCasedStarNameToActual(starName))
			) {
				yield return "sendtochat {0}, !{1} " + string.Format("Star \"{0}\" not found", starName);
				yield break;
			}
			StarObject target = starByName[StarData.LowerCasedStarNameToActual(starName)];
			yield return null;
			if (target.cell.path.Count > 3) yield return "waiting music";
			foreach (MapGenerator.CellStar cell in target.cell.path) {
				StarObject star = starByName[cell.name];
				star.GetComponent<KMSelectable>().Highlight.transform.GetChild(0).gameObject.SetActive(true);
				star.Highlight();
				yield return new WaitForSeconds(5f);
				star.GetComponent<KMSelectable>().Highlight.transform.GetChild(0).gameObject.SetActive(false);
				star.RemoveHighlight();
				yield return null;
				if (TwitchShouldCancelCommand) {
					yield return "cancelled";
					yield break;
				}
			}
			yield break;
		}
		if (command.StartsWith("send ")) {
			string[] starsName = command.Split(' ').Skip(1).Where((n) => n.Length > 0).ToArray();
			string[] unknownStars = starsName.Where((s) => (
				!StarData.HasLowerCasedStarName(s) || !starByName.ContainsKey(StarData.LowerCasedStarNameToActual(s))
			)).ToArray();
			if (unknownStars.Length > 0) {
				yield return "sendtochat {0}, !{1} " + string.Format(
					"Stars {0} not found",
					unknownStars.Select((s) => string.Format("\"{0}\"", s)).Join(", ")
				);
				yield break;
			}
			foreach (StarObject star in starsName.Select((s) => starByName[StarData.LowerCasedStarNameToActual(s)])) {
				bool success = OnStarPressed(star);
				if (!success) break;
			}
			yield return new KMSelectable[] { };
			yield break;
		}
		if (command.StartsWith("look ")) {
			string[] starsName = command.Split(' ').Skip(1).Where((n) => n.Length > 0).ToArray();
			string[] unknownStars = starsName.Where((s) => (
				!StarData.HasLowerCasedStarName(s) || !starByName.ContainsKey(StarData.LowerCasedStarNameToActual(s))
			)).ToArray();
			if (unknownStars.Length > 0) {
				yield return "sendtochat {0}, !{1} " + string.Format(
					"Stars {0} not found",
					unknownStars.Select((s) => string.Format("\"{0}\"", s)).Join(", ")
				);
				yield break;
			}
			yield return null;
			if (starsName.Count() > 3) yield return "waiting music";
			foreach (string starName in starsName) {
				StarObject star = starByName[StarData.LowerCasedStarNameToActual(starName)];
				star.GetComponent<KMSelectable>().Highlight.transform.GetChild(0).gameObject.SetActive(true);
				star.Highlight();
				yield return new WaitForSeconds(5f);
				star.GetComponent<KMSelectable>().Highlight.transform.GetChild(0).gameObject.SetActive(false);
				star.RemoveHighlight();
				yield return null;
				if (TwitchShouldCancelCommand) {
					yield return "cancelled";
					yield break;
				}
			}
			yield break;
		}
		yield return null;
	}

	public void TwitchHandleForcedSolve() {
		if (solved) return;
		Debug.LogFormat("[Space Traders #{0}] Module force-solved", _moduleId);
		foreach (StarObject star in starByName.Values.Where((s) => s.cell.adjacentStars.Count == 1)) {
			int requiredTax = star.cell.path.Where((c) => StarData.HasTaxAt(c, this)).Select((c) => c.tax).Sum();
			if (requiredTax <= maxTax) {
				foreach (StarObject pathStar in star.cell.path.Select((pathCell) => starByName[pathCell.name])) {
					pathStar.HypercorridorToSun.GetComponent<Renderer>().material = UsedHypercorridorMaterial;
				}
			}
		}
		soldProductsCount = productsCountToBeSold;
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
			star.GetComponent<KMSelectable>().OnInteract += () => {
				OnStarPressed(star);
				return false;
			};
		}
	}

	private bool OnStarPressed(StarObject star) {
		if (
			star.cell.adjacentStars.Count() != 1
			|| star.cell.name == MapGenerator.SUN_NAME
			|| _submittedStars.Contains(star.cell.name)
			|| solved
		) {
			Audio.PlaySoundAtTransform("NotOutskirts", star.transform);
			return true;
		}
		Debug.LogFormat("[Space Traders #{0}] Pressed star: {1}", _moduleId, star.cell.name);
		Debug.LogFormat("[Space Traders #{0}] Current time: {1}", _moduleId, BombInfo.GetFormattedTime());
		Debug.LogFormat("[Space Traders #{0}] Path: {1}", _moduleId, star.cell.path.Select((c) => c.name).Join(","));
		IEnumerable<MapGenerator.CellStar> starsWithTax = star.cell.path.Where((s) => StarData.HasTaxAt(s, this));
		Debug.LogFormat("[Space Traders #{0}] Stars with tax: {1}", _moduleId,
			starsWithTax.Select((c) => c.name).Join(","));
		int tax = starsWithTax.Select((s) => s.tax).Sum();
		Debug.LogFormat("[Space Traders #{0}] Required tax: {1}", _moduleId, tax);
		if (tax > maxTax) {
			Debug.LogFormat("[Space Traders #{0}] Required tax greater than maximum allowed", _moduleId);
			BombModule.HandleStrike();
			Debug.LogFormat("[Space Traders #{0}] Reseting module", _moduleId);
			ResetModule(true);
			return false;
		}
		Audio.PlaySoundAtTransform("StarSubmitted", star.transform);
		if (soldProductsCount + 1 == productsCountToBeSold) _forceSolved = false;
		soldProductsCount += 1;
		Debug.LogFormat("[Space Traders #{0}] Sold products count: {1}/{2}", _moduleId, soldProductsCount,
			productsCountToBeSold);
		_submittedStars.Add(star.cell.name);
		foreach (StarObject pathStar in star.cell.path.Select((pathCell) => starByName[pathCell.name])) {
			pathStar.HypercorridorToSun.GetComponent<Renderer>().material = UsedHypercorridorMaterial;
		}
		return true;
	}

	private void ResetModule(bool generateNewRegimes = false) {
		foreach (GameObject hypercorridor in _hypercorridors) {
			hypercorridor.GetComponent<Renderer>().material = HypercorridorMaterial;
		}
		_submittedStars = new HashSet<string>();
		List<StarObject> stars = starByName.Values.ToList();
		HashSet<MapGenerator.CellStar> cells = new HashSet<MapGenerator.CellStar>(stars.Select((s) => s.cell));
		if (generateNewRegimes) {
			soldProductsCount = 0;
			foreach (MapGenerator.CellStar cell in cells) {
				string newRegime = StarData.regimeNames.PickRandom();
				if (cell.regime == newRegime) continue;
				Debug.LogFormat("[Space Traders #{0}] Star {1}: regime changed to {2}", _moduleId, cell.name,
					newRegime);
				cell.regime = newRegime;
			}
			GenerateMaxTaxAndGoodsToSoldCount();
		}
		foreach (StarObject star in stars) star.cell = star.cell;
	}

	private void GenerateMaxTaxAndGoodsToSoldCount() {
		List<StarObject> stars = starByName.Values.ToList();
		HashSet<MapGenerator.CellStar> cells = new HashSet<MapGenerator.CellStar>(stars.Select((s) => s.cell));
		int _maxTax;
		int _goodsToBeSoldCount;
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
		productsCountToBeSold = _goodsToBeSoldCount;
	}

	private void OnSolved() {
		solved = true;
		foreach (StarObject star in starByName.Values) star.disabled = true;
		BombModule.HandlePass();
	}
}
