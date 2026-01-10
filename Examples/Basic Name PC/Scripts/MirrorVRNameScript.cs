using System.Collections;
using UnityEngine;

using TMPro;

using EpicTransport;
using Epic.OnlineServices.Connect;

namespace Mirror.VR.Extensions.NamePC
{
    public class MirrorVRNameScript : MonoBehaviour
    {
        [Header("This is an example PC using MirrorVR.\nFeel free to look at the code and make your own PC out of it!")]
        [Space(15)]
        [SerializeField] private string playerName;
        [SerializeField, Range(4, ConnectInterface.USERLOGININFO_DISPLAYNAME_MAX_LENGTH)] private int nameLimit = 12;
        [SerializeField] private TMP_Text nameText;
        

        private void Start()
        {
            StartCoroutine(Login());
        }

        private IEnumerator Login()
        {
            yield return new WaitUntil(() => EOSManager.Initialized);

            playerName = MirrorVRManager.GetUsername();
            nameText.text = "Name: " + playerName;
        }

        internal void AddCharacter(string letter)
        {
            if (EOSManager.Initialized)
            {
                if (playerName.Length >= nameLimit)
                    playerName = playerName.Substring(0, nameLimit);
                else
                    playerName += letter;

                nameText.text = $"Name: {playerName}";
            }
        }

        internal void RemoveCharacter()
        {
            if (EOSManager.Initialized)
            {
                playerName = playerName.Substring(0, playerName.Length - 1);
                nameText.text = $"Name: {playerName}";
            }
        }

        internal void PressEnter()
        {
            if (EOSManager.Initialized)
            {
                MirrorVRManager.SetUsername(playerName);
                nameText.text = $"Name: {playerName}";
            }
        }
    }
}
