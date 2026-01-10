using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Mirror.VR.Extensions.RoomPC
{
    public class MirrorVRRoomLetter : MonoBehaviour
    {
        public KeyType keyType;
        [SerializeField, Tooltip("The letter that will be entered into the computer. Leave this blank if the key type is anything other than Letter.")]
        private string letter;

        [Space]
        public string handTag = "HandTag";

        private MirrorVRRoomScript roomScript;

        private void Start()
        {
            roomScript = GetComponentInParent<MirrorVRRoomScript>();
        }

        internal void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(handTag))
            {
                switch (keyType)
                {
                    case KeyType.Letter:
                        roomScript.AddCharacter(letter);
                        break;

                    case KeyType.Backspace:
                        roomScript.RemoveCharacter();
                        break;

                    case KeyType.Join:
                        roomScript.PressJoin();
                        break;

                    case KeyType.Leave:
                        roomScript.LeaveLobby();
                        break;
                }
            }
        }

        public enum KeyType
        {
            Letter,
            Backspace,
            Join,
            Leave
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(MirrorVRRoomLetter))]
    [CanEditMultipleObjects]
    public class MvrRoomLetterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            MirrorVRRoomLetter letter = (MirrorVRRoomLetter)target;

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
