using UnityEngine;

[RequireComponent(typeof(Transform)),RequireComponent(typeof(Camera))]
public class CamFollower:MonoBehaviour {
    [Tooltip("Hedef Transform")]
    public Transform target;
    private Camera _camera; // Alan adýný deðiþtirerek gizlemeyi önledik
    void Start() {
        if(target==null) {
            Debug.LogError("Hedef Transform atanmadý!");
            enabled=false;
            return;
        }
        if(TryGetComponent(out Camera cam)) {
            _camera=cam;
        }
        else {
            Debug.LogError("Camera bileþeni bulunamadý!");
            enabled=false;
            return;
        }

    }

    // Update is called once per frame
    void Update() {
        _camera.transform.LookAt(target);
    }
}
