using System.Collections;
using UnityEngine;

public class EngineSoundController:MonoBehaviour {
    [Header("Throttle Kaynaðý")]
    public FixedWingController flight;
    // Eðer FixedWingController kullanýyorsan yukarýyý deðiþtir:
    // public FixedWingController flight;

    [Header("Sesler")]
    public AudioSource startSource;  // motor ilk çalýþma sesi (loop = false)
    public AudioSource loopSource;   // motor sürekli sesi (loop = true)

    [Header("Throttle Eþik Deðerleri")]
    [Tooltip("Bu deðerin üstüne çýkýnca motor çalýþmaya baþlar")]
    public float throttleOnThreshold = 0.05f;

    [Tooltip("Bu deðerin altýna inince motor durur")]
    public float throttleOffThreshold = 0.02f;

    [Header("Loop Ses Ayarlarý")]
    public float minPitch = 0.9f;
    public float maxPitch = 1.8f;

    public float minVolume = 0.15f;
    public float maxVolume = 0.9f;

    [Tooltip("Pitch/volume yumuþak geçiþ süresi")]
    public float smoothSpeed = 5f;

    private bool engineRunning = false;
    private Coroutine startRoutine;

    void Update() {
        if(flight==null)
            return;

        float t = Mathf.Clamp01(flight.GetThrust);

        // Motoru baþlat
        if(!engineRunning&&t>throttleOnThreshold) {
            if(startRoutine!=null)
                StopCoroutine(startRoutine);

            startRoutine=StartCoroutine(StartEngine());
        }
        // Motoru durdur
        else if(engineRunning&&t<throttleOffThreshold) {
            StopEngine();
        }

        // Motor çalýþýyorsa loop sesini throttle'a göre ayarla
        if(engineRunning&&loopSource!=null&&loopSource.isPlaying) {
            float targetPitch = Mathf.Lerp(minPitch,maxPitch,t);
            float targetVolume = Mathf.Lerp(minVolume,maxVolume,t);

            loopSource.pitch=Mathf.Lerp(loopSource.pitch,targetPitch,Time.deltaTime*smoothSpeed);
            loopSource.volume=Mathf.Lerp(loopSource.volume,targetVolume,Time.deltaTime*smoothSpeed);
        }
    }

    private IEnumerator StartEngine() {
        engineRunning=true;

        // 1) Start sesini çal
        if(startSource!=null&&startSource.clip!=null) {
            startSource.Stop();
            startSource.Play();

            // Start sesi bitene kadar bekle
            yield return new WaitForSeconds(startSource.clip.length);
        }

        // 2) Loop sesini baþlat
        if(loopSource!=null&&loopSource.clip!=null) {
            if(!loopSource.isPlaying) {
                loopSource.volume=0f; // yumuþak baþlayabilsin
                loopSource.Play();
            }
        }
    }

    private void StopEngine() {
        engineRunning=false;

        // Loop sesini durdur (istersen burada fade out da yapabiliriz)
        if(loopSource!=null&&loopSource.isPlaying) {
            loopSource.Stop();
        }

        // Start sesi hâlâ çalýyorsa kes
        if(startSource!=null&&startSource.isPlaying) {
            startSource.Stop();
        }
    }
}
