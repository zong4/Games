using Camera;
using UnityEngine;
using System.Collections;

namespace Scenes
{
    public class LoadScene : MonoBehaviour
    {
        public float timeToWait = 3f;
        public CameraWake cameraWake;
        public BlinkController blinkController;
        public float openTime = 0.5f;
        private bool hasLoaded = false;
        
        private void Start()
        {
            Invoke("Load", timeToWait);
        }
        
        public void Load()
        {
            if (hasLoaded) return;
            Debug.Log("Scene Loaded");
            hasLoaded = true;

            blinkController.BlinkOnce(0f, 1f, openTime);
            StartCoroutine(DelayedWake());
            transform.GetChild(0).gameObject.SetActive(false);
        }

        private IEnumerator DelayedWake()
        {
            yield return new WaitForSeconds(0.1f);
            cameraWake.StartWakeEffect();
        }
    }
}

