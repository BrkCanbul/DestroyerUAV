using UnityEngine;

public class ChaseCamera : MonoBehaviour
{
    [Header("Target Transform")]
    public Transform target;
    [Header("Offset Settings")]
    public Vector3 offset = new Vector3(0f,3f,-8f); // Uçaðýn arkasýnda & biraz üstünde
    public float positionSmoothTime = 0.15f;

    [Header("Rotation")]
    public float rotationLerpSpeed = 8f; // Kameranýn hedefe bakma hýzýný belirler

    private Vector3 _currentVelocity;


    void LateUpdate()
    {

        if(target == null)
            return;

        GoToDesiredCamPosition();
        ChaseCamRotate();




    }
    private void GoToDesiredCamPosition() {
        Vector3 desiredPosition = target.TransformPoint(offset);

        // 2) Kamerayý bu pozisyona yumuþak bir þekilde taþý
        transform.position=Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref _currentVelocity,
            positionSmoothTime
        );

    }
    private void ChaseCamRotate() {
        // 3) Kameranýn bakacaðý yönü hesapla (uçaðýn ilerisini hedef alýyoruz)
        Vector3 lookTarget = target.position+target.forward*10f;
        Vector3 lookDirection = lookTarget-transform.position;

        if(lookDirection.sqrMagnitude>0.001f) {
            Quaternion desiredRotation = Quaternion.LookRotation(lookDirection,target.up);

            // 4) Rotasyonu yumuþak bir þekilde çevir
            transform.rotation=Quaternion.Slerp(
                transform.rotation,
                desiredRotation,
                rotationLerpSpeed*Time.deltaTime
            );
        }
    }
}
