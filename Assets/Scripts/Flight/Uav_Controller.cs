using System;
using System.Runtime.CompilerServices;
using UnityEngine;

// Basit sabit kanat aerodinamiği: Lift, Drag, Thrust, Stall, Kontrol yüzeylerinden tork
[RequireComponent(typeof(Rigidbody))]
public class FixedWingController:MonoBehaviour {

   
    [Tooltip("Hava yoğunluğu (kg/m^3)"), Range(0,100f)]
    public float AirDensity = 1.225f; // kg/m^3 (deniz seviyesinde)

    [Tooltip("Kanat alanı (m^2)"), Range(0.1f,100f)]
    public float WingArea = 0.55f; // m^2 (Cessna 172 için yaklaşık)

    [Header("Drag")]
    public float DragCoeff = 0.045f; // Cd0 gibi düşün, küçük bir değer
    public float ReferenceArea = 0.55f; // Kanat alanı ile aynı olabilir

    public float GetThrust {
        get => thrustInput;
    }

    [Tooltip("Lift katsayısı eğrisi (Cl)"), SerializeField]
    public AnimationCurve Cl = new AnimationCurve(
    new Keyframe(-10f,-0.4f),
    new Keyframe(-5f,-0.2f),
    new Keyframe(0f,0.0f),
    new Keyframe(5f,0.5f),
    new Keyframe(10f,0.9f),
    new Keyframe(12f,1.1f),
    new Keyframe(14f,1.2f),
    new Keyframe(16f,1.0f),
    new Keyframe(18f,0.6f),
    new Keyframe(20f,0.2f),
    new Keyframe(25f,0.0f)
    );



    public float EnginePower = 25f; // Sabit itme kuvveti (N)

    [Tooltip("Motor konumu")]
    public Transform EnginePosition;

    [Tooltip("Lift Konumu")]
    public Transform LiftPosition;


    [Header("Kontroller")]
    public KeyCode throttleUp = KeyCode.LeftShift;
    public KeyCode throttleDown = KeyCode.LeftControl;
    public KeyCode yawLeft = KeyCode.Q;
    public KeyCode yawRight = KeyCode.E;
    public string pitchAxis = "Vertical";
    public string rollAxis = "Horizontal";
    public bool isCatapultLaunch = false;
    public KeyCode launchKey = KeyCode.Space;

    [Header("Kontrol Torkları")]
    [Tooltip("Pitch (X ekseni) için maksimum tork")]
    public float MaxPitchTorque = 5000f;

    [Tooltip("Roll (Z ekseni) için maksimum tork")]
    public float MaxRollTorque = 4000f;

    [Tooltip("Yaw (Y ekseni) için maksimum tork")]
    public float MaxYawTorque = 2000f;

    [Header("Damping (opsiyonel)")]
    public float PitchDamping = 200f;
    public float RollDamping = 150f;
    public float YawDamping = 100f;

    private float throttleRate = 2.0f;// Gaz artış/azalış hızı (saniye başına)
    private float AoA;
    private float AoAYaw;
    private Vector3 Velocity;
    private Vector3 LocalVelocity;
    private Vector3 LocalAngularVelocity;

    private float lift;
    private float pitchInput;
    private float rollInput;
    private float yawInput;
    private float thrustInput;




    private Rigidbody rb;

    void Awake() {
        rb=GetComponent<Rigidbody>();
        if(EnginePosition==null)
            Debug.LogError("[FixedWingController] EnginePosition atanmadı!");
        if(LiftPosition==null)
            Debug.LogError("[FixedWingController] LiftPosition atanmadı!");
    }

    void Update() {
    }

    private void ApplyDrag() {
        // Dünya uzayında hız
        Vector3 v = Velocity;
        float speed = v.magnitude;
        if(speed<0.1f)
            return;

        float q = 0.5f*AirDensity*speed*speed; // dinamik basınç
        float dragMagnitude = q*DragCoeff*WingArea;

        // Drag her zaman hızın ters yönünde
        Vector3 dragForce = -v.normalized*dragMagnitude;

        rb.AddForce(dragForce);

        Debug.DrawRay(transform.position,dragForce*0.01f,Color.blue);
    }

    private void ReadInputs() {
        // Unity Input Manager'daki default eksenler:
        // "Vertical" = W/S veya ↑/↓  → Pitch
        // "Horizontal" = A/D veya ←/→ → Roll
        pitchInput=Input.GetAxis("Vertical");   // çekince (W) pozitif → burun yukarı
        rollInput=Input.GetAxis("Horizontal"); // sağa roll pozitif

        // Unity'de default "Yaw" ekseni yok → Q/E ile basit bir okuma yapalım
        yawInput=0f;
        if(Input.GetKey(KeyCode.Q))
            yawInput=-1f; // sola rudder
        if(Input.GetKey(KeyCode.E))
            yawInput=1f; // sağa rudder

        if(Input.GetKey(throttleUp))
            thrustInput=Mathf.Clamp01(thrustInput+throttleRate*Time.deltaTime);
        if(Input.GetKey(throttleDown))
            thrustInput=Mathf.Clamp01(thrustInput-throttleRate*Time.deltaTime);
        if(Input.GetKeyDown(launchKey)) {
            LaunchIfLanded();
        }
    }

    private void LaunchIfLanded() {
        if(rb.linearVelocity.magnitude>5.5f) {
            return;
        }
        if(isCatapultLaunch&&Input.GetKeyDown(launchKey)) {
            
            rb.AddForce(transform.forward*EnginePower*5f,ForceMode.Impulse);
        }
    }
    void FixedUpdate() {
        ReadInputs();
        CalculateState();
        ApplyForces();
        ApplyControlTorques();
        ApplyDrag();
        Debug.DrawRay(LiftPosition.position,LiftPosition.up*lift*0.1f,Color.green);
        Debug.DrawRay(EnginePosition.position,-EnginePosition.forward*EnginePower*0.1f,Color.red);
    }
    
    private void ApplyForces() {
        rb.AddForceAtPosition(EnginePosition.forward*(EnginePower*thrustInput),EnginePosition.position);
        lift=CalculateLift();
        rb.AddForceAtPosition(LiftPosition.up*lift,LiftPosition.position);

    }
    private float CalculateLift() {
        float speed = LocalVelocity.magnitude;
        float q = 0.5f*AirDensity*speed*speed;
        return Cl.Evaluate(AoA)*q*WingArea;
    }

    //private void CalculateAoA() {
    //    AoA=Mathf.Atan2(-LocalVelocity.y,LocalVelocity.z)*Mathf.Rad2Deg;
    //    AoAYaw=Mathf.Atan2(LocalVelocity.x,LocalVelocity.z)*Mathf.Rad2Deg;

    //}
    private void CalculateAoA() {
        Vector3 v = rb.linearVelocity;
        if(v.sqrMagnitude<0.01f) {
            AoA=0f;
            AoAYaw=0f;
            return;
        }

        Vector3 vDir = v.normalized;

        // Pitch AoA: hız vektörünü sağ/sol ekseni (transform.right) üzerine dik projekte ediyoruz,
        // böylece sadece up-forward düzleminde kalan bileşenle açı hesaplıyoruz.
        Vector3 vPitchPlane = Vector3.ProjectOnPlane(vDir,transform.right);
        AoA=Vector3.SignedAngle(transform.forward,vPitchPlane,transform.right);

        // Yaw (sideslip) açısı: bu sefer up ekseni etrafında
        Vector3 vYawPlane = Vector3.ProjectOnPlane(vDir,transform.up);
        AoAYaw=Vector3.SignedAngle(transform.forward,vYawPlane,transform.up);
    }


    private void CalculateState() {
        var inverseRotation = Quaternion.Inverse(rb.rotation);
        Velocity=rb.linearVelocity;
        LocalVelocity=inverseRotation*Velocity;
        LocalAngularVelocity=inverseRotation*rb.angularVelocity;
        CalculateAoA();

    }
    private void ApplyControlTorques() {
        Vector3 controlTorque = new Vector3(
            pitchInput*MaxPitchTorque,     // X: pitch
            yawInput*MaxYawTorque,       // Y: yaw
           -rollInput*MaxRollTorque       // Z: roll (sağa roll = negatif Z ile uyumlu olsun diye - işareti)
        );

        rb.AddRelativeTorque(controlTorque,ForceMode.Force);

        Vector3 angVelLocal = transform.InverseTransformDirection(rb.angularVelocity);
        Vector3 dampingTorque = new Vector3(
            -angVelLocal.x*PitchDamping,
            -angVelLocal.y*YawDamping,
            -angVelLocal.z*RollDamping
        );

        rb.AddRelativeTorque(dampingTorque,ForceMode.Force);
    }
}
