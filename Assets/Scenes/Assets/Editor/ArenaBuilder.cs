using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#endif

/// <summary>
/// Gera automaticamente todo o jogo Arena of Survival:
/// sprites pixel art, material de física, prefabs, cenas MainMenu e Arena,
/// interface completa e configurações de build.
/// Menu: Arena of Survival > Construir Jogo Completo
/// </summary>
public static class ArenaBuilder
{
    public const string SpritesDir = "Assets/Sprites";
    public const string PrefabsDir = "Assets/Prefabs";
    public const string ScenesDir = "Assets/Scenes";
    public const string MaterialsDir = "Assets/Materials";
    public const string AudioDir = "Assets/Audio";
    public const string MenuScenePath = "Assets/Scenes/MainMenu.unity";
    public const string ArenaScenePath = "Assets/Scenes/Arena.unity";
    const int PPU = 16;

    // Ordem de desenho
    const int OrderFloor = -100;
    const int OrderDecor = -40;
    const int OrderWall = -20;
    const int OrderGate = -10;
    const int OrderObstacle = 0;
    const int OrderKey = 5;
    const int OrderCharacter = 10;
    const int OrderEffect = 20;

    class Art
    {
        public Sprite floor, wall, wallBroken, rock, crate, pillar, bush, grass, skull;
        public Sprite gateClosed, gateOpen, key, glow;
        public Sprite playerDown, playerUp, playerSide, enemy, slash;
        public Sprite heartFull, heartEmpty, uiPanel, uiButton;
    }

    class Prefabs
    {
        public GameObject player, enemy, key, rock, crate, pillar;
    }

    // =====================================================================
    // MENUS DO EDITOR
    // =====================================================================

    [MenuItem("Arena of Survival/Construir Jogo Completo", false, 0)]
    public static void BuildAll()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Arena of Survival", "Saia do modo Play antes de gerar o jogo.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        try
        {
            EditorUtility.DisplayProgressBar("Arena of Survival", "Criando pastas...", 0.05f);
            EnsureFolders();

            EditorUtility.DisplayProgressBar("Arena of Survival", "Gerando sprites pixel art...", 0.2f);
            Art art = GenerateArt();

            EditorUtility.DisplayProgressBar("Arena of Survival", "Criando prefabs...", 0.45f);
            PhysicsMaterial2D noFriction = CreatePhysicsMaterial();
            Prefabs prefabs = BuildPrefabs(art, noFriction);

            EditorUtility.DisplayProgressBar("Arena of Survival", "Montando a cena Arena...", 0.65f);
            BuildArenaScene(art, prefabs);

            EditorUtility.DisplayProgressBar("Arena of Survival", "Montando o Menu Principal...", 0.85f);
            BuildMainMenuScene(art);

            EditorUtility.DisplayProgressBar("Arena of Survival", "Configurando o projeto...", 0.95f);
            ConfigureProject();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        EditorUtility.DisplayDialog("Arena of Survival",
            "Jogo gerado com sucesso!\n\n" +
            "Cenas: Assets/Scenes/MainMenu.unity e Arena.unity\n" +
            "A cena MainMenu está aberta: pressione Play para jogar.", "OK");
    }

    [MenuItem("Arena of Survival/Jogar (abrir Menu Principal e dar Play)", false, 20)]
    public static void PlayFromMenu()
    {
        if (EditorApplication.isPlaying) return;
        if (!File.Exists(MenuScenePath) || !File.Exists(ArenaScenePath)) BuildAll();
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    // =====================================================================
    // PASTAS, MATERIAIS E CONFIGURAÇÃO
    // =====================================================================

    static void EnsureFolders()
    {
        string[] folders = { "Sprites", "Prefabs", "Scenes", "Materials", "Audio" };
        foreach (string f in folders)
            if (!AssetDatabase.IsValidFolder("Assets/" + f))
                AssetDatabase.CreateFolder("Assets", f);
    }

    static PhysicsMaterial2D CreatePhysicsMaterial()
    {
        string path = MaterialsDir + "/NoFriction.physicsMaterial2D";
        PhysicsMaterial2D mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (mat == null)
        {
            mat = new PhysicsMaterial2D("NoFriction");
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.friction = 0f;
        mat.bounciness = 0f;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void ConfigureProject()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MenuScenePath, true),
            new EditorBuildSettingsScene(ArenaScenePath, true)
        };
        PlayerSettings.productName = "Arena of Survival";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1600;
        PlayerSettings.defaultScreenHeight = 900;
        PlayerSettings.resizableWindow = true;
    }

    // =====================================================================
    // GERAÇÃO DE SPRITES (pixel art procedural, sem assets externos)
    // =====================================================================

    class PixelCanvas
    {
        public readonly int W, H;
        readonly Color32[] px;

        public PixelCanvas(int w, int h) { W = w; H = h; px = new Color32[w * h]; }

        public bool Inside(int x, int y) { return x >= 0 && y >= 0 && x < W && y < H; }
        public void Set(int x, int y, Color32 c) { if (Inside(x, y)) px[y * W + x] = c; }
        public Color32 Get(int x, int y) { return Inside(x, y) ? px[y * W + x] : new Color32(0, 0, 0, 0); }

        public void Rect(int x, int y, int w, int h, Color32 c)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++) Set(i, j, c);
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color32 c)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                }
        }

        public void Line(int x0, int y0, int x1, int y1, Color32 c)
        {
            int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
            int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
            int err = dx + dy;
            while (true)
            {
                Set(x0, y0, c);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 >= dy) { err += dy; x0 += sx; }
                if (e2 <= dx) { err += dx; y0 += sy; }
            }
        }

        public void AddOutline(Color32 c)
        {
            Color32[] copy = (Color32[])px.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (copy[y * W + x].a != 0) continue;
                    bool near = false;
                    if (x > 0 && copy[y * W + x - 1].a == 255) near = true;
                    if (x < W - 1 && copy[y * W + x + 1].a == 255) near = true;
                    if (y > 0 && copy[(y - 1) * W + x].a == 255) near = true;
                    if (y < H - 1 && copy[(y + 1) * W + x].a == 255) near = true;
                    if (near) px[y * W + x] = c;
                }
        }

        public void Shadow(float cx, float cy, float rx, float ry, byte alpha)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (Get(x, y).a != 0) continue;
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) Set(x, y, new Color32(0, 0, 0, alpha));
                }
        }

        public byte[] ToPNG()
        {
            Texture2D tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);
            return bytes;
        }
    }

    static Color32 C(int r, int g, int b, int a = 255)
    {
        return new Color32((byte)Mathf.Clamp(r, 0, 255), (byte)Mathf.Clamp(g, 0, 255),
                           (byte)Mathf.Clamp(b, 0, 255), (byte)Mathf.Clamp(a, 0, 255));
    }

    static Color32 Sh(Color32 c, int delta)
    {
        return C(c.r + delta, c.g + delta, c.b + delta, c.a);
    }

    static Sprite SaveSprite(string name, PixelCanvas cv, bool repeat = false, Vector4? border = null)
    {
        string assetPath = SpritesDir + "/" + name + ".png";
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
        File.WriteAllBytes(fullPath, cv.ToPNG());
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spritePixelsPerUnit = PPU;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.filterMode = FilterMode.Point;
        settings.mipmapEnabled = false;
        settings.alphaIsTransparency = true;
        settings.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        if (border.HasValue) settings.spriteBorder = border.Value;
        importer.SetTextureSettings(settings);

        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    static Art GenerateArt()
    {
        Art a = new Art();
        a.floor = SaveSprite("floor_arena", DrawFloor(30, 16));
        a.wall = SaveSprite("wall_stone", DrawWall(), true);
        a.wallBroken = SaveSprite("wall_broken", DrawBrokenWall());
        a.rock = SaveSprite("rock", DrawRock());
        a.crate = SaveSprite("crate", DrawCrate());
        a.pillar = SaveSprite("pillar_broken", DrawPillar());
        a.bush = SaveSprite("bush", DrawBush());
        a.grass = SaveSprite("grass", DrawGrass());
        a.skull = SaveSprite("skull", DrawSkull());
        a.gateClosed = SaveSprite("gate_closed", DrawGate(false));
        a.gateOpen = SaveSprite("gate_open", DrawGate(true));
        a.key = SaveSprite("key", DrawKey());
        a.glow = SaveSprite("glow", DrawGlow());
        a.playerDown = SaveSprite("player_down", DrawPlayer(0));
        a.playerUp = SaveSprite("player_up", DrawPlayer(1));
        a.playerSide = SaveSprite("player_side", DrawPlayer(2));
        a.enemy = SaveSprite("enemy", DrawEnemy());
        a.slash = SaveSprite("slash", DrawSlash());
        a.heartFull = SaveSprite("heart_full", DrawHeart(true));
        a.heartEmpty = SaveSprite("heart_empty", DrawHeart(false));
        a.uiPanel = SaveSprite("ui_panel", DrawUIPanel(), false, new Vector4(5, 5, 5, 5));
        a.uiButton = SaveSprite("ui_button", DrawUIButton(), false, new Vector4(4, 4, 4, 4));
        return a;
    }

    static PixelCanvas DrawFloor(int tilesX, int tilesY)
    {
        System.Random rng = new System.Random(7);
        PixelCanvas cv = new PixelCanvas(tilesX * 16, tilesY * 16);
        Color32 mortar = C(52, 50, 50);
        Color32 crack = C(46, 44, 44);

        for (int ty = 0; ty < tilesY; ty++)
            for (int tx = 0; tx < tilesX; tx++)
            {
                int ox = tx * 16, oy = ty * 16;
                int baseV = rng.Next(82, 104);
                int warm = rng.Next(0, 6);

                for (int y = 0; y < 16; y++)
                    for (int x = 0; x < 16; x++)
                    {
                        int n = rng.Next(-5, 6);
                        Color32 c = C(baseV + n + warm, baseV + n + warm / 2, baseV + n - 2);
                        if (x == 0 || y == 0) c = mortar;
                        else if (x == 1 || y == 15) c = Sh(c, 10);
                        else if (x == 15 || y == 1) c = Sh(c, -12);
                        cv.Set(ox + x, oy + y, c);
                    }

                if (rng.NextDouble() < 0.4)
                {
                    int x = rng.Next(3, 13), y = rng.Next(3, 13), len = rng.Next(5, 12);
                    for (int i = 0; i < len; i++)
                    {
                        cv.Set(ox + x, oy + y, crack);
                        x = Mathf.Clamp(x + rng.Next(-1, 2), 2, 14);
                        y = Mathf.Clamp(y + rng.Next(-1, 2), 2, 14);
                    }
                }

                if (rng.NextDouble() < 0.18)
                {
                    int mx = rng.Next(2, 14), my = rng.Next(2, 5), count = rng.Next(3, 7);
                    for (int i = 0; i < count; i++)
                        cv.Set(ox + mx + rng.Next(-1, 2), oy + my + rng.Next(-1, 2), C(46, 78 + rng.Next(0, 20), 40));
                }

                if (rng.NextDouble() < 0.2)
                {
                    int px = ox + rng.Next(3, 13), py = oy + rng.Next(3, 13);
                    cv.Set(px, py, C(132, 128, 122));
                    cv.Set(px + 1, py, C(70, 68, 66));
                }
            }
        return cv;
    }

    static PixelCanvas DrawWall()
    {
        System.Random rng = new System.Random(21);
        PixelCanvas cv = new PixelCanvas(16, 16);
        Color32 mortar = C(54, 52, 56);
        for (int row = 0; row < 4; row++)
        {
            int y0 = row * 4, off = (row % 2) * 4;
            for (int x = 0; x < 16; x++)
            {
                int bx = (x + off) / 8;
                int shade = 100 + ((bx * 37 + row * 61) % 4) * 9;
                for (int y = y0; y < y0 + 4; y++)
                {
                    bool isMortar = y == y0 || (x + off) % 8 == 0;
                    Color32 c;
                    if (isMortar) c = mortar;
                    else
                    {
                        int n = rng.Next(-6, 7);
                        c = C(shade + n, shade + n - 2, shade + n + 2);
                        if (y == y0 + 3) c = Sh(c, 16);
                        else if (y == y0 + 1) c = Sh(c, -12);
                    }
                    cv.Set(x, y, c);
                }
            }
        }
        return cv;
    }

    static PixelCanvas DrawBrokenWall()
    {
        PixelCanvas cv = DrawWall();
        System.Random rng = new System.Random(5);
        for (int x = 6; x < 16; x++)
        {
            int cut = 16 - (int)((x - 5) * 1.2f) - rng.Next(0, 2);
            for (int y = cut; y < 16; y++) cv.Set(x, y, new Color32(0, 0, 0, 0));
        }
        Color32 stone = C(112, 108, 112);
        cv.Rect(13, 10, 2, 2, stone);
        cv.Rect(10, 13, 2, 1, stone);
        cv.Set(14, 13, Sh(stone, -15));
        cv.AddOutline(C(36, 34, 38));
        return cv;
    }

    static PixelCanvas DrawRock()
    {
        System.Random rng = new System.Random(3);
        PixelCanvas cv = new PixelCanvas(16, 16);
        Color32 baseC = C(128, 124, 118);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float dx = (x + 0.5f - 8f) / 6.5f, dy = (y + 0.5f - 7.5f) / 5.5f;
                if (dx * dx + dy * dy <= 1f)
                {
                    int shade = (int)((dy - dx) * 18f) + rng.Next(-5, 6);
                    cv.Set(x, y, Sh(baseC, shade));
                }
            }
        Color32 dark = C(70, 68, 66);
        cv.Line(6, 9, 8, 7, dark);
        cv.Line(8, 7, 7, 5, dark);
        cv.Line(10, 10, 11, 8, dark);
        cv.Set(5, 10, C(176, 172, 164));
        cv.Set(6, 11, C(176, 172, 164));
        cv.AddOutline(C(38, 36, 40));
        cv.Shadow(8.5f, 2.5f, 6.5f, 2.2f, 70);
        return cv;
    }

    static PixelCanvas DrawCrate()
    {
        System.Random rng = new System.Random(9);
        PixelCanvas cv = new PixelCanvas(16, 16);
        Color32 wood = C(146, 98, 54), dark = C(104, 66, 34), frame = C(118, 78, 40);
        cv.Rect(1, 1, 14, 14, wood);
        for (int y = 1; y < 15; y++)
            for (int x = 1; x < 15; x++)
                if (rng.NextDouble() < 0.15) cv.Set(x, y, Sh(wood, -12));
        for (int x = 1; x < 15; x++) { cv.Set(x, 5, dark); cv.Set(x, 10, dark); }
        cv.Line(3, 3, 12, 12, frame);
        cv.Line(4, 3, 12, 11, frame);
        cv.Rect(1, 1, 14, 2, frame);
        cv.Rect(1, 13, 14, 2, frame);
        cv.Rect(1, 1, 2, 14, frame);
        cv.Rect(13, 1, 2, 14, frame);
        cv.Rect(1, 14, 14, 1, Sh(frame, 25));
        Color32 nail = C(72, 72, 76);
        cv.Set(2, 2, nail); cv.Set(13, 2, nail); cv.Set(2, 13, nail); cv.Set(13, 13, nail);
        cv.AddOutline(C(48, 30, 14));
        return cv;
    }

    static PixelCanvas DrawPillar()
    {
        System.Random rng = new System.Random(13);
        PixelCanvas cv = new PixelCanvas(16, 16);
        Color32 stone = C(156, 152, 144);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float dx = x + 0.5f - 8f, dy = y + 0.5f - 8f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r > 6.5f) continue;
                if (dx > 3f && dy > 3f && r > 5f) continue; // lasca quebrada
                int shade = (int)((dy - dx) * 2.5f) + rng.Next(-4, 5);
                Color32 c = Sh(stone, shade);
                if (r > 4f && r < 4.9f) c = Sh(c, -22);
                if (r < 1.5f) c = Sh(c, 12);
                cv.Set(x, y, c);
            }
        cv.AddOutline(C(46, 44, 48));
        cv.Shadow(9.5f, 5.5f, 6.5f, 4.5f, 70);
        return cv;
    }

    static PixelCanvas DrawBush()
    {
        System.Random rng = new System.Random(17);
        PixelCanvas cv = new PixelCanvas(16, 16);
        Color32 dark = C(30, 70, 36), mid = C(44, 98, 48), light = C(72, 132, 62);
        cv.Ellipse(6f, 7f, 4.5f, 4f, dark);
        cv.Ellipse(10f, 8f, 4.5f, 4.2f, dark);
        cv.Ellipse(8f, 10.5f, 4f, 3.5f, dark);
        cv.Ellipse(6f, 7.5f, 3.5f, 3f, mid);
        cv.Ellipse(10f, 8.5f, 3.5f, 3.2f, mid);
        cv.Ellipse(8f, 11f, 3f, 2.6f, mid);
        for (int i = 0; i < 12; i++)
        {
            int x = rng.Next(3, 13), y = rng.Next(8, 14);
            if (cv.Get(x, y).a == 255) cv.Set(x, y, light);
        }
        cv.AddOutline(C(18, 40, 22));
        cv.Shadow(8f, 3f, 6f, 2f, 60);
        return cv;
    }

    static PixelCanvas DrawGrass()
    {
        PixelCanvas cv = new PixelCanvas(16, 16);
        Color32 dark = C(34, 76, 38), mid = C(52, 108, 50), light = C(84, 140, 66);
        cv.Line(4, 2, 3, 7, mid);
        cv.Line(6, 2, 6, 9, light);
        cv.Line(8, 2, 9, 8, mid);
        cv.Line(10, 2, 12, 6, dark);
        cv.Line(7, 2, 5, 6, dark);
        cv.Line(11, 2, 11, 5, light);
        return cv;
    }

    static PixelCanvas DrawSkull()
    {
        PixelCanvas cv = new PixelCanvas(16, 16);
        Color32 bone = C(222, 214, 188), boneDark = C(170, 160, 134), hole = C(40, 34, 30);
        cv.Line(2, 2, 14, 5, boneDark);
        cv.Line(2, 5, 14, 2, bone);
        cv.Ellipse(8f, 10f, 4f, 3.6f, bone);
        cv.Rect(6, 6, 5, 2, bone);
        cv.Rect(6, 9, 2, 2, hole);
        cv.Rect(9, 9, 2, 2, hole);
        cv.Set(8, 8, hole);
        cv.Set(7, 6, hole);
        cv.Set(9, 6, hole);
        cv.AddOutline(C(60, 52, 44));
        return cv;
    }

    static PixelCanvas DrawGate(bool open)
    {
        System.Random rng = new System.Random(31);
        PixelCanvas cv = new PixelCanvas(32, 16);
        Color32 stone = C(120, 116, 112), wood = C(120, 76, 40), woodDark = C(86, 52, 26);
        Color32 iron = C(64, 66, 74), ironLight = C(118, 122, 132), gold = C(232, 190, 58);

        // Moldura de pedra
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 32; x++)
            {
                bool frame = x < 2 || x > 29 || y > 13;
                if (!frame) continue;
                Color32 c = Sh(stone, rng.Next(-8, 9));
                if ((y > 13 && x % 6 == 0) || (x < 2 && y % 4 == 0) || (x > 29 && y % 4 == 0)) c = C(60, 58, 60);
                cv.Set(x, y, c);
            }

        if (!open)
        {
            cv.Rect(2, 0, 28, 14, wood);
            int[] planks = { 5, 9, 13, 19, 23, 27 };
            foreach (int px in planks) cv.Rect(px, 0, 1, 14, woodDark);
            cv.Rect(15, 0, 2, 14, woodDark);
            cv.Rect(2, 3, 28, 2, iron);
            cv.Rect(2, 10, 28, 2, iron);
            for (int x = 3; x < 30; x += 4) { cv.Set(x, 4, ironLight); cv.Set(x, 11, ironLight); }
            // Cadeado
            cv.Rect(14, 5, 4, 4, gold);
            cv.Rect(15, 9, 2, 1, gold);
            cv.Set(14, 9, gold); cv.Set(17, 9, gold);
            cv.Set(15, 6, C(60, 40, 10)); cv.Set(15, 7, C(60, 40, 10));
        }
        else
        {
            for (int y = 0; y < 14; y++)
            {
                float t = y / 13f;
                Color32 c = C((int)Mathf.Lerp(70, 12, t), (int)Mathf.Lerp(62, 10, t), (int)Mathf.Lerp(40, 14, t));
                for (int x = 2; x < 30; x++) cv.Set(x, y, c);
            }
            cv.Rect(2, 0, 3, 14, wood);
            cv.Rect(27, 0, 3, 14, wood);
            cv.Rect(2, 3, 3, 2, iron); cv.Rect(2, 10, 3, 2, iron);
            cv.Rect(27, 3, 3, 2, iron); cv.Rect(27, 10, 3, 2, iron);
            cv.Rect(8, 0, 16, 1, C(140, 126, 80));
        }
        return cv;
    }

    static PixelCanvas DrawKey()
    {
        PixelCanvas cv = new PixelCanvas(16, 16);
        Color32 gold = C(232, 186, 54), light = C(255, 236, 140), dark = C(170, 118, 30);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float dx = x + 0.5f - 4.5f, dy = y + 0.5f - 10.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                if (r <= 3.4f && r >= 1.5f) cv.Set(x, y, gold);
            }
        cv.Rect(7, 10, 8, 2, gold);
        cv.Rect(7, 10, 8, 1, dark);
        cv.Rect(10, 7, 2, 3, gold);
        cv.Rect(13, 8, 2, 2, gold);
        cv.Set(3, 12, light); cv.Set(4, 13, light); cv.Set(2, 11, light);
        cv.Set(8, 11, light); cv.Set(9, 11, light); cv.Set(10, 11, light);
        cv.AddOutline(C(84, 54, 12));
        return cv;
    }

    static PixelCanvas DrawGlow()
    {
        PixelCanvas cv = new PixelCanvas(32, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                float dx = x + 0.5f - 16f, dy = y + 0.5f - 16f;
                float k = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) / 16f);
                cv.Set(x, y, C(255, 255, 255, (int)(k * k * 255f)));
            }
        return cv;
    }

    /// <param name="dir">0 = baixo, 1 = cima, 2 = lado (direita)</param>
    static PixelCanvas DrawPlayer(int dir)
    {
        PixelCanvas cv = new PixelCanvas(16, 16);
        Color32 skin = C(236, 188, 148), steel = C(178, 184, 196), steelD = C(118, 124, 140);
        Color32 tunic = C(46, 104, 190), tunicD = C(30, 70, 134), belt = C(112, 72, 36);
        Color32 boots = C(76, 50, 30), blade = C(214, 220, 230), hilt = C(222, 182, 64);
        Color32 grip = C(90, 56, 30), eye = C(28, 28, 40), hair = C(96, 62, 34);

        if (dir == 2)
        {
            cv.Rect(6, 1, 2, 2, boots); cv.Rect(9, 1, 2, 2, boots);
            cv.Rect(6, 3, 2, 1, tunicD); cv.Rect(9, 3, 2, 1, tunicD);
            cv.Rect(5, 4, 6, 5, tunic); cv.Rect(5, 4, 1, 5, tunicD); cv.Rect(5, 6, 6, 1, belt);
            cv.Rect(6, 9, 5, 3, skin); cv.Rect(5, 9, 1, 3, hair);
            cv.Rect(5, 12, 6, 3, steel); cv.Rect(6, 15, 4, 1, steel); cv.Rect(5, 12, 6, 1, steelD);
            cv.Set(9, 10, eye); cv.Set(11, 10, skin);
            cv.Rect(9, 6, 2, 2, skin);
            cv.Rect(11, 5, 1, 3, hilt);
            cv.Rect(12, 6, 4, 1, blade);
        }
        else
        {
            cv.Rect(5, 1, 2, 2, boots); cv.Rect(9, 1, 2, 2, boots);
            cv.Rect(5, 3, 2, 1, tunicD); cv.Rect(9, 3, 2, 1, tunicD);
            cv.Rect(4, 4, 8, 5, tunic); cv.Rect(11, 4, 1, 5, tunicD); cv.Rect(4, 6, 8, 1, belt);
            cv.Rect(3, 5, 1, 3, skin); cv.Rect(12, 5, 1, 3, skin);
            if (dir == 0)
            {
                cv.Set(8, 6, hilt);
                cv.Rect(5, 9, 6, 3, skin);
                cv.Rect(4, 12, 8, 3, steel); cv.Rect(5, 15, 6, 1, steel); cv.Rect(4, 12, 8, 1, steelD);
                cv.Set(7, 11, steelD); cv.Set(8, 11, steelD);
                cv.Set(6, 10, eye); cv.Set(9, 10, eye);
                cv.Rect(13, 7, 1, 6, blade); cv.Rect(12, 6, 3, 1, hilt); cv.Set(13, 5, grip);
            }
            else
            {
                cv.Rect(5, 9, 6, 2, hair); cv.Rect(6, 9, 4, 1, skin);
                cv.Rect(4, 11, 8, 4, steel); cv.Rect(5, 15, 6, 1, steel); cv.Rect(4, 11, 8, 1, steelD);
                cv.Rect(7, 12, 2, 4, steelD);
                cv.Rect(2, 7, 1, 6, blade); cv.Rect(1, 6, 3, 1, hilt); cv.Set(2, 5, grip);
            }
        }
        cv.AddOutline(C(24, 22, 30));
        cv.Shadow(8f, 1f, 5f, 1.6f, 80);
        return cv;
    }

    static PixelCanvas DrawEnemy()
    {
        PixelCanvas cv = new PixelCanvas(16, 16);
        Color32 purple = C(116, 52, 146), purpleD = C(76, 32, 98), purpleL = C(158, 96, 186);
        Color32 bone = C(232, 222, 196), eyeC = C(252, 222, 64), mouth = C(40, 10, 30);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float dx = (x + 0.5f - 8f) / 5.6f, dy = (y + 0.5f - 7f) / 5f;
                if (dx * dx + dy * dy <= 1f) cv.Set(x, y, Sh(purple, (int)((dy - dx) * 22f)));
            }
        cv.Ellipse(8f, 6f, 3f, 2.4f, purpleL);
        cv.Rect(4, 1, 2, 2, purpleD); cv.Rect(10, 1, 2, 2, purpleD);
        cv.Rect(2, 5, 1, 3, purpleD); cv.Rect(13, 5, 1, 3, purpleD);
        cv.Set(2, 4, bone); cv.Set(13, 4, bone);
        cv.Set(4, 11, bone); cv.Set(4, 12, bone); cv.Set(3, 13, bone);
        cv.Set(11, 11, bone); cv.Set(11, 12, bone); cv.Set(12, 13, bone);
        cv.Rect(5, 8, 2, 2, eyeC); cv.Rect(9, 8, 2, 2, eyeC);
        cv.Set(6, 8, mouth); cv.Set(9, 8, mouth);
        cv.Rect(5, 4, 6, 2, mouth);
        cv.Set(6, 5, bone); cv.Set(8, 5, bone); cv.Set(10, 5, bone);
        cv.AddOutline(C(28, 10, 34));
        cv.Shadow(8f, 1.2f, 5.5f, 1.6f, 80);
        return cv;
    }

    static PixelCanvas DrawSlash()
    {
        PixelCanvas cv = new PixelCanvas(16, 16);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float dx = x + 0.5f - 2f, dy = y + 0.5f - 8f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                if (Mathf.Abs(ang) > 65f || r < 8f || r > 13.5f) continue;
                float k = 1f - Mathf.Abs(r - 10.8f) / 2.8f;
                if (k <= 0f) continue;
                cv.Set(x, y, C(255, 250, 222, (int)(Mathf.Clamp01(k) * 230f)));
            }
        return cv;
    }

    static PixelCanvas DrawHeart(bool full)
    {
        PixelCanvas cv = new PixelCanvas(16, 16);
        Color32 fill = full ? C(220, 40, 52) : C(70, 64, 64);
        Color32 shade = full ? C(160, 24, 36) : C(52, 48, 48);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                float X = (x + 0.5f - 8f) / 5.6f, Y = (y + 0.5f - 7.6f) / 5.6f;
                float a = X * X + Y * Y - 1f;
                if (a * a * a - X * X * Y * Y * Y <= 0f)
                    cv.Set(x, y, (X - Y) > 0.7f ? shade : fill);
            }
        if (full)
        {
            cv.Set(4, 11, C(255, 140, 140)); cv.Set(5, 12, C(255, 140, 140)); cv.Set(4, 10, C(255, 110, 110));
        }
        cv.AddOutline(full ? C(70, 10, 16) : C(25, 20, 22));
        return cv;
    }

    static PixelCanvas DrawUIPanel()
    {
        System.Random rng = new System.Random(41);
        PixelCanvas cv = new PixelCanvas(32, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
            {
                int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(31 - x, 31 - y));
                Color32 c;
                if (edge == 0) c = C(24, 22, 26);
                else if (edge <= 4)
                {
                    c = C(112 + rng.Next(-8, 9), 106 + rng.Next(-8, 9), 100 + rng.Next(-8, 9));
                    if (edge == 1) c = Sh(c, 22);
                    if (edge == 4) c = Sh(c, -30);
                }
                else c = C(30 + rng.Next(-3, 4), 26 + rng.Next(-3, 4), 30 + rng.Next(-3, 4), 238);
                cv.Set(x, y, c);
            }
        Color32 rivet = C(196, 160, 80);
        cv.Set(2, 2, rivet); cv.Set(29, 2, rivet); cv.Set(2, 29, rivet); cv.Set(29, 29, rivet);
        return cv;
    }

    static PixelCanvas DrawUIButton()
    {
        System.Random rng = new System.Random(43);
        PixelCanvas cv = new PixelCanvas(48, 16);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 48; x++)
            {
                int edge = Mathf.Min(Mathf.Min(x, y), Mathf.Min(47 - x, 15 - y));
                Color32 c;
                if (edge == 0) c = C(40, 24, 12);
                else if (y == 14 || x == 1) c = C(176, 128, 74);
                else if (y == 1 || x == 46) c = C(92, 58, 28);
                else
                {
                    c = C(136 + rng.Next(-6, 7), 90 + rng.Next(-5, 6), 48 + rng.Next(-4, 5));
                    if (y == 5 || y == 10) c = C(110, 72, 38);
                }
                cv.Set(x, y, c);
            }
        return cv;
    }

    // =====================================================================
    // PREFABS
    // =====================================================================

    static GameObject NewSprite(string name, Sprite sprite, Vector3 position, int order, Transform parent = null)
    {
        GameObject go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = position;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        return go;
    }

    static void ConfigureBody(Rigidbody2D rb, PhysicsMaterial2D mat)
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.mass = 1f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        rb.sharedMaterial = mat;
    }

    static GameObject SavePrefab(GameObject go, string name)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabsDir + "/" + name + ".prefab");
        Object.DestroyImmediate(go);
        return prefab;
    }

    static Prefabs BuildPrefabs(Art art, PhysicsMaterial2D mat)
    {
        Prefabs p = new Prefabs();

        // ---------- Jogador ----------
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        ConfigureBody(player.AddComponent<Rigidbody2D>(), mat);
        CircleCollider2D pc = player.AddComponent<CircleCollider2D>();
        pc.radius = 0.36f;
        pc.offset = new Vector2(0f, -0.12f);
        SpriteRenderer pBody = NewSprite("Body", art.playerDown, Vector3.zero, OrderCharacter, player.transform).GetComponent<SpriteRenderer>();
        GameObject slash = NewSprite("Slash", art.slash, Vector3.zero, OrderEffect, player.transform);
        slash.transform.localScale = Vector3.one * 1.3f;
        SpriteRenderer slashSr = slash.GetComponent<SpriteRenderer>();
        slashSr.enabled = false;

        PlayerController controller = player.AddComponent<PlayerController>();
        controller.body = pBody;
        controller.spriteDown = art.playerDown;
        controller.spriteUp = art.playerUp;
        controller.spriteSide = art.playerSide;
        PlayerCombat combat = player.AddComponent<PlayerCombat>();
        combat.slashEffect = slashSr;
        PlayerHealth health = player.AddComponent<PlayerHealth>();
        health.body = pBody;
        p.player = SavePrefab(player, "Player");

        // ---------- Inimigo ----------
        GameObject enemy = new GameObject("Enemy");
        ConfigureBody(enemy.AddComponent<Rigidbody2D>(), mat);
        CircleCollider2D ec = enemy.AddComponent<CircleCollider2D>();
        ec.radius = 0.4f;
        ec.offset = new Vector2(0f, -0.08f);
        SpriteRenderer eBody = NewSprite("Body", art.enemy, Vector3.zero, OrderCharacter - 1, enemy.transform).GetComponent<SpriteRenderer>();
        EnemyController ectrl = enemy.AddComponent<EnemyController>();
        ectrl.body = eBody;
        EnemyHealth ehealth = enemy.AddComponent<EnemyHealth>();
        ehealth.body = eBody;
        p.enemy = SavePrefab(enemy, "Enemy");

        // ---------- Chave ----------
        GameObject key = new GameObject("Key");
        CircleCollider2D kc = key.AddComponent<CircleCollider2D>();
        kc.isTrigger = true;
        kc.radius = 0.55f;
        GameObject glow = NewSprite("Glow", art.glow, Vector3.zero, OrderKey - 1, key.transform);
        SpriteRenderer glowSr = glow.GetComponent<SpriteRenderer>();
        glowSr.color = new Color(1f, 0.85f, 0.3f, 0.55f);
        glow.transform.localScale = Vector3.one * 1.6f;
        GameObject keyVisual = NewSprite("Visual", art.key, Vector3.zero, OrderKey, key.transform);
        KeyItem keyItem = key.AddComponent<KeyItem>();
        keyItem.visual = keyVisual.transform;
        keyItem.glow = glowSr;
        p.key = SavePrefab(key, "Key");

        // ---------- Obstáculos ----------
        GameObject rock = NewSprite("Rock", art.rock, Vector3.zero, OrderObstacle);
        CircleCollider2D rc = rock.AddComponent<CircleCollider2D>();
        rc.radius = 0.42f;
        rc.offset = new Vector2(0f, 0.02f);
        p.rock = SavePrefab(rock, "Rock");

        GameObject crate = NewSprite("Crate", art.crate, Vector3.zero, OrderObstacle);
        crate.AddComponent<BoxCollider2D>().size = new Vector2(0.9f, 0.9f);
        p.crate = SavePrefab(crate, "Crate");

        GameObject pillar = NewSprite("Pillar", art.pillar, Vector3.zero, OrderObstacle);
        pillar.AddComponent<CircleCollider2D>().radius = 0.42f;
        p.pillar = SavePrefab(pillar, "Pillar");

        return p;
    }

    static GameObject Place(GameObject prefab, Vector2 position, Transform parent, string name = null)
    {
        GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (parent != null) go.transform.SetParent(parent, true);
        go.transform.position = new Vector3(position.x, position.y, 0f);
        if (!string.IsNullOrEmpty(name)) go.name = name;
        PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(go);
        return go;
    }

    // =====================================================================
    // CENÁRIO
    // =====================================================================

    static Camera CreateCamera()
    {
        GameObject go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        Camera cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 9f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color32(18, 16, 20, 255);
        go.transform.position = new Vector3(0f, 0f, -10f);
        go.AddComponent<AudioListener>();
        go.AddComponent<CameraFit>();
        return cam;
    }

    static GameObject WallSegment(Transform parent, Sprite sprite, string name, Vector2 center, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = size;
        sr.sortingOrder = OrderWall;
        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.size = size;
        return go;
    }

    /// <summary>
    /// Arena: área interna x ∈ [-14, 14], y ∈ [-7, 7]. Muros de 1 unidade ao redor.
    /// Abertura de 2 unidades no muro superior (x ∈ [-1, 1]) para o portão.
    /// </summary>
    static void BuildFloorAndWalls(Art art, Transform parent)
    {
        NewSprite("Chao", art.floor, Vector3.zero, OrderFloor, parent);
        Transform walls = new GameObject("Muros").transform;
        walls.SetParent(parent, false);
        WallSegment(walls, art.wall, "Muro_Superior_Esq", new Vector2(-8f, 7.5f), new Vector2(14f, 1f));
        WallSegment(walls, art.wall, "Muro_Superior_Dir", new Vector2(8f, 7.5f), new Vector2(14f, 1f));
        WallSegment(walls, art.wall, "Muro_Inferior", new Vector2(0f, -7.5f), new Vector2(30f, 1f));
        WallSegment(walls, art.wall, "Muro_Esquerdo", new Vector2(-14.5f, 0f), new Vector2(1f, 14f));
        WallSegment(walls, art.wall, "Muro_Direito", new Vector2(14.5f, 0f), new Vector2(1f, 14f));
    }

    static void InteriorWallTile(Transform parent, Sprite sprite, Vector2 pos, bool flip)
    {
        GameObject go = NewSprite("ParedeDestruida", sprite, pos, OrderWall, parent);
        go.GetComponent<SpriteRenderer>().flipX = flip;
        go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 1f);
    }

    static void Decor(Transform parent, Sprite sprite, Vector2 pos, float scale = 1f, bool flip = false)
    {
        GameObject go = NewSprite(sprite.name, sprite, pos, OrderDecor, parent);
        go.transform.localScale = Vector3.one * scale;
        go.GetComponent<SpriteRenderer>().flipX = flip;
    }

    static void BuildDecor(Art art, Transform parent)
    {
        Vector2[] bushes = { new Vector2(-13.3f, 6.3f), new Vector2(13.3f, 6.3f), new Vector2(-13.4f, -6.5f),
                             new Vector2(6.5f, -6.4f), new Vector2(-6.5f, 6.4f), new Vector2(9.2f, 6.4f),
                             new Vector2(-2.8f, 6.5f), new Vector2(3.0f, 6.5f) };
        foreach (Vector2 b in bushes) Decor(parent, art.bush, b, 1f, b.x > 0f);

        Vector2[] grass = { new Vector2(-8f, -6.4f), new Vector2(1.5f, -6.6f), new Vector2(10f, -5.8f),
                            new Vector2(-11f, 0.5f), new Vector2(6.2f, 5.8f), new Vector2(-6.8f, -0.8f),
                            new Vector2(12.8f, 3.4f), new Vector2(-1.8f, 2.6f), new Vector2(8.6f, -4.2f),
                            new Vector2(-9.4f, 5.6f) };
        foreach (Vector2 g in grass) Decor(parent, art.grass, g);

        Decor(parent, art.skull, new Vector2(-6.5f, -5.5f));
        Decor(parent, art.skull, new Vector2(8.2f, 4.8f), 1f, true);
        Decor(parent, art.skull, new Vector2(2.2f, 1.8f));

        Decor(parent, art.rock, new Vector2(2f, 5f), 0.5f);
        Decor(parent, art.rock, new Vector2(-9f, -4.6f), 0.5f);
        Decor(parent, art.rock, new Vector2(13.2f, -6.3f), 0.5f);
        Decor(parent, art.rock, new Vector2(-12.2f, -1.8f), 0.45f);
    }

    static ExitGate BuildGate(Art art, Transform parent)
    {
        GameObject gateGo = NewSprite("PortaoSaida", art.gateClosed, new Vector3(0f, 7.5f, 0f), OrderGate, parent);
        BoxCollider2D block = gateGo.AddComponent<BoxCollider2D>();
        block.size = new Vector2(2f, 1f);
        BoxCollider2D trigger = gateGo.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(2.2f, 1.8f);
        trigger.offset = new Vector2(0f, -0.5f);

        GameObject glow = NewSprite("BrilhoDesbloqueio", art.glow, new Vector3(0f, 6.9f, 0f), OrderGate - 1, gateGo.transform);
        glow.transform.localScale = new Vector3(2.8f, 1.4f, 1f);
        SpriteRenderer glowSr = glow.GetComponent<SpriteRenderer>();
        glowSr.color = new Color(1f, 0.85f, 0.3f, 0.5f);

        ExitGate gate = gateGo.AddComponent<ExitGate>();
        gate.gateRenderer = gateGo.GetComponent<SpriteRenderer>();
        gate.closedSprite = art.gateClosed;
        gate.openSprite = art.gateOpen;
        gate.blockingCollider = block;
        gate.unlockGlow = glowSr;
        return gate;
    }

    static void BuildArenaScene(Art art, Prefabs prefabs)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();

        Transform env = new GameObject("Ambiente").transform;
        BuildFloorAndWalls(art, env);
        ExitGate gate = BuildGate(art, env);

        // Paredes parcialmente destruídas no interior
        Transform ruins = new GameObject("Ruinas").transform;
        ruins.SetParent(env, false);
        InteriorWallTile(ruins, art.wall, new Vector2(-10.5f, -2.5f), false);
        InteriorWallTile(ruins, art.wall, new Vector2(-9.5f, -2.5f), false);
        InteriorWallTile(ruins, art.wallBroken, new Vector2(-8.5f, -2.5f), false);
        InteriorWallTile(ruins, art.wall, new Vector2(10.5f, -2.5f), false);
        InteriorWallTile(ruins, art.wall, new Vector2(9.5f, -2.5f), false);
        InteriorWallTile(ruins, art.wallBroken, new Vector2(8.5f, -2.5f), true);
        InteriorWallTile(ruins, art.wall, new Vector2(-5.5f, 4.5f), false);
        InteriorWallTile(ruins, art.wallBroken, new Vector2(-5.5f, 3.5f), false);
        InteriorWallTile(ruins, art.wall, new Vector2(5.5f, 4.5f), false);
        InteriorWallTile(ruins, art.wallBroken, new Vector2(5.5f, 3.5f), true);

        // Obstáculos com colisão
        Transform obstacles = new GameObject("Obstaculos").transform;
        obstacles.SetParent(env, false);
        Vector2[] pillars = { new Vector2(-4.5f, -0.5f), new Vector2(4.5f, -0.5f), new Vector2(-11.5f, 4.5f), new Vector2(11.5f, 4.5f) };
        foreach (Vector2 v in pillars) Place(prefabs.pillar, v, obstacles, "Pilar");
        Vector2[] crates = { new Vector2(-12.5f, -5.5f), new Vector2(-11.5f, -5.5f), new Vector2(-12.5f, -4.5f),
                             new Vector2(12.5f, -5.5f), new Vector2(12.5f, 0.5f), new Vector2(11.5f, 0.5f) };
        foreach (Vector2 v in crates) Place(prefabs.crate, v, obstacles, "Caixa");
        Vector2[] rocks = { new Vector2(-7.5f, 1.5f), new Vector2(7.5f, 1.5f), new Vector2(3.5f, -4.5f),
                            new Vector2(-3.5f, -4.5f), new Vector2(-12.5f, 2.5f), new Vector2(12.5f, -2.5f) };
        foreach (Vector2 v in rocks) Place(prefabs.rock, v, obstacles, "Pedra");

        Transform decor = new GameObject("Decoracao").transform;
        decor.SetParent(env, false);
        BuildDecor(art, decor);

        // Personagens
        Transform chars = new GameObject("Personagens").transform;
        GameObject playerGo = Place(prefabs.player, new Vector2(0f, -5.5f), chars, "Player");
        Place(prefabs.enemy, new Vector2(-9.5f, 3f), chars, "Inimigo_1");
        Place(prefabs.enemy, new Vector2(9.5f, 3f), chars, "Inimigo_2");
        Place(prefabs.enemy, new Vector2(0f, 4f), chars, "Inimigo_3");

        GameObject keyGo = Place(prefabs.key, new Vector2(0f, 0.5f), null, "Chave");

        // Interface
        UIManager ui = BuildGameUI(art);
        CreateEventSystem();

        // Gerenciador do jogo
        GameObject gmGo = new GameObject("GameManager");
        GameManager gm = gmGo.AddComponent<GameManager>();
        gm.ui = ui;
        gm.player = playerGo.GetComponent<PlayerController>();
        gm.playerHealth = playerGo.GetComponent<PlayerHealth>();
        gm.key = keyGo.GetComponent<KeyItem>();
        gm.gate = gate;
        gm.totalEnemies = 3;
        gm.mainMenuScene = "MainMenu";

        EditorSceneManager.SaveScene(scene, ArenaScenePath);
    }

    // =====================================================================
    // INTERFACE (uGUI)
    // =====================================================================

    static Font cachedFont;

    static Font GetFont()
    {
        if (cachedFont != null) return cachedFont;
        string[] names = { "LegacyRuntime.ttf", "Arial.ttf" };
        foreach (string n in names)
        {
            try { cachedFont = Resources.GetBuiltinResource<Font>(n); }
            catch (System.Exception) { cachedFont = null; }
            if (cachedFont != null) break;
        }
        return cachedFont;
    }

    static GameObject NewCanvas(string name, int sortingOrder)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    static void CreateEventSystem()
    {
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
        go.AddComponent<InputSystemUIInputModule>();
#else
        go.AddComponent<StandaloneInputModule>();
#endif
    }

    static RectTransform NewUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.layer = 5;
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static void SetRect(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Text NewText(Transform parent, string name, string content, int size, Color color, TextAnchor align, bool bold)
    {
        RectTransform rt = NewUI(name, parent);
        Text t = rt.gameObject.AddComponent<Text>();
        t.font = GetFont();
        t.text = content;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.supportRichText = true;
        t.raycastTarget = false;
        Outline o = rt.gameObject.AddComponent<Outline>();
        o.effectColor = new Color(0f, 0f, 0f, 0.85f);
        o.effectDistance = new Vector2(2f, -2f);
        return t;
    }

    static Image NewImage(Transform parent, string name, Sprite sprite, Color color)
    {
        RectTransform rt = NewUI(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        return img;
    }

    static Image NewSliced(Transform parent, string name, Sprite sprite, Color color)
    {
        Image img = NewImage(parent, name, sprite, color);
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1f;
        return img;
    }

    static Button NewButton(Transform parent, string name, string label, Sprite sprite, Vector2 pos, Vector2 size)
    {
        Image img = NewSliced(parent, name, sprite, Color.white);
        SetRect(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 0.9f, 0.65f, 1f);
        cb.pressedColor = new Color(0.72f, 0.62f, 0.48f, 1f);
        cb.selectedColor = Color.white;
        b.colors = cb;
        Navigation nav = new Navigation();
        nav.mode = Navigation.Mode.None;
        b.navigation = nav;

        Text t = NewText(img.transform, "Texto", label, 40, new Color(1f, 0.95f, 0.85f), TextAnchor.MiddleCenter, true);
        Stretch(t.rectTransform);
        return b;
    }

    static GameObject NewOverlay(Transform parent, string name, Color color)
    {
        Image img = NewImage(parent, name, null, color);
        Stretch(img.rectTransform);
        img.raycastTarget = true;
        return img.gameObject;
    }

    static RectTransform NewFrame(Transform parent, Sprite panel, Vector2 size)
    {
        Image img = NewSliced(parent, "Moldura", panel, Color.white);
        SetRect(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        return img.rectTransform;
    }

    static GameObject BuildDialog(Transform parent, Art art, string name, Color overlayColor,
                                  string title, Color titleColor, string subtitle,
                                  string label1, string label2, out Button button1, out Button button2)
    {
        GameObject overlay = NewOverlay(parent, name, overlayColor);
        RectTransform frame = NewFrame(overlay.transform, art.uiPanel, new Vector2(980f, 600f));

        Text t = NewText(frame, "Titulo", title, 96, titleColor, TextAnchor.MiddleCenter, true);
        SetRect(t.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(900f, 130f));
        t.GetComponent<Outline>().effectDistance = new Vector2(4f, -4f);

        Text s = NewText(frame, "Subtitulo", subtitle, 40, Color.white, TextAnchor.MiddleCenter, false);
        SetRect(s.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -180f), new Vector2(900f, 100f));

        button1 = NewButton(frame, "Botao_1", label1, art.uiButton, new Vector2(0f, -60f), new Vector2(560f, 96f));
        button2 = NewButton(frame, "Botao_2", label2, art.uiButton, new Vector2(0f, -180f), new Vector2(560f, 96f));

        overlay.SetActive(false);
        return overlay;
    }

    static UIManager BuildGameUI(Art art)
    {
        GameObject canvasGo = NewCanvas("HUD", 10);
        Transform canvas = canvasGo.transform;
        UIManager ui = canvasGo.AddComponent<UIManager>();
        Color gold = new Color(1f, 0.85f, 0.35f);

        // --- Vidas (superior esquerdo) ---
        RectTransform lives = NewUI("Vidas", canvas);
        SetRect(lives, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -4f), new Vector2(520f, 64f));
        Text livesLabel = NewText(lives, "Rotulo", "VIDAS:", 40, Color.white, TextAnchor.MiddleLeft, true);
        SetRect(livesLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(170f, 64f));
        Image[] hearts = new Image[3];
        for (int i = 0; i < 3; i++)
        {
            Image h = NewImage(lives, "Coracao_" + (i + 1), art.heartFull, Color.white);
            SetRect(h.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(170f + i * 62f, 0f), new Vector2(54f, 54f));
            h.preserveAspect = true;
            h.raycastTarget = false;
            hearts[i] = h;
        }

        // --- Inimigos (superior direito) ---
        Text enemies = NewText(canvas, "ContadorInimigos", "INIMIGOS DERROTADOS: 0/3", 40, Color.white, TextAnchor.MiddleRight, true);
        SetRect(enemies.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -4f), new Vector2(800f, 64f));

        // --- Indicador de chave ---
        RectTransform keyInd = NewUI("IndicadorChave", canvas);
        SetRect(keyInd, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -72f), new Vector2(320f, 56f));
        Image keyIcon = NewImage(keyInd, "Icone", art.key, Color.white);
        SetRect(keyIcon.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(52f, 52f));
        keyIcon.preserveAspect = true;
        keyIcon.raycastTarget = false;
        Text keyText = NewText(keyInd, "Texto", "CHAVE", 34, gold, TextAnchor.MiddleRight, true);
        SetRect(keyText.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-62f, 0f), new Vector2(240f, 56f));

        // --- Mensagens (parte inferior) ---
        Image msgBox = NewSliced(canvas, "CaixaMensagem", art.uiPanel, new Color(1f, 1f, 1f, 0.92f));
        msgBox.raycastTarget = false;
        SetRect(msgBox.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(1250f, 84f));
        Text msgText = NewText(msgBox.transform, "Texto", "", 36, gold, TextAnchor.MiddleCenter, true);
        Stretch(msgText.rectTransform);

        // --- Painéis ---
        Button resume, pauseMenu, retry, overMenu, again, winMenu;
        GameObject pause = BuildDialog(canvas, art, "PainelPausa", new Color(0f, 0f, 0f, 0.7f),
            "PAUSADO", new Color(0.95f, 0.85f, 0.55f), "Pressione ESC para continuar.",
            "CONTINUAR", "MENU PRINCIPAL", out resume, out pauseMenu);
        GameObject over = BuildDialog(canvas, art, "PainelDerrota", new Color(0.06f, 0f, 0f, 0.9f),
            "GAME OVER", new Color(0.92f, 0.2f, 0.2f), "VOCÊ NÃO SOBREVIVEU À ARENA!",
            "TENTAR NOVAMENTE", "MENU PRINCIPAL", out retry, out overMenu);
        GameObject win = BuildDialog(canvas, art, "PainelVitoria", new Color(0f, 0f, 0f, 0.75f),
            "VITÓRIA!", new Color(1f, 0.84f, 0.3f), "PARABÉNS! VOCÊ ESCAPOU DA ARENA!",
            "JOGAR NOVAMENTE", "MENU PRINCIPAL", out again, out winMenu);

        ui.hearts = hearts;
        ui.heartFull = art.heartFull;
        ui.heartEmpty = art.heartEmpty;
        ui.enemiesText = enemies;
        ui.keyIndicator = keyInd.gameObject;
        ui.messageBox = msgBox.gameObject;
        ui.messageText = msgText;
        ui.pausePanel = pause;
        ui.gameOverPanel = over;
        ui.victoryPanel = win;
        ui.resumeButton = resume;
        ui.pauseMenuButton = pauseMenu;
        ui.retryButton = retry;
        ui.gameOverMenuButton = overMenu;
        ui.playAgainButton = again;
        ui.victoryMenuButton = winMenu;

        keyInd.gameObject.SetActive(false);
        msgBox.gameObject.SetActive(false);
        return ui;
    }

    // =====================================================================
    // MENU PRINCIPAL
    // =====================================================================

    static void BuildMainMenuScene(Art art)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CreateCamera();

        // Fundo: a própria arena abandonada
        Transform env = new GameObject("Ambiente").transform;
        BuildFloorAndWalls(art, env);
        NewSprite("Portao", art.gateClosed, new Vector3(0f, 7.5f, 0f), OrderGate, env);
        Transform decor = new GameObject("Decoracao").transform;
        decor.SetParent(env, false);
        BuildDecor(art, decor);
        NewSprite("Pilar", art.pillar, new Vector3(-11.5f, 4.5f, 0f), OrderObstacle, env);
        NewSprite("Pilar", art.pillar, new Vector3(11.5f, 4.5f, 0f), OrderObstacle, env);
        NewSprite("Caixa", art.crate, new Vector3(-12.5f, -5.5f, 0f), OrderObstacle, env);
        NewSprite("Caixa", art.crate, new Vector3(12.5f, -5.5f, 0f), OrderObstacle, env);

        GameObject hero = NewSprite("Heroi", art.playerDown, new Vector3(0f, -5.4f, 0f), OrderCharacter, env);
        hero.transform.localScale = Vector3.one * 2f;
        GameObject e1 = NewSprite("Criatura", art.enemy, new Vector3(-5f, -5.2f, 0f), OrderCharacter, env);
        e1.transform.localScale = Vector3.one * 1.8f;
        GameObject e2 = NewSprite("Criatura", art.enemy, new Vector3(5f, -5.2f, 0f), OrderCharacter, env);
        e2.transform.localScale = Vector3.one * 1.8f;

        // Interface
        GameObject canvasGo = NewCanvas("MenuCanvas", 10);
        Transform canvas = canvasGo.transform;
        MainMenu menu = canvasGo.AddComponent<MainMenu>();
        Color gold = new Color(1f, 0.84f, 0.3f);

        NewOverlay(canvas, "Escurecer", new Color(0f, 0f, 0f, 0.5f)).GetComponent<Image>().raycastTarget = false;

        // Painel principal
        RectTransform mainPanel = NewUI("PainelPrincipal", canvas);
        Stretch(mainPanel);

        Text title = NewText(mainPanel, "Titulo", "ARENA OF SURVIVAL", 120, gold, TextAnchor.MiddleCenter, true);
        SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 300f), new Vector2(1600f, 170f));
        Outline titleOutline = title.GetComponent<Outline>();
        titleOutline.effectColor = new Color(0.25f, 0.13f, 0.03f, 1f);
        titleOutline.effectDistance = new Vector2(5f, -5f);
        Shadow titleShadow = title.gameObject.AddComponent<Shadow>();
        titleShadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        titleShadow.effectDistance = new Vector2(8f, -8f);

        Text sub = NewText(mainPanel, "Subtitulo", "Derrote os inimigos. Encontre a chave. Escape.", 38,
            new Color(0.92f, 0.9f, 0.85f), TextAnchor.MiddleCenter, false);
        SetRect(sub.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 195f), new Vector2(1400f, 60f));

        Button play = NewButton(mainPanel, "Botao_Jogar", "JOGAR", art.uiButton, new Vector2(0f, 50f), new Vector2(560f, 100f));
        Button howTo = NewButton(mainPanel, "Botao_ComoJogar", "COMO JOGAR", art.uiButton, new Vector2(0f, -70f), new Vector2(560f, 100f));
        Button quit = NewButton(mainPanel, "Botao_Sair", "SAIR", art.uiButton, new Vector2(0f, -190f), new Vector2(560f, 100f));

        // Painel "Como Jogar"
        GameObject howPanel = NewOverlay(canvas, "PainelComoJogar", new Color(0f, 0f, 0f, 0.6f));
        RectTransform frame = NewFrame(howPanel.transform, art.uiPanel, new Vector2(1150f, 840f));
        Text howTitle = NewText(frame, "Titulo", "COMO JOGAR", 80, gold, TextAnchor.MiddleCenter, true);
        SetRect(howTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(1000f, 110f));
        howTitle.GetComponent<Outline>().effectDistance = new Vector2(4f, -4f);

        string body =
            "<color=#F2C94C><b>OBJETIVO</b></color>\n" +
            "Derrote os três inimigos, encontre a chave e escape pelo portão.\n\n" +
            "<color=#F2C94C><b>CONTROLES</b></color>\n" +
            "WASD — Movimentar\n" +
            "ESPAÇO — Atacar\n" +
            "ESC — Pausar\n\n" +
            "<color=#E8574A><b>CUIDADO!</b></color>\n" +
            "Você possui apenas três vidas.";
        Text howBody = NewText(frame, "Texto", body, 38, Color.white, TextAnchor.UpperLeft, false);
        SetRect(howBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(960f, 520f));
        howBody.lineSpacing = 1.1f;

        Button back = NewButton(frame, "Botao_Voltar", "VOLTAR", art.uiButton, new Vector2(0f, -330f), new Vector2(460f, 96f));
        howPanel.SetActive(false);

        menu.gameSceneName = "Arena";
        menu.mainPanel = mainPanel.gameObject;
        menu.howToPlayPanel = howPanel;
        menu.playButton = play;
        menu.howToPlayButton = howTo;
        menu.quitButton = quit;
        menu.backButton = back;

        CreateEventSystem();
        EditorSceneManager.SaveScene(scene, MenuScenePath);
    }
}

/// <summary>
/// Ao abrir o projeto pela primeira vez, oferece gerar o jogo automaticamente.
/// </summary>
[InitializeOnLoad]
static class ArenaBuilderStartup
{
    static ArenaBuilderStartup()
    {
        EditorApplication.delayCall += Check;
    }

    static void Check()
    {
        if (SessionState.GetBool("ArenaOfSurvival_Asked", false)) return;
        SessionState.SetBool("ArenaOfSurvival_Asked", true);
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists(ArenaBuilder.ArenaScenePath) && File.Exists(ArenaBuilder.MenuScenePath)) return;

        if (EditorUtility.DisplayDialog("Arena of Survival",
                "As cenas do jogo ainda não foram geradas.\nDeseja gerar o jogo completo agora?",
                "Gerar agora", "Depois"))
        {
            ArenaBuilder.BuildAll();
        }
    }
}
