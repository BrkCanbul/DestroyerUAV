using UnityEngine;

public class EngineSim : MonoBehaviour
{

    public FixedWingController fixedWingController;
    public GameObject propeller;
    [SerializeField]
    public float RotationMultiplier = 1000f;
    void Start()
    {
        if(fixedWingController == null)
        {
            Debug.LogError("[EngineSim] FixedWingController component is not assigned!");
            return;
        }
        if(propeller == null)
        {
            Debug.LogError("[EngineSim] Propeller GameObject is not assigned!");
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        turnPropellerByThrust();
    }

    private void turnPropellerByThrust() {
        if(fixedWingController == null || propeller == null)
            return;
        float thrust = fixedWingController.GetThrust; // Sabit itme kuvveti (%)
        float rotationSpeed = thrust * RotationMultiplier; // Ýtme kuvvetine baðlý dönüþ hýzý
        propeller.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);


    }
}
