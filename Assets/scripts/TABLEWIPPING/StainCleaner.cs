using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;
using XCharts.Runtime;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class StainWipe2D : MonoBehaviour
{
    public static StainWipe2D Instance;
    private float brushSize;

    private SpriteRenderer sr;
    private Texture2D runtimeTex;
    private Collider2D col;
    public float totalStainPixels;
    public float erasedPixels;
    public bool isCompleted;
    public float totalpixels;
    
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

        Sprite sprite = sr.sprite;
        Texture2D source = sprite.texture;
        Rect r = sprite.textureRect;

        runtimeTex = new Texture2D(
            (int)r.width,
            (int)r.height,
            TextureFormat.RGBA32,
            false
        );

        Color[] pixels = source.GetPixels(
            (int)r.x,
            (int)r.y,
            (int)r.width,
            (int)r.height
        );

        runtimeTex.SetPixels(pixels);
        runtimeTex.Apply();
      
        brushSize = TWGameController.Instance.scrubSize;
        InitializeStain();
       
     
        sr.sprite = Sprite.Create(
            runtimeTex,
            new Rect(0, 0, runtimeTex.width, runtimeTex.height),
            new Vector2(0.5f, 0.5f),
            sprite.pixelsPerUnit
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
        Debug.Log($"{TWPlayer.instance.zEndPoint}/{TWPlayer.instance.yEndPoint}");
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        TWGameController.Instance.SetPlayerOut();
        Debug.Log($"{TWPlayer.instance.zEndPoint}/{TWPlayer.instance.yEndPoint}");
    }
    void InitializeStain()
    {
        totalStainPixels = 0;
        erasedPixels = 0;
        isCompleted = false;

        Color[] pixels = runtimeTex.GetPixels();
        totalpixels = pixels.Length;    
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a > 0.01f)
                totalStainPixels++;
        }
        GetTotalStainArea_M2();
    }
    public float GetTotalStainArea_M2()
    {
        float Width, Hight,area;
        Width = sr.bounds.size.x;
        Hight = sr.bounds.size.y;

        area = (Width * Hight);

        float ppu = sr.sprite.pixelsPerUnit;

        float metersPerPixel = 1f / ppu;
        float areaPerPixel = metersPerPixel * metersPerPixel;

        float baseArea = totalStainPixels * areaPerPixel;
        float baseAreaT = totalpixels * areaPerPixel;

        Vector3 scale = transform.lossyScale;

        float scaledArea = baseArea * scale.x * scale.y;
        float scaledAreaT = baseAreaT*scale.x * scale.y;

        float brusharea =( math.PI * (brushSize * brushSize))*areaPerPixel; // area of the brush
       
        Debug.Log($"Base Area: {baseArea} m2 {baseAreaT}m2 | Scaled Area: {scaledArea}m2 {scaledAreaT}m2 | TotalPixels : {totalStainPixels},{totalpixels}| brush area {brusharea}{brushSize}");
        UnityAreaToRobotArea(scaledArea,area,brusharea,scaledAreaT);
        return scaledArea;
    }
    public float UnityAreaToRobotArea(float unityAreap, float unityarea , float brusharea ,float unityAreaT)
    {
        float ppu = sr.sprite.pixelsPerUnit;

        float metersPerPixel = 1f / ppu;
        float areaPerPixel = metersPerPixel * metersPerPixel;

        float unityWidth = MarsGameDefs.TableWiping.RIGHTLIMIT - MarsGameDefs.TableWiping.LEFTLIMIT; 
        float unityHeight = MarsGameDefs.TableWiping.TOPLIMIT - MarsGameDefs.TableWiping.BOTTOMLIMIT;  

        float robotWidth = TWPlayer.instance.zEndPointMax - TWPlayer.instance.zEndPointMin;          
        float robotHeight = TWPlayer.instance.yEndPointMax - TWPlayer.instance.yEndPointMin;          

        float areaScale = (robotWidth / unityWidth) *
                          (robotHeight / unityHeight);
       
        Debug.Log($"robot Area brush {brusharea * areaScale}");
        Debug.Log($"Robot Area bound {unityarea * areaScale}");
        Debug.Log($"Robot Area stainpixel {unityAreap * areaScale}");
        Debug.Log($"Robot Area Totalpixel {unityAreaT * areaScale}");

        float brushAreaReal_m2 = 0.00001f; // 0.000025f → 0.0001f
        Debug.Log($"real world brush to unity{brushAreaReal_m2 / areaScale}");
        Debug.Log($"real worlt to pixels{math.sqrt((brushAreaReal_m2 / areaScale)/areaPerPixel)}");

        return unityAreap * areaScale;
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
     
        for (int i = -(int)brushSize; i <= brushSize; i++)
        {
            for (int j = -(int)brushSize; j <= brushSize; j++)
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
