using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class DisableAfterVideo : MonoBehaviour
{
    [SerializeField] private VideoPlayer _videoPlayer;

    // Update is called once per frame
    void Update()
    {
        if (_videoPlayer != null && _videoPlayer.isPlaying)
        {
            _videoPlayer.loopPointReached += OnLoopPointReached;
        }
    }

    private void OnLoopPointReached(VideoPlayer vp)
    {
        this.gameObject.SetActive(false);
    }
}
