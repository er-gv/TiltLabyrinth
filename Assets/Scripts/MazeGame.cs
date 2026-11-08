
using PhysMat = UnityEngine.PhysicsMaterial;
using PhysCombine = UnityEngine.PhysicsMaterialCombine;

using UnityEngine;
using UnityEngine.Rendering;

namespace WoodenMaze
{
    /// <summary>
    /// The whole game. Put this on one GameObject in an otherwise empty scene and press Play:
    /// it builds the wooden box, the tilting maze board, the silver marble, camera and lighting,
    /// then runs the rules (reach FINISH to win, fall in a hole to lose).
    /// Menu "Wooden Maze > Create Game Scene" sets this up for you.
    /// </summary>
    [DisallowMultipleComponent]
    public class MazeGame : MonoBehaviour
    {
        /*
        [Header("Controls and physics")]
        public float maxTilt = 10f;
        public float tiltSpeed = 40f;
        public float levelSpeed = 25f;
        [Tooltip("On: the board eases back to level when no arrow key is held.")]
        public bool autoLevel = true;
        [Tooltip("Gravity is multiplied by this. The board is much bigger than a real one, so real gravity feels slow.")]
        public float gravityMultiplier = 3f;

        [Header("Materials (optional, created automatically when empty)")]
        public Material woodMaterial;
        public Material boxMaterial;
        public Material floorMaterial;
        public Material holeMaterial;
        public Material marbleMaterial;
        public Material goalMaterial;

        [Header("Scene")]
        public bool setupCamera = true;
        [Tooltip("Pixels per cell for the painted floor texture.")]
        public int floorTextureDetail = 160;
        */
        enum State { Ready, Playing, Won, Lost }

        State _state;
        float _elapsed;
        float _best;
        int _lostHole;

        /*
        MazeLayout _layout;
        MazeBoard _board;
        Rigidbody _marble;
        Transform _goalMarker;
        Transform _knobX, _knobY;
        Texture2D _floorTex, _woodTex, _boxTex;
        Mesh _floorMesh;
        Material _mWood, _mBox, _mFloor, _mHole, _mMarble, _mGoal;
        PhysMat _boardPhys, _marblePhys;
        */
        Color _goalColor = new Color(1f, 0.72f, 0.18f);

        MazeBuilder _mazeBuilder;

        

        // ------------------------------------------------------------------ setup

        void Awake(){
            _mazeBuilder = GetComponent<MazeBuilder>();
            Physics.gravity = Vector3.down * 9.81f * gravityMultiplier;
            holeRadius = Mathf.Max(holeRadius, marbleRadius * 1.15f);

            //CreateMaterials();
            //_boardPhys = MakePhysicsMaterial("Board", 0.45f, 0.15f);
            //_marblePhys = MakePhysicsMaterial("Marble", 0.45f, 0.2f);

            _mazeBuilder.GenerateNewMaze();
            //BuildBox();
            //BuildMarble();
            //BuildBoard();
            //SetupCameraAndLight();
            Restart();
        }

        
        void OnDestroy(){
            if (_floorTex) Destroy(_floorTex);
            if (_woodTex) Destroy(_woodTex);
            if (_boxTex) Destroy(_boxTex);
            if (_floorMesh) Destroy(_floorMesh);
        }

        /*void CreateMaterials()
        {
            _woodTex = MazeTextures.CreateWood(512, 11, new Color(0.60f, 0.38f, 0.21f), new Color(0.33f, 0.19f, 0.09f));
            _boxTex = MazeTextures.CreateWood(512, 47, new Color(0.55f, 0.35f, 0.20f), new Color(0.30f, 0.17f, 0.08f));

            // Wood is matte: no specular highlights, no glossy reflections.
            _mWood = Instance(woodMaterial, "Maze Wood");
            MazeMaterials.Configure(_mWood, Color.white, 0f, 0f, _woodTex, specularHighlights: false);
            _mBox = Instance(boxMaterial, "Box Wood");
            MazeMaterials.Configure(_mBox, Color.white, 0f, 0f, _boxTex, specularHighlights: false);
            _mFloor = Instance(floorMaterial, "Maze Floor");
            MazeMaterials.Configure(_mFloor, Color.white, 0f, 0f, null, specularHighlights: false);
            _mHole = Instance(holeMaterial, "Hole Inside");
            MazeMaterials.Configure(_mHole, new Color(0.04f, 0.035f, 0.03f), 0f, 0f, null, specularHighlights: false);

            // Silver marble: fully metallic and very smooth so it reflects the board.
            _mMarble = Instance(marbleMaterial, "Silver Marble");
            MazeMaterials.Configure(_mMarble, new Color(0.86f, 0.87f, 0.9f), 1f, 0.92f);

            _mGoal = Instance(goalMaterial, "Goal");
            MazeMaterials.Configure(_mGoal, _goalColor, 0f, 0.3f);
            MazeMaterials.SetEmission(_mGoal, _goalColor * 0.6f);
        }

        static Material Instance(Material source, string name)
        {
            // Copy so play mode never edits the material assets on disk.
            var m = source != null ? new Material(source) : MazeMaterials.Create(name);
            m.name = name;
            return m;
        }

        static PhysMat MakePhysicsMaterial(string name, float friction, float bounce)
        {
            return new PhysMat(name)
            {
                dynamicFriction = friction,
                staticFriction = friction,
                bounciness = bounce,
                frictionCombine = PhysCombine.Average,
                bounceCombine = PhysCombine.Average
            };
        }

        /// <summary>The fixed outer wooden box the board tilts inside, plus the two knobs.</summary>
        void BuildBox()
        {
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

        Transform Knob(string name, Transform parent, Vector3 pos, Quaternion rot)
        {
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

        void BuildMarble()
        {
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
        void BuildBoard()
        {
            if (_board != null)
            {
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

        void BuildWalls(Transform parent)
        {
            var walls = new GameObject("Walls").transform;
            walls.SetParent(parent, false);
            float x0 = -BoardW * 0.5f, z0 = -BoardH * 0.5f;
            float y = wallHeight * 0.5f;

            // Walls along the x axis (north side of cells). Neighbouring pieces merge into one stick.
            for (int cy = 0; cy < height - 1; cy++)
            {
                int cx = 0;
                while (cx < width)
                {
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
            for (int cx = 0; cx < width - 1; cx++)
            {
                int cy = 0;
                while (cy < height)
                {
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

        void BuildFrame(Transform parent)
        {
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

        static void Block(string name, Transform parent, Vector3 localPos, Vector3 size, Material mat, PhysMat phys)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            go.GetComponent<BoxCollider>().sharedMaterial = phys;
        }

        void SetupCameraAndLight()
        {
            if (setupCamera)
            {
                var cam = Camera.main;
                if (cam == null)
                {
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
            if (!hasLight)
            {
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
                
        */


        // ------------------------------------------------------------------ game loop

        Vector3 CellLocal(Vector2Int c) =>
            new Vector3((c.x + 0.5f) * cellSize - BoardW * 0.5f, 0f, (c.y + 0.5f) * cellSize - BoardH * 0.5f);

        string BestKey => $"WoodenMaze.Best.{seed}.{width}x{height}.{wallRemoval:0.00}.{holeDensity:0.00}";

        void Restart()
        {
            _state = State.Ready;
            _elapsed = 0f;
            _board.forceLevel = false;
            _board.ResetTilt();

            _marble.transform.SetParent(transform, true);
            _marble.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _marble.isKinematic = false;
            Vector3 p = _board.transform.TransformPoint(CellLocal(_layout.Start) + Vector3.up * (marbleRadius + 0.02f));
            _marble.position = p;
            _marble.transform.position = p;
            _marble.rotation = Quaternion.identity;
            _marble.SetVelocity(Vector3.zero);
            _marble.angularVelocity = Vector3.zero;
            _marble.WakeUp();
        }

        void NewMaze()
        {
            seed = Random.Range(1, 100000);
            BuildBoard();
            Restart();
        }

        void Update(){
            if (MazeInput.QuitPressed()){
                #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
                #else
                Application.Quit();
                #endif
                return;
            }
            if (MazeInput.NewMazePressed()) { NewMaze(); return; }
            if (MazeInput.RestartPressed()) { Restart(); return; }

            Vector2 keys = MazeInput.Tilt();
            switch (_state){
                case State.Ready:
                    _board.input = keys;
                    if (keys != Vector2.zero) _state = State.Playing;
                    CheckOutcome();
                    break;
                case State.Playing:
                    _board.input = keys;
                    _elapsed += Time.deltaTime;
                    CheckOutcome();
                    break;
                default:
                    _board.input = Vector2.zero;
                    _board.forceLevel = true;
                    break;
            }

            // Knobs turn with the board.
            if (_knobX) _knobX.localRotation = Quaternion.Euler(0f, 0f, 90f) * Quaternion.Euler(0f, _board.Tilt.x * 9f, 0f);
            if (_knobY) _knobY.localRotation = Quaternion.Euler(90f, 0f, 0f) * Quaternion.Euler(0f, -_board.Tilt.y * 9f, 0f);

            // Pulse the finish disc.
            float pulse = 0.4f + 0.35f * Mathf.Sin(Time.time * 4f);
            MazeMaterials.SetEmission(_mGoal, _goalColor * (_state == State.Won ? 1.5f : pulse));
        }

        void CheckOutcome()
        {
            Vector3 local = _board.transform.InverseTransformPoint(_marble.position);

            // Below the floor surface: it went down a hole (or somehow left the board).
            if (local.y < -floorThickness * 0.5f || _marble.position.y < transform.position.y - 20f)
            {
                _state = State.Lost;
                _lostHole = NearestHole(local);
                return;
            }

            Vector3 g = CellLocal(_layout.Goal);
            float dx = local.x - g.x, dz = local.z - g.z;
            if (dx * dx + dz * dz < (0.22f * cellSize) * (0.22f * cellSize) && local.y < marbleRadius + 0.15f)
                Win();
        }

        int NearestHole(Vector3 local)
        {
            int best = 0;
            float bestD = float.MaxValue;
            for (int i = 0; i < _layout.Holes.Count; i++)
            {
                Vector3 h = CellLocal(_layout.Holes[i]);
                float d = (h.x - local.x) * (h.x - local.x) + (h.z - local.z) * (h.z - local.z);
                if (d < bestD) { bestD = d; best = i + 1; }
            }
            return best;
        }

        void Win()
        {
            _state = State.Won;
            if (_best <= 0f || _elapsed < _best)
            {
                _best = _elapsed;
                PlayerPrefs.SetFloat(BestKey, _best);
                PlayerPrefs.Save();
            }
            // Hold the marble in the finish cup while the board levels out.
            _marble.SetVelocity(Vector3.zero);
            _marble.angularVelocity = Vector3.zero;
            _marble.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _marble.isKinematic = true;
            _marble.transform.SetParent(_board.transform, true);
        }

        void Lost(){
            _state = State.Lost;
            // UpdateUI will show which hole it fell into, if any.
            // Let the marble fall into the hole while the board levels out.
            _marble.SetVelocity(Vector3.zero);
            _marble.angularVelocity = Vector3.zero;
            _marble.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
        // ------------------------------------------------------------------ UI

        GUIStyle _hud, _big, _small;

        void OnGUI()
        {
            float s = Screen.height / 720f;
            if (_hud == null)
            {
                _hud = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
                _big = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                _small = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerCenter };
            }
            _hud.fontSize = Mathf.RoundToInt(22 * s);
            _big.fontSize = Mathf.RoundToInt(54 * s);
            _small.fontSize = Mathf.RoundToInt(17 * s);

            string best = _best > 0f ? $"   Best {_best:0.0}s" : "";
            Shadowed(new Rect(18 * s, 12 * s, 700 * s, 34 * s), $"Time {_elapsed:0.0}s{best}", _hud, Color.white);
            Shadowed(new Rect(18 * s, 40 * s, 700 * s, 34 * s), $"Maze #{seed}   Holes {_layout.Holes.Count}", _hud, new Color(1f, 1f, 1f, 0.75f));

            var center = new Rect(0, Screen.height * 0.32f, Screen.width, 90 * s);
            var under = new Rect(0, Screen.height * 0.32f + 80 * s, Screen.width, 40 * s);
            var underStyle = new GUIStyle(_hud) { alignment = TextAnchor.MiddleCenter };
            switch (_state)
            {
                case State.Ready:
                    Shadowed(under, "Tilt the maze with the arrow keys and roll the marble to FINISH", underStyle, Color.white);
                    break;
                case State.Won:
                    Shadowed(center, "You made it!", _big, new Color(1f, 0.85f, 0.3f));
                    Shadowed(under, $"{_elapsed:0.0} seconds.  R to play again, N for a new maze", underStyle, Color.white);
                    break;
                case State.Lost:
                    Shadowed(center, _lostHole > 0 ? $"Down hole {_lostHole}!" : "Off the board!", _big, new Color(1f, 0.45f, 0.35f));
                    Shadowed(under, "R to try again, N for a new maze", underStyle, Color.white);
                    break;
            }

            Shadowed(new Rect(0, 0, Screen.width, Screen.height - 10 * s),
                "Arrow keys: tilt     R / Space: restart     N: new maze     Esc: quit", _small, new Color(1f, 1f, 1f, 0.8f));
        }

        static void Shadowed(Rect r, string text, GUIStyle style, Color color)
        {
            var old = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, 0.7f);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
            style.normal.textColor = color;
            GUI.Label(r, text, style);
            style.normal.textColor = old;
        }
    }

    /// <summary>Rigidbody API names changed in Unity 6; this keeps both versions compiling cleanly.</summary>
    static class RigidbodyCompat{
        public static void SetVelocity(this Rigidbody rb, Vector3 v) => rb.linearVelocity = v;
        

        public static void SetDamping(this Rigidbody rb, float linear, float angular){
            rb.linearDamping = linear;
            rb.angularDamping = angular;
        }
    }
}
