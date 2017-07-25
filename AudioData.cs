using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AudioData : ScriptableObject
{
    [FMODUnity.EventRef]
    public string clickEffect;
    [HideInInspector]
    public FMOD.Studio.EventInstance clickEffectEv;

    [FMODUnity.EventRef]
    public string[] announcerEffects;

    /// <summary>
    /// <para> 0: local player ahead </para>
    /// <para> 1: enemy deity dies </para>
    /// <para> 2: local player captured city </para>
    /// <para> 3: enemy player captured city </para>
    /// <para> 4: local player captured all required objectives </para>
    /// <para> 5: enemy player ahead </para>
    /// <para> 6: local player lost shrine </para>
    /// <para> 7: enemy player lost shrine </para>
    /// <para> 8: local player captured shrine </para>
    /// <para> 9: enemy player captured shrine </para>
    /// <para> 10: enemy player captured all required objectives </para>
    /// </summary>
    [HideInInspector]
    public FMOD.Studio.EventInstance[] announcerEffectsEv;

    [FMODUnity.EventRef]
    public string[] ingameBGM;

    /// <summary>
    /// <para> 0: baltic track </para>
    /// <para> 1: baltic track adaptive </para>
    /// <para> 2: order track </para>
    /// </summary>
    [HideInInspector]
    public FMOD.Studio.EventInstance[] ingameBGMEv;
    [HideInInspector]
    public FMOD.Studio.ParameterInstance[] ingameBGMParam;
    [HideInInspector]
    public float ingameBGMTension = 0f;

    [FMODUnity.EventRef]
    public string[] combatBGM;
    [HideInInspector]
    public FMOD.Studio.EventInstance[] combatBGMEv;
    [HideInInspector]
    public FMOD.Studio.ParameterInstance[] combatBGMParam;

    [FMODUnity.EventRef]
    public string[] victoryBGM;
    [HideInInspector]
    public FMOD.Studio.EventInstance[] victoryBGMEv;
    [HideInInspector]
    public FMOD.Studio.ParameterInstance[] victoryBGMParam;

    [FMODUnity.EventRef]
    public string[] menuBGM;
    [HideInInspector]
    public FMOD.Studio.EventInstance[] menuBGMEv;
    [HideInInspector]
    public FMOD.Studio.ParameterInstance[] menuBGMParam;

    [FMODUnity.EventRef]
    public string[] defeatBGM;
    [HideInInspector]
    public FMOD.Studio.EventInstance[] defeatBGMEv;
    [HideInInspector]
    public FMOD.Studio.ParameterInstance[] defeatBGMParam;

    // ==========================================
    // Michael Abilities
    // ==========================================

    [FMODUnity.EventRef]
    public string[] holyConflagration;
    [HideInInspector]
    public FMOD.Studio.EventInstance[] holyConflagrationEv;
    [FMODUnity.EventRef]
    public string[] armyOfGod;
    [HideInInspector]
    public FMOD.Studio.EventInstance[] armyOfGodEv;
    [FMODUnity.EventRef]
    public string[] saintsBlessing;
    [HideInInspector]
    public FMOD.Studio.EventInstance[] saintsBlessingEv;
    [FMODUnity.EventRef]
    public string[] protectorPeople;
    [HideInInspector]
    public FMOD.Studio.EventInstance[] protectorPeopleEv;

    // ==========================================
    // Deity Specific Voice Acting
    // ==========================================

    [FMODUnity.EventRef]
    public string attackJungle;
    [HideInInspector]
    public FMOD.Studio.EventInstance attackJungleEv;
    [FMODUnity.EventRef]
    public string attackFollower;
    [HideInInspector]
    public FMOD.Studio.EventInstance attackFollowerEv;
    [FMODUnity.EventRef]
    public string attackMythical;
    [HideInInspector]
    public FMOD.Studio.EventInstance attackMythicalEv;
    [FMODUnity.EventRef]
    public string attackDeity;
    [HideInInspector]
    public FMOD.Studio.EventInstance attackDeityEv;

    // ==========================================
    // Mythical Abilities
    // ==========================================

    [FMODUnity.EventRef]
    public string[] divineArmor;
    [HideInInspector]
    public FMOD.Studio.EventInstance[] divineArmorEv;

    [FMODUnity.EventRef]
    public string[] gaze;
    [HideInInspector]
    public FMOD.Studio.EventInstance[] gazeEv;

    // ==========================================
    // Follower Abilities
    // ==========================================

    [FMODUnity.EventRef]
    public string[] deploy;
    [HideInInspector]
    public FMOD.Studio.EventInstance[] deployEv;

    [FMODUnity.EventRef]
    public string[] notifications;
    [HideInInspector]
    public FMOD.Studio.EventInstance[] notificationsEv;

    private List<FMOD.Studio.EventInstance> musicEventInstancelist;
    private List<FMOD.Studio.EventInstance> oneshotInstanceList;
    private List<FMOD.Studio.EventInstance> oneshotInstanceList3D;
    private List<FMOD.Studio.EventInstance> announcerInstanceList;
    private List<FMOD.Studio.EventInstance> halvedVolumeInstanceList;

    public List<FMODUnity.StudioEventEmitter> ambienceEmittersList;

    private List<FMOD.Studio.EventInstance> currentPlayingSoundsList;
    private Queue<Announcement> soundQueue = new Queue<Announcement>();
    private List<FMOD.Studio.EventInstance> importantSounds;

    public void Initialize()
    { 
        // Initializing all the event instances and parameters
        ingameBGMEv = new FMOD.Studio.EventInstance[ingameBGM.Length];
        ingameBGMParam = new FMOD.Studio.ParameterInstance[ingameBGM.Length];
        combatBGMEv = new FMOD.Studio.EventInstance[combatBGM.Length];
        combatBGMParam = new FMOD.Studio.ParameterInstance[combatBGM.Length];
        victoryBGMEv = new FMOD.Studio.EventInstance[victoryBGM.Length];
        victoryBGMParam = new FMOD.Studio.ParameterInstance[victoryBGM.Length];
        menuBGMEv = new FMOD.Studio.EventInstance[menuBGM.Length];
        menuBGMParam = new FMOD.Studio.ParameterInstance[menuBGM.Length];
        defeatBGMEv = new FMOD.Studio.EventInstance[defeatBGM.Length];
        defeatBGMParam = new FMOD.Studio.ParameterInstance[defeatBGM.Length];
        announcerEffectsEv = new FMOD.Studio.EventInstance[announcerEffects.Length];
        notificationsEv = new FMOD.Studio.EventInstance[notifications.Length];
        holyConflagrationEv = new FMOD.Studio.EventInstance[holyConflagration.Length];
        armyOfGodEv = new FMOD.Studio.EventInstance[armyOfGod.Length];
        saintsBlessingEv = new FMOD.Studio.EventInstance[saintsBlessing.Length];
        protectorPeopleEv = new FMOD.Studio.EventInstance[protectorPeople.Length];
        divineArmorEv = new FMOD.Studio.EventInstance[divineArmor.Length];
        deployEv = new FMOD.Studio.EventInstance[deploy.Length];
        gazeEv = new FMOD.Studio.EventInstance[gaze.Length];

        musicEventInstancelist = new List<FMOD.Studio.EventInstance>();
        oneshotInstanceList = new List<FMOD.Studio.EventInstance>();
        oneshotInstanceList3D = new List<FMOD.Studio.EventInstance>();
        announcerInstanceList = new List<FMOD.Studio.EventInstance>();
        currentPlayingSoundsList = new List<FMOD.Studio.EventInstance>();
        importantSounds = new List<FMOD.Studio.EventInstance>();
        halvedVolumeInstanceList = new List<FMOD.Studio.EventInstance>();
        ambienceEmittersList = new List<FMODUnity.StudioEventEmitter>();

        for (int i = 0; i < announcerEffects.Length; i++)
        {
            announcerEffectsEv[i] = FMODUnity.RuntimeManager.CreateInstance(announcerEffects[i]);
            announcerInstanceList.Add(announcerEffectsEv[i]);
        }

        for (int i = 0; i < ingameBGM.Length; i++)
        {
            ingameBGMEv[i] = FMODUnity.RuntimeManager.CreateInstance(ingameBGM[i]);
            musicEventInstancelist.Add(ingameBGMEv[i]);
        }

        for (int i = 0; i < combatBGM.Length; i++)
        {
            combatBGMEv[i] = FMODUnity.RuntimeManager.CreateInstance(combatBGM[i]);
            musicEventInstancelist.Add(combatBGMEv[i]);
        }

        for (int i = 0; i < victoryBGM.Length; i++)
        {
            victoryBGMEv[i] = FMODUnity.RuntimeManager.CreateInstance(victoryBGM[i]);
            musicEventInstancelist.Add(victoryBGMEv[i]);
        }

        for (int i = 0; i < menuBGM.Length; i++)
        {
            menuBGMEv[i] = FMODUnity.RuntimeManager.CreateInstance(menuBGM[i]);
            musicEventInstancelist.Add(menuBGMEv[i]);
        }

        for (int i = 0; i < defeatBGM.Length; i++)
        {
            defeatBGMEv[i] = FMODUnity.RuntimeManager.CreateInstance(defeatBGM[i]);
            musicEventInstancelist.Add(defeatBGMEv[i]);
        }

        for(int i = 0; i < notifications.Length; i++)
        {
            notificationsEv[i] = FMODUnity.RuntimeManager.CreateInstance(notifications[i]);
            announcerInstanceList.Add(notificationsEv[i]);
        }

        for (int i = 0; i < holyConflagration.Length; i++)
        {
            holyConflagrationEv[i] = FMODUnity.RuntimeManager.CreateInstance(holyConflagration[i]);
            oneshotInstanceList3D.Add(holyConflagrationEv[i]);
        }

        for (int i = 0; i < armyOfGod.Length; i++)
        {
            armyOfGodEv[i] = FMODUnity.RuntimeManager.CreateInstance(armyOfGod[i]);
            oneshotInstanceList3D.Add(armyOfGodEv[i]);
        }

        for (int i = 0; i < saintsBlessing.Length; i++)
        {
            saintsBlessingEv[i] = FMODUnity.RuntimeManager.CreateInstance(saintsBlessing[i]);
            oneshotInstanceList3D.Add(saintsBlessingEv[i]);
        }

        for(int i = 0; i < protectorPeople.Length; i++)
        {
            protectorPeopleEv[i] = FMODUnity.RuntimeManager.CreateInstance(protectorPeople[i]);
            oneshotInstanceList3D.Add(protectorPeopleEv[i]);
        }

        for(int i = 0; i < divineArmor.Length; i++)
        {
            divineArmorEv[i] = FMODUnity.RuntimeManager.CreateInstance(divineArmor[i]);
            oneshotInstanceList3D.Add(divineArmorEv[i]);
        }

        for(int i = 0; i < gaze.Length; i++)
        {
            gazeEv[i] = FMODUnity.RuntimeManager.CreateInstance(gaze[i]);
            oneshotInstanceList3D.Add(gazeEv[i]);
        }

        for (int i = 0; i < deploy.Length; i++)
        {
            deployEv[i] = FMODUnity.RuntimeManager.CreateInstance(deploy[i]);
            oneshotInstanceList3D.Add(deployEv[i]);
        }

        soundQueue.Clear();               
        clickEffectEv = FMODUnity.RuntimeManager.CreateInstance(clickEffect);

        attackJungleEv = FMODUnity.RuntimeManager.CreateInstance(attackJungle);
        oneshotInstanceList.Add(attackJungleEv);
        attackFollowerEv = FMODUnity.RuntimeManager.CreateInstance(attackFollower);
        oneshotInstanceList.Add(attackFollowerEv);
        attackMythicalEv = FMODUnity.RuntimeManager.CreateInstance(attackMythical);
        oneshotInstanceList.Add(attackMythicalEv);
        attackDeityEv = FMODUnity.RuntimeManager.CreateInstance(attackDeity);
        oneshotInstanceList.Add(attackDeityEv);

        importantSounds.AddRange(announcerInstanceList);

        if (!SetParameterInstance("Tension", ingameBGMEv, ingameBGMParam, 1))
            Debug.LogError("FMOD: Set parameter instance \"Tension\" failed");

        FMODUnity.RuntimeManager.LowlevelSystem.set3DSettings(1f, 1f, 0.2f);
        FMODUnity.RuntimeManager.LowlevelSystem.setSoftwareChannels(100);
    }

    public bool SetParameterInstance(string paramName, FMOD.Studio.EventInstance[] eventInstance, FMOD.Studio.ParameterInstance[] outputParam, int index)
    {
        if (eventInstance == null)
            return false;

        if (index >= eventInstance.Length)
            return false;

        if (eventInstance[index] == null)
            return false;

        eventInstance[index].getParameter(paramName, out outputParam[index]);

        return outputParam[index] != null;
    }

    public void SetParameterInstance(string paramName, FMOD.Studio.EventInstance eventInstance, FMOD.Studio.ParameterInstance outputParam)
    {
        eventInstance.getParameter(paramName, out outputParam);
    }

    public FMOD.Studio.EventInstance GetEventInstance(FMOD.Studio.EventInstance eventInstance)
    {
        return eventInstance;
    }

    public FMOD.Studio.EventInstance GetEventInstance(FMOD.Studio.EventInstance[] eventInstance, int index)
    {
        return eventInstance[index];
    }

    public FMOD.Studio.ParameterInstance GetParameterInstance(FMOD.Studio.ParameterInstance[] paramInstance, int index)
    {
        return paramInstance[index];
    }

    public List<FMOD.Studio.EventInstance> GetMusicEventInstanceList()
    {
        return musicEventInstancelist;
    }

    public List<FMOD.Studio.EventInstance> GetOneshotInstanceList()
    {
        return oneshotInstanceList;
    }

    public List<FMOD.Studio.EventInstance> GetOneshotInstanceList3D()
    {
        return oneshotInstanceList3D;
    }

    public List<FMOD.Studio.EventInstance> GetAnnouncerInstanceList()
    {
        return announcerInstanceList;
    }

    public Queue<Announcement> GetSoundQueue()
    {
        return soundQueue;
    }

    public List<FMOD.Studio.EventInstance> GetCurrentPlayingSoundsList()
    {
        return currentPlayingSoundsList;
    }

    public List<FMOD.Studio.EventInstance> GetImportantSoundsList()
    {
        return importantSounds;
    }

    public List<FMOD.Studio.EventInstance> GetHalvedVolumeInstanceList()
    {
        return halvedVolumeInstanceList;
    }

    public List<FMODUnity.StudioEventEmitter> GetAmbienceEmittersList()
    {
        return ambienceEmittersList;
    }
}
