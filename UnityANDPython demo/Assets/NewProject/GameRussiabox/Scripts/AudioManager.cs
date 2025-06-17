
using UnityEngine;
using System;
using System.Collections.Generic;

public class AudioManager : MonoSingleton<AudioManager>
{
    //如无必要勿增实体   因为这个案例比较简单 因此没做太复杂的音效管理
    AudioSource audio;
    AudioSource audio1;
    AudioSource audio2;
    bool mute = false;
    float vulume=1;
    private void Awake()
    {
        audio = gameObject.AddComponent<AudioSource>();
        audio1 = gameObject.AddComponent<AudioSource>();
        audio2 = gameObject.AddComponent<AudioSource>();
    }
    /// <summary>
    /// 播放瞬间的小音效
    /// </summary>
    /// <param name="path"></param>
    public void PlaySound(string path)
    {
        audio.clip = Resources.Load<AudioClip>(path);
        audio.Play();
    }
    public void PlaySound1(string path)
    {
        audio2.clip = Resources.Load<AudioClip>(path);
        audio2.Play();
    }
    /// <summary>
    /// 播放较长的音效
    /// </summary>
    /// <param name="path"></param>
    public void PlayLongSound(string path)
    {
        audio1.clip = Resources.Load<AudioClip>(path);
        audio1.Play();
    }
    public void Mute()
    {
        if (!mute)
        {
            mute = true;
            audio.volume = 0;
            audio1.volume = 0;
        }
        else
        {
            mute = false;
            audio.volume = vulume;
            audio1.volume = vulume;
        }
    }
    public void SetVolume(float vulume)
    {
        this.vulume = vulume;
    }
}