using Mirror.VR;
using UnityEngine;

namespace Mirror.VR.Extensions.ColorExample
{
    public class SetColor : MonoBehaviour
    {
        [SerializeField] private Color color;
        [SerializeField] private string HandTag = "HandTag";

        private void Start() => GetComponent<Renderer>().material.color = color;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(HandTag)) MirrorVRManager.SetColor(color);
        }
    }
}
