using UnityEngine;

public class PongPlayerController : MonoBehaviour
{
    public static PongPlayerController instance;

	public float speed = 10;
    static float topBound = 3.6F;
	static float bottomBound = -3.6F;
    
    Vector2 direction;
    public static float playSize;
   
    public static int[] DEPENDENT = new int[] {0,-1,1};
    public MarsArom currRom;
    public GameObject player;

    public float yMax, yMin,unityValY;

    void Start ()
    {
        playSize = topBound - bottomBound;  
        currRom = AppData.Instance.selectedMovement.currentArom;
        yMin = currRom.bottomAdjusted.y;
        yMax = currRom.topAdjusted.y;
    }

    // Update is called once per frame
    void Update ()
    {
        updatePlayerPosition();    
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        pongGameController.Instance.BallHitted();
    }
    
    public void updatePlayerPosition()
    {
        if (Mathf.Abs(MarsComm.angle1) > (Mathf.Abs(MovementSceneHandler.initialAngle) - 20))
        {
            unityValY = Angle2ScreenZ(MarsComm.epPosInThePlane.y, yMin, yMax);
            this.transform.position = new Vector2(this.transform.position.x, unityValY);
            if (transform.position.y > topBound)
            {
                transform.position = new Vector3(transform.position.x, topBound, 0);
            }
            else if (transform.position.y < bottomBound)
            {
                transform.position = new Vector3(transform.position.x, bottomBound, 0);
            }
            player.GetComponent<SpriteRenderer>().color = new Color32(0, 255, 0, 255);
        }
        else
        {
            player.GetComponent<SpriteRenderer>().color = new Color32(255, 255, 0, 255);
        }
    }

    public static float Angle2ScreenZ(float angley, float y_Min, float y_Max)
    {
        return Mathf.Clamp(bottomBound + (angley - y_Min) * (playSize / (y_Max - y_Min)), -3.6f * playSize, 3.6f * playSize);
    }
}
