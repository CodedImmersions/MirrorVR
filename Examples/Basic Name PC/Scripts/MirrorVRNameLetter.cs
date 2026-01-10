using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mirror.VR.Extensions.NamePC
{
    public class MirrorVRNameLetter : MonoBehaviour
    {
        public KeyType keyType;
        [SerializeField, Tooltip("The letter that will be entered into the computer. Leave this blank if the key type is anything other than Letter.")]
        private string letter;

        [Space]
        public string handTag = "HandTag";

        private MirrorVRNameScript nameScript;

        private void Start()
        {
            nameScript = GetComponentInParent<MirrorVRNameScript>();
        }

        internal void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(handTag))
            {
                switch (keyType)
                {
                    case KeyType.Letter:
                        nameScript.AddCharacter(letter);
                        break;

                    case KeyType.Backspace:
                        nameScript.RemoveCharacter();
                        break;

                    case KeyType.Enter:
                        nameScript.PressEnter();
                        break;
                }
            }
        }

        public enum KeyType
        {
            Letter,
            Backspace,
            Enter
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MirrorVRNameLetter))]
    [CanEditMultipleObjects]
    public class MvrNameLetterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            MirrorVRNameLetter letter = (MirrorVRNameLetter)target;

            GUILayout.Space(10);

            if (GUILayout.Button("Test Button Press"))
            {
                if (Application.isPlaying)
                {
                    GameObject hand = GameObject.FindGameObjectWithTag(letter.handTag);
                    Collider collider = hand.GetComponent<Collider>();

                    bool addedcoll = false;

                    if (collider == null)
                        collider = hand.AddComponent<SphereCollider>(); addedcoll = true;

                    letter.OnTriggerEnter(collider);

                    if (addedcoll)
                        Destroy(collider);
                }
            }
        }
    }
#endif
}
