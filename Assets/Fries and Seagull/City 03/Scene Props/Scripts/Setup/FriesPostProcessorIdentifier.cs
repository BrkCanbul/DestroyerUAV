using UnityEngine;

namespace Seagull.City_03.SceneProps.Setup {
    public class FriesPostProcessorIdentifier : MonoBehaviour {

        private FriesManagerBRP friesManagerBrp;
        
        private void Start() {
            friesManagerBrp = GameObject.Find("Fries Props Manager").GetComponent<FriesManagerBRP>();
            gameObject.name = "Post Process Volume (Fries)";
            gameObject.layer = friesManagerBrp.postProcessLayer;
        }

        private void FixedUpdate() {
            gameObject.name = "Post Process Volume (Fries)";
            if (gameObject.layer != friesManagerBrp.postProcessLayer) gameObject.layer = friesManagerBrp.postProcessLayer;
        }
    }
}