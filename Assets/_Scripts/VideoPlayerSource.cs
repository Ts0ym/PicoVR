using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

public class VideoPlayerSource : MonoBehaviour
{
    public VideoPlayer player;
        public RenderTexture targetTexture;
    
        void Start()
        {
            // Путь внутри StreamingAssets
            string path = Path.Combine(Application.streamingAssetsPath, "360clip.mp4");
            player.source = VideoSource.Url;
            player.url = path;
    
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = targetTexture;
    
            player.Play();
        }
}
