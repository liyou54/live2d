using Sirenix.OdinInspector;
using UnityEngine;

namespace VertexEditor
{
    public class Live2dEditor : MonoBehaviour
    {
        private string _live2dPath;
        [Button("添加子节点")]
        public void AddChild()
        {
            var go = new GameObject("Live2d");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;
            go.transform.localScale = Vector3.one;
            go.transform.localRotation = Quaternion.identity;
        }
    }
}