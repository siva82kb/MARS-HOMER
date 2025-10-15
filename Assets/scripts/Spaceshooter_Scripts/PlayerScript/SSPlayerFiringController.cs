
using UnityEngine;

public class SSPlayerFiringController : MonoBehaviour
{
    public static SSPlayerFiringController instance;

    [SerializeField]
    private GameObject Player_bullet;

    private AudioSource audioSource;

    public bool isInitialized { get; private set; } = false;

    [SerializeField]
    private Transform Spawn_point;
    
    // Timer to track intervals between shots
    private float timeSinceLastShot = 0f;
    private Vector3 prevEpPos;
    private Vector3 currVel;
  

    private void Awake() => instance = this;

    public void Initialize()
    {
        // Get the audio source component
        audioSource = GetComponent<AudioSource>();
        // Set previous position.
        prevEpPos = MarsComm.epPosInThePlane;
        // Compute endpoint velocity.
        currVel = Vector3.Lerp((MarsComm.epPosInThePlane - prevEpPos) / Time.deltaTime, currVel, 0.8f);
        isInitialized = true;
    }

    public void FixedUpdate()
    {
        // Nothing to do if not initialized
        if (!isInitialized) return;

        // Check if shooting is to be done.
        bool fireFlag = SpaceShooterGameContoller.Instance == null
            || SpaceShooterGameContoller.Instance.isGameFinished
            || !SpaceShooterGameContoller.Instance.isGameStarted;
        // Stop shooting when the game is over
        if (fireFlag) return;

        // Game still running.
        // Compute the current velocity of the endpoint.
        // Compute endpoint velocity.
        currVel = Vector3.Lerp((MarsComm.epPosInThePlane - prevEpPos) / Time.deltaTime, currVel, 0.8f);
        // Update previous position.
        prevEpPos = MarsComm.epPosInThePlane;

        // Shooting is faster when slow, slow when fast.
        bool _slow = 100f * currVel.magnitude < MarsGameDefs.Spaceshooter.LOW_SPEED_THRESHOLD;
        timeSinceLastShot += _slow ? 1.25f * Time.deltaTime : 0.25f * Time.deltaTime;
        // Is it time to fire?
        if (timeSinceLastShot >= MarsGameDefs.Spaceshooter.FIRING_INTERVAL)
        {
            Destroy(Instantiate(Player_bullet, Spawn_point.position, Quaternion.identity), 1.5f);
            // Reset timer after shooting
            timeSinceLastShot = 0f;
        }   
    }
}
