using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(FixedWingController))]
public class EnemyAI:MonoBehaviour {

    [SerializeField]
    public AIStateMachine stateMachine = new AIStateMachine();

    [Header("Flight Settings")]
    [Tooltip("Başlangıç hızı")]
    public float initialSpeed = 50f;
    public float baseThrust = 0.7f;

    [Header("Patrol Settings")]
    public Transform[] PatrolLocations;

    [Header("Chase Settings")]
    public float maxVisionDistance = 300f;
    public float fieldOfView = 90f;
    public float minEnemyAltitudeToSee = 50f;
    public float chaseDistance = 50f;
    public Transform playerTransform;

    [Header("Combat Tactics")]
    [Range(0f,1f)]
    [Tooltip("0 = Kolay (aptal), 1 = Zor (ace pilot)")]
    public float difficulty = 0.5f;

    public float leadPursuitTime = 1.5f; // Gelecek pozisyon tahmini
    public float gunRange = 200f;
    public float optimalRange = 150f;
    public float minSafeSpeed = 30f; // Bu hızın altında defensive
    public float maxSafeAoA = 12f; // Bu AoA'nın üstünde limit
    public float lagPursuitOffset = 80f; // Lag pursuit mesafesi
    public float reactionDelay = 0.2f; // Tepki gecikmesi

    [Header("Controller Settings")]
    public PID rollPid = new PID {
        Kp=0.8f,
        Ki=0.0f,
        Kd=1.0f,
    };
    public PID pitchPid = new PID {
        Kp=0.8f,
        Ki=0.0f,
        Kd=1.0f,
    };
    public PID yawPid = new PID {
        Kp=0.3f,
        Ki=0.0f,
        Kd=0.1f,
    };

    public float maxBankAngle = 45f;
    public float maxPitchAngle = 20f;
    public float targetReachThreshold = 10f;
    public float desiredAltitude = 100f;
    public float yawFromRollFactor = 0.5f;
    public float minSlipSpeed = 5f;

    // References
    private FixedWingController controller;
    private Rigidbody rb;
    private Rigidbody playerRb;
    private Transform targetTransform;

    // Combat state
    private ChaseMode currentChaseMode = ChaseMode.PURE_PURSUIT;
    private float lastModeChangeTime;
    private float nextReactionTime;

    // Getters
    private float CurrentBankAngle => getCurrentBankAngle();
    private float CurrentPitchAngle => getCurrentPitchAngle();
    private float CurrentYawAngle => getCurrentYawAngle();

    private enum ChaseMode {
        PURE_PURSUIT,    // Direkt kovalama (uzak)
        LEAD_PURSUIT,    // Öngörülü takip (orta)
        LAG_PURSUIT,     // Geri kalarak takip (çok yakın)
        DEFENSIVE        // Enerji kazanma (tehlikeli)
    }

    private float getCurrentBankAngle() {
        Vector3 localUp = transform.InverseTransformDirection(Vector3.up);
        float bank = Mathf.Atan2(localUp.x,localUp.y)*Mathf.Rad2Deg;
        return bank;
    }

    private float getCurrentPitchAngle() {
        Vector3 forward = transform.forward;
        Vector3 flatForward = Vector3.ProjectOnPlane(forward,Vector3.up).normalized;
        float pitch = Vector3.SignedAngle(flatForward,forward,transform.right);
        return pitch;
    }

    private float getCurrentYawAngle() {
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized;
        float yaw = Vector3.SignedAngle(Vector3.forward,flatForward,Vector3.up);
        return yaw;
    }

    public void OnDied() {
        stateMachine.TransitionTo(AIStateTransition.DIED);
        baseThrust=0f;
        controller.SetInputs(0f,0f,0f,0f);
    }

    private void SetInıtialSpeed() {
        if(rb!=null) {
            rb.linearVelocity=transform.forward*initialSpeed;
        }
        controller.SetInputs(0f,0f,0f,baseThrust);
    }

    private void Awake() {
        controller=GetComponent<FixedWingController>();
        rb=GetComponent<Rigidbody>();
    }

    void Start() {
        SetInıtialSpeed();
        if(PatrolLocations.Length>0) {
            targetTransform=PatrolLocations[0];
        }
        if(playerTransform!=null) {
            playerRb=playerTransform.GetComponent<Rigidbody>();
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

        float distanceToPlayer = Vector3.Distance(transform.position,playerTransform.position);

        if(distanceToPlayer>maxVisionDistance&&stateMachine.currentState==AIState.CHASE) {
            stateMachine.TransitionTo(AIStateTransition.LOSE_PLAYER);
            Debug.Log("PLAYER LOST");
            SwitchTargets(PatrolLocations[0]);
            return;
        }

        if(distanceToPlayer<maxVisionDistance&&stateMachine.currentState!=AIState.CHASE) {
            stateMachine.TransitionTo(AIStateTransition.SEE_PLAYER);
            Debug.Log("PLAYER SPOTTED "+Time.time);
            SwitchTargets(playerTransform);
        }
    }

    // ==================== COMBAT LOGIC ====================

    private Vector3 CalculateLeadTarget() {
        if(playerRb==null)
            return playerTransform.position;

        Vector3 playerVelocity = playerRb.linearVelocity;
        float distanceToPlayer = Vector3.Distance(transform.position,playerTransform.position);

        // Zorluk: Düşük difficulty = daha az lead
        float difficultyMultiplier = Mathf.Lerp(0.2f,1.5f,difficulty);
        float adjustedLeadTime = leadPursuitTime*difficultyMultiplier*Mathf.Clamp01(distanceToPlayer/300f);

        // Düşük zorlukta hata ekle
        Vector3 error = Vector3.zero;
        if(difficulty<0.7f) {
            float errorAmount = (1f-difficulty)*20f; // Kolay modda 20m hata
            error=new Vector3(
                Random.Range(-errorAmount,errorAmount),
                Random.Range(-errorAmount*0.5f,errorAmount*0.5f),
                Random.Range(-errorAmount,errorAmount)
            );
        }

        Vector3 predictedPosition = playerTransform.position+(playerVelocity*adjustedLeadTime)+error;
        return predictedPosition;
    }

    private float GetEnergyState() {
        float currentSpeed = rb.linearVelocity.magnitude;
        float speedRatio = Mathf.Clamp01(currentSpeed/minSafeSpeed);

        // AoA kontrolü
        float currentAoA = Mathf.Abs(controller.GetAoA);
        float aoaRatio = 1f-Mathf.Clamp01(currentAoA/maxSafeAoA);

        return speedRatio*aoaRatio; // 0-1 arası
    }

    private float GetRelativeSpeed() {
        if(playerRb==null)
            return 0f;

        Vector3 toPlayer = (playerTransform.position-transform.position).normalized;
        float mySpeed = Vector3.Dot(rb.linearVelocity,toPlayer);
        float playerSpeed = Vector3.Dot(playerRb.linearVelocity,toPlayer);

        return mySpeed-playerSpeed; // Pozitif = yaklaşıyoruz
    }

    private void UpdateChaseMode() {
        // Tepki gecikmesi - düşük zorlukta yavaş tepki
        if(Time.time<nextReactionTime) {
            return; // Henüz tepki verme
        }

        float distanceToPlayer = Vector3.Distance(transform.position,playerTransform.position);
        float energyState = GetEnergyState();
        float relativeSpeed = GetRelativeSpeed();

        ChaseMode newMode = currentChaseMode;

        // Zorluk bazlı eşikler
        float defensiveThreshold = Mathf.Lerp(0.7f,0.4f,difficulty); // Kolay: 0.7, Zor: 0.4
        float lagDistanceThreshold = Mathf.Lerp(80f,120f,difficulty); // Kolay: 80m, Zor: 120m

        // Defensive - Enerji düşükse
        if(energyState<defensiveThreshold) {
            newMode=ChaseMode.DEFENSIVE;
            baseThrust=1f;
        }
        // Lag Pursuit - Çok yakın ve hızlıysa
        else if(distanceToPlayer<lagDistanceThreshold&&relativeSpeed>10f&&difficulty>0.3f) {
            // Düşük zorlukta lag pursuit yok
            newMode=ChaseMode.LAG_PURSUIT;
            baseThrust=0.8f;
        }
        // Lead Pursuit - İdeal menzilde
        else if(distanceToPlayer<300f&&distanceToPlayer>100f&&difficulty>0.2f) {
            // Çok kolay modda lead pursuit yok
            newMode=ChaseMode.LEAD_PURSUIT;
            baseThrust=0.85f;
        }
        // Pure Pursuit - Uzakta veya kolay mod
        else {
            newMode=ChaseMode.PURE_PURSUIT;
            baseThrust=0.9f;
        }

        // Mode değiştiyse tepki gecikmesi ekle
        if(newMode!=currentChaseMode) {
            currentChaseMode=newMode;
            lastModeChangeTime=Time.time;
            // Zorluk: Kolay = 1s gecikme, Zor = 0.1s gecikme
            nextReactionTime=Time.time+Mathf.Lerp(1.0f,reactionDelay,difficulty);
        }
    }

    private void UpdateChase() {
        UpdateChaseMode();
        HeadTowardsCombatTarget();
    }

    private void HeadTowardsCombatTarget() {
        Vector3 targetPosition;
        float bankLimiter = 1f;
        float pitchLimiter = 1f;

        switch(currentChaseMode) {
            case ChaseMode.PURE_PURSUIT:
                targetPosition=playerTransform.position;
                bankLimiter=1f;
                pitchLimiter=1f;
                break;

            case ChaseMode.LEAD_PURSUIT:
                targetPosition=CalculateLeadTarget();
                bankLimiter=1f;
                pitchLimiter=1f;
                break;

            case ChaseMode.LAG_PURSUIT:
                // Oyuncunun GERİSİNE git (hız kazan)
                Vector3 playerVelocityDir = playerRb!=null ? playerRb.linearVelocity.normalized : playerTransform.forward;
                Vector3 lagPoint = playerTransform.position-playerVelocityDir*lagPursuitOffset;
                targetPosition=lagPoint;
                bankLimiter=0.7f; // Daha az agresif
                pitchLimiter=0.7f;
                break;

            case ChaseMode.DEFENSIVE:
                // Hız kazan: İleri ve yukarı
                targetPosition=transform.position+transform.forward*150f+Vector3.up*80f;
                bankLimiter=0.5f; // Çok yumuşak
                pitchLimiter=0.8f;
                break;

            default:
                targetPosition=playerTransform.position;
                bankLimiter=1f;
                pitchLimiter=1f;
                break;
        }

        HeadTowardsSimpleTarget(targetPosition,bankLimiter,pitchLimiter);
    }

    // ==================== NAVIGATION ====================

    private void UpdateRTB() {
        if(Vector3.Distance(transform.position,targetTransform.position)<targetReachThreshold) {
            stateMachine.TransitionTo(AIStateTransition.REACHED_BASE);
            SwitchTargets(PatrolLocations[0]);
        }
    }

    private void UpdatePatrol() {
        if(PatrolLocations.Length==0)
            return;

        if(Vector3.Distance(transform.position,targetTransform.position)<targetReachThreshold) {
            int currentIndex = System.Array.IndexOf(PatrolLocations,targetTransform);
            int nextIndex = (currentIndex+1)%PatrolLocations.Length;
            SwitchTargets(PatrolLocations[nextIndex]);
        }

        HeadTowardsTarget();
    }

    private void SwitchTargets(Transform newTarget) {
        Debug.Log("Switching Targets to "+newTarget.name);
        targetTransform=newTarget;
        rollPid.Reset();
        pitchPid.Reset();
        yawPid.Reset();
    }

    private void HeadTowardsTarget() {
        if(targetTransform==null)
            return;

        // Patrol ve RTB için basit hedefleme
        HeadTowardsSimpleTarget(targetTransform.position,1f,1f);
    }

    private void HeadTowardsSimpleTarget(Vector3 targetPosition,float bankLimiter = 1f,float pitchLimiter = 1f) {
        Vector3 toTarget = targetPosition-transform.position;
        if(toTarget.sqrMagnitude<0.01f)
            return;

        Vector3 targetDir = toTarget.normalized;

        // Yatay düzlemde hedefe dönmek için gereken açı
        Vector3 flatTargetDir = Vector3.ProjectOnPlane(targetDir,Vector3.up).normalized;
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward,Vector3.up).normalized;
        float headingError = Vector3.SignedAngle(flatForward,flatTargetDir,Vector3.up);

        // Pitch
        float pitchToTarget = -Mathf.Asin(Mathf.Clamp(targetDir.y,-1f,1f))*Mathf.Rad2Deg;

        // Energy state'e göre limitleri ayarla
        float energyState = stateMachine.currentState==AIState.CHASE ? GetEnergyState() : 1f;

        // Zorluk ile limit ayarla - Kolay mod daha az agresif
        float difficultyBankFactor = Mathf.Lerp(0.6f,1.0f,difficulty);
        float difficultyPitchFactor = Mathf.Lerp(0.7f,1.0f,difficulty);

        float safeMaxBank = maxBankAngle*energyState*bankLimiter*difficultyBankFactor;
        float safeMaxPitch = maxPitchAngle*energyState*pitchLimiter*difficultyPitchFactor;

        // İSTENEN DEĞERLER
        // Zorluk: Heading error multiplier - kolay modda daha yavaş dön
        float headingMultiplier = Mathf.Lerp(0.5f,0.8f,difficulty);
        float desiredPitch = Mathf.Clamp(pitchToTarget,-safeMaxPitch,safeMaxPitch);
        float desiredBank = -Mathf.Clamp(headingError*headingMultiplier,-safeMaxBank,safeMaxBank);

        // HATA HESAPLAMA
        float pitchError = Mathf.DeltaAngle(CurrentPitchAngle,desiredPitch);
        float bankError = -Mathf.DeltaAngle(CurrentBankAngle,desiredBank);

        // PID - Zorluk ile response ayarla
        float pidMultiplier = Mathf.Lerp(0.7f,1.0f,difficulty);
        float pitchInput = pitchPid.Calculate(pitchError,Time.deltaTime)*pidMultiplier;
        float rollInput = rollPid.Calculate(bankError,Time.deltaTime)*pidMultiplier;
        float yawInput = yawPid.Calculate(headingError,Time.deltaTime)*pidMultiplier;

        pitchInput=Mathf.Clamp(pitchInput,-1f,1f);
        rollInput=Mathf.Clamp(rollInput,-1f,1f);
        yawInput=Mathf.Clamp(yawInput,-1f,1f);

        if(stateMachine.currentState==AIState.CHASE) {
            Debug.Log($"Difficulty: {difficulty:F1} | Mode: {currentChaseMode} | Energy: {energyState:F2} | Speed: {rb.linearVelocity.magnitude:F1} m/s");
        }

        controller.SetInputs(pitchInput,rollInput,yawInput,baseThrust);

        Debug.DrawLine(transform.position,targetPosition,stateMachine.currentState==AIState.CHASE ? Color.red : Color.green);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos() {
        if(stateMachine.currentState==AIState.CHASE) {
            // Chase mode göstergesi
            Gizmos.color=Color.red;
            Gizmos.DrawWireSphere(transform.position,5f);

            // Lead target göster
            if(currentChaseMode==ChaseMode.LEAD_PURSUIT) {
                Vector3 leadPos = CalculateLeadTarget();
                Gizmos.color=Color.yellow;
                Gizmos.DrawSphere(leadPos,3f);
                Gizmos.DrawLine(transform.position,leadPos);
            }
        }

        UnityEditor.Handles.Label(transform.position+Vector3.up*5f,
            $"State: {stateMachine.currentState}\n"+
            $"Difficulty: {difficulty:F1}\n"+
            $"Chase Mode: {currentChaseMode}\n"+
            $"Speed: {(rb!=null ? rb.linearVelocity.magnitude : 0f):F1} m/s\n"+
            $"Energy: {(stateMachine.currentState==AIState.CHASE ? GetEnergyState() : 1f):F2}\n"+
            $"Bank: {CurrentBankAngle:F1}°\n"+
            $"Distance: {(playerTransform!=null ? Vector3.Distance(transform.position,playerTransform.position) : 0f):F1} m"
        );
    }
#endif
}

// ==================== STATE MACHINE ====================

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

// ==================== PID CONTROLLER ====================

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