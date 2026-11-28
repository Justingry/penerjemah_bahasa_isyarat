using UnityEngine;

public class PerformanceHUD : MonoBehaviour
{
    [Header("Pengaturan HUD")]
    public bool showHUD = true;
    public KeyCode toggleKey = KeyCode.F1;

    [Header("Anggaran Frame")]
    [Tooltip("Target frame rate acuan untuk menghitung anggaran.")]
    [Range(30f, 120f)] public float targetFps = 90f;

    // --- Variabel Internal ---
    private float _frameTimeHalus;
    private float _akumulasi;
    private int _hitunganFrame;
    private float _frameTimeTercatat;

    private int _totalTriangles = 0;
    private float _nextTriangleUpdate = 0f;
    public float triangleUpdateInterval = 0.5f;

    void Start()
    {
        // Matikan VSync agar frame time mencerminkan beban sebenarnya
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;

        CalculateTotalTriangles(); // Hitung awal
    }

    void Update()
    {
        // 1. Hitung Frame Time (Gaya PerformanceLab)
        _akumulasi += Time.unscaledDeltaTime;
        _hitunganFrame++;

        if (_akumulasi >= 0.25f)
        {
            _frameTimeTercatat = (_akumulasi / _hitunganFrame) * 1000f;
            _akumulasi = 0f;
            _hitunganFrame = 0;
        }
        _frameTimeHalus = Mathf.Lerp(_frameTimeHalus, _frameTimeTercatat, Time.unscaledDeltaTime * 5f);

        // 2. Toggle HUD
        if (Input.GetKeyDown(toggleKey))
        {
            showHUD = !showHUD;
        }

        // 3. Hitung Triangle secara berkala
        if (Time.time >= _nextTriangleUpdate)
        {
            CalculateTotalTriangles();
            _nextTriangleUpdate = Time.time + triangleUpdateInterval;
        }
    }

    void CalculateTotalTriangles()
    {
        _totalTriangles = 0;
        MeshFilter[] allMeshFilters = FindObjectsOfType<MeshFilter>();

        foreach (MeshFilter mf in allMeshFilters)
        {
            if (mf.gameObject.activeInHierarchy && mf.sharedMesh != null)
            {
                _totalTriangles += mf.sharedMesh.triangles.Length / 3;
            }
        }
    }

    void OnGUI()
    {
        // Jika HUD disembunyikan, tampilkan teks kecil di pojok
        if (!showHUD)
        {
            var styleKecil = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true };
            GUI.Label(new Rect(10, 10, 400, 20), 
                $"<color=#aaaaaa>[{toggleKey}] Tampilkan Performance HUD</color>", styleKecil);
            return;
        }

        // --- Hitung Metrik ---
        float anggaran = 1000f / targetFps;
        bool lewat = _frameTimeHalus > anggaran;
        float fpsSetara = 1000f / Mathf.Max(_frameTimeHalus, 0.01f);

        string warna = lewat ? "#ff6b6b" : "#00d1b2";
        string vonis = lewat ? "MELEWATI ANGGARAN" : "di dalam anggaran";

        // --- Susun Teks Panel ---
        // (Menggunakan gaya teks persis seperti void OnGUI yang Anda kirim)
        string teks =
            $"<b>PERFORMANCE HUD</b>\n" +
            $"Frame time : <color={warna}><b>{_frameTimeHalus:F2} ms</b></color>   " +
            $"(anggaran {anggaran:F2} ms pada {targetFps:F0} Hz)\n" +
            $"Status     : <color={warna}>{vonis}</color>   ·   setara {fpsSetara:F0} fps\n" +
            $"\n" +
            $"Total Triangles   : <b>{_totalTriangles:N0}</b>\n" +
            $"Status Toggle     : <color={(showHUD ? "#00d1b2>AKTIF" : "mati")}</color>   ({toggleKey})\n" +
            $"\n" +
            $"<color=#aaaaaa>Tekan <b>{toggleKey}</b> untuk menyembunyikan panel ini.</color>";

        // Box Utama (Sama persis ukurannya dengan void OnGUI)
        var st = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 15,
            richText = true
        };
        GUI.Box(new Rect(10, 10, 600, 170), teks, st);

        // --- Bar Anggaran ---
        float lebar = 580f;
        float rasio = Mathf.Clamp01(_frameTimeHalus / (anggaran * 2f));

        // Background bar
        GUI.Box(new Rect(20, 188, lebar, 20), "");
        // Isi bar (Hijau/Merah)
        GUI.color = lewat ? new Color(1f, 0.42f, 0.42f) : new Color(0f, 0.82f, 0.70f);
        GUI.Box(new Rect(20, 188, lebar * rasio, 20), "");
        GUI.color = Color.white;
        // Penanda garis anggaran di tengah
        GUI.Box(new Rect(20 + lebar * 0.5f - 1, 184, 2, 28), "");

        // --- Label Keterangan Bawah ---
        var kecil = new GUIStyle(GUI.skin.label) { fontSize = 12 };
        GUI.Label(new Rect(20, 214, 760, 20),
            $"<color=#aaaaaa>Garis tengah = batas anggaran   ·   Triangles diperbarui tiap {triangleUpdateInterval:F1} detik</color>", kecil);
    }
}