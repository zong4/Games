using UnityEngine;

namespace Audio
{
    public class BreatheManager : MonoBehaviour
    {
        private AudioSource _audioSource;
        public float interval = 30f;
        public float offset = 10f;
        private float _realInterval;

        private float _timer;

        private void Start()
        {
            _audioSource = GetComponent<AudioSource>();
            _realInterval = interval + Random.Range(-offset, offset);
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            if (_timer >= _realInterval)
            {
                _timer = 0f;
                PlaySound();
            }
        }

        private void PlaySound()
        {
            _audioSource.Play();
            _realInterval = interval + Random.Range(-offset, offset);
        }
    }
}
