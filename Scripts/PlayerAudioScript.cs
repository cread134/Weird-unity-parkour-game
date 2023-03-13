using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudioScript : MonoBehaviour
{

    private AudioSource a_Source;
    [Header("AudioClips")]
    public AudioClip[] footsteps;

    // Start is called before the first frame update
    void Start()
    {
        a_Source = GetComponent<AudioSource>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void PlayPlayerSound(AudioClip aClip)
    {
        a_Source.PlayOneShot(aClip);
    }

    public void PlayFootStepSound()
    {
        AudioClip footSound = footsteps[Random.Range(0, footsteps.Length)];
        PlayPlayerSound(footSound);
    }

    public void PlayLoopingSound(AudioClip clip)
    {
        a_Source.clip = clip;
        a_Source.loop = true;
        a_Source.Play();
    }

    public void StopPlaying()
    {
        a_Source.loop = false;
    }
}
