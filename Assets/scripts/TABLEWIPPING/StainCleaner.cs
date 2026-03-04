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
    float unityUnitPerPixel;
    float areaPerPixel;
    public float totalAreaPixels;
    public float erasedAreaPixels;
    public float areaPixels;
    int eraseCount =0;
    private bool hasErasedThisEntry = false;
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
        float ppu = sr.sprite.pixelsPerUnit;
        unityUnitPerPixel = 1f / ppu;
        areaPerPixel = unityUnitPerPixel * unityUnitPerPixel;
        GetTotalStainArea_M2();

    }

    Vector2 lastPos;

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.name != "Cloth") return;

        Vector2 clothPos = other.bounds.center;

        if (Vector2.Distance(clothPos, lastPos) > 0.01f) // threshold in Unity units
        {
            Erase(clothPos);
            lastPos = clothPos;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        hasErasedThisEntry = false;
        TWGameController.Instance.SetPlayerIn();
        Debug.Log($"{TWPlayer.instance.zEndPoint}/{TWPlayer.instance.yEndPoint}robotarea_end_points enter");
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        TWGameController.Instance.SetPlayerOut();
        Debug.Log($"{TWPlayer.instance.zEndPoint}/{TWPlayer.instance.yEndPoint}robotarea_end_points exit");
        eraseCount = 0;
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
       
    }
    public float GetTotalStainArea_M2()
    {
        float Width, Hight, area;
        Width = sr.bounds.size.x;
        Hight = sr.bounds.size.y;
        Debug.Log($"{Hight}hight,{Width}width");
        area = (Width * Hight);
       
      
        float baseArea = totalStainPixels * areaPerPixel;
        float baseAreaT = totalpixels * areaPerPixel;

        Vector3 scale = transform.lossyScale;

        float scaledArea = baseArea * scale.x * scale.y;
        float scaledAreaT = baseAreaT*scale.x * scale.y;

        Debug.Log($"Base Area: {baseArea}uu {baseAreaT}uu | Scaled Area: {scaledArea}uu {scaledAreaT}uu | TotalPixels : {totalStainPixels},{totalpixels}");
        UnityAreaToRobotArea(scaledArea,area,scaledAreaT);
        return scaledArea;
    }
    public float UnityAreaToRobotArea(float unityAreap, float unityarea ,float unityAreaT)
    {
        float ppu = sr.sprite.pixelsPerUnit;

        float unityUnitPerPixels = 1f / ppu;
        float areaPerPixel = unityUnitPerPixels * unityUnitPerPixels;

        float unityWidth = MarsGameDefs.TableWiping.RIGHTLIMIT - MarsGameDefs.TableWiping.LEFTLIMIT; 
        float unityHeight = MarsGameDefs.TableWiping.TOPLIMIT - MarsGameDefs.TableWiping.BOTTOMLIMIT;  

        float robotWidth = TWPlayer.instance.zEndPointMax - TWPlayer.instance.zEndPointMin;          
        float robotHeight = TWPlayer.instance.yEndPointMax - TWPlayer.instance.yEndPointMin;          

        float areaScale = (robotWidth / unityWidth) *
                          (robotHeight / unityHeight);
        totalAreaPixels = unityAreap * areaScale;
        Debug.Log($"Robot Area bound {unityarea * areaScale}");
        Debug.Log($"Robot Area stainpixel {unityAreap * areaScale}");
        Debug.Log($"Robot Area Totalpixel {unityAreaT * areaScale}");


        return unityAreap * areaScale;
    }
    public void erasedUnityAreaToRobotArea(float er)
    {
        float ppu = sr.sprite.pixelsPerUnit;

        float unityUnitPerPixels = 1f / ppu;
        float areaPerPixel = unityUnitPerPixels * unityUnitPerPixels;

        float unityWidth = MarsGameDefs.TableWiping.RIGHTLIMIT - MarsGameDefs.TableWiping.LEFTLIMIT;
        float unityHeight = MarsGameDefs.TableWiping.TOPLIMIT - MarsGameDefs.TableWiping.BOTTOMLIMIT;

        float robotWidth = TWPlayer.instance.zEndPointMax - TWPlayer.instance.zEndPointMin;
        float robotHeight = TWPlayer.instance.yEndPointMax - TWPlayer.instance.yEndPointMin;

        float areaScale = (robotWidth / unityWidth) *
                          (robotHeight / unityHeight);

        erasedAreaPixels = er * areaScale;
        Debug.Log($"erased Aread Pixel{er * areaScale}");

    }
    void Erase(Vector2 worldPos)
    {
        
        // In Erase() function, add at the top:
        Debug.Log($"Erase called! Count: {++eraseCount}, brushRadius: {brushSize}");

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
                if (c.a > 0.01f)
                {
                    c.a = 0f;
                    tex.SetPixel(px, py, c);
                    erasedPixels++;
                }
            }
        }
     
        tex.Apply();
      
        Vector3 scale = transform.lossyScale;
        float area = erasedPixels * areaPerPixel;
        erasedUnityAreaToRobotArea( area * scale.x * scale.y);
    }

}
