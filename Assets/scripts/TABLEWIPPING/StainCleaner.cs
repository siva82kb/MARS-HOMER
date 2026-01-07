using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class StainWipe2D : MonoBehaviour
{
    public static StainWipe2D Instance;
    public int brushSize = 20;

    private SpriteRenderer sr;
    private Texture2D runtimeTex;
    private Collider2D col;
    public float totalStainPixels;
    public float erasedPixels;
    public bool isCompleted;
    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        // Auto-add collider if missing
        if (col == null)
            col = gameObject.AddComponent<BoxCollider2D>();

        Texture2D source = sr.sprite.texture;

        runtimeTex = new Texture2D(
            source.width,
            source.height,
            TextureFormat.RGBA32,
            false
        );
      
        runtimeTex.SetPixels32(source.GetPixels32());
        runtimeTex.Apply();
        InitializeStain();
        sr.sprite = Sprite.Create(
            runtimeTex,
            new Rect(0, 0, runtimeTex.width, runtimeTex.height),
            new Vector2(0.5f, 0.5f),
            sr.sprite.pixelsPerUnit
        );
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.name != "Cloth") return;
        
        Vector2 clothPos = other.bounds.center;
        Erase(clothPos);

    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        TWGameController.Instance.SetPlayerIn();
       
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        TWGameController.Instance.SetPlayerOut();
    }
    void InitializeStain()
    {
        totalStainPixels = 0;
        erasedPixels = 0;
        isCompleted = false;

        Color[] pixels = runtimeTex.GetPixels();
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a > 0f)
                totalStainPixels++;
        }
    }
    void Erase(Vector2 worldPos)
    {
        
        Sprite sprite = sr.sprite;
        Texture2D tex = runtimeTex;

        float ppu = sprite.pixelsPerUnit;
        Rect texRect = sprite.textureRect;

        // Convert world position → local
        Vector2 localPos = transform.InverseTransformPoint(worldPos);

        // Convert local → pixel
        int x = Mathf.RoundToInt(texRect.x + (localPos.x * ppu) + texRect.width * 0.5f);
        int y = Mathf.RoundToInt(texRect.y + (localPos.y * ppu) + texRect.height * 0.5f);
     
      

        for (int i = -brushSize; i <= brushSize; i++)
        {
            for (int j = -brushSize; j <= brushSize; j++)
            {
                int px = x + i;
                int py = y + j;

                if (px < 0 || py < 0 || px >= tex.width || py >= tex.height)
                    continue;

                // Circle brush
                if (i * i + j * j > brushSize * brushSize)
                    continue;

                Color c = tex.GetPixel(px, py);
                if (c.a > 0f)
                {
                    c.a = 0f;
                    tex.SetPixel(px, py, c);
                    erasedPixels++;
                }
            }
        }

        tex.Apply();
    }

}
