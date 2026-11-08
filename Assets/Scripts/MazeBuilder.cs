using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using PhysMat = UnityEngine.PhysicsMaterial;
using PhysCombine = UnityEngine.PhysicsMaterialCombine;

namespace WoodenMaze{
public class MazeBuilder : MonoBehaviour{
    
    [Header("Materials (assets in Assets/Materials)")]
    [SerializeField] PhysMat _boardPhys;
    [SerializeField] PhysMat _marblePhys;
    [SerializeField] Material woodMaterial;
    [SerializeField] Material boxMaterial;
    [SerializeField] Material floorMaterial;
    [SerializeField] Material holeMaterial;
    [SerializeField] Material marbleMaterial;
    [SerializeField] Material goalMaterial;

       
    

    [Header("Maze layout")]
    [Tooltip("Same seed = same maze. Press N in play mode for a new random one.")]
    public int seed = 1977;
    [Range(4, 30)] public int width = 12;
    [Range(4, 30)] public int height = 10;
    [Tooltip("Share of extra walls removed so the board is open like the real game.")]
    [Range(0f, 0.8f)] public float wallRemoval = 0.3f;
    [Tooltip("Share of the cells off the route that become holes.")]
    [Range(0f, 1f)] public float holeDensity = 0.6f;

    [Header("Sizes (world units)")]
    public float cellSize = 1f;
    public float floorThickness = 0.3f;
    [Tooltip("Must be bigger than the marble radius, or the marble rolls over the holes.")]
    public float holeRadius = 0.35f;
    public float wallHeight = 0.45f;
    public float wallThickness = 0.12f;
    public float marbleRadius = 0.27f;

        
    Mesh _floorMesh;
    //Material _mWood, _mBox, _mFloor, _mHole, _mMarble, _mGoal;
    MazeLayout _layout;
    MazeBoard _board;
    Rigidbody _marble;
    Transform _goalMarker;
    Transform _knobX, _knobY;
    Texture2D _floorTex;
    //Color _goalColor = new Color(1f, 0.72f, 0.18f);
    
    const int MarbleLayer = 2; // "Ignore Raycast": lets the reflection probe skip the marble itself.

    float BoardW => width * cellSize;
    float BoardH => height * cellSize;
    const float FrameThickness = 0.3f;
    const float FrameHeight = 0.65f;
    /*void CreateMaterials(){
        // Wood, box, floor and hole materials are assets in Assets/Materials (matte, textures baked in).
        _mWood = Instance(woodMaterial, "Maze Wood");
        _mBox = Instance(boxMaterial, "Box Wood");
        _mFloor = Instance(floorMaterial, "Maze Floor");
        _mHole = Instance(holeMaterial, "Hole Inside");

        // Silver marble: fully metallic and very smooth so it reflects the board.
        _mMarble = Instance(marbleMaterial, "Silver Marble");
        MazeMaterials.Configure(_mMarble, new Color(0.86f, 0.87f, 0.9f), 1f, 0.92f);

        _mGoal = Instance(goalMaterial, "Goal");
        MazeMaterials.Configure(_mGoal, _goalColor, 0f, 0.3f);
        MazeMaterials.SetEmission(_mGoal, _goalColor * 0.6f);
    }*/

    /*static Material Instance(Material source, string name){
        // Copy so play mode never edits the material assets on disk.
        var m = source != null ? new Material(source) : MazeMaterials.Create(name);
        m.name = name;
        return m;
    }*/

    void BuildBox(){
        var box = new GameObject("Labyrinth Box").transform;
        box.SetParent(transform, false);

        float gap = 0.45f, wall = 0.35f;
        float ix = BoardW * 0.5f + FrameThickness + gap;
        float iz = BoardH * 0.5f + FrameThickness + gap;
        float floorY = -2.6f, rimY = 0.15f;
        float wallH = rimY - (floorY - 0.4f);
        float wallCy = (rimY + floorY - 0.4f) * 0.5f;

        Block("Box Bottom", box, new Vector3(0, floorY - 0.2f, 0),
            new Vector3(2 * (ix + wall), 0.4f, 2 * (iz + wall)), _mBox, _boardPhys);
        Block("Box North", box, new Vector3(0, wallCy, iz + wall * 0.5f), new Vector3(2 * (ix + wall), wallH, wall), _mBox, _boardPhys);
        Block("Box South", box, new Vector3(0, wallCy, -iz - wall * 0.5f), new Vector3(2 * (ix + wall), wallH, wall), _mBox, _boardPhys);
        Block("Box East", box, new Vector3(ix + wall * 0.5f, wallCy, 0), new Vector3(wall, wallH, 2 * iz), _mBox, _boardPhys);
        Block("Box West", box, new Vector3(-ix - wall * 0.5f, wallCy, 0), new Vector3(wall, wallH, 2 * iz), _mBox, _boardPhys);

        // Knobs like the real game: the front one tilts forward/back, the right one left/right.
        _knobY = Knob("Knob Front", box, new Vector3(0, -1.2f, -iz - wall - 0.3f), Quaternion.Euler(90f, 0f, 0f));
        _knobX = Knob("Knob Right", box, new Vector3(ix + wall + 0.3f, -1.2f, 0), Quaternion.Euler(0f, 0f, 90f));
    }

    Transform Knob(string name, Transform parent, Vector3 pos, Quaternion rot){
        var pivot = new GameObject(name).transform;
        pivot.SetParent(parent, false);
        pivot.localPosition = pos;
        pivot.localRotation = rot;

        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Knob";
        body.transform.SetParent(pivot, false);
        body.transform.localScale = new Vector3(0.9f, 0.3f, 0.9f);
        body.GetComponent<Renderer>().sharedMaterial = _mWood;
        Destroy(body.GetComponent<CapsuleCollider>());

        var notch = GameObject.CreatePrimitive(PrimitiveType.Cube);
        notch.name = "Grip";
        notch.transform.SetParent(pivot, false);
        notch.transform.localPosition = new Vector3(0f, -0.32f, 0f); // sticks out of the knob's outer face
        notch.transform.localScale = new Vector3(0.75f, 0.12f, 0.14f);
        notch.GetComponent<Renderer>().sharedMaterial = _mBox;
        Destroy(notch.GetComponent<BoxCollider>());
        return pivot;
    }

    void BuildMarble(){
        // The marble: a Sphere GameObject with a SphereCollider, a Rigidbody and a silver metallic material.
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Silver Marble";
        go.layer = MarbleLayer;
        go.transform.SetParent(transform, false);
        go.transform.localScale = Vector3.one * marbleRadius * 2f;
        go.GetComponent<Renderer>().sharedMaterial = _mMarble;
        go.GetComponent<SphereCollider>().sharedMaterial = _marblePhys;

        _marble = go.AddComponent<Rigidbody>();
        _marble.mass = 1f;
        _marble.useGravity = true;
        _marble.interpolation = RigidbodyInterpolation.Interpolate;
        _marble.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        _marble.sleepThreshold = 0f; // never fall asleep while the board moves under it
        _marble.maxAngularVelocity = 100f;
        _marble.SetDamping(0.05f, 0.2f);
    }

    /// <summary>(Re)builds the tilting board for the current seed.</summary>
    void BuildBoard(){
        if (_board != null){
            _board.gameObject.SetActive(false);
            Destroy(_board.gameObject);
        }
        if (_floorTex) Destroy(_floorTex);
        if (_floorMesh) Destroy(_floorMesh);

        _layout = MazeLayout.Generate(width, height, seed, wallRemoval, holeDensity);
        _best = PlayerPrefs.GetFloat(BestKey, 0f);

        var root = new GameObject("Maze Board");
        root.transform.SetParent(transform, false);
        root.AddComponent<Rigidbody>();
        _board = root.AddComponent<MazeBoard>();
        _board.maxTilt = maxTilt;
        _board.tiltSpeed = tiltSpeed;
        _board.levelSpeed = levelSpeed;
        _board.autoLevel = autoLevel;

        // Floor with real holes, and the painted route, numbers and labels.
        _floorTex = MazeTextures.CreateFloor(_layout, Mathf.Clamp(floorTextureDetail, 32, 256),
            holeRadius / cellSize, 0.065f);
        MazeMaterials.Configure(_mFloor, Color.white, 0f, 0f, _floorTex, specularHighlights: false);
        _floorMesh = MazeMeshes.BuildFloor(_layout, cellSize, floorThickness, holeRadius);

        var floor = new GameObject("Floor");
        floor.transform.SetParent(root.transform, false);
        floor.AddComponent<MeshFilter>().sharedMesh = _floorMesh;
        floor.AddComponent<MeshRenderer>().sharedMaterials = new[] { _mFloor, _mHole };
        var floorCol = floor.AddComponent<MeshCollider>();
        floorCol.sharedMesh = _floorMesh;
        floorCol.sharedMaterial = _boardPhys;

        BuildWalls(root.transform);
        BuildFrame(root.transform);

        // Glowing FINISH disc (visual only).
        var goal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        goal.name = "Finish";
        var goalCol = goal.GetComponent<CapsuleCollider>();
        goalCol.enabled = false;
        Destroy(goalCol);
        goal.transform.SetParent(root.transform, false);
        goal.transform.localPosition = CellLocal(_layout.Goal) + Vector3.up * 0.004f;
        goal.transform.localScale = new Vector3(0.4f * cellSize, 0.003f, 0.4f * cellSize);
        var goalRenderer = goal.GetComponent<Renderer>();
        goalRenderer.sharedMaterial = _mGoal;
        goalRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _goalMarker = goal.transform;
    }

    void BuildWalls(Transform parent){
        var walls = new GameObject("Walls").transform;
        walls.SetParent(parent, false);
        float x0 = -BoardW * 0.5f, z0 = -BoardH * 0.5f;
        float y = wallHeight * 0.5f;

        // Walls along the x axis (north side of cells). Neighbouring pieces merge into one stick.
        for (int cy = 0; cy < height - 1; cy++){
            int cx = 0;
            while (cx < width){
                if (!_layout.WallNorth[cx, cy]) { cx++; continue; }
                int start = cx;
                while (cx < width && _layout.WallNorth[cx, cy]) cx++;
                int len = cx - start;
                Block("Wall", walls,
                    new Vector3(x0 + (start + len * 0.5f) * cellSize, y, z0 + (cy + 1) * cellSize),
                    new Vector3(len * cellSize + wallThickness, wallHeight, wallThickness), _mWood, _boardPhys);
            }
        }

        // Walls along the z axis (east side of cells).
        for (int cx = 0; cx < width - 1; cx++){
            int cy = 0;
            while (cy < height){
                if (!_layout.WallEast[cx, cy]) { cy++; continue; }
                int start = cy;
                while (cy < height && _layout.WallEast[cx, cy]) cy++;
                int len = cy - start;
                Block("Wall", walls,
                    new Vector3(x0 + (cx + 1) * cellSize, y, z0 + (start + len * 0.5f) * cellSize),
                    new Vector3(wallThickness, wallHeight, len * cellSize + wallThickness), _mWood, _boardPhys);
            }
        }
    }

    void BuildFrame(Transform parent){
        var frame = new GameObject("Frame").transform;
        frame.SetParent(parent, false);
        float t = FrameThickness, hgt = FrameHeight + floorThickness;
        float cy = (FrameHeight - floorThickness) * 0.5f;
        float hw = BoardW * 0.5f, hh = BoardH * 0.5f;
        Block("Frame North", frame, new Vector3(0, cy, hh + t * 0.5f), new Vector3(BoardW + 2 * t, hgt, t), _mWood, _boardPhys);
        Block("Frame South", frame, new Vector3(0, cy, -hh - t * 0.5f), new Vector3(BoardW + 2 * t, hgt, t), _mWood, _boardPhys);
        Block("Frame East", frame, new Vector3(hw + t * 0.5f, cy, 0), new Vector3(t, hgt, BoardH), _mWood, _boardPhys);
        Block("Frame West", frame, new Vector3(-hw - t * 0.5f, cy, 0), new Vector3(t, hgt, BoardH), _mWood, _boardPhys);
    }

    static void Block(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat, PhysMat phys){
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        go.GetComponent<BoxCollider>().sharedMaterial = phys;
    }

    void SetupCameraAndLight(){
        if (setupCamera){
            var cam = Camera.main;
            if (cam == null){
                var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            float k = Mathf.Max(BoardH, BoardW * 0.75f);
            cam.fieldOfView = 50f;
            cam.transform.position = transform.position + new Vector3(0f, k * 1.3f, -k * 0.95f);
            cam.transform.LookAt(transform.position + new Vector3(0f, 0f, -0.4f));
        }

        bool hasLight = false;
        foreach (var l in FindObjectsByType<Light>())
            if (l.type == LightType.Directional) { hasLight = true; break; }
        if (!hasLight){
            var light = new GameObject("Directional Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
        }

        // Live reflections so the silver marble mirrors the wooden board around it.
        var probe = new GameObject("Marble Reflections").AddComponent<ReflectionProbe>();
        probe.transform.SetParent(transform, false);
        probe.transform.localPosition = new Vector3(0f, 1.5f, 0f);
        probe.mode = ReflectionProbeMode.Realtime;
        probe.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
        probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        probe.size = new Vector3(BoardW + 8f, 10f, BoardH + 8f);
        probe.resolution = 128;
        probe.cullingMask = ~(1 << MarbleLayer);
    }
         
}}