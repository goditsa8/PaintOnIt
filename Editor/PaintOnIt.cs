using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.UI;
using System.Collections;

public class PaintOnIt : EditorWindow {
	private Tool lastTool = Tool.None;
	private float mouseDistance = 0f;
	private Vector3 prevPoint = Vector3.zero;	// point to measure mouseDistance

	// GUI vars
	private Vector2 scrollPos;

	// raycast
	private RaycastHit hit;
	private Ray ray;
	private const float RAY_DISTANCE = 100000f;

	// physics elements for raycast 
	private Collider tmpCollider;
	private Rigidbody rigidbody;
	private bool rbodyIsKinematic;

	// painting
	private GameObject canvas;
	private GameObject painting;
	private GameObject brush;

	// brush
	private enum BrushMode {
		Pen,
		Brush
	};
	private BrushMode brushMode;
	private float brushRadius;
	private int brushDensity;

	// stroke
	private enum StrokeMode {
		OnClick,
		OnHold
	};
	private StrokeMode strokeMode;
	private int strokeSpacing;
	private float strokeSpacingUnits;

	// scale
	private enum ScaleMode {
		FixedUniform,
		FixedNonUniform,
		RandomUniform,
		RandomNonUniform
	};
	private ScaleMode scaleMode;
	private float scaleUniformFactor;
	private Vector3 scaleNonUniformFactor;
	private float minScale;
	private float maxScale;
	private float minXScale;
	private float maxXScale;
	private float minYScale;
	private float maxYScale;
	private float minZScale;
	private float maxZScale;

	// rotation
	private enum RotationMode {
		Fixed,
		Random
	};
	private RotationMode rotationMode;
	private enum RotationAlign {
		NoAlign,
		CanvasNormal
	};
	private RotationAlign rotationAlign = RotationAlign.NoAlign;
	private Vector3 eulerRot;
	private float minRotX;
	private float maxRotX;
	private float minRotY;
	private float maxRotY;
	private float minRotZ;
	private float maxRotZ;

	// position
	private enum PositionMode {
		Fixed,
		Random
	};
	private PositionMode positionMode = PositionMode.Fixed;
	private Vector3 offsetFixed;
	private float minXOffset;
	private float maxXOffset;
	private float minYOffset;
	private float maxYOffset;
	private float minZOffset;
	private float maxZOffset;

	void OnEnable() {
		if (!Selection.activeGameObject)	// no selection?
			return;
		else {
			canvas = Selection.activeGameObject;

			// create painting gameObject
			painting = new GameObject();
			painting.name = "Painting";
			painting.transform.parent = canvas.transform;

			// turn off rigidbody to allow canvas raycasting
			rigidbody = canvas.GetComponent<Rigidbody>();
			if (rigidbody) {
				rbodyIsKinematic = rigidbody.isKinematic;
				rigidbody.isKinematic = true;
			}

			if (canvas.GetComponent<Terrain>())
				tmpCollider = canvas.GetComponent<TerrainCollider>();
			else {
				// add temporary MeshCollider to allow canvas raycasting
				tmpCollider = canvas.AddComponent<MeshCollider>();
				tmpCollider.hideFlags = HideFlags.HideInInspector;
			}

			// give access to OnSceneGUI()
			SceneView.onSceneGUIDelegate += OnSceneGUI;

			// switch viewport tool to none
			lastTool = Tools.current;
			Tools.current = Tool.None;

			// restore user settigns
			brushMode = (BrushMode)EditorPrefs.GetInt("BrushMode", (int)BrushMode.Brush);
			brushRadius = EditorPrefs.GetFloat("BrushRadius", 1f);
			brushDensity = EditorPrefs.GetInt("BrushDensity", 10);

			strokeMode = (StrokeMode)EditorPrefs.GetInt("StrokeMode", (int)StrokeMode.OnHold);
			strokeSpacing = EditorPrefs.GetInt("StrokeSpacing", 30);
			strokeSpacingUnits = EditorPrefs.GetFloat("StrokeSpacingUnits", 1f);

			scaleMode = (ScaleMode)EditorPrefs.GetInt("ScaleMode", (int)ScaleMode.FixedUniform);
			scaleUniformFactor = EditorPrefs.GetFloat("ScaleUniformFactor", 1f);
			scaleNonUniformFactor.x = EditorPrefs.GetFloat("ScaleNUFX", 1f);
			scaleNonUniformFactor.y = EditorPrefs.GetFloat("ScaleNUFY", 1f);
			scaleNonUniformFactor.z = EditorPrefs.GetFloat("ScaleNUFZ", 1f);
			minScale = EditorPrefs.GetFloat("MinScale", 0.5f);
			maxScale = EditorPrefs.GetFloat("MaxScale", 2f);
			minXScale = EditorPrefs.GetFloat("MinXScale", 0.5f);
			maxXScale = EditorPrefs.GetFloat("MaxXScale", 2f);
			minYScale = EditorPrefs.GetFloat("MinYScale", 0.5f);
			maxYScale = EditorPrefs.GetFloat("MaxYScale", 2f);
			minZScale = EditorPrefs.GetFloat("MinZScale", 0.5f);
			maxZScale = EditorPrefs.GetFloat("MaxZScale", 2f);

			rotationMode = (RotationMode)EditorPrefs.GetInt("RotationMode", (int)RotationMode.Fixed);
			rotationAlign = (RotationAlign)EditorPrefs.GetInt("RotationAlign", (int)RotationAlign.CanvasNormal);
			eulerRot.x = EditorPrefs.GetFloat("EulerRotX", 0f);
			eulerRot.y = EditorPrefs.GetFloat("EulerRotY", 0f);
			eulerRot.z = EditorPrefs.GetFloat("EulerRotZ", 0f);
			minRotX = EditorPrefs.GetFloat("MinRotX", 0f);
			maxRotX = EditorPrefs.GetFloat("MaxRotX", 360f);
			minRotY = EditorPrefs.GetFloat("MinRotY", 0f);
			maxRotY = EditorPrefs.GetFloat("MaxRotY", 360f);
			minRotZ = EditorPrefs.GetFloat("MinRotZ", 0f);
			maxRotZ = EditorPrefs.GetFloat("MaxRotZ", 360f);

			positionMode = (PositionMode)EditorPrefs.GetInt("PositionMode", (int)PositionMode.Fixed);
			offsetFixed.x = EditorPrefs.GetFloat("OffsetFixedX", 0f);
			offsetFixed.y = EditorPrefs.GetFloat("OffsetFixedY", 0f);
			offsetFixed.z = EditorPrefs.GetFloat("OffsetFixedZ", 0f);
			minXOffset = EditorPrefs.GetFloat("MinXOffset", 0f);
			maxXOffset = EditorPrefs.GetFloat("MaxXOffset", 0f);
			minYOffset = EditorPrefs.GetFloat("MinYOffset", 0f);
			maxYOffset = EditorPrefs.GetFloat("MaxYOffset", 0f);
			minZOffset = EditorPrefs.GetFloat("MinZOffset", 0f);
			maxZOffset = EditorPrefs.GetFloat("MaxZOffset", 0f);
		}
	}

	void OnDisable() {
		// deny access to OnSceneGUI()
		SceneView.onSceneGUIDelegate -= OnSceneGUI;

		// restore last used tool
		Tools.current = lastTool;

		if (canvas) {
			if (!canvas.GetComponent<Terrain>())
				DestroyImmediate(tmpCollider);	// remove temporary collider

			if (rigidbody)
				rigidbody.isKinematic = rbodyIsKinematic;	// restore rigidnody settings

			if (painting.transform.childCount == 0)	// there was no drawing?
				DestroyImmediate(painting);	// remove painting object
			
			// save user settings
			EditorPrefs.SetInt("BrushMode", (int)brushMode);
			EditorPrefs.SetFloat("BrushRadius", brushRadius);
			EditorPrefs.SetInt("BrushDensity", brushDensity);
			
			EditorPrefs.SetInt("StrokeMode", (int)strokeMode);
			EditorPrefs.SetInt("StrokeSpacing", strokeSpacing);
			EditorPrefs.SetFloat("StrokeSpacingUnits", strokeSpacingUnits);
			
			EditorPrefs.SetInt("ScaleMode", (int)scaleMode);
			EditorPrefs.SetFloat("ScaleUniformFactor", scaleUniformFactor);
			EditorPrefs.SetFloat("ScaleNUFX", scaleNonUniformFactor.x);
			EditorPrefs.SetFloat("ScaleNUFY", scaleNonUniformFactor.y);
			EditorPrefs.SetFloat("ScaleNUFZ", scaleNonUniformFactor.z);
			EditorPrefs.SetFloat("MinScale", minScale);
			EditorPrefs.SetFloat("MaxScale", maxScale);
			EditorPrefs.SetFloat("MinXScale", minXScale);
			EditorPrefs.SetFloat("MaxXScale", maxXScale);
			EditorPrefs.SetFloat("MinYScale", minYScale);
			EditorPrefs.SetFloat("MaxYScale", maxYScale);
			EditorPrefs.SetFloat("MinZScale", minZScale);
			EditorPrefs.SetFloat("MaxZScale", maxZScale);
			
			EditorPrefs.SetInt("RotationMode", (int)rotationMode);
			EditorPrefs.SetInt("RotationAlign", (int)rotationAlign);
			EditorPrefs.SetFloat("EulerRotX", eulerRot.x);
			EditorPrefs.SetFloat("EulerRotY", eulerRot.y);
			EditorPrefs.SetFloat("EulerRotZ", eulerRot.z);
			EditorPrefs.SetFloat("MinRotX", minRotX);
			EditorPrefs.SetFloat("MaxRotX", maxRotX);
			EditorPrefs.SetFloat("MinRotY", minRotY);
			EditorPrefs.SetFloat("MaxRotY", maxRotY);
			EditorPrefs.SetFloat("MinRotZ", minRotZ);
			EditorPrefs.SetFloat("MaxRotZ", maxRotZ);

			EditorPrefs.SetInt("PositionMode", (int)positionMode);
			EditorPrefs.SetFloat("OffsetFixedX", offsetFixed.x);
			EditorPrefs.SetFloat("OffsetFixedY", offsetFixed.y);
			EditorPrefs.SetFloat("OffsetFixedZ", offsetFixed.z);
			EditorPrefs.SetFloat("MinXOffset", minXOffset);
			EditorPrefs.SetFloat("MaxXOffset", maxXOffset);
			EditorPrefs.SetFloat("MinYOffset", minYOffset);
			EditorPrefs.SetFloat("MaxYOffset", maxYOffset);
			EditorPrefs.SetFloat("MinZOffset", minZOffset);
			EditorPrefs.SetFloat("MaxZOffset", maxZOffset);
		}
	}

	[MenuItem("GoToSolutions/Paint On It...")]
	public static void ShowWindow() {
		EditorWindow.GetWindow(typeof(PaintOnIt), true, "PaintOnIt!");
	}

	void OnGUI() {
		if (canvas == null || !canvas.GetComponent<MeshFilter>() && !canvas.GetComponent<Terrain>()) {
			EditorGUILayout.HelpBox("Select mesh or terrain object in the scene and restart tool", MessageType.Warning);
		}
		else {
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

			// canvas section
			EditorGUILayout.LabelField("Canvas", EditorStyles.boldLabel);
			EditorGUILayout.LabelField(canvas.name);
			EditorGUILayout.Space();

			// brush section
			EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);
			brush = (GameObject)EditorGUILayout.ObjectField("Brush", brush, typeof(GameObject), false);
			if (!brush)
				EditorGUILayout.HelpBox("Select brush object", MessageType.Warning);
			brushMode = (BrushMode)EditorGUILayout.EnumPopup("Mode", brushMode);
			if (brushMode == BrushMode.Brush) {
				brushRadius = EditorGUILayout.Slider("Radius",brushRadius, 0.01f, 1000f);
				brushDensity = EditorGUILayout.IntSlider("Density", brushDensity, 1, 1000);
			}
			EditorGUILayout.Space();

			// stroke section
			EditorGUILayout.LabelField("Stroke", EditorStyles.boldLabel);
			strokeMode = (StrokeMode)EditorGUILayout.EnumPopup("Mode", strokeMode);
			if (strokeMode == StrokeMode.OnHold) {
				switch (brushMode) {
					case BrushMode.Brush:
						strokeSpacing = EditorGUILayout.IntSlider("Spacing (%)", strokeSpacing, 1, 100);
						break;
					case BrushMode.Pen:
						strokeSpacingUnits = EditorGUILayout.Slider("Spacing (units)", strokeSpacingUnits, 0.0001f, 1000f);
						break;
				}
			}

			// scale section
			EditorGUILayout.LabelField("Scale Settings", EditorStyles.boldLabel);
			scaleMode = (ScaleMode)EditorGUILayout.EnumPopup("Mode", scaleMode);
			switch(scaleMode) {
				case ScaleMode.FixedUniform:
					scaleUniformFactor = EditorGUILayout.FloatField("Factor", scaleUniformFactor);
					break;
				case ScaleMode.FixedNonUniform:
					scaleNonUniformFactor = EditorGUILayout.Vector3Field("Factor", scaleNonUniformFactor);
					break;
				case ScaleMode.RandomUniform:
					minScale = EditorGUILayout.FloatField("Min", minScale);
					maxScale = EditorGUILayout.FloatField("Max", maxScale);
					break;
				case ScaleMode.RandomNonUniform:
					minXScale = EditorGUILayout.FloatField("Min X", minXScale);
					maxXScale = EditorGUILayout.FloatField("Max X", maxXScale);
					minYScale = EditorGUILayout.FloatField("Min Y", minYScale);
					maxYScale = EditorGUILayout.FloatField("Max Y", maxYScale);
					minZScale = EditorGUILayout.FloatField("Min Z", minZScale);
					maxZScale = EditorGUILayout.FloatField("Max Z", maxZScale);
					break;
			}
			EditorGUILayout.Space();

			// rotation section
			EditorGUILayout.LabelField("Rotation Settings", EditorStyles.boldLabel);
			rotationAlign = (RotationAlign)EditorGUILayout.EnumPopup("Align", rotationAlign);
			rotationMode = (RotationMode)EditorGUILayout.EnumPopup("Mode", rotationMode);
			switch (rotationMode) {
				case RotationMode.Fixed:
					eulerRot = EditorGUILayout.Vector3Field("Euler Rotation", eulerRot);
					break;
				case RotationMode.Random:
					minRotX = EditorGUILayout.FloatField("Mix X", minRotX);
					maxRotX = EditorGUILayout.FloatField("Max X", maxRotX);
					minRotY = EditorGUILayout.FloatField("Mix Y", minRotY);
					maxRotY = EditorGUILayout.FloatField("Max Y", maxRotY);
					minRotZ = EditorGUILayout.FloatField("Mix Z", minRotZ);
					maxRotZ = EditorGUILayout.FloatField("Max Z", maxRotZ);
					break;
			}
			EditorGUILayout.Space();

			// position section
			EditorGUILayout.LabelField("Position Settings", EditorStyles.boldLabel);
			positionMode = (PositionMode)EditorGUILayout.EnumPopup("Mode", positionMode);
			switch (positionMode) {
				case PositionMode.Fixed:
					offsetFixed = EditorGUILayout.Vector3Field("Offset", offsetFixed);
					break;
				case PositionMode.Random:
					minXOffset = EditorGUILayout.FloatField("Min X", minXOffset);
					maxXOffset = EditorGUILayout.FloatField("Max X", maxXOffset);
					minYOffset = EditorGUILayout.FloatField("Min Y", minYOffset);
					maxYOffset = EditorGUILayout.FloatField("Max Y", maxYOffset);
					minZOffset = EditorGUILayout.FloatField("Min Z", minZOffset);
					maxZOffset = EditorGUILayout.FloatField("Max Z", maxZOffset);
					break;
			}
			EditorGUILayout.EndScrollView();
		}
	}

	// paint on mesh
	void Paint() {
		prevPoint = hit.point;

		if (tmpCollider.Raycast(ray, out hit, RAY_DISTANCE) && hit.collider.gameObject == canvas && brush) {
			Vector3[] points = brushMode == BrushMode.Pen ? new Vector3[1] : new Vector3[(int)(Mathf.PI * brushRadius * brushDensity + 1)];	// generated points
			Quaternion rot = new Quaternion();
			rot.SetLookRotation(hit.normal);

			foreach (Vector3 point in points) {
				switch (brushMode) {
					case BrushMode.Pen:
						break;
					case BrushMode.Brush:
						Vector2 randPoint = Random.insideUnitCircle * brushRadius;	// generate random point
						point.Set(randPoint.x, randPoint.y, 0);
						break;
				}
				Vector3 newPoint = rot * point;	// rotate point to hit normal
				point.Set(newPoint.x, newPoint.y, newPoint.z);
				point.Set(point.x + hit.point.x, point.y + hit.point.y, point.z + hit.point.z);	// translate point to hit
				point.Set(point.x + hit.normal.x, point.y + hit.normal.y, point.z + hit.normal.z);	// push back point from canvas

				Ray pointRay = new Ray(point, -1f * hit.normal);
				RaycastHit pointHit;
				if (tmpCollider.Raycast(pointRay, out pointHit, RAY_DISTANCE) && pointHit.collider.gameObject == canvas) {
					GameObject paint = Instantiate(brush);
					Undo.RegisterCreatedObjectUndo(paint, "Painting");
					paint.transform.parent = painting.transform;
					paint.transform.position = pointHit.point;
					if (rotationAlign == RotationAlign.CanvasNormal)
						paint.transform.up = pointHit.normal;

					// scale
					switch (scaleMode) {
						case ScaleMode.FixedUniform:
							paint.transform.localScale *= scaleUniformFactor;
							break;
						case ScaleMode.FixedNonUniform:
							paint.transform.localScale = Vector3.Scale(paint.transform.localScale, scaleNonUniformFactor);
							break;
						case ScaleMode.RandomUniform:
							float randScale = Random.Range(minScale, maxScale);
							paint.transform.localScale = Vector3.Scale(paint.transform.localScale, new Vector3(randScale, randScale, randScale));
							break;
						case ScaleMode.RandomNonUniform:
							paint.transform.localScale = Vector3.Scale(paint.transform.localScale, new Vector3(Random.Range(minXScale, maxXScale), Random.Range(minYScale, maxYScale), Random.Range(minZScale, maxZScale)));
							break;
					}

					// rotation
					switch (rotationMode) {
						case RotationMode.Fixed:
							paint.transform.Rotate(eulerRot);
							break;
						case RotationMode.Random:
							paint.transform.Rotate(Random.Range(minRotX, maxRotX), Random.Range(minRotY, maxRotY), Random.Range(minRotZ, maxRotZ));
							break;
					}

					// position
					switch (positionMode) {
						case PositionMode.Fixed:
							paint.transform.Translate(offsetFixed);
							break;
						case PositionMode.Random:
							paint.transform.Translate(Random.Range(minXOffset, maxXOffset), Random.Range(minYOffset, maxYOffset), Random.Range(minZOffset, maxZOffset));
							break;
					}
				}
			}
		}
	}

	public void OnSceneGUI(SceneView sceneView) {
		// prevent selection changes if there is selected object
		if (canvas)	// object selected?
			HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));	// lock selection

		// handle mouse event
		Event e = Event.current;
		if (e.type == EventType.MouseMove) {
			ray = Camera.current.ScreenPointToRay(new Vector2(e.mousePosition.x, Camera.current.pixelHeight - e.mousePosition.y));	// emit ray from camera
			tmpCollider.Raycast(ray, out hit, RAY_DISTANCE);
		}
		else if (e.type == EventType.MouseDown && e.button == 0) {	// is left mouse button down?
			Paint();
		}
		else if (e.type == EventType.MouseDrag && e.button == 0 && strokeMode == StrokeMode.OnHold) {
			ray = Camera.current.ScreenPointToRay(new Vector2(e.mousePosition.x, Camera.current.pixelHeight - e.mousePosition.y));	// emit ray from camera
			if (tmpCollider.Raycast(ray, out hit, RAY_DISTANCE) && hit.collider.gameObject == canvas) {
				mouseDistance += Vector3.Distance(prevPoint, hit.point);
				prevPoint = hit.point;
				switch (brushMode) {
					case BrushMode.Brush:
						if (mouseDistance >= 2 * brushRadius * strokeSpacing / 100f) {
							Paint();
							mouseDistance = 0f;
						}
						break;
					case BrushMode.Pen:
						if (mouseDistance >= strokeSpacingUnits) {
							Paint();
							mouseDistance = 0f;
						}
						break;
				}
			}
		}
		else if (e.type == EventType.MouseUp && e.button == 0) {
			mouseDistance = 0f;
		}

		// brush handler
		switch (brushMode) {
			case BrushMode.Pen:
				break;
			case BrushMode.Brush:
				Handles.color = Color.blue;
				Handles.DrawWireDisc(hit.point, hit.normal, brushRadius);
				break;
		}
	}
}