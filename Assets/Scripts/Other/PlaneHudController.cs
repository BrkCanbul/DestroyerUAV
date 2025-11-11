using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlaneHudController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Header("U�ak Referansları")]
    public Rigidbody planeRb;
    public FixedWingController planeController;
    public AircraftWeaponManager weaponManager;

    [Header("HUD Elemanları")]
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI headingText;
    public Image altitudeBar;
    public Image throttleBar;
    public Image horizonLine;
    public TextMeshProUGUI missileText;
    public TextMeshProUGUI ammoText;

    [Header("HUD Ayarları")]
    public float maxAltitude = 1000f;   

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(planeRb==null||planeController==null)
            return;

        // --- HIZ ---
        float speedKmh = planeRb.linearVelocity.magnitude*3.6f;
        speedText.text=$"{speedKmh:0}";

        // --- Y�N ---
        float heading = planeRb.transform.eulerAngles.y;
        headingText.text=$"{heading:0}°";

        // --- �RT�FA ---
        float altitude = Mathf.Clamp(planeRb.transform.position.y,0,maxAltitude);
        altitudeBar.fillAmount=altitude/maxAltitude;

        // --- THROTTLE ---
        throttleBar.fillAmount=planeController.GetThrust;

        // --- CEPhane ---
        missileText.text=$"{weaponManager.BombCount}";
        ammoText.text=$"{weaponManager.AmmoCount}";
    }
}
