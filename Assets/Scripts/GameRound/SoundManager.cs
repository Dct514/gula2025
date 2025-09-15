using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class SoundManager : MonoBehaviourPunCallbacks
{
    public AudioClip[] audioClips;

    public void SoundPlay(int clipnum)
    {
        photonView.RPC("SoundStart", RpcTarget.All, clipnum);
    }

    [PunRPC]
    public void SoundStart(int clipnum)
    {
        AudioSource audioSource = GetComponent<AudioSource>();
        audioSource.clip = audioClips[clipnum];
        audioSource.Play();
    }
}
