    using UnityEngine;
    using UnityEngine.XR.ARFoundation;
    using Google.XR.ARCoreExtensions;
    using Photon.Pun;
    using Hashtable = ExitGames.Client.Photon.Hashtable;
    using System.Collections;

    public class CloudAnchorManager : MonoBehaviourPunCallbacks
    {
        [Header("AR Bileþenleri")]
        public ARAnchorManager anchorManager;

        private GameObject arenaObject;

        // HOST – Master Client çaðýrýr
        public void HostCloudAnchor(GameObject arena)
        {
            if (arena == null || anchorManager == null) return;

            arenaObject = arena;

            ARAnchor anchor = arenaObject.GetComponent<ARAnchor>();
            if (anchor == null)
                anchor = arenaObject.AddComponent<ARAnchor>();

            // TTL = 1 gün
            ARCloudAnchor cloudAnchor =
                anchorManager.HostCloudAnchor(anchor, 1);

            StartCoroutine(CheckHostingState(cloudAnchor));
        }

        private IEnumerator CheckHostingState(ARCloudAnchor cloudAnchor)
        {
            while (cloudAnchor.cloudAnchorState ==
                   CloudAnchorState.TaskInProgress)
            {
                yield return null;
            }

            if (cloudAnchor.cloudAnchorState == CloudAnchorState.Success)
            {
                string anchorId = cloudAnchor.cloudAnchorId;

                Hashtable props = new Hashtable
                {
                    { "AnchorID", anchorId }
                };

                PhotonNetwork.CurrentRoom.SetCustomProperties(props);

                Debug.Log("<color=green>Cloud Anchor Host edildi: </color>" + anchorId);
            }
            else
            {
                Debug.LogError("Cloud Anchor Host baþarýsýz: "
                    + cloudAnchor.cloudAnchorState);
            }
        }

        // Photon odasýnda AnchorID gelince
        public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
        {
            if (!PhotonNetwork.IsMasterClient &&
                propertiesThatChanged.ContainsKey("AnchorID"))
            {
                ResolveAnchor((string)propertiesThatChanged["AnchorID"]);
            }
        }

        // RESOLVE – Diðer clientlar
        private void ResolveAnchor(string id)
        {
            if (anchorManager == null) return;

            ARCloudAnchor cloudAnchor =
                anchorManager.ResolveCloudAnchorId(id);

            StartCoroutine(CheckResolvingState(cloudAnchor));
        }

        private IEnumerator CheckResolvingState(ARCloudAnchor cloudAnchor)
        {
            while (cloudAnchor.cloudAnchorState ==
                   CloudAnchorState.TaskInProgress)
            {
                yield return null;
            }

            if (cloudAnchor.cloudAnchorState == CloudAnchorState.Success)
            {
                GameObject arena = GameObject.FindGameObjectWithTag("Arena");

                if (arena != null)
                {
                    arena.transform.SetPositionAndRotation(
                        cloudAnchor.transform.position,
                        cloudAnchor.transform.rotation
                    );

                    Debug.Log("<color=cyan>Dünyalar senkronize edildi!</color>");
                }
            }
            else
            {
                Debug.LogError("Cloud Anchor Resolve baþarýsýz: "
                    + cloudAnchor.cloudAnchorState);
            }
        }
    }
    