using UnityEngine;

namespace Seagull.City_03.SceneProps {
    public class FenceData : MonoBehaviour {
        public Vector3 start;
        public Vector3 end;
        
        public Vector3 getStartWorldPos() {
            return transform.TransformPoint(start);
        }

        public Vector3 getEndWorldPos() {
            return transform.TransformPoint(end);
        }

        public float getLength() {
            return (start - end).magnitude / transform.localScale.x;
        }

        public float getWorldSpaceLength() {
            return (getStartWorldPos() - getEndWorldPos()).magnitude;
        }
        
        private void OnDrawGizmos() {
            // 设置 Gizmos 的颜色
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            // 绘制 pos1 和 pos2 的球体表示
            Gizmos.DrawSphere(getStartWorldPos(), 0.2f);
            Gizmos.color = new Color(0, 0, 1, 0.5f);
            Gizmos.DrawSphere(getEndWorldPos(), 0.2f);
        }
    }
}