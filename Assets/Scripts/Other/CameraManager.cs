using UnityEditor.Rendering;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [Tooltip("Oyuncu kameraları")]
    public Camera[] Cameras;
    [Header("Başlangıç Kamerası")]
    public int StartingCameraIndex = 0;
    private int _currentCameraIndex = -1;



    [Header("Kamera Değiştirme Tuşu")]
    public KeyCode camSwitchKey = KeyCode.C;
    void Start()
    {
        if(Cameras==null||Cameras.Length==0) {
            Debug.LogError("[CameraManager ]: Kamera atanmadı!");
            return;
        }
        // Hepsini kapat, sadece startIndex'i aç
        for(int i = 0; i<Cameras.Length; i++) {
            bool active = (i==StartingCameraIndex);
            Cameras[i].gameObject.SetActive(active);

            // AudioListener varsa, sadece aktif kameradakini aç
            var listener = Cameras[i].GetComponent<AudioListener>();
            if(listener!=null)
                listener.enabled=active;
        }
        _currentCameraIndex= Mathf.Clamp(StartingCameraIndex, 0, Cameras.Length - 1);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(camSwitchKey)) {
            SwitchToNextCamera();
        }

    }

    public void SwitchToNextCamera() {
        if(Cameras==null||Cameras.Length==0)
            return;

        // Mevcut kamerayı kapat
        SetCameraActive(_currentCameraIndex,false);

        // Sonraki index’e geç (0 → 1 → 2 → 0 → ...)
        _currentCameraIndex=(_currentCameraIndex+1)%Cameras.Length;

        // Yeni kamerayı aç
        SetCameraActive(_currentCameraIndex,true);

        Debug.Log("Aktif kamera: "+Cameras[_currentCameraIndex].name);
    }

    private void SetCameraActive(int index,bool active) {
        if(index<0||index>=Cameras.Length)
            return;

        Cameras[index].gameObject.SetActive(active);

        var listener = Cameras[index].GetComponent<AudioListener>();
        if(listener!=null)
            listener.enabled=active;
    }
}
