using UnityEngine;

public class BombImpactPredictor:MonoBehaviour {
    [Header("References")]
    public Transform bombSpawn;        // Bombanın bırakıldığı nokta
    public Rigidbody planeRb;          // Uçağın Rigidbody'si
    public LayerMask groundMask;       // Zemin layer'ı
    public Transform impactMarker;     // Dünyadaki gösterge objesi

    [Header("Prediction Settings")]
    public float simulationStep = 0.02f;
    public float maxSimulationTime = 5f;
    public float bombDrag = 0f;
    public float bombMass = 1f;
    public float markerHeightOffset = 0.2f; // yere biraz yukarıdan koymak için

    private Vector3 impactPoint;
    private Vector3 impactNormal = Vector3.up;
    private bool hasValidImpactPoint = false;

    void Update() {
        PredictImpactPoint();
        UpdateImpactMarker();
    }

    void PredictImpactPoint() {
        hasValidImpactPoint=false;

        if(bombSpawn==null||planeRb==null)
            return;

        Vector3 pos = bombSpawn.position;
        Vector3 vel = planeRb.linearVelocity;   // bombanın ilk hızı

        float t = 0f;

        while(t<maxSimulationTime) {
            vel+=Physics.gravity*simulationStep;
            vel-=vel*bombDrag*simulationStep*(1f/Mathf.Max(bombMass,0.0001f));

            Vector3 nextPos = pos+vel*simulationStep;

            // pos → nextPos arasında çarpışma var mı
            if(Physics.Raycast(pos,nextPos-pos,out RaycastHit hit,(nextPos-pos).magnitude,groundMask)) {
                impactPoint=hit.point;
                impactNormal = hit.normal;
                hasValidImpactPoint=true;
                return;
            }

            pos=nextPos;
            t+=simulationStep;
        }
    }
   
    void UpdateImpactMarker() {
        if(impactMarker==null)
            return;
        if(planeRb.transform.position.y<10) {
            impactMarker.gameObject.SetActive(false);
            return;
        }
        if(planeRb.linearVelocity.y>=0) {
            impactMarker.gameObject.SetActive(false);
            return;
        }
        if(!hasValidImpactPoint) {
            impactMarker.gameObject.SetActive(false);
            return;
        }

        impactMarker.gameObject.SetActive(true);

        // Pozisyon
        impactMarker.position=impactPoint+impactNormal*markerHeightOffset;

        // Normale yapıştır (marker'ın "up" ekseni zemine dik olsun)
        impactMarker.rotation=Quaternion.FromToRotation(Vector3.up,impactNormal);
        // İstersen kameraya baksın (billboard)
        //if(Camera.main!=null) {
        //    Vector3 lookPos = Camera.main.transform.position;
        //    lookPos.y=impactMarker.position.y; // düz dursun, devrilmesin
        //    impactMarker.LookAt(lookPos);
        //}
    }
}
