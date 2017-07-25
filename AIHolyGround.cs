using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TownHallPosition
{
    public Quaternion rotation;
    public Vector3 pos;
    public bool taken;

    public TownHallPosition(Quaternion _rotation, Vector3 _pos, bool _taken)
    {
        rotation = _rotation;
        pos = _pos;
        taken = _taken;
    }
}

public class AIStructure
{
    private Structure building;
    private List<Useable> buildingUseables;
    private string name;
    public TownHallPosition townhallPosition;

    public AIStructure(Structure _building)
    {
        building = _building;
        buildingUseables = new List<Useable>();
        buildingUseables.AddRange(building.GetUseables());

        name = building.Data.GetDisplayName();
    }

    public List<Useable> GetBuildingUseables()
    {
        return buildingUseables;
    }

    public Structure GetBuilding()
    {
        return building;
    }

    public string GetName()
    {
        return name;
    }
}

// ==========================================
// LIST OF CONTENT
// ==========================================

// INITIALIZATION AND DECLARATION
// DECISION MAKING 
// CAPTURE CODE
// LEVEL UP CODE
// BUILDING MANAGEMENT CODE

public class AIHolyGround : MonoBehaviour
{
    // ==========================================
    // INITIALIZATION AND DECLARATION STARTS HERE
    // ==========================================

    GameController controller;
    Deity deity;

    List<Unit> units;
    List<Camp> camps;
    List<ShrineSocket> shrines;
    List<City> cities;
    List<City> ownedCities;
    List<TownHallPosition> townHallPositions;
    List<AIStructure> AIStructures;
    /// <summary>
    /// <para> 0: Holy Conflagration </para>
    /// <para> 1: Protector of the People </para>
    /// <para> 2: Saints Blessing </para>
    /// <para> 3: Army of God </para>
    /// <para> 4: Angel of Death </para>
    /// <para> 5: Slayer of Evil </para>
    /// </summary>
    List<Useable> deitySkills;
    /// <summary>
    /// <para> 0: Barracks </para>
    /// <para> 1: Workshop </para>
    /// <para> 2: Cathedral </para>
    /// <para> 3: Tower </para>
    /// <para> 4: BuildingArmorUpgrade </para>
    /// <para> 5: Higher Gold Income </para>
    /// <para> 6: WallUpgrade </para>
    /// </summary>
    List<Useable> buildings;

    List<AIStructure> barracks;
    List<AIStructure> cathedrals;
    List<AIStructure> workShops;

    Camp currentCamp;
    City currentCity;
    ShrineSocket currentShrine;

    private int cityCount;
    private int shrineCount;
    private float updatecounter;
    private bool captured;

    private int campIndex;
    private int shrineIndex;
    private float closestCampDist;
    private float closestShrineDist;

    static private string stateString;
    static private string previousData;

    public enum CurrentOperation { NONE, IDLE, CAPTURE_CAMP, CAPTURE_CITY, CAPTURE_SHRINE, RETREATING }
    static public CurrentOperation currentOperation;

    public enum buildingState { BARRACKS, CATHEDRAL, WORKSHOP, DONE }
    public buildingState currentBuildingState = buildingState.BARRACKS;

    public enum BarracksState { MILITIA, RIFLEMAN, KNIGHT }
    public BarracksState currentBarracksUnit = BarracksState.MILITIA;

    public enum CathedralState { COCKATRICE, GRIFFINRIDER }
    public CathedralState currentCathedralUnit = CathedralState.COCKATRICE;

    public enum UpgradeState { ARMOR_UPGRADE, ATTACK_UPGRADE, LONGRIFLE_UPGRADE, RANGEDATTACK_UPGRADE }
    public UpgradeState currentUpgrade = UpgradeState.ARMOR_UPGRADE;

    public int buildingPlots = 6;

    public void Awake()
    {
        Loading.OnFinishedLoading += Loading_OnFinishedLoading;

        units = new List<Unit>();
        camps = new List<Camp>();
        shrines = new List<ShrineSocket>();
        cities = new List<City>();
        ownedCities = new List<City>();
        deitySkills = new List<Useable>();
        buildings = new List<Useable>();
        townHallPositions = new List<TownHallPosition>();
        AIStructures = new List<AIStructure>();

        barracks = new List<AIStructure>();
        cathedrals = new List<AIStructure>();
        workShops = new List<AIStructure>();

        SetOperation(CurrentOperation.NONE);
        updatecounter = 0;
    }

    private void Loading_OnFinishedLoading(int loadedLevel)
    {
        controller = GameObject.Find("GC 1 (local)").GetComponent<GameController>();
        deity = controller.deity;
        camps.AddRange(FindObjectsOfType<Camp>());
        cities.AddRange(FindObjectsOfType<City>());
        shrines.AddRange(FindObjectsOfType<ShrineSocket>());

        AddNewUnits(Grid.GetAllUnits(Grid.Layer.FRIENDLY));
        deitySkills.AddRange(deity.GetUseables());

        deity.OnLevelUpEvent += OnLevelUpEvent;

        for (int i = 0; i < camps.Count; i++)
        {
            camps[i].OnRespawn += OnRespawn;

            for (int j = 0; j < camps[i].GetUnits().Count; j++)
            {
                camps[i].GetUnits()[j].OnConversion += OnConversion;
            }
        }

        for (int i = 0; i < units.Count; i++)
        {
            units[i].OnTargetFound += OnTargetFound;
            units[i].OnDemise += OnDemise;
        }

        for (int i = 0; i < cities.Count; i++)
        {
            cities[i].OnCapturedEvent += OnCapturedEvent;
        }

        GetNearestCampData(out closestCampDist, out campIndex);
        GetNearestShrineData(out closestShrineDist, out shrineIndex);

        LevelUpSkill(deitySkills[0]);
        GoToNearestCamp();

        Loading.OnFinishedLoading -= Loading_OnFinishedLoading;
    }

    // ==========================================
    // // DECISION MAKING STARTS HERE
    // ==========================================

    private void SetOperation(CurrentOperation _currentOperation)
    {       
        currentOperation = _currentOperation;

        stateString += System.Environment.NewLine + currentOperation.ToString() + " Time: " + Time.time;     
    }

    private void WriteStateData(string data)
    {
        if(previousData != data)
            stateString += System.Environment.NewLine + data + " Time: " + Time.time;

        previousData = data;
    }

    public void MakeDecision()
    {
        if (currentOperation == CurrentOperation.RETREATING)
            return;

        GetNearestCampData(out closestCampDist, out campIndex);
        GetNearestShrineData(out closestShrineDist, out shrineIndex);

        // temp regroup method
        StartCoroutine(RegroupToDeity());
    }

    private Unit GetRandomUnit()
    {
        if (units.Count == 0)
            return null;

        return units[Random.Range(0, units.Count)];
    }

    public IEnumerator RegroupToDeity()
    {
        // make this something that is based on the distance from the deity

        if (currentOperation != CurrentOperation.NONE)
        {
            WriteStateData("REGROUPING");
            currentOperation = CurrentOperation.NONE;

            for (int i = 0; i < units.Count; i++)
            {
                if (units[i] == deity)
                    continue;

                units[i].InteractWithGround(deity.transform.position, Quaternion.identity, false);
            }

            yield return new WaitForSeconds(1.5f);

            if (cityCount < 1)
                GoToNearestCity();
            else if (closestCampDist < closestShrineDist)
                GoToNearestCamp();
            else if (shrines[shrineIndex].GetSquadBehaviour().GetGuards().Count <= units.Count)
            {
                GoToNearestShrine();
            }
            else
                GoToNearestCamp();
        }
    }

    public void AddNewUnit(Unit unit)
    {
        unit.OnDemise -= OnDemise;
        unit.OnDemise += OnDemise;
        unit.OnTargetFound -= OnTargetFound;
        unit.OnTargetFound += OnTargetFound;
        units.Add(unit);
    }

    public void AddNewUnits(List<Unit> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            list[i].OnDemise -= OnDemise;
            list[i].OnDemise += OnDemise;
            list[i].OnTargetFound -= OnTargetFound;
            list[i].OnTargetFound += OnTargetFound;
        }

        units.AddRange(list);
    }

    // ==========================================
    // CAPTURE CODE STARTS HERE
    // ==========================================

    public Vector3 GetAverageUnitPos()
    {
        Vector3 averagePos = Vector3.zero;

        for (int i = 0; i < units.Count; i++)
        {
            Vector3 pos = units[i].transform.position;
            averagePos += pos;
        }
        averagePos /= units.Count;

        return averagePos;
    }

    public void GetNearestCampData(out float closestCampDist, out int closestIndex)
    {
        closestCampDist = float.PositiveInfinity;
        closestIndex = 0;

        for (int i = 0; i < camps.Count; i++)
        {
            if (!camps[i].GetActiveState())
                continue;

            float averageToCampDist = (camps[i].transform.position - GetAverageUnitPos()).magnitude;

            if (averageToCampDist < closestCampDist)
            {
                closestCampDist = averageToCampDist;
                closestIndex = i;
            }
        }
    }

    public void GoToNearestCamp()
    {
        SetOperation(CurrentOperation.CAPTURE_CAMP);

        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] == deity)
                units[i].InteractWithGround(camps[campIndex].transform.position, Quaternion.identity, true);
            else
                units[i].InteractWithGround(camps[campIndex].transform.position, Quaternion.identity, false);
        }

        currentCamp = camps[campIndex];
        StartCoroutine(CheckConvertRange());
    }

    public IEnumerator CheckConvertRange()
    {
        for (int i = 0; i < float.PositiveInfinity; i++)
        {
            if (currentCamp == null)
                break;

            if (currentCamp.IsInRange(deity))
            {
                if(deity.GetTarget() == null)
                    deity.InteractWithGround(camps[campIndex].transform.position, Quaternion.identity, false);
            }
            else
                deity.InteractWithGround(camps[campIndex].transform.position, Quaternion.identity, true);

            if (captured)
            {
                captured = false;
                break;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    public void GetNearestShrineData(out float closestShrineDist, out int closestIndex)
    {
        closestShrineDist = float.PositiveInfinity;
        closestIndex = 0;

        for (int i = 0; i < shrines.Count; i++)
        {
            if (shrines[i].GetActiveState())
                continue;

            float averageToShrineDist = (shrines[i].transform.position - GetAverageUnitPos()).magnitude;

            if (averageToShrineDist < closestShrineDist)
            {
                closestShrineDist = averageToShrineDist;
                closestIndex = i;
            }
        }
    }

    public void GoToNearestShrine()
    {
        SetOperation(CurrentOperation.CAPTURE_SHRINE);

        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] == deity)
                units[i].InteractWithGround(shrines[shrineIndex].transform.position, Quaternion.identity, false);
            else
                units[i].InteractWithGround(shrines[shrineIndex].transform.position - new Vector3(5, 0, 5), Quaternion.identity, false);
        }

        currentShrine = shrines[shrineIndex];
        StartCoroutine(CheckDeityToShrineDist());
    }

    public IEnumerator CheckDeityToShrineDist()
    {
        bool finished = false;

        if (!currentShrine.GetActiveState())
        {
            for (int i = 0; i < float.PositiveInfinity; i++)
            {
                if (currentShrine == null)
                    break;

                if (currentShrine.CanCapture())
                {
                    currentShrine.Activate();
                    finished = true;
                    shrineCount++;
                    MakeDecision();
                    break;
                }
                else
                    yield return new WaitForSeconds(1f);
            }
        }

        if (finished)
            currentShrine = null;
    }

    public int GetNearestCityIndex()
    {
        float closestCityDist = float.PositiveInfinity;
        int closestIndex = 0;

        for (int i = 0; i < cities.Count; i++)
        {
            if (cities[i].GetActiveState())
                continue;

            float averageToCityDist = (cities[i].transform.position - GetAverageUnitPos()).sqrMagnitude;

            if (averageToCityDist < closestCityDist)
            {
                closestCityDist = averageToCityDist;
                closestIndex = i;
            }
        }

        return closestIndex;
    }

    public void GoToNearestCity()
    {
        SetOperation(CurrentOperation.CAPTURE_CITY);
        int closestIndex = 0;

        closestIndex = GetNearestCityIndex();

        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] == deity)
                units[i].InteractWithGround(cities[closestIndex].transform.position, Quaternion.identity, false);
            else
                units[i].InteractWithGround(cities[closestIndex].transform.position - new Vector3(5, 0, 5), Quaternion.identity, false);
        }

        currentCity = cities[closestIndex];
        StartCoroutine(CheckDeityToCityDist());
    }

    public IEnumerator CheckDeityToCityDist()
    {
        bool finished = false;

        if (!currentCity.active)
        {
            for (int i = 0; i < float.PositiveInfinity; i++)
            {
                if (currentCity == null)
                    break;

                if (currentCity.CanCapture())
                {
                    currentCity.Activate();
                    finished = true;
                    cityCount++;
                    MakeDecision();
                    break;
                }
                else
                    yield return new WaitForSeconds(1f);
            }
        }

        if (finished)
            currentCity = null;
    }

    // ==========================================
    // LEVEL UP CODE STARTS HERE
    // ==========================================

    private void LevelUpSkill(Useable skill)
    {
        if (deity.GetSkillPoints() > 0)
        {
            deity.LevelUpSkill(skill as Skill);
            deity.RemoveSkillPoint();
        }
    }

    private void CastSkill(Entity entity, bool targetGround, Entity target, Useable skill)
    {
        if (targetGround)
            entity.Cast(skill as Skill, target.transform.position);
        else
            entity.Cast(skill as Skill, target);
    }

    private Useable ChooseLowestSkill()
    {
        int[] skillLevels = new int[deitySkills.Count];
        float lowestSkillLevel = float.PositiveInfinity;
        int lowestIndex = 0;

        for (int i = 0; i < deitySkills.Count; i++)
        {
            if (deity.GetLevel() <= (deitySkills[i] as Skill).GetRequiredLevel(deity.GetSkillLevel(deitySkills[i] as Skill)))
                continue;

            skillLevels[i] = deity.GetSkillLevel(deitySkills[i] as Skill);

            if (skillLevels[i] < lowestSkillLevel)
            {
                lowestSkillLevel = skillLevels[i];
                lowestIndex = i;
            }
        }

        return deitySkills[lowestIndex] as Skill;
    }

    private Useable ChooseRandomSkill()
    {
        List<int> removedNumbers = new List<int>();
        int currentIndex = 0;

        for (int i = 0; i < deitySkills.Count; i++)
        {
            if (deity.GetLevel() <= (deitySkills[i] as Skill).GetRequiredLevel(deity.GetSkillLevel(deitySkills[i] as Skill)))
            {
                removedNumbers.Add(i);
                continue;
            }

            if ((deitySkills[i] as Passive) != null)
            {
                if (deity.GetSkillLevel(deitySkills[i] as Skill) >= 1)
                {
                    removedNumbers.Add(i);
                    continue;
                }
            }
        }

        for (int i = 0; i < float.PositiveInfinity; i++)
        {
            currentIndex = Random.Range(0, deitySkills.Count);
            bool availableIndex = true;

            for (int j = 0; j < removedNumbers.Count; j++)
            {
                if (currentIndex == removedNumbers[j])
                    availableIndex = false;
            }

            if (!availableIndex)
                continue;

            break;
        }

        return deitySkills[currentIndex] as Skill;
    }

    private void OnLevelUpEvent()
    {
        Useable skill = ChooseRandomSkill();
        LevelUpSkill(skill);

        if (skill == deitySkills[1])
        {
            CastSkill(deity, false, deity, deitySkills[1]);
            (deitySkills[1] as ToggleSkill).triggered = true;
        }
    }

    // ==========================================
    // LEVEL UP CODE ENDS HERE
    // ==========================================

    private void OnDemise(Entity entity)
    {
        if (entity.IsUnit())
        {
            units.Remove(entity as Unit);
            entity.OnDemise -= OnDemise;
            entity.OnTargetFound -= OnTargetFound;

            if (entity == deity)
            {
                deity.OnRespawnDeity += OnRespawnDeity;

                // ownedcity index has to be changed eventually
                for (int i = 0; i < units.Count; i++)
                {
                    units[i].retaliate = false;
                    units[i].InteractWithGround(ownedCities[0].transform.position, Quaternion.identity, true);               
                }
            }
        }
        else if (entity.IsStructure())
        {
            for (int i = 0; i < AIStructures.Count; i++)
            {
                if (AIStructures[i].GetBuilding() == (entity as Structure))
                {
                    // find a way to remove the string comparison later... (make it ID data ID)
                    if (AIStructures[i].GetName() == "Barracks")
                        barracks.Remove(AIStructures[i]);
                    else if (AIStructures[i].GetName() == "Cathedral")
                        cathedrals.Remove(AIStructures[i]);
                    else if (AIStructures[i].GetName() == "Workshop")
                        workShops.Remove(AIStructures[i]);

                    AIStructures[i].townhallPosition.taken = false;
                    AIStructures.Remove(AIStructures[i]);
                    entity.OnDemise -= OnDemise;
                    entity.OnTargetFound -= OnTargetFound;
                }
            }

        }
    }

    private void OnRespawnDeity()
    {
        AddNewUnit(deity);

        for(int i = 0; i < units.Count; i++)
        {
            units[i].retaliate = true;
        }

        MakeDecision();

        deity.OnRespawnDeity -= OnRespawnDeity;
    }

    // ==========================================
    // BUILDING MANAGEMENT CODE STARTS HERE
    // ==========================================

    private void SetBuildingPlots(int amount, TownHall townHall)
    {
        for (int i = 0; i < amount; i++)
        {
            townHallPositions.Add(new TownHallPosition(townHall.transform.rotation, townHall.transform.position, false));
            Vector3 position = Quaternion.Euler(0f, (float)(360f / amount) * i, 0f) * new Vector3(23f, 0f, 0f) + townHall.transform.position;

            townHallPositions[i].pos = position;
            townHallPositions[i].rotation = Quaternion.identity;
        }
    }

    private Useable GetPrioBuilding()
    {
        if (GameController.local.faith >= 300 && currentBuildingState == buildingState.BARRACKS && GameController.local.gold >= buildings[0].goldCost)
            return buildings[0];
        else if (GameController.local.faith >= 800 && currentBuildingState == buildingState.CATHEDRAL && GameController.local.gold >= buildings[2].goldCost)
            return buildings[2];
        else if (units.Count >= 20 && currentBuildingState == buildingState.WORKSHOP && GameController.local.gold >= buildings[1].goldCost)
            return buildings[1];
        else
            return null;
    }

    // faction specific priority list methods for testing purposes (should be moved into specific faction class later) 
    private Useable GetPrioBarracksUnit()
    {
        if (barracks.Count < 1)
            return null;

        if (GameController.local.gold < 250 && currentBuildingState != buildingState.DONE)
            return null;

        if (barracks[0].GetBuildingUseables()[0].CanUse(GameController.local.GetOwnedTownHalls()[0]) && currentBarracksUnit == BarracksState.MILITIA)
            return barracks[0].GetBuildingUseables()[0];
        else if (barracks[0].GetBuildingUseables()[1].CanUse(GameController.local.GetOwnedTownHalls()[0]) && currentBarracksUnit == BarracksState.RIFLEMAN)
            return barracks[0].GetBuildingUseables()[1];
        else if (workShops.Count < 1)
        {
            if (currentBarracksUnit == BarracksState.KNIGHT)
                currentBarracksUnit = 0;
            else
                currentBarracksUnit++;

            return null;
        }
        else if (barracks[0].GetBuildingUseables()[2].CanUse(GameController.local.GetOwnedTownHalls()[0]) && currentBarracksUnit == BarracksState.KNIGHT)
            return barracks[0].GetBuildingUseables()[2];
        else
            return null;
    }

    private Useable GetPrioCathedralUnit()
    {
        if (cathedrals.Count < 1)
            return null;

        if (cathedrals[0].GetBuildingUseables()[0].CanUse(GameController.local.GetOwnedTownHalls()[0]) && currentCathedralUnit == CathedralState.COCKATRICE)
            return cathedrals[0].GetBuildingUseables()[0];
        else if (cathedrals[0].GetBuildingUseables()[1].CanUse(GameController.local.GetOwnedTownHalls()[0]) && currentCathedralUnit == CathedralState.GRIFFINRIDER)
            return cathedrals[0].GetBuildingUseables()[1];
        else
            return null;
    }

    private Useable GetPrioUpgrade(out AIStructure structure)
    {
        structure = null;
        if (barracks.Count < 2)
            return null;

        structure = barracks[1];

        if (barracks[1].GetBuildingUseables()[3].CanUse(GameController.local.GetOwnedTownHalls()[0]) && currentUpgrade == UpgradeState.ATTACK_UPGRADE)
            return barracks[1].GetBuildingUseables()[3];
        else if (barracks[1].GetBuildingUseables()[4].CanUse(GameController.local.GetOwnedTownHalls()[0]) && currentUpgrade == UpgradeState.ARMOR_UPGRADE)
            return barracks[1].GetBuildingUseables()[4];

        if (workShops.Count < 1)
        {
            ++currentUpgrade;
            return null;
        }

        structure = workShops[0];

        if (workShops[0].GetBuildingUseables()[2].CanUse(GameController.local.GetOwnedTownHalls()[0]) && currentUpgrade == UpgradeState.LONGRIFLE_UPGRADE)
            return workShops[0].GetBuildingUseables()[2];
        else if (workShops[0].GetBuildingUseables()[3].CanUse(GameController.local.GetOwnedTownHalls()[0]) && currentUpgrade == UpgradeState.RANGEDATTACK_UPGRADE)
            return workShops[0].GetBuildingUseables()[3];
        else
            return null;
    }

    private void Build(Useable building, int townHallIndex)
    {
        TownHall townHall = GameController.local.GetOwnedTownHalls()[townHallIndex];

        int index = 0;
        List<int> usedNumbers = new List<int>();
        bool canBuild = true;

        for (int i = 0; i < float.PositiveInfinity; i++)
        {
            index = Random.Range(0, townHallPositions.Count);

            if (townHallPositions[index].taken)
            {
                if (!usedNumbers.Contains(i))
                    usedNumbers.Add(i);

                if (usedNumbers.Count > townHallPositions.Count)
                {
                    canBuild = false;
                    break;
                }

                continue;
            }

            break;
        }

        if ((building as StructureData).CanUse(townHall) && canBuild)
        {
            NetworkInstantiation.InstantiateStructure((building as StructureData), townHallPositions[index].pos, townHallPositions[index].rotation, townHall);
            GameController.local.gold -= building.goldCost;
            GameController.local.faith -= building.faithCost;
            townHallPositions[index].taken = true;

            AIStructure currentAIStructure = new AIStructure(GameController.local.GetOwnedTownHalls()[0].GetStructures()[AIStructures.Count + 1]);
            InitializeNewStructure(currentAIStructure, index);
        }
    }

    private void InitializeNewStructure(AIStructure AIstructure, int townhallIndex)
    {
        AIStructures.Add(AIstructure);
        AIstructure.GetBuilding().OnCreateUnit += OnCreateUnit;
        AIstructure.GetBuilding().OnDemise += OnDemise;
        AIstructure.townhallPosition = townHallPositions[townhallIndex];

        // find a way to remove the string comparison later... (make it ID data ID)
        if (AIstructure.GetName() == "Barracks")
            barracks.Add(AIstructure);
        else if (AIstructure.GetName() == "Cathedral")
            cathedrals.Add(AIstructure);
        else if (AIstructure.GetName() == "Workshop")
            workShops.Add(AIstructure);
    }

    private IEnumerator BuildNextBuilding()
    {
        for (int i = 0; i < float.PositiveInfinity; i++)
        {
            if (GetPrioBuilding() != null)
            {
                Build(GetPrioBuilding(), 0);
                ++currentBuildingState;
            }
            else
            {
                if (currentBuildingState == buildingState.DONE)
                    break;
            }

            yield return new WaitForSeconds(2f);
        }
    }

    private IEnumerator BuildUnits()
    {
        for (int i = 0; i < float.PositiveInfinity; i++)
        {
            if (GetPrioBarracksUnit() != null)
            {
                QueueUseable(barracks[0], GetPrioBarracksUnit());
                // make this state something like DONE later
                if (currentBarracksUnit == BarracksState.KNIGHT)
                    currentBarracksUnit = 0;
                else
                    ++currentBarracksUnit;
            }

            if (GetPrioCathedralUnit() != null)
            {
                QueueUseable(cathedrals[0], GetPrioCathedralUnit());

                if (currentCathedralUnit == CathedralState.GRIFFINRIDER)
                    currentCathedralUnit = 0;
                else
                    ++currentCathedralUnit;
            }


            yield return new WaitForSeconds(2f);
        }
    }

    private IEnumerator BuyUpgrade()
    {
        AIStructure structure = null;

        for (int i = 0; i < float.PositiveInfinity; i++)
        {
            Useable prioUpgrade = GetPrioUpgrade(out structure);
            if (prioUpgrade != null)
            {
                QueueUseable(structure, prioUpgrade);

                if (currentUpgrade == UpgradeState.RANGEDATTACK_UPGRADE)
                    currentUpgrade = 0;
                else
                    ++currentUpgrade;
            }

            yield return new WaitForSeconds(2f);
        }
    }

    private void OnCreateUnit(Unit unit)
    {
        AddNewUnit(unit);

        if(!deity.IsDead())
            unit.InteractWithGround(deity.GetDestination() - new Vector3(5, 0, 5), Quaternion.identity, true);
    }

    public void QueueUseable(AIStructure structure, Useable useable)
    {
        GameController.local.gold -= useable.goldCost;
        GameController.local.faith -= useable.faithCost;
        structure.GetBuilding().QueueUseable(useable);
    }

    private void OnCapturedEvent(City city)
    {
        ownedCities.Add(city);
        buildings.AddRange(GameController.local.GetOwnedTownHalls()[0].GetUseables());
        SetBuildingPlots(buildingPlots, GameController.local.GetOwnedTownHalls()[0]);

        Build(buildings[0], 0);
        StartCoroutine(BuildNextBuilding());
        StartCoroutine(BuildUnits());
        StartCoroutine(BuyUpgrade());
    }

    // ==========================================
    // BUILDING MANAGEMENT CODE ENDS HERE
    // ==========================================

    private void OnConversion(NetworkBehaviour converted, int newOwner)
    {
        StartCoroutine(OnConversionEnum());
    }

    private IEnumerator OnConversionEnum()
    {
        yield return new WaitForEndOfFrame();

        if (currentCamp != null)
        {
            AddNewUnits(currentCamp.GetUnits(false));
            MakeDecision();
            captured = true;
            currentCamp = null;
        }
    }

    private void OnTargetFound(Entity target, Entity targetedBy)
    {
        if (targetedBy == deity)
            StartCoroutine(TargetFoundEnum(target));

        // find out later why this is returning an error on the target
        //if (!deity.IsDead())
        //IdleUnitsEngage(targetedBy, target);
    }

    private void IdleUnitsEngage(Entity unit, Entity target)
    {
        if (unit == deity || target == null)
            return;

        unit.InteractWithGround(deity.GetDestination(), Quaternion.identity, false);
    }

    public IEnumerator TargetFoundEnum(Entity target)
    {
        WriteStateData("INTERRUPTED");

        for (int i = 0; i < float.PositiveInfinity; i++)
        {
            if (deity.GetTarget() == null)
            {
                MakeDecision();
                break;
            }
            else
            {
                if (target != null)
                {
                    if (currentOperation == CurrentOperation.CAPTURE_CAMP)
                    {
                        // retreating should be called in different ways later. Currently here for test purposes.
                        if (!Camp.IsCampUnit(target as Unit))
                            StartCoroutine(Retreat(target));
                    }
                    CastSkill(deity, true, target, deitySkills[0]);
                    // base this on unit health later
                    CastSkill(deity, false, GetRandomUnit(), deitySkills[2]);
                    // army of god should be dependent on the enemy army later 
                    CastSkill(deity, true, deity, deitySkills[3]);

                    if (deity.GetStatus().GetHealth() > (deity.GetStatus().GetMaxHealth() / 2) && !(deitySkills[1] as ToggleSkill).triggered)
                    {
                        CastSkill(deity, false, deity, deitySkills[1]);
                        (deitySkills[1] as ToggleSkill).triggered = true;
                    }
                    else if (deity.GetStatus().GetHealth() < (deity.GetStatus().GetMaxHealth() / 2) && (deitySkills[1] as ToggleSkill).triggered)
                    {
                        CastSkill(deity, false, deity, deitySkills[1]);
                        (deitySkills[1] as ToggleSkill).triggered = false;
                    }
                }

                yield return new WaitForSeconds(1f);
            }
        }
    }

    private IEnumerator Retreat(Entity target)
    {
        yield return new WaitForEndOfFrame();

        if (currentOperation != CurrentOperation.RETREATING)
        {
            if (deity.GetTarget() == target)
            {
                SetOperation(CurrentOperation.RETREATING);

                Vector3 toRetreatPos = (target.transform.position - deity.transform.position).normalized;

                for (int i = 0; i < units.Count; i++)
                {
                    units[i].InteractWithGround(cities[0].transform.position, Quaternion.identity, true);
                    units[i].retaliate = false;
                }

                yield return new WaitForSeconds(5f);

                for (int i = 0; i < units.Count; i++)
                {
                    units[i].retaliate = true;
                }

                currentOperation = CurrentOperation.NONE;
                MakeDecision();
            }
        }
    }

    private void OnRespawn(List<Unit> newlySpawned)
    {
        for (int i = 0; i < newlySpawned.Count; i++)
        {
            newlySpawned[i].OnConversion -= OnConversion;
            newlySpawned[i].OnConversion += OnConversion;
        }
    }

    public void OnDestroy()
    {
        Loading.OnFinishedLoading -= Loading_OnFinishedLoading;

        for (int i = 0; i < camps.Count; i++)
        {
            camps[i].OnRespawn -= OnRespawn;
            for (int j = 0; j < camps[i].GetUnits().Count; j++)
            {
                if (camps[i].GetUnits()[j] != null)
                    camps[i].GetUnits()[j].OnConversion -= OnConversion;
            }
        }

        for (int i = 0; i < units.Count; i++)
        {
            units[i].OnTargetFound -= OnTargetFound;
        }
    }

    static public string GetAIState()
    {
        return stateString;
    }
}
