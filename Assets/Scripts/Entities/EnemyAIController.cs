using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions.Must;

[RequireComponent(typeof(FixedWingController))]
public class EnemyAI : MonoBehaviour {
    [SerializeField]
    public AIStateMachine stateMachine = new AIStateMachine();

    

    [Header("Flight Settings")]
    [Tooltip("Başlangıç hızı")]
    public float initialSpeed = 50f;
    public float baseThrust = 0.7f;

    [Header("Patrol Settings ")]
    public Transform[] PatrolLocations;


    [Header("Chase Settings")]
    public float maxVisionDistance = 300f;
    public float fieldOfView = 90f;
    public float minEnemyAltitudeToSee = 50f;
    public float chaseDistance = 50f;
    public Transform playerTransform;


    [Header("Energy / Dogfight Settings")]
    public float stallSpeed = 60f;          // m/s, uçağına göre ayarla
    public float cruiseSpeed = 120f;        // normal seyir hızı
    public float chaseDesiredDistance = 300f;
    public float lagDistance = 100f;        // yakınken hedefi biraz geriye kaydırma
    public float energyPitchDown = -5f;     // hız kazanmak için burun eğme açısı

    [Header("Turn Rate Limits")]
    public float bankSlewRateDeg = 60f;     // saniyede max roll değişimi
    public float pitchSlewRateDeg = 40f;    // saniyede max pitch değişimi



    [Header("controller settings")]
    public PID rollPid = new PID {
        Kp=1.5f,
        Ki=0.01f,
        Kd=2.0f,
    };
    public PID pitchPid = new PID {
        Kp=1.2f,
        Ki=0.01f,
        Kd=1.5f,
    };
    public PID yawPid = new PID {
        Kp=0.5f,
        Ki=0.0f,
        Kd=0.2f,
    };

    public float maxBankAngle = 45f;
    public float maxPitchAngle = 20f;
    public float targetReachThreshold = 10f;
    public float desiredAltitude = 100f;
    public float yawFromRollFactor = 0.5f;
    public float minSlipSpeed = 5f;


    // references
    private FixedWingController controller;
    private Rigidbody rb;
    private Transform targetTransform;

    // getters
    private float CurrentBankAngle => transform.rotation.eulerAngles.z >180f ? transform.rotation.eulerAngles.z - 360f : transform.rotation.eulerAngles.z;
    private float CurrentPitchAngle => transform.rotation.eulerAngles.x >180f ? transform.rotation.eulerAngles.x - 360f : transform.rotation.eulerAngles.x;
    private float CurrentYawAngle => transform.rotation.eulerAngles.y >180f ? transform.rotation.eulerAngles.y - 360f : transform.rotation.eulerAngles.y;
    private float CurrentSpeed => rb!=null ? rb.linearVelocity.magnitude : 0f;


    public void OnDied() {
        stateMachine.TransitionTo(AIStateTransition.DIED);
        baseThrust=0f;
        controller.SetInputs(0f,0f,0f,0f);
    }
    private void SetInıtialSpeed() {
        
        if(rb !=null) {
            rb.linearVelocity = transform.forward * initialSpeed;
        }

        controller.SetInputs(0f, 0f, 0f,baseThrust);

    }
    private void Awake() {
        controller = GetComponent<FixedWingController>();
        rb = GetComponent<Rigidbody>();

    }

    void Start() {
        SetInıtialSpeed();
        if(PatrolLocations.Length >0) {
            targetTransform = PatrolLocations[0];

        }

    }

    
    void Update() {
        switch(stateMachine.currentState) {
            
            case AIState.PATROL:
                UpdatePatrol();
                break;
            case AIState.CHASE:
                UpdateChase();
                break;
            case AIState.RETURN_TO_BASE:
                UpdateRTB();
                break;
            case AIState.DEAD:
                return;

        }
        HandleVision();
    }

    private void HandleVision() {
        if(playerTransform.position.y<minEnemyAltitudeToSee) {
            if(stateMachine.currentState==AIState.CHASE) {
                stateMachine.TransitionTo(AIStateTransition.LOSE_PLAYER);
                Debug.Log("PLAYER LOST DUE TO LOW ALTITUDE");
                SwitchTargets(PatrolLocations[0]);

            }

            return;
        }
        if(Vector3.Distance(transform.position,playerTransform.position)>maxVisionDistance 
            &&stateMachine.currentState==AIState.CHASE) {
            stateMachine.TransitionTo(AIStateTransition.LOSE_PLAYER);
            Debug.Log("PLAYER LOST");
            SwitchTargets(PatrolLocations[0]);
            return;
        }


        // FoV Based Vision Check
        //Vector3 directionToEnemy = (playerTransform.position-transform.position).normalized;
        //float angleToEnemy = Vector3.Angle(transform.forward,directionToEnemy);
        //if(angleToEnemy<fieldOfView/2f&&stateMachine.currentState!=AIState.CHASE) {
        //    stateMachine.TransitionTo(AIStateTransition.SEE_PLAYER);
        //    SwitchTargets(playerTransform);
        //}
        // Distance Based Vision Check
        if(Vector3.Distance(transform.position,playerTransform.position)<maxVisionDistance

            &&stateMachine.currentState!=AIState.CHASE) {

            stateMachine.TransitionTo(AIStateTransition.SEE_PLAYER);
            Debug.Log("PLAYER SPOTTED" + Time.time);
            SwitchTargets(playerTransform);
        }
    }



    private void UpdateChase() {
        DogFightChase();

    }

    private void DogFightChase() {
        if(playerTransform==null)
            return;

        float dt = Time.deltaTime;
        float speed = CurrentSpeed;
        float stallMargin = stallSpeed*1.1f;

        // 1) Enerji modu
        if(speed<stallMargin) {
            ApplyAttitudeCommand(energyPitchDown,0f,0f,dt);
            return;
        }

        // 2) Hedef noktasını belirle (lag pursuit vs pure pursuit)
        Vector3 toPlayer = playerTransform.position-transform.position;
        float distance = toPlayer.magnitude;

        Vector3 targetPoint = playerTransform.position;
        if(distance<chaseDesiredDistance) {
            // Lag pursuit → oyuncunun arkasına bak
            targetPoint=playerTransform.position-playerTransform.forward*lagDistance;
        }

        Vector3 localTarget = transform.InverseTransformPoint(targetPoint);

        float yawErrorDeg = Mathf.Atan2(localTarget.x,localTarget.z)*Mathf.Rad2Deg;
        float pitchErrDeg = Mathf.Atan2(localTarget.y,localTarget.z)*Mathf.Rad2Deg;

        // 3) Hıza göre dinamik limitler
        float speed01 = Mathf.InverseLerp(stallMargin,cruiseSpeed,speed);
        float dynMaxBank = Mathf.Lerp(20f,maxBankAngle,speed01);
        float dynMaxPitch = Mathf.Lerp(10f,maxPitchAngle,speed01);

        float targetPitch = -Mathf.Clamp(pitchErrDeg,-dynMaxPitch,dynMaxPitch);
        float desiredBank = -Mathf.Clamp(yawErrorDeg,-dynMaxBank,dynMaxBank);

        // 4) İnsan gibi dönüş hızı (slew)
        float limitedBank = Mathf.MoveTowards(CurrentBankAngle,desiredBank,bankSlewRateDeg*dt);
        float limitedPitch = Mathf.MoveTowards(CurrentPitchAngle,targetPitch,pitchSlewRateDeg*dt);

        // 5) Son komut → helper
        ApplyAttitudeCommand(limitedPitch,limitedBank,yawErrorDeg,dt);

        Debug.DrawLine(transform.position,targetPoint,Color.red);
    }

    private void ApplyAttitudeCommand(float targetPitchDeg,float targetBankDeg,float yawErrorDeg,float dt) {

        float bankError = targetBankDeg-CurrentBankAngle;
        float pitchError = targetPitchDeg-CurrentPitchAngle;

        float pitchInput = pitchPid.Calculate(pitchError,dt);
        float rollInput = rollPid.Calculate(bankError,dt);

        float yawFromRoll = rollInput*yawFromRollFactor;
        float yawInput = yawPid.Calculate(yawErrorDeg,dt)*0.3f+yawFromRoll;

        controller.SetInputs(pitchInput,-rollInput,yawInput,baseThrust);


    }
    private void UpdateRTB() {
        if(Vector3.Distance(transform.position, targetTransform.position) < targetReachThreshold) {
            stateMachine.TransitionTo(AIStateTransition.REACHED_BASE);
            SwitchTargets(PatrolLocations[0]);
        }

    }
    private void UpdatePatrol() {
        if(PatrolLocations.Length == 0)
            return;
        if(Vector3.Distance(transform.position, targetTransform.position) < targetReachThreshold) {

            int currentIndex = System.Array.IndexOf(PatrolLocations,targetTransform);
            int nextIndex = (currentIndex+1)%PatrolLocations.Length;
            SwitchTargets(PatrolLocations[nextIndex]);
        }

        HeadTowardsTarget();
    }
    private void SwitchTargets(Transform newTarget) {
        Debug.Log("Switching Targets to " + newTarget.name);
        targetTransform=newTarget;
        rollPid.Reset();
        pitchPid.Reset();
        yawPid.Reset();
    }

    private void HeadTowardsTarget() {
        // get local target position;
        Vector3 localTarget = transform.InverseTransformPoint(targetTransform.position);
        // calculate errors
        float yawError = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg ;
        float pitchError = Mathf.Atan2(localTarget.y, localTarget.z) * Mathf.Rad2Deg;
        pitchError = -Mathf.Clamp(pitchError, -maxPitchAngle, maxPitchAngle);

        float targetBankAngle = -Mathf.Clamp(yawError,-maxBankAngle,maxBankAngle);

        // calculate bank error
        float bankError = targetBankAngle - CurrentBankAngle;
        ApplyAttitudeCommand(pitchError,targetBankAngle,yawError,Time.deltaTime);
        Debug.DrawLine(transform.position, targetTransform.position, Color.red);
    }
#if UNITY_EDITOR
    private void OnDrawGizmos() {
        UnityEditor.Handles.Label(transform.position+Vector3.up*5f,$"State: {stateMachine.currentState}\n"+
            $"Pitch: {CurrentPitchAngle:F1}°\n"+
            $"Yaw: {CurrentYawAngle:F1}°\n"+
            $"Bank: {CurrentBankAngle:F1}°\n" +
            $"Distance to Target: {(targetTransform != null ? Vector3.Distance(transform.position, targetTransform.position) : 0f):F1} m\n" +
            $"Distance to Player: {(targetTransform != null ? Vector3.Distance(transform.position, playerTransform.position) : 0f):F1} m"
            );
    }
#endif
}






[System.Serializable]
public class AIStateMachine {

    public AIState currentState = AIState.PATROL;

    public void TransitionTo(AIStateTransition t) {
        Debug.Log($"Transitioning from {currentState} with trigger {t}");
        switch(currentState) {

            case AIState.PATROL:
                HandlePatrol(t);
                break;

            case AIState.CHASE:
                HandleChase(t);
                break;

            case AIState.RETURN_TO_BASE:
                HandleReturnToBase(t);
                break;
            case AIState.DEAD:
                // Do nothing
                break;
        }
    }



    private void HandlePatrol(AIStateTransition t) {
        if(t==AIStateTransition.SEE_PLAYER)
            currentState=AIState.CHASE;
        else if(t==AIStateTransition.LOW_HEALTH)
            currentState=AIState.RETURN_TO_BASE;
        else if(t==AIStateTransition.LOSE_PLAYER)
            currentState=AIState.PATROL;
        else if(t==AIStateTransition.DIED)
            currentState=AIState.DEAD;
    }

    private void HandleChase(AIStateTransition t) {
        if(t==AIStateTransition.LOW_HEALTH)
            currentState=AIState.RETURN_TO_BASE;
        else if(t==AIStateTransition.LOSE_PLAYER)
            currentState=AIState.PATROL;
        else if(t==AIStateTransition.DIED)
            currentState=AIState.DEAD;
    }

    private void HandleReturnToBase(AIStateTransition t) {
        if(t==AIStateTransition.REACHED_BASE)
            currentState=AIState.PATROL;
        else if(t==AIStateTransition.SEE_PLAYER)
            currentState=AIState.CHASE;
        else if(t==AIStateTransition.DIED)
            currentState=AIState.DEAD;
    }
}



[System.Serializable]
public class PID {
    public float Kp;
    public float Ki;
    public float Kd;

    public float outputMin = -1f;
    public float outputMax = 1f;
    public float integralMin = -0.5f;
    public float integralMax = 0.5f;

    private float p;
    private float i;
    private float d;
    private float previousError;
    private bool hasPrevError = false;

    public float Calculate(float error,float deltaTime) {
        if(deltaTime<=0f)
            return 0f;

        p=Kp*error;

        i+=Ki*error*deltaTime;
        i=Mathf.Clamp(i,integralMin,integralMax);

        if(hasPrevError) {
            d=Kd*(error-previousError)/deltaTime;
        }
        else {
            d=0f;
            hasPrevError=true;
        }

        previousError=error;

        float output = p+i+d;
        return Mathf.Clamp(output,outputMin,outputMax);
    }

    public float Reset() {
        p=i=d=0f;
        previousError=0f;
        hasPrevError=false;
        return 0f;
    }
}
