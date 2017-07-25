//#define TOBY_BGM_MUTE

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using HaHExtensions;

public class Announcement
{
    private FMOD.Studio.EventInstance soundFile;
    private string text;

    public Announcement(FMOD.Studio.EventInstance _soundFile, string _text)
    {
        soundFile = _soundFile;
        text = _text;
    }

    public string GetText()
    {
        return text;
    }

    public FMOD.Studio.EventInstance GetSoundFile()
    {
        return soundFile;
    }
}

public class Audio
{
    static private Audio singleton;

    static public float sfxVolume = 1;
    static public float bgmVolume = 1;

    static public void Awake()
    {
        singleton = new Audio();

        Loading.OnFinishedLoading -= singleton.Loading_OnFinishedLoading;
        Loading.OnFinishedLoading += singleton.Loading_OnFinishedLoading;

        GetAudioData().Initialize();

        //SetVolume(0);
    }

    private void Loading_OnFinishedLoading(int loadedLevel)
    {
        ResetMusic(GetAudioData().GetMusicEventInstanceList());
        PlayEventBGM(GetAudioData().ingameBGMEv, 1);
        GetAudioData().GetAmbienceEmittersList().AddRange(Object.FindObjectsOfType<FMODUnity.StudioEventEmitter>());
    }

    public FMOD.Studio.EventInstance GetEventInstance(FMOD.Studio.EventInstance[] eventInstance, int index)
    {
        try
        {
            return eventInstance[index];
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e);
            return null;
        }
    }

    static public bool CanPlay(List<FMOD.Studio.EventInstance> eventInstanceList)
    {
        FMOD.Studio.PLAYBACK_STATE state;
        int c = eventInstanceList.Count;

        if (eventInstanceList == null)
            return false;

        for (int i = 0; i < c; i++)
        {
            if (eventInstanceList[i] == null)
                continue;

            eventInstanceList[i].getPlaybackState(out state);

            if (state == FMOD.Studio.PLAYBACK_STATE.PLAYING)
                return false;
        }

        return true;
    }

    static public bool CanPlay(FMOD.Studio.EventInstance eventInstanceList)
    {
        FMOD.Studio.PLAYBACK_STATE state;

        eventInstanceList.getPlaybackState(out state);

        if (state == FMOD.Studio.PLAYBACK_STATE.PLAYING)
            return false;

        return true;
    }

    static public void CheckCurrentPlayingList()
    {
        List<FMOD.Studio.EventInstance> list = GetAudioData().GetCurrentPlayingSoundsList();
        FMOD.Studio.PLAYBACK_STATE state;

        for (int i = list.Count - 1; i >= 0; --i)
        {
            list[i].getPlaybackState(out state);

            if (state == FMOD.Studio.PLAYBACK_STATE.STOPPED)
            {
                list.Remove(list[i]);
                continue;
            }
        }

        if (list.Count != 0)
            return;

        if (GetAudioData().GetSoundQueue().Count == 0)
            return;

        if (HUD.IsTextShowing())
            return;

        Announcement announcement = GetAudioData().GetSoundQueue().Dequeue();
        HUD.AnnounceImportantMessage(announcement.GetSoundFile(), announcement.GetText());
    }

    static public void AddToQueue(FMOD.Studio.EventInstance eventInstance, string text)
    {
        GetAudioData().GetSoundQueue().Enqueue(new Announcement(eventInstance, text));
    }

    static public bool PlayOneShot(FMOD.Studio.EventInstance eventInstance, List<FMOD.Studio.EventInstance> overlapList = null, string text = "")
    {
        return PlayOneShot(eventInstance, Listener.GetListener().transform.position, overlapList, text);
    }

    static public void Set3DPositionSpecific(FMOD.Studio.EventInstance eventInstance, Vector3 pos)
    {
        if (eventInstance == null)
        {
            Debug.LogError("FMOD could not find an event, dunno which cause it's null XD");
            return;
        }

        FMOD.ATTRIBUTES_3D attributes;
        GetAudioData().GetEventInstance(eventInstance).get3DAttributes(out attributes);
        FMOD.VECTOR fmodVector;
        fmodVector = pos.ToFMODVector();

        attributes.position = fmodVector;
        eventInstance.set3DAttributes(attributes);
    }

    static public bool PlayOneShot(FMOD.Studio.EventInstance eventInstance, Vector3 point, List<FMOD.Studio.EventInstance> overlapList = null, string text = "")
    {
        if (eventInstance == null)
        {
            Debug.LogError("FMOD instance not found (" + text + ")");
            return false;
        }

        FMOD.ATTRIBUTES_3D attributes;

        FMOD.Studio.EventInstance instance = GetAudioData().GetEventInstance(eventInstance);

        if (instance == null)
        {
            Debug.LogError("FMOD instance not found (" + eventInstance.ToString() + ")");
            return false;
        }

        instance.get3DAttributes(out attributes);
        attributes.position = point.ToFMODVector();
        eventInstance.set3DAttributes(attributes);

        if (overlapList != null)
        {
            if (!CanPlay(overlapList))
            {
                if (overlapList == GetAudioData().GetImportantSoundsList())
                    AddToQueue(eventInstance, text);
                                       
                return false;
            }
        }
       
        if (overlapList == GetAudioData().GetImportantSoundsList())
            GetAudioData().GetCurrentPlayingSoundsList().Add(eventInstance);

        GetAudioData().GetEventInstance(eventInstance).start();
        return true;
    }

    static public void PlayOneShotArr(string[] clip, int index)
    {
        PlayOneShotArr(clip, index, CameraController.GetCamera().transform.position);
    }

    static public void PlayOneShotArr(string[] clip, int index, Vector3 point)
    {
        try
        {
            CanPlay(GetAudioData().GetOneshotInstanceList());
            FMODUnity.RuntimeManager.PlayOneShot(clip[index], point);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e);
        }
    }

    static public void SetSoundEventParam(FMOD.Studio.ParameterInstance[] ParamInstance, int index, float parameter)
    {
        try
        {
            ParamInstance[index].setValue(parameter);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e);
        }
    }

    static public void SetSoundEventParam(FMOD.Studio.ParameterInstance ParamInstance, float parameter)
    {
        try
        {
            ParamInstance.setValue(parameter);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e);
        }
    }

    static public void PlayEventBGM(FMOD.Studio.EventInstance[] eventInstance, int index, bool resetMusic = true, FMOD.Studio.STOP_MODE stopMode = FMOD.Studio.STOP_MODE.ALLOWFADEOUT)
    {
#if TOBY_BGM_MUTE
        return;
#else

        try
        {
            if (resetMusic)
                ResetMusic(Audio.GetAudioData().GetMusicEventInstanceList(), stopMode);

            GetAudioData().GetEventInstance(eventInstance, index).start();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e);
        }
#endif
    }

    static public void PlayRandomEventBGM(FMOD.Studio.EventInstance[] eventInstance, bool resetMusic = true, FMOD.Studio.STOP_MODE stopMode = FMOD.Studio.STOP_MODE.ALLOWFADEOUT)
    {
        try
        {
            int rand = Random.Range(0, eventInstance.Length);
            PlayEventBGM(eventInstance, rand, resetMusic, stopMode);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e);
        }
    }

    static public void StopEventBGM(FMOD.Studio.EventInstance[] eventInstance, int index, FMOD.Studio.STOP_MODE stopMode = FMOD.Studio.STOP_MODE.ALLOWFADEOUT)
    {
        try
        {
            GetAudioData().GetEventInstance(eventInstance, index).stop(stopMode);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e);
        }       
    }

    static public void SetVolumeSpecific(FMOD.Studio.EventInstance eventInstance, float volume)
    {
        try
        {
            eventInstance.setVolume(volume);
        }
        catch(System.Exception e)
        {
            Debug.LogWarning(e);
        }
    }

    static public void SetVolume(float volume, List<FMOD.Studio.EventInstance> eventInstanceList)
    {
        try
        {
            for (int i = 0; i < eventInstanceList.Count; i++)
            {
                eventInstanceList[i].setVolume(volume);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e);
        }
    }

    static public float GetVolume(FMOD.Studio.EventInstance eventInstance)
    {
        try
        {
            eventInstance.getVolume(out bgmVolume);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e);
        }

        return bgmVolume;
    }

    static public AudioData GetAudioData()
    {
        return GameData.audioData;
    }

    static public void ResetMusic(List<FMOD.Studio.EventInstance> list, FMOD.Studio.STOP_MODE stopMode = FMOD.Studio.STOP_MODE.ALLOWFADEOUT)
    {
        try
        {
            for (int i = 0; i < list.Count; i++)
            {
                list[i].stop(stopMode);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e);
        }
    }

    static public void ResetMusicSpecific(FMOD.Studio.EventInstance eventInstance, FMOD.Studio.STOP_MODE stopMode = FMOD.Studio.STOP_MODE.ALLOWFADEOUT)
    {
        try
        {
            eventInstance.stop(stopMode);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e);
        }
    }
}
