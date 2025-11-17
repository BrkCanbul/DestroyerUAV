using UnityEngine;

public class AircraftWeaponManager : MonoBehaviour
{

    public ControlMode controlMode = ControlMode.PlayerControl;
    [Header("Bomb Settings")]
    public Transform BombSpawnLocation;
    public GameObject BombPrefab;
    public int BombCount = 5;
    public int dropBombCooldown = 2;



    [Header("Gun Settings")]
    public AudioSource ShotSound;
    public GameObject ammoPrefab;
    public Transform shootPoint;
    public int shootsPerMinute = 500;
    public int AmmoCount = 400;
    
    public float shootForce = 500f;




    [Header("Input Settings")]
    public KeyCode DropBombKey = KeyCode.B;
    public KeyCode ShootKey = KeyCode.Mouse0;

    private bool canShoot = true;
    private bool canDropBomb = true;
    private float SecondBetweenShots {
        get =>1f/((shootsPerMinute)/60f);
    }
    private float shootTimer = 0f;
    private float BombDropTimer = 0;

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if(controlMode != ControlMode.PlayerControl)
            return;

        canShoot=shootTimer<=0f&&AmmoCount>=0;
        canDropBomb= BombDropTimer<=0&&BombCount>0;
        if(Input.GetKeyDown(DropBombKey))
        {
            DropBomb();
        }
        if(Input.GetKey(ShootKey))
        {
            Shot();
        }

        shootTimer = shootTimer<0 ? shootTimer: shootTimer - Time.deltaTime;
        BombDropTimer = BombDropTimer<0 ? BombDropTimer: BombDropTimer - Time.deltaTime;

    }

    private void DropBomb()
    {
        if(!canDropBomb)
            return;
        BombDropTimer = dropBombCooldown;
        BombCount--;
        var instantiatedObject = Instantiate(BombPrefab, BombSpawnLocation.position, BombSpawnLocation.rotation);
        if(instantiatedObject.GetComponent<Rigidbody>() != null && GetComponent<Rigidbody>() != null)
            instantiatedObject.GetComponent<Rigidbody>().linearVelocity = GetComponent<Rigidbody>().linearVelocity;
    }
    private void Shot() {
        if(canShoot) {
            AmmoCount--;
            ShotSound.PlayOneShot(ShotSound.clip);
            shootTimer = SecondBetweenShots;
            var instantiatedObject = Instantiate(ammoPrefab, shootPoint.position, shootPoint.rotation);
            var ammoRb = instantiatedObject.GetComponent<Rigidbody>();
            if(ammoRb != null) {
                ammoRb.AddForce(shootPoint.forward * shootForce, ForceMode.Impulse);
            }
        }
        
    }
}
