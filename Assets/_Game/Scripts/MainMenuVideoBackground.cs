using System.IO;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Points the VideoPlayer at the background video in StreamingAssets and plays it via URL
/// instead of an embedded VideoClip. The embedded-VideoClip path streams through Unity's
/// internal unityvfs:// scheme, which fails to open on this machine because the build's
/// absolute path (/Volumes/ auxinn/...) contains a space in the volume name that scheme
/// doesn't handle - AVFoundationVideoMedia::OpenForRead errors out on every attempt. Loading
/// the same file via a plain file:// URL (the same code path StreamingAssets always uses)
/// isn't affected by that bug.
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class MainMenuVideoBackground : MonoBehaviour
{
    [SerializeField] private string fileName = "MainMenuBackground.mp4"; // video file name inside StreamingAssets

    void Start()
    {
        var player = GetComponent<VideoPlayer>();
        player.source = VideoSource.Url; // switch from embedded VideoClip to a plain file URL
        // Application.streamingAssetsPath resolves to the right folder automatically per platform.
        player.url = Path.Combine(Application.streamingAssetsPath, fileName);
        player.Play(); // start the background video immediately
    }
}
