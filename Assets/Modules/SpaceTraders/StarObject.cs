﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class StarObject : MonoBehaviour {
	public const float INFO_PANEL_OFFSET = 7f;
	public const float INFO_PANEL_WIDTH = .05f;

	public static readonly Vector3 INFO_PANEL_POSITION = new Vector3(0f, 9f, 0f);

	public static readonly Color[] starColors = new Color[] {
		new Color32(0x9b, 0xb2, 0xff, 0xff),
		new Color32(0xd3, 0xdd, 0xff, 0xff),
		new Color32(0xfe, 0xf9, 0xff, 0xff),
		new Color32(0xff, 0xeb, 0xd6, 0xff),
		new Color32(0xff, 0xdf, 0xb8, 0xff),
		new Color32(0xff, 0xd0, 0x96, 0xff),
		new Color32(0xff, 0x52, 0x00, 0xff),
	};

	public GameObject InfoPanel;
	public GameObject HypercorridorToSun;
	public Renderer SphereRenderer;
	public TextMesh StarNameMesh;
	public TextMesh RaceNameMesh;
	public TextMesh RegimeNameMesh;
	public TextMesh TaxMesh;

	private bool _sold = false;
	public bool sold { get { return _sold; } }

	private MapGenerator.CellStar _cell;
	public MapGenerator.CellStar cell {
		get { return _cell; }
		set {
			_cell = value;
			StarNameMesh.text = cell.name;
			RaceNameMesh.text = string.Format("Race: {0}", cell.race);
			RegimeNameMesh.text = string.Format("Regime: {0}", cell.regime);
			TaxMesh.text = string.Format("Tax: {0} GCr", cell.tax.ToString());
		}
	}

	public Color GetRandomStarColor() {
		int index = Random.Range(0, starColors.Length - 1);
		float lerp = Random.Range(0f, 1f);
		Color from = starColors[index];
		Color to = starColors[index + 1];
		float[] colorComponents = new float[3].Select((_, i) => {
			float min = Mathf.Min(from[i], to[i]);
			float max = Mathf.Max(from[i], to[i]);
			return min + lerp * (max - min);
		}).ToArray();
		return new Color(colorComponents[0], colorComponents[1], colorComponents[2]);
	}

	private void Start() {
		Color starColor = GetRandomStarColor();
		SphereRenderer.material.SetColor("_Color", starColor);
		Color haloColor = GetRandomStarColor();
		SerializedObject halo = new SerializedObject(SphereRenderer.GetComponent("Halo"));
		halo.FindProperty("m_Color").colorValue = haloColor;
		halo.ApplyModifiedProperties();
		KMSelectable selfSelectable = GetComponent<KMSelectable>();
		InfoPanel.transform.localScale = Vector3.zero;
		selfSelectable.OnHighlight += () => {
			if (_sold) return;
			InfoPanel.transform.localScale = Vector3.one;
			Vector3 offset = new Vector3(0f, 0f, -Mathf.Sign(transform.localPosition.z) * INFO_PANEL_OFFSET);
			offset.x -= Mathf.Min(0f, transform.localPosition.x - INFO_PANEL_WIDTH + .08f) * 200f;
			offset.x -= Mathf.Max(0f, transform.localPosition.x + INFO_PANEL_WIDTH - .08f) * 200f;
			InfoPanel.transform.localPosition = transform.localPosition + offset + INFO_PANEL_POSITION;
		};
		selfSelectable.OnHighlightEnded += () => InfoPanel.transform.localScale = Vector3.zero;
	}

	public void Sell() {
		_sold = true;
		InfoPanel.transform.localScale = Vector3.zero;
	}
}
