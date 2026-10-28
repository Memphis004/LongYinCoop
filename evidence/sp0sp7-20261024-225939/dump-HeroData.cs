public class HeroData : ICloneable // TypeDefIndex: 8240
{
	// Fields
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public bool isSummon; // 0x10
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public int summonID; // 0x14
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public float summonLv; // 0x18
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public int summonMoveRange; // 0x1C
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public bool summonControlable; // 0x20
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public string summonSourceHero; // 0x28
	public bool interestingStar; // 0x30
	public float manageAiHour; // 0x34
	public bool dailyAIManaged; // 0x38
	public HeroAIData heroAIData; // 0x40
	public HeroAIData heroAIDataArriveTargetRecord; // 0x48
	public HeroAISettingData heroAISettingData; // 0x50
	public int heroID; // 0x58
	public bool speHero; // 0x5C
	public bool tempPlotHero; // 0x5D
	public bool recruitAble; // 0x5E
	public bool loveAble; // 0x5F
	public bool hide; // 0x60
	public bool dead; // 0x61
	public string heroName; // 0x68
	public string heroFamilyName; // 0x70
	public string heroNickName; // 0x78
	public bool isFemale; // 0x80
	public int belongForceID; // 0x84
	public int skillForceID; // 0x88
	public bool outsideForce; // 0x8C
	public bool replacedForce; // 0x8D
	public int forceJobType; // 0x90
	public int forceJobID; // 0x94
	public int forceJobCD; // 0x98
	public int branchLeaderAreaID; // 0x9C
	public float thisMonthContribution; // 0xA0
	public float lastMonthContribution; // 0xA4
	public float thisYearContribution; // 0xA8
	public float lastYearContribution; // 0xAC
	public float lastFightContribution; // 0xB0
	public bool isLeader; // 0xB4
	public int heroForceLv; // 0xB8
	public float heroStrengthLv; // 0xBC
	public int atAreaID; // 0xC0
	public BigMapPos bigMapPos; // 0xC8
	public bool inSafeArea; // 0xD0
	public bool inPrison; // 0xD1
	public int age; // 0xD4
	public int generation; // 0xD8
	public float voicePitch; // 0xDC
	public HeroFaceData faceData; // 0xE0
	public float skinColorDark; // 0xE8
	public int defaultSkinID; // 0xEC
	public int skinID; // 0xF0
	public int skinLv; // 0xF4
	public int changeSkinCd; // 0xF8
	public bool playerSetSkin; // 0xFC
	public int setSkinID; // 0x100
	public int setSkinLv; // 0x104
	public List<int> kungfuSkillFocus; // 0x108
	public List<int> livingSkillFocus; // 0x110
	public List<string> goodKungfuSkillName; // 0x118
	public bool haveMeet; // 0x120
	public float favor; // 0x124
	public List<float> baseAttri; // 0x128
	public List<float> maxAttri; // 0x130
	public List<float> totalAttri; // 0x138
	public List<float> baseFightSkill; // 0x140
	public List<float> maxFightSkill; // 0x148
	public List<float> totalFightSkill; // 0x150
	public List<float> baseLivingSkill; // 0x158
	public List<float> maxLivingSkill; // 0x160
	public List<float> totalLivingSkill; // 0x168
	public List<float> expLivingSkill; // 0x170
	public float hp; // 0x178
	public float maxhp; // 0x17C
	public float realMaxHp; // 0x180
	public float power; // 0x184
	public float maxPower; // 0x188
	public float realMaxPower; // 0x18C
	public float mana; // 0x190
	public float maxMana; // 0x194
	public float realMaxMana; // 0x198
	public float armor; // 0x19C
	public float externalInjury; // 0x1A0
	public float internalInjury; // 0x1A4
	public float poisonInjury; // 0x1A8
	public bool isGovern; // 0x1AC
	public int governLv; // 0x1B0
	public float governContribution; // 0x1B4
	public bool isHornord; // 0x1B8
	public int hornorLv; // 0x1BC
	public float forceContribution; // 0x1C0
	public float fame; // 0x1C4
	public float badFame; // 0x1C8
	public float loyal; // 0x1CC
	public float evil; // 0x1D0
	public float chaos; // 0x1D4
	public int nature; // 0x1D8
	public int talent; // 0x1DC
	public List<int> hobby; // 0x1E0
	public bool rest; // 0x1E8
	public int cureType; // 0x1EC
	public int salary; // 0x1F0
	public int population; // 0x1F4
	public HeroEquipmentData nowEquipment; // 0x1F8
	public int horseSaveRecord; // 0x200
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public ItemData horse; // 0x208
	public int horseArmorSaveRecord; // 0x210
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public ItemData horseArmor; // 0x218
	public ItemListData itemListData; // 0x220
	public ItemListData selfStorage; // 0x228
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public PartPostureData partPosture; // 0x230
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public int selfCureTime; // 0x238
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public float restCureTotalRate; // 0x23C
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public float medResist; // 0x240
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public bool fightProtectTarget; // 0x244
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public bool fightForceEnter; // 0x245
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public bool fightSpeControl; // 0x246
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public BattlePrepareSpellData battlePrepareSpellData; // 0x248
	public List<SkillMaxPracticeExpData> skillMaxPracticeExpData; // 0x250
	public List<int> skillCount; // 0x258
	public List<KungfuSkillLvData> kungfuSkills; // 0x260
	public int internalSkillSaveRecord; // 0x268
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public KungfuSkillLvData internalSkill; // 0x270
	public int dodgeSkillSaveRecord; // 0x278
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public KungfuSkillLvData dodgeSkill; // 0x280
	public int uniqueSkillSaveRecord; // 0x288
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public KungfuSkillLvData uniqueSkill; // 0x290
	public List<int> attackSkillSaveRecord; // 0x298
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public List<KungfuSkillLvData> attackSkills; // 0x2A0
	public int nowActiveSkill; // 0x2A8
	public HeroSpeAddData baseAddData; // 0x2B0
	public HeroSpeAddData totalAddData; // 0x2B8
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public HeroSpeAddData heroBuff; // 0x2C0
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public Dictionary<int, float> heroBuffLevel; // 0x2C8
	public List<int> autoSetting; // 0x2D0
	public bool heroDetailDirty; // 0x2D8
	public bool heroBuffDirty; // 0x2D9
	public int heroIconDirtyCount; // 0x2DC
	public MissionData forceMission; // 0x2E0
	public List<MissionData> missions; // 0x2E8
	public bool inTeam; // 0x2F0
	public int teamLeader; // 0x2F4
	public List<int> teamMates; // 0x2F8
	public int autoLeaveTeamDay; // 0x300
	public bool autoLeaveTeamDestroy; // 0x304
	public bool bodyGuard; // 0x305
	public PlayerInteractionTimeData playerInteractionTimeData; // 0x308
	public int plotNumCount; // 0x310
	public int missionNumCount; // 0x314
	public int playerBecomeTeacherTime; // 0x318
	public int Teacher; // 0x31C
	public List<int> Students; // 0x320
	public int Lover; // 0x328
	public List<int> PreLovers; // 0x330
	public List<int> Relatives; // 0x338
	public List<int> Brothers; // 0x340
	public List<int> Friends; // 0x348
	public List<int> Haters; // 0x350
	public List<string> recordLog; // 0x358
	public float selfHouseTotalAdd; // 0x360
	public float heroTagPoint; // 0x364
	public List<HeroTagData> heroTagData; // 0x368
	public bool bookWriteWorking; // 0x370
	public bool wantRemove; // 0x371
	public bool equipLock; // 0x372
	public bool skillLock; // 0x373
	public int studyNewSkillSetting; // 0x374
	public string settingName; // 0x378
	public int servantForceID; // 0x380
	public bool recruitByPlayer; // 0x384
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	private GameObject shownHpBarRoot; // 0x388
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	private float shownHp; // 0x390
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	private float shownMaxHp; // 0x394
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	private float shownRealMaxHp; // 0x398
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	private GameObject shownMpBarRoot; // 0x3A0
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	private float shownMana; // 0x3A8
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	private float shownMaxMana; // 0x3AC
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	private float shownRealMaxMana; // 0x3B0
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	private GameObject shownPowerBarRoot; // 0x3B8
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	private float shownPower; // 0x3C0
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	private float shownMaxPower; // 0x3C4
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	private float shownRealMaxPower; // 0x3C8
	public bool isTempHero; // 0x3CC
	public bool isRandomEnemy; // 0x3CD
	public bool needRemove; // 0x3CE
	public bool inWater; // 0x3CF
	public bool inMountain; // 0x3D0
	public bool inHill; // 0x3D1
	public float fightScore; // 0x3D4

	// Properties
	public bool HeroIconDirty { get; set; }

	// Methods

	// RVA: 0x8AB5D0 Offset: 0x8AA9D0 VA: 0x1808AB5D0
	public void set_HeroIconDirty(bool value) { }

	// RVA: 0x8AB5C0 Offset: 0x8AA9C0 VA: 0x1808AB5C0
	public bool get_HeroIconDirty() { }

	// RVA: 0x8AA1D0 Offset: 0x8A95D0 VA: 0x1808AA1D0
	public void .ctor() { }

	[OnSerializingAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	// RVA: 0x89D670 Offset: 0x89CA70 VA: 0x18089D670
	internal void OnSerializingMethod(StreamingContext context) { }

	[OnDeserializedAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	// RVA: 0x89CD10 Offset: 0x89C110 VA: 0x18089CD10
	internal void OnDeserializedMethod(StreamingContext context) { }

	// RVA: 0x887F60 Offset: 0x887360 VA: 0x180887F60
	public string GenerateFaceCode() { }

	// RVA: 0x89AB30 Offset: 0x899F30 VA: 0x18089AB30
	public bool LoadFaceCode(string faceCode) { }

	// RVA: 0x88C040 Offset: 0x88B440 VA: 0x18088C040
	public int GetForceLvContributionToMoneyRate() { }

	// RVA: 0x88FC80 Offset: 0x88F080 VA: 0x18088FC80
	public int GetMaxTagNum() { }

	// RVA: 0x894E40 Offset: 0x894240 VA: 0x180894E40
	public float GetTotalTagPoint() { }

	// RVA: 0x8972A0 Offset: 0x8966A0 VA: 0x1808972A0
	public bool HaveForceFunction(int forceID) { }

	// RVA: 0x896C00 Offset: 0x896000 VA: 0x180896C00
	public int GetWeaponResearchWeaponType() { }

	// RVA: 0x897B70 Offset: 0x896F70 VA: 0x180897B70
	public bool HaveSpeInteractWithNPC() { }

	// RVA: 0x88FEA0 Offset: 0x88F2A0 VA: 0x18088FEA0
	public string GetMeditationTopic() { }

	// RVA: 0x8A3C70 Offset: 0x8A3070 VA: 0x1808A3C70
	public void SetHeroID(int _heroID) { }

	// RVA: 0x8A29B0 Offset: 0x8A1DB0 VA: 0x1808A29B0
	public void ResetHeroSkillID() { }

	// RVA: 0x888CA0 Offset: 0x8880A0 VA: 0x180888CA0
	public int GetAskJoinTeamNeedFavor() { }

	// RVA: 0x8981C0 Offset: 0x8975C0 VA: 0x1808981C0
	public bool IsSpeTeammate() { }

	// RVA: 0x888960 Offset: 0x887D60 VA: 0x180888960
	public int GetAskJoinTeamCostFavor() { }

	// RVA: 0x888A80 Offset: 0x887E80 VA: 0x180888A80
	public int GetAskJoinTeamMaxLv() { }

	// RVA: 0x8A2750 Offset: 0x8A1B50 VA: 0x1808A2750
	public void ResetAutoSetting() { }

	// RVA: 0x890830 Offset: 0x88FC30 VA: 0x180890830
	public int GetPreferWeaponType() { }

	// RVA: 0x89F1F0 Offset: 0x89E5F0 VA: 0x18089F1F0
	public void RandomFaceData(bool includeNoRandom = False) { }

	// RVA: 0x894810 Offset: 0x893C10 VA: 0x180894810
	public Color GetSkinColorByDark() { }

	// RVA: 0x897550 Offset: 0x896950 VA: 0x180897550
	public bool HaveMission(string targetMissionName) { }

	// RVA: 0x887350 Offset: 0x886750 VA: 0x180887350
	public MissionData FindMission(string targetMissionName) { }

	// RVA: 0x887440 Offset: 0x886840 VA: 0x180887440
	public MissionData FindMission(int targetMissionID) { }

	// RVA: 0x89CAB0 Offset: 0x89BEB0 VA: 0x18089CAB0
	public bool MissionKeepInTeam() { }

	// RVA: 0x88DA90 Offset: 0x88CE90 VA: 0x18088DA90
	public int GetHeroPermanentTagNum() { }

	// RVA: 0x88A970 Offset: 0x889D70 VA: 0x18088A970
	public int GetEquipmentWeightLv() { }

	// RVA: 0x88FFA0 Offset: 0x88F3A0 VA: 0x18088FFA0
	public int GetMissMeetingReduceContribution() { }

	// RVA: 0x88E0A0 Offset: 0x88D4A0 VA: 0x18088E0A0
	public int GetHitMoveRange() { }

	// RVA: 0x8A9EA0 Offset: 0x8A92A0 VA: 0x1808A9EA0
	public float UsePoisonRate() { }

	// RVA: 0x89BDD0 Offset: 0x89B1D0 VA: 0x18089BDD0
	public void ManagePoisonEquipment(ItemData targetItem) { }

	// RVA: 0x89C460 Offset: 0x89B860 VA: 0x18089C460
	public void ManagePoisonItem(ItemData targetItem) { }

	// RVA: 0x88AB80 Offset: 0x889F80 VA: 0x18088AB80
	public float GetFameForceLv() { }

	// RVA: 0x890260 Offset: 0x88F660 VA: 0x180890260
	public float GetNextForceLvFame() { }

	// RVA: 0x88B420 Offset: 0x88A820 VA: 0x18088B420
	public int GetFightTime(HeroData enemyData) { }

	// RVA: 0x8A3050 Offset: 0x8A2450 VA: 0x1808A3050
	public void SetHeroAIData(HeroAIData target) { }

	// RVA: 0x8A2700 Offset: 0x8A1B00 VA: 0x1808A2700
	public void ResetAI() { }

	// RVA: 0x8940C0 Offset: 0x8934C0 VA: 0x1808940C0
	public SkillMaxPracticeExpData GetSkillMaxPracticeExp(int targetID) { }

	// RVA: 0x87AE10 Offset: 0x87A210 VA: 0x18087AE10
	public void AddSkillMaxPracticeExp(SkillMaxPracticeExpData target) { }

	// RVA: 0x884780 Offset: 0x883B80 VA: 0x180884780
	public void ChangeTagPoint(float delta, bool showInfo) { }

	// RVA: 0x893FD0 Offset: 0x8933D0 VA: 0x180893FD0
	public string GetSkeletonHorseIdleAnim() { }

	// RVA: 0x894020 Offset: 0x893420 VA: 0x180894020
	public string GetSkeletonHorseRunAnim() { }

	// RVA: 0x894070 Offset: 0x893470 VA: 0x180894070
	public string GetSkeletonHorseWalkAnim() { }

	// RVA: 0x88B9F0 Offset: 0x88ADF0 VA: 0x18088B9F0
	public float GetForceJobEffectSkillNum() { }

	// RVA: 0x88BA10 Offset: 0x88AE10 VA: 0x18088BA10
	public float GetForceJobEffectSkillNum(int jobType, int jobID) { }

	// RVA: 0x88BE10 Offset: 0x88B210 VA: 0x18088BE10
	public float GetForceJobSpeAddResult(int forceSpeAddDataType) { }

	// RVA: 0x89C820 Offset: 0x89BC20 VA: 0x18089C820
	public bool MeetForceJobRequire(int jobType) { }

	// RVA: 0x888520 Offset: 0x887920 VA: 0x180888520
	public int GetAISettingPriorityLv(AISettingType targetType) { }

	// RVA: 0x8884B0 Offset: 0x8878B0 VA: 0x1808884B0
	public int GetAISettingFocus(AISettingType targetType) { }

	// RVA: 0x887170 Offset: 0x886570 VA: 0x180887170
	public float Favor(bool floorToInt = False) { }

	// RVA: 0x88FC70 Offset: 0x88F070 VA: 0x18088FC70
	public int GetMaxTeamMate() { }

	// RVA: 0x88FC70 Offset: 0x88F070 VA: 0x18088FC70
	public int GetMaxStudent() { }

	// RVA: 0x88F4E0 Offset: 0x88E8E0 VA: 0x18088F4E0
	public int GetMaxDoctorTime() { }

	// RVA: 0x8894B0 Offset: 0x8888B0 VA: 0x1808894B0
	public float GetBaseAttriNum(BaseAttriType targetAttri) { }

	// RVA: 0x8A3180 Offset: 0x8A2580 VA: 0x1808A3180
	public void SetHeroFavorUI(GameObject heroFavorUI, bool showFullInfo = False) { }

	// RVA: 0x88C2E0 Offset: 0x88B6E0 VA: 0x18088C2E0
	public string GetFullSetName(bool useGrayColor = False) { }

	// RVA: 0x890A50 Offset: 0x88FE50 VA: 0x180890A50
	public string GetQuickDetail(bool fullDetail, bool showForceContribution = False, bool showTagPoint = False, bool showSkillNum = False) { }

	// RVA: 0x884370 Offset: 0x883770 VA: 0x180884370
	public void ChangeSkillPower(SkillChangePowerType skillChangePowerType, float deltaPower = 0) { }

	// RVA: 0x8941A0 Offset: 0x8935A0 VA: 0x1808941A0
	public float GetSkillPowerChargeSpeed(FightSkillType targetSkillType) { }

	// RVA: 0x89CC60 Offset: 0x89C060 VA: 0x18089CC60
	public string Name(bool colored) { }

	// RVA: 0x895420 Offset: 0x894820 VA: 0x180895420
	public int GetUpgradeForceLvNeedSkillNum() { }

	// RVA: 0x88FB10 Offset: 0x88EF10 VA: 0x18088FB10
	public float GetMaxSkillNum(int targetRateLv) { }

	// RVA: 0x894210 Offset: 0x893610 VA: 0x180894210
	public float GetSkillRareLvExpRate(int targetRareLv) { }

	// RVA: 0x889ED0 Offset: 0x8892D0 VA: 0x180889ED0
	public int GetBountyMissionNum() { }

	// RVA: 0x88F2D0 Offset: 0x88E6D0 VA: 0x18088F2D0
	public int GetMaxBountyMissionNum() { }

	// RVA: 0x884340 Offset: 0x883740 VA: 0x180884340
	public void ChangeSelfHouseTotalAdd(float delta) { }

	// RVA: 0x89E160 Offset: 0x89D560 VA: 0x18089E160
	public void PlayHeroSound(AudioClip targetClip, float volume = 1, float pitch = -1) { }

	// RVA: 0x88D850 Offset: 0x88CC50 VA: 0x18088D850
	public AudioClip GetHeroLittleTalkSound() { }

	// RVA: 0x88DF30 Offset: 0x88D330 VA: 0x18088DF30
	public float GetHeroSoundVoiceAgePitch() { }

	// RVA: 0x88DEA0 Offset: 0x88D2A0 VA: 0x18088DEA0
	public float GetHeroSoundAgePitch() { }

	// RVA: 0x88DE80 Offset: 0x88D280 VA: 0x18088DE80
	public int GetHeroSoundAgeID() { }

	// RVA: 0x88D900 Offset: 0x88CD00 VA: 0x18088D900
	public AudioClip GetHeroMeetSound(string meetType = "Meet") { }

	// RVA: 0x88DCB0 Offset: 0x88D0B0 VA: 0x18088DCB0
	public AudioClip GetHeroShoutSound() { }

	// RVA: 0x88D560 Offset: 0x88C960 VA: 0x18088D560
	public AudioClip GetHeroHurtSound() { }

	// RVA: 0x88CA80 Offset: 0x88BE80 VA: 0x18088CA80
	public AudioClip GetHeroDieSound() { }

	// RVA: 0x88DB90 Offset: 0x88CF90 VA: 0x18088DB90
	public AudioClip GetHeroRecoverSound() { }

	// RVA: 0x888590 Offset: 0x887990 VA: 0x180888590
	public int GetAreaID(bool includeNearArea) { }

	// RVA: 0x87BFE0 Offset: 0x87B3E0 VA: 0x18087BFE0
	public string AtAreaName() { }

	// RVA: 0x889B70 Offset: 0x888F70 VA: 0x180889B70
	public BigMapPos GetBigMapPos() { }

	// RVA: 0x892B00 Offset: 0x891F00 VA: 0x180892B00
	public string GetRecordLog() { }

	// RVA: 0x879D30 Offset: 0x879130 VA: 0x180879D30
	public void AddLog(string newLog) { }

	// RVA: 0x88E110 Offset: 0x88D510 VA: 0x18088E110
	public string GetHobbyDescribe() { }

	// RVA: 0x884BF0 Offset: 0x883FF0 VA: 0x180884BF0
	public void CheckHeroDetailDirty(bool forceCount) { }

	// RVA: 0x887520 Offset: 0x886920 VA: 0x180887520
	public ItemData FindRandomItem(int minItemLv, int maxItemLv, bool includeEquipment, int targetItemType = -1) { }

	// RVA: 0x887580 Offset: 0x886980 VA: 0x180887580
	public KungfuSkillLvData FindRandomSkill(int maxSkillRareLv, HeroData exclusiveHero) { }

	// RVA: 0x8971B0 Offset: 0x8965B0 VA: 0x1808971B0
	public void GoInPrison() { }

	// RVA: 0x8971C0 Offset: 0x8965C0 VA: 0x1808971C0
	public void GoOutPrison() { }

	// RVA: 0x892DC0 Offset: 0x8921C0 VA: 0x180892DC0
	public int GetRecruitCost(bool tempRecruit, float rate = 1) { }

	// RVA: 0x889C60 Offset: 0x889060 VA: 0x180889C60
	public int GetBodyGuardNum() { }

	// RVA: 0x87D750 Offset: 0x87CB50 VA: 0x18087D750
	public bool CanUseAttackSkill() { }

	// RVA: 0x87D810 Offset: 0x87CC10 VA: 0x18087D810
	public bool CanUseSkill(KungfuSkillLvData targetSkill) { }

	// RVA: 0x87D5A0 Offset: 0x87C9A0 VA: 0x18087D5A0
	public bool CanMove() { }

	// RVA: 0x88E5C0 Offset: 0x88D9C0 VA: 0x18088E5C0
	public float GetIdentifyKnowledge() { }

	// RVA: 0x8A8210 Offset: 0x8A7610 VA: 0x1808A8210
	public void TryIdentifyAllItem(bool showInfo) { }

	// RVA: 0x8A52D0 Offset: 0x8A46D0 VA: 0x1808A52D0
	public void SetNowActiveSkill(KungfuSkillLvData targetSkill) { }

	// RVA: 0x890290 Offset: 0x88F690 VA: 0x180890290
	public KungfuSkillLvData GetNowActiveSkill() { }

	// RVA: 0x88AA70 Offset: 0x889E70 VA: 0x18088AA70
	public float GetExtraMaxHp() { }

	// RVA: 0x88AAA0 Offset: 0x889EA0 VA: 0x18088AAA0
	public float GetExtraMaxMana() { }

	// RVA: 0x898080 Offset: 0x897480 VA: 0x180898080
	public void InvalidateBarCache() { }

	// RVA: 0x8A4250 Offset: 0x8A3650 VA: 0x1808A4250
	public void SetHpBar(GameObject hpBarRoot, bool force = False) { }

	// RVA: 0x8A4E30 Offset: 0x8A4230 VA: 0x1808A4E30
	public void SetMpBar(GameObject mpBarRoot, bool force = False) { }

	// RVA: 0x8A53E0 Offset: 0x8A47E0 VA: 0x1808A53E0
	public void SetPowerBar(GameObject powerBarRoot, bool force = False) { }

	// RVA: 0x8A51B0 Offset: 0x8A45B0 VA: 0x1808A51B0
	public void SetNeedRemove() { }

	// RVA: 0x89F050 Offset: 0x89E450 VA: 0x18089F050
	public void RandomBigMapMovePos() { }

	// RVA: 0x8A5760 Offset: 0x8A4B60 VA: 0x1808A5760
	public void SetRandomEnemyBadFame() { }

	// RVA: 0x8A2B20 Offset: 0x8A1F20 VA: 0x1808A2B20
	public bool SameForce(HeroData targetHero) { }

	// RVA: 0x87CE10 Offset: 0x87C210 VA: 0x18087CE10
	public void AutoGetFightExp() { }

	// RVA: 0x87C9B0 Offset: 0x87BDB0 VA: 0x18087C9B0
	public void AutoGetFightExp(float rate) { }

	// RVA: 0x8A8160 Offset: 0x8A7560 VA: 0x1808A8160
	public bool StuffStoppable() { }

	// RVA: 0x89FB90 Offset: 0x89EF90 VA: 0x18089FB90
	public void RefreshHorseState(bool showInfo = False) { }

	// RVA: 0x89BCC0 Offset: 0x89B0C0 VA: 0x18089BCC0
	public void ManageHeroHorseRest(float deltaTime) { }

	// RVA: 0x89BCB0 Offset: 0x89B0B0 VA: 0x18089BCB0
	public void ManageHeroHorseMove(float deltaTime) { }

	// RVA: 0x89BA70 Offset: 0x89AE70 VA: 0x18089BA70
	public void ManageHeroHorseMove(float deltaTime, bool isInSafeArea) { }

	// RVA: 0x89BD60 Offset: 0x89B160 VA: 0x18089BD60
	public void ManageHeroHorseTime(float deltaTime) { }

	// RVA: 0x88B4D0 Offset: 0x88A8D0 VA: 0x18088B4D0
	public float GetFinalTravelSpeed() { }

	// RVA: 0x895340 Offset: 0x894740 VA: 0x180895340
	public float GetTravelSpeed() { }

	// RVA: 0x896ED0 Offset: 0x8962D0 VA: 0x180896ED0
	public float GetWeighChangeTravelSpeed() { }

	// RVA: 0x896CD0 Offset: 0x8960D0 VA: 0x180896CD0
	public float GetWeatherChangeTravelSpeed() { }

	// RVA: 0x894B30 Offset: 0x893F30 VA: 0x180894B30
	public float GetTerrainChangeTravelSpeed() { }

	// RVA: 0x895220 Offset: 0x894620 VA: 0x180895220
	public float GetTravelSpeed(bool havePower, bool isSprint) { }

	// RVA: 0x88E540 Offset: 0x88D940 VA: 0x18088E540
	public float GetHorseTravelSpeed() { }

	// RVA: 0x88E430 Offset: 0x88D830 VA: 0x18088E430
	public float GetHorseTravelSpeed(bool havePower, bool isSprint) { }

	// RVA: 0x88E350 Offset: 0x88D750 VA: 0x18088E350
	public float GetHorseScore() { }

	// RVA: 0x8936E0 Offset: 0x892AE0 VA: 0x1808936E0
	public float GetSeeRange() { }

	// RVA: 0x88A9E0 Offset: 0x889DE0 VA: 0x18088A9E0
	public float GetExploreStepRate() { }

	// RVA: 0x2C1A70 Offset: 0x2C0E70 VA: 0x1802C1A70
	public int MaxSelfCureTime() { }

	// RVA: 0x8934D0 Offset: 0x8928D0 VA: 0x1808934D0
	public float GetSelfCureRate() { }

	// RVA: 0x893CB0 Offset: 0x8930B0 VA: 0x180893CB0
	public float GetSelfCurePower() { }

	// RVA: 0x893CA0 Offset: 0x8930A0 VA: 0x180893CA0
	public float GetSelfCurePostureRate() { }

	// RVA: 0x8934F0 Offset: 0x8928F0 VA: 0x1808934F0
	public float GetRestCureRate() { }

	// RVA: 0x8934E0 Offset: 0x8928E0 VA: 0x1808934E0
	public float GetRestCurePower() { }

	// RVA: 0x8934D0 Offset: 0x8928D0 VA: 0x1808934D0
	public float GetRestCurePostureRate() { }

	// RVA: 0x890460 Offset: 0x88F860 VA: 0x180890460
	public float GetPostureCurePower() { }

	// RVA: 0x890450 Offset: 0x88F850 VA: 0x180890450
	public float GetPostureCurePostureRate() { }

	// RVA: 0x88AB00 Offset: 0x889F00 VA: 0x18088AB00
	public static float GetExtraStateValueRate(float statePercent) { }

	// RVA: 0x88AA50 Offset: 0x889E50 VA: 0x18088AA50
	public float GetExtraHpRecoverValueRate() { }

	// RVA: 0x88AAD0 Offset: 0x889ED0 VA: 0x18088AAD0
	public float GetExtraPowerValueRate() { }

	// RVA: 0x896770 Offset: 0x895B70 VA: 0x180896770
	public float GetUseItemValue(ItemData targetItem, bool useExtraStateValueRate = True) { }

	// RVA: 0x893CC0 Offset: 0x8930C0 VA: 0x180893CC0
	public float GetSelfCureValue() { }

	// RVA: 0x893500 Offset: 0x892900 VA: 0x180893500
	public float GetRestValue() { }

	// RVA: 0x890760 Offset: 0x88FB60 VA: 0x180890760
	public float GetPostureValue() { }

	// RVA: 0x890470 Offset: 0x88F870 VA: 0x180890470
	public float GetPostureValue(float recoverRate) { }

	// RVA: 0x87D4B0 Offset: 0x87C8B0 VA: 0x18087D4B0
	public bool BattleControlable() { }

	// RVA: 0x8982F0 Offset: 0x8976F0 VA: 0x1808982F0
	public bool ItemControlable() { }

	// RVA: 0x8984A0 Offset: 0x8978A0 VA: 0x1808984A0
	public bool ItemExchangeable() { }

	// RVA: 0x898650 Offset: 0x897A50 VA: 0x180898650
	public bool ItemLockable() { }

	// RVA: 0x89E280 Offset: 0x89D680 VA: 0x18089E280
	public bool PlayerLeadForce() { }

	// RVA: 0x8898A0 Offset: 0x888CA0 VA: 0x1808898A0
	public float GetBetrayForceRate() { }

	// RVA: 0x89CCE0 Offset: 0x89C0E0 VA: 0x18089CCE0
	public bool NoLoyal() { }

	// RVA: 0x882210 Offset: 0x881610 VA: 0x180882210
	public void ChangeLoyal(float num, bool showInfo = False) { }

	// RVA: 0x8A2A70 Offset: 0x8A1E70 VA: 0x1808A2A70
	public void ResetLoyal() { }

	// RVA: 0x88F100 Offset: 0x88E500 VA: 0x18088F100
	public float GetLoyalWorkRate() { }

	// RVA: 0x88F0B0 Offset: 0x88E4B0 VA: 0x18088F0B0
	public float GetLoyalExpRate() { }

	// RVA: 0x87C250 Offset: 0x87B650 VA: 0x18087C250
	public void AutoChangeLoyal() { }

	// RVA: 0x898670 Offset: 0x897A70 VA: 0x180898670
	public void JoinForceServant(int _forceID) { }

	// RVA: 0x89A870 Offset: 0x899C70 VA: 0x18089A870
	public void LeaveServantForce() { }

	// RVA: 0x8A2F50 Offset: 0x8A2350 VA: 0x1808A2F50
	public void SetForce(int _forceID, int _forceLv) { }

	// RVA: 0x8988D0 Offset: 0x897CD0 VA: 0x1808988D0
	public void JoinForce(int _forceID, int _forceLv = -1, int _generation = -1, bool showInfo = True, bool setTeacherToLeader = True) { }

	// RVA: 0x884E30 Offset: 0x884230 VA: 0x180884E30
	public void CheckOutForceContribution() { }

	// RVA: 0x8854A0 Offset: 0x8848A0 VA: 0x1808854A0
	public void ClearContributionRecord() { }

	// RVA: 0x899700 Offset: 0x898B00 VA: 0x180899700
	public void LeaveForce(bool showInfo, bool removeTeacher = True) { }

	// RVA: 0x8854C0 Offset: 0x8848C0 VA: 0x1808854C0
	public void ClearForceJob() { }

	// RVA: 0x88DC80 Offset: 0x88D080 VA: 0x18088DC80
	public static int GetHeroSalary(int _forceLv) { }

	// RVA: 0x88DB60 Offset: 0x88CF60 VA: 0x18088DB60
	public static int GetHeroPopulation(int _forceLv) { }

	// RVA: 0x8877B0 Offset: 0x886BB0 VA: 0x1808877B0
	public ItemData FindSameBook(ItemData target) { }

	// RVA: 0x8974D0 Offset: 0x8968D0 VA: 0x1808974D0
	public bool HaveHobby(ItemData target) { }

	// RVA: 0x87C1D0 Offset: 0x87B5D0 VA: 0x18087C1D0
	public bool AttackSkillSlotUnlocked(int skillSlotID) { }

	// RVA: 0x8871B0 Offset: 0x8865B0 VA: 0x1808871B0
	public void FightReset() { }

	// RVA: 0x887320 Offset: 0x886720 VA: 0x180887320
	public void FightSettingReset() { }

	// RVA: 0x893EA0 Offset: 0x8932A0 VA: 0x180893EA0
	public SkeletonGraphic GetSkeletonGraphic(Transform targetSkeletonParent) { }

	// RVA: 0x8AA0A0 Offset: 0x8A94A0 VA: 0x1808AA0A0
	public bool UseSpeSkeleton() { }

	// RVA: 0x8A6300 Offset: 0x8A5700 VA: 0x1808A6300
	public void SetSkeletonGraphic(Transform targetSkeletonParent, int forceSkinID = -99, int forceSkinLv = -1) { }

	// RVA: 0x8A6030 Offset: 0x8A5430 VA: 0x1808A6030
	public void SetSkeletonGraphicSkinColor(SkeletonGraphic targetSkeleton) { }

	// RVA: 0x8A5D80 Offset: 0x8A5180 VA: 0x1808A5D80
	public void SetSkeletonGraphicFaceSlot(SkeletonGraphic targetSkeleton, int i, int targetID = -99) { }

	// RVA: 0x8880D0 Offset: 0x8874D0 VA: 0x1808880D0
	public SkeletonAnimation GenerateHeroSkeleton(GameObject parent, Vector3 scale) { }

	// RVA: 0x894910 Offset: 0x893D10 VA: 0x180894910
	public string GetSkinName(bool skeletonAnim = True, int forceSkinID = -99, int forceSkinLv = -1) { }

	// RVA: 0x88A770 Offset: 0x889B70 VA: 0x18088A770
	public int GetDefaultSkinID() { }

	// RVA: 0x8A28A0 Offset: 0x8A1CA0 VA: 0x1808A28A0
	public void ResetDefaultSkin() { }

	// RVA: 0x8A8050 Offset: 0x8A7450 VA: 0x1808A8050
	public void SetSkin(int _skinID, int _skinLv) { }

	// RVA: 0x8882D0 Offset: 0x8876D0 VA: 0x1808882D0
	public SkeletonAnimation GenerateHeroSkeleton(SkeletonAnimation skeleton) { }

	// RVA: 0x89F8D0 Offset: 0x89ECD0 VA: 0x18089F8D0
	public void RefreshHeroSkeleton(SkeletonAnimation skeleton) { }

	// RVA: 0x8A0070 Offset: 0x89F470 VA: 0x1808A0070
	public void RefreshSkeletonHorse(SkeletonAnimation targetSkeleton) { }

	// RVA: 0x8A74D0 Offset: 0x8A68D0 VA: 0x1808A74D0
	public void SetSkeletonSkinColor(SkeletonAnimation targetSkeleton) { }

	// RVA: 0x8A5860 Offset: 0x8A4C60 VA: 0x1808A5860
	public void SetSkeletonFaceSlot(SkeletonAnimation targetSkeleton, int i) { }

	// RVA: 0x8A3D60 Offset: 0x8A3160 VA: 0x1808A3D60
	public void SetHeroWeapon(SkeletonAnimation targetSkeleton) { }

	// RVA: 0x88DF80 Offset: 0x88D380 VA: 0x18088DF80
	public string GetHeroWeaponAttackAnim() { }

	// RVA: 0x8A7BC0 Offset: 0x8A6FC0 VA: 0x1808A7BC0
	public void SetSkillWeapon(SkeletonAnimation targetSkeleton, string weaponName) { }

	// RVA: 0x8A3FA0 Offset: 0x8A33A0 VA: 0x1808A3FA0
	public void SetHeroWeapon(SkeletonAnimation targetSkeleton, string weaponName) { }

	// RVA: 0x88B2F0 Offset: 0x88A6F0 VA: 0x18088B2F0
	public float GetFightScore(bool includeTeamMate) { }

	// RVA: 0x88E5A0 Offset: 0x88D9A0 VA: 0x18088E5A0
	public float GetHpPercent() { }

	// RVA: 0x88F140 Offset: 0x88E540 VA: 0x18088F140
	public float GetManaPercent() { }

	// RVA: 0x890810 Offset: 0x88FC10 VA: 0x180890810
	public float GetPowerPercent() { }

	// RVA: 0x88C210 Offset: 0x88B610 VA: 0x18088C210
	public int GetFullRecoverTime(float baseRecoverRate) { }

	// RVA: 0x892C00 Offset: 0x892000 VA: 0x180892C00
	public float GetRecoverRate(float baseRecoverRate) { }

	// RVA: 0x888E00 Offset: 0x888200 VA: 0x180888E00
	public float GetAttriRate(int attriID) { }

	// RVA: 0x89C680 Offset: 0x89BA80 VA: 0x18089C680
	public void ManageTagTime() { }

	// RVA: 0x897CC0 Offset: 0x8970C0 VA: 0x180897CC0
	public bool HaveTag(int id) { }

	// RVA: 0x87B310 Offset: 0x87A710 VA: 0x18087B310
	public void AddTag(int id, float time = -1, string source, bool showInfo = False, bool needFreshHeroDetail = True) { }

	// RVA: 0x887CE0 Offset: 0x8870E0 VA: 0x180887CE0
	public HeroTagData FindTag(int tagID) { }

	// RVA: 0x8A1E40 Offset: 0x8A1240 VA: 0x1808A1E40
	public void RemoveTag(string tagName, bool showInfo = False) { }

	// RVA: 0x8A2120 Offset: 0x8A1520 VA: 0x1808A2120
	public void RemoveTag(int tagID, bool showInfo = False) { }

	// RVA: 0x886820 Offset: 0x885C20 VA: 0x180886820
	public void DisUnderstandTag(int id) { }

	// RVA: 0x8A85E0 Offset: 0x8A79E0 VA: 0x1808A85E0
	public void UnderstandTag(string tagName, bool showInfo) { }

	// RVA: 0x8A87F0 Offset: 0x8A7BF0 VA: 0x1808A87F0
	public void UnderstandTag(int id, bool showInfo) { }

	// RVA: 0x885330 Offset: 0x884730 VA: 0x180885330
	public void ClearAllTempTag() { }

	// RVA: 0x8A02F0 Offset: 0x89F6F0 VA: 0x1808A02F0
	public void RemoveAllDebuff() { }

	// RVA: 0x88A3D0 Offset: 0x8897D0 VA: 0x18088A3D0
	public float GetBuffLevel(int id) { }

	// RVA: 0x88A240 Offset: 0x889640 VA: 0x18088A240
	public string GetBuffLevelString(int id) { }

	// RVA: 0x8A2C60 Offset: 0x8A2060 VA: 0x1808A2C60
	public void SetBuffTime(int id, float time) { }

	// RVA: 0x8794E0 Offset: 0x8788E0 VA: 0x1808794E0
	public void AddBuff(int id, float time) { }

	// RVA: 0x897270 Offset: 0x896670 VA: 0x180897270
	public bool HaveBuff(HeroSpeAddDataType buff) { }

	// RVA: 0x897240 Offset: 0x896640 VA: 0x180897240
	public bool HaveBuff(int buff) { }

	// RVA: 0x8A2840 Offset: 0x8A1C40 VA: 0x1808A2840
	public void ResetBuff() { }

	// RVA: 0x87C160 Offset: 0x87B560 VA: 0x18087C160
	public bool AttackSelfTeam() { }

	// RVA: 0x897F20 Offset: 0x897320 VA: 0x180897F20
	public string HeroName(bool useSetName = False) { }

	// RVA: 0x87D6B0 Offset: 0x87CAB0 VA: 0x18087D6B0
	public bool CanSetName() { }

	// RVA: 0x897A90 Offset: 0x896E90 VA: 0x180897A90
	public bool HaveSetName() { }

	// RVA: 0x897E20 Offset: 0x897220 VA: 0x180897E20
	public string HeroFamilyName() { }

	// RVA: 0x88DA50 Offset: 0x88CE50 VA: 0x18088DA50
	public string GetHeroName(bool fullDescribe) { }

	// RVA: 0x88CF70 Offset: 0x88C370 VA: 0x18088CF70
	public string GetHeroForceLvDescribe(bool fullName = False) { }

	// RVA: 0x88CBE0 Offset: 0x88BFE0 VA: 0x18088CBE0
	public string GetHeroForceLvDescribeSimplify() { }

	// RVA: 0x892E50 Offset: 0x892250 VA: 0x180892E50
	public int GetRecruitUnlockCost() { }

	// RVA: 0x892FD0 Offset: 0x8923D0 VA: 0x180892FD0
	public float GetRecruitUnlockRate() { }

	// RVA: 0x889890 Offset: 0x888C90 VA: 0x180889890
	public int GetBetrayForceBadTime() { }

	// RVA: 0x889880 Offset: 0x888C80 VA: 0x180889880
	public int GetBetrayForceBadFame() { }

	// RVA: 0x87D5D0 Offset: 0x87C9D0 VA: 0x18087D5D0
	public bool CanPlayerMeet() { }

	// RVA: 0x88FF80 Offset: 0x88F380 VA: 0x18088FF80
	public float GetMeetNeedFame() { }

	// RVA: 0x88F570 Offset: 0x88E970 VA: 0x18088F570
	public float GetMaxFavor(float maxFavor = 100) { }

	// RVA: 0x8900C0 Offset: 0x88F4C0 VA: 0x1808900C0
	public float GetNatureFavorRate() { }

	// RVA: 0x88AC70 Offset: 0x88A070 VA: 0x18088AC70
	public float GetFavorRate(float num) { }

	// RVA: 0x87EE30 Offset: 0x87E230 VA: 0x18087EE30
	public void ChangeFavor(float num, bool showPopInfo = True, float maxFavor = 100, float forceRate = 0, bool successSound = False) { }

	// RVA: 0x894A00 Offset: 0x893E00 VA: 0x180894A00
	public float GetStartFavor(HeroData targetHero) { }

	// RVA: 0x8A3D40 Offset: 0x8A3140 VA: 0x1808A3D40
	public void SetHeroMeet(bool showInfo = True, float startFavor = -999) { }

	// RVA: 0x8A49D0 Offset: 0x8A3DD0 VA: 0x1808A49D0
	public void SetMeetFavor(bool showPopInfo, float startFavor = -999) { }

	// RVA: 0x8A2DC0 Offset: 0x8A21C0 VA: 0x1808A2DC0
	public void SetFavor(float num, bool showPopInfo) { }

	// RVA: 0x897410 Offset: 0x896810 VA: 0x180897410
	public bool HaveFriend(int _heroID) { }

	// RVA: 0x8796F0 Offset: 0x878AF0 VA: 0x1808796F0
	public void AddFriend(int _heroID, bool showInfo = False) { }

	// RVA: 0x8A0A00 Offset: 0x89FE00 VA: 0x1808A0A00
	public void RemoveFriend(int _heroID, bool showInfo) { }

	// RVA: 0x897470 Offset: 0x896870 VA: 0x180897470
	public bool HaveHater(int _heroID) { }

	// RVA: 0x8799C0 Offset: 0x878DC0 VA: 0x1808799C0
	public void AddHater(int _heroID, bool showInfo = False) { }

	// RVA: 0x8A0D10 Offset: 0x8A0110 VA: 0x1808A0D10
	public void RemoveHater(int _heroID, bool showInfo) { }

	// RVA: 0x8971E0 Offset: 0x8965E0 VA: 0x1808971E0
	public bool HaveBrother(int _heroID) { }

	// RVA: 0x879150 Offset: 0x878550 VA: 0x180879150
	public void AddBrother(int _heroID, bool showInfo = False) { }

	// RVA: 0x8A06F0 Offset: 0x89FAF0 VA: 0x1808A06F0
	public void RemoveBrother(int _heroID, bool showInfo) { }

	// RVA: 0x8A1680 Offset: 0x8A0A80 VA: 0x1808A1680
	public void RemoveRelative(int _heroID, bool showInfo) { }

	// RVA: 0x897E10 Offset: 0x897210 VA: 0x180897E10
	public bool HaveTeacher() { }

	// RVA: 0x894A60 Offset: 0x893E60 VA: 0x180894A60
	public HeroData GetTeacher() { }

	// RVA: 0x897C60 Offset: 0x897060 VA: 0x180897C60
	public bool HaveStudent(int _heroID) { }

	// RVA: 0x87AED0 Offset: 0x87A2D0 VA: 0x18087AED0
	public void AddStudent(int _heroID, bool showInfo = False) { }

	// RVA: 0x8A1990 Offset: 0x8A0D90 VA: 0x1808A1990
	public void RemoveStudent(int _heroID, bool showInfo) { }

	// RVA: 0x8A0620 Offset: 0x89FA20 VA: 0x1808A0620
	public void RemoveAllStudent(bool showInfo) { }

	// RVA: 0x897540 Offset: 0x896940 VA: 0x180897540
	public bool HaveLover() { }

	// RVA: 0x8A45D0 Offset: 0x8A39D0 VA: 0x1808A45D0
	public void SetLover(int _heroID, bool showInfo) { }

	// RVA: 0x8A0FE0 Offset: 0x8A03E0 VA: 0x1808A0FE0
	public void RemoveLover(bool showInfo) { }

	// RVA: 0x897630 Offset: 0x896A30 VA: 0x180897630
	public bool HavePrelover(int _heroID) { }

	// RVA: 0x87A130 Offset: 0x879530 VA: 0x18087A130
	public void AddPrelover(int _heroID, bool showInfo = False) { }

	// RVA: 0x8A12D0 Offset: 0x8A06D0 VA: 0x1808A12D0
	public void RemovePrelover(int _heroID, bool showInfo) { }

	// RVA: 0x8A0550 Offset: 0x89F950 VA: 0x1808A0550
	public void RemoveAllPrelover(bool showInfo) { }

	// RVA: 0x884F60 Offset: 0x884360 VA: 0x180884F60
	public void CheckPlayerMakeLoverUnhappy() { }

	// RVA: 0x8A23F0 Offset: 0x8A17F0 VA: 0x1808A23F0
	public void RemoveTeacher(bool showInfo) { }

	// RVA: 0x893110 Offset: 0x892510 VA: 0x180893110
	public string GetRelationShipText(int _heroID, bool fullDescribe = True, bool useColor = False) { }

	// RVA: 0x897690 Offset: 0x896A90 VA: 0x180897690
	public bool HaveRelationBetterThanFriend(int _heroID, bool includeFriend = False, bool includeTeacherStudent = True) { }

	// RVA: 0x897D90 Offset: 0x897190 VA: 0x180897D90
	public bool HaveTeacherStudentRelation(int _heroID) { }

	// RVA: 0x87C6C0 Offset: 0x87BAC0 VA: 0x18087C6C0
	public void AutoFightQuickChangeState(float rate, float injuryRate = 1, bool showInfo = False) { }

	// RVA: 0x880F60 Offset: 0x880360 VA: 0x180880F60
	public void ChangeHp(float num, bool useRecoverRate = True, bool noDead = False, bool needRefresh = True, bool showInfo = False) { }

	// RVA: 0x886800 Offset: 0x885C00 VA: 0x180886800
	public void DeadToAlive() { }

	// RVA: 0x882DE0 Offset: 0x8821E0 VA: 0x180882DE0
	public void ChangeMaxHp(float num, bool showInfo = False) { }

	// RVA: 0x882690 Offset: 0x881A90 VA: 0x180882690
	public void ChangeMana(float num, bool useRecoverRate = True, bool needRefresh = True, bool showInfo = False) { }

	// RVA: 0x883290 Offset: 0x882690 VA: 0x180883290
	public void ChangeMaxMana(float num, bool showInfo = False) { }

	// RVA: 0x883AB0 Offset: 0x882EB0 VA: 0x180883AB0
	public void ChangePower(float num, bool showInfo = False) { }

	// RVA: 0x883470 Offset: 0x882870 VA: 0x180883470
	public void ChangeMaxPower(float num, bool showInfo = False) { }

	// RVA: 0x897170 Offset: 0x896570 VA: 0x180897170
	public float GetWoundResist() { }

	// RVA: 0x88A460 Offset: 0x889860 VA: 0x18088A460
	public float GetDamageResist() { }

	// RVA: 0x883D70 Offset: 0x883170 VA: 0x180883D70
	public void ChangeRandomInjury(float num, bool showText = False, bool gainExp = False) { }

	// RVA: 0x87E7D0 Offset: 0x87DBD0 VA: 0x18087E7D0
	public float ChangeExternalInjury(float num, bool showText = False, bool gainExp = False, bool extraResist = False) { }

	// RVA: 0x881180 Offset: 0x880580 VA: 0x180881180
	public float ChangeInternalInjury(float num, bool showText = False, bool gainExp = False, bool extraResist = False) { }

	// RVA: 0x883810 Offset: 0x882C10 VA: 0x180883810
	public float ChangePoisonInjury(float num, bool showText = False, bool gainExp = False, bool extraResist = False) { }

	// RVA: 0x887DC0 Offset: 0x8871C0 VA: 0x180887DC0
	public void FullRecover(bool showInfo) { }

	// RVA: 0x894D60 Offset: 0x894160 VA: 0x180894D60
	public float GetTotalInjury() { }

	// RVA: 0x87C4E0 Offset: 0x87B8E0 VA: 0x18087C4E0
	public void AutoCureSelfInjury() { }

	// RVA: 0x8938F0 Offset: 0x892CF0 VA: 0x1808938F0
	public float GetSelfCureInjury(float targetSKillNum, float speRate = 1) { }

	// RVA: 0x893750 Offset: 0x892B50 VA: 0x180893750
	public float GetSelfCureExternalInjurySkill(float buildingSkill = 0) { }

	// RVA: 0x893870 Offset: 0x892C70 VA: 0x180893870
	public float GetSelfCureExternalInjury(float speRate = 1) { }

	// RVA: 0x8937C0 Offset: 0x892BC0 VA: 0x1808937C0
	public int GetSelfCureExternalInjuryTime(float buildingSkillNum = 0, float speRate = 1) { }

	// RVA: 0x8939B0 Offset: 0x892DB0 VA: 0x1808939B0
	public float GetSelfCureInternalInjurySkill(float buildingSkill = 0) { }

	// RVA: 0x893AC0 Offset: 0x892EC0 VA: 0x180893AC0
	public float GetSelfCureInternalInjury(float speRate = 1) { }

	// RVA: 0x893A60 Offset: 0x892E60 VA: 0x180893A60
	public int GetSelfCureInternalInjuryTime(float buildingSkillNum = 0, float speRate = 1) { }

	// RVA: 0x893B00 Offset: 0x892F00 VA: 0x180893B00
	public float GetSelfCurePoisonInjurySkill(float buildingSkill = 0) { }

	// RVA: 0x893C20 Offset: 0x893020 VA: 0x180893C20
	public float GetSelfCurePoisonInjury(float speRate = 1) { }

	// RVA: 0x893B70 Offset: 0x892F70 VA: 0x180893B70
	public int GetSelfCurePoisonInjuryTime(float buildingSkillNum = 0, float speRate = 1) { }

	// RVA: 0x881420 Offset: 0x880820 VA: 0x180881420
	public void ChangeLivingSkillExp(int id, float num, bool showText = False) { }

	// RVA: 0x88F040 Offset: 0x88E440 VA: 0x18088F040
	public float GetLivingSkillExpMax(int id) { }

	// RVA: 0x881BD0 Offset: 0x880FD0 VA: 0x180881BD0
	public void ChangeLivingSkill(int id, float num, bool showText, bool skillUpgrade = False) { }

	// RVA: 0x87F500 Offset: 0x87E900 VA: 0x18087F500
	public void ChangeFightSkill(int id, float num, bool showText, bool skillUpgrade = False) { }

	// RVA: 0x87DC10 Offset: 0x87D010 VA: 0x18087DC10
	public void ChangeAttri(int id, float num, bool showText, bool skillUpgrade = False) { }

	// RVA: 0x884A40 Offset: 0x883E40 VA: 0x180884A40
	public void CheckForceJobDetailDirty(EquipmentData equipmentData) { }

	// RVA: 0x884AB0 Offset: 0x883EB0 VA: 0x180884AB0
	public void CheckForceJobDetailDirty(HeroSpeAddData heroSpeAddData) { }

	// RVA: 0x8848D0 Offset: 0x883CD0 VA: 0x1808848D0
	public void CheckForceJobDetailDirty(int livingSkill) { }

	// RVA: 0x8828C0 Offset: 0x881CC0 VA: 0x1808828C0
	public void ChangeMaxAttri(int id, int num, bool showInfo) { }

	// RVA: 0x882B50 Offset: 0x881F50 VA: 0x180882B50
	public void ChangeMaxFightSkill(int id, int num, bool showInfo) { }

	// RVA: 0x882FC0 Offset: 0x8823C0 VA: 0x180882FC0
	public void ChangeMaxLivingSkill(int id, int num, bool showInfo) { }

	// RVA: 0x87DA10 Offset: 0x87CE10 VA: 0x18087DA10
	public void ChangeAttriAndSkill(HeroSpeAddDataType speAddDataType, float num) { }

	// RVA: 0x892A80 Offset: 0x891E80 VA: 0x180892A80
	public float GetRealMaxPoint(float nowMaxPoint, float limitPoint) { }

	// RVA: 0x88F960 Offset: 0x88ED60 VA: 0x18088F960
	public int GetMaxPointLv(float nowMaxPoint, float limitPoint) { }

	// RVA: 0x88EFD0 Offset: 0x88E3D0 VA: 0x18088EFD0
	public float GetLeftMaxPoint(float nowMaxPoint, float limitPoint) { }

	// RVA: 0x88F9A0 Offset: 0x88EDA0 VA: 0x18088F9A0
	public int GetMaxSkillNeedPoint(int maxSkill) { }

	// RVA: 0x896510 Offset: 0x895910 VA: 0x180896510
	public int GetUpgradeNeedMaxAttri(int id) { }

	// RVA: 0x896250 Offset: 0x895650 VA: 0x180896250
	public int GetUpgradeLeftMaxAttri(int id) { }

	// RVA: 0x88F160 Offset: 0x88E560 VA: 0x18088F160
	public float GetMaxAttri(int id) { }

	// RVA: 0x896640 Offset: 0x895A40 VA: 0x180896640
	public int GetUpgradeNeedMaxFightSkill(int id) { }

	// RVA: 0x8963B0 Offset: 0x8957B0 VA: 0x1808963B0
	public int GetUpgradeLeftMaxFightSkill(int id) { }

	// RVA: 0x88F6F0 Offset: 0x88EAF0 VA: 0x18088F6F0
	public float GetMaxFightSkill(int id) { }

	// RVA: 0x88F860 Offset: 0x88EC60 VA: 0x18088F860
	public float GetMaxLivingSkill(int id) { }

	// RVA: 0x89FCF0 Offset: 0x89F0F0 VA: 0x18089FCF0
	public void RefreshMaxAttriAndSkill() { }

	// RVA: 0x889680 Offset: 0x888A80 VA: 0x180889680
	public int GetBaseMoveRange() { }

	// RVA: 0x88FFC0 Offset: 0x88F3C0 VA: 0x18088FFC0
	public int GetMoveRange() { }

	// RVA: 0x88A060 Offset: 0x889460 VA: 0x18088A060
	public void GetBounty(int num, HeroData bountyTarget, bool showInfo) { }

	// RVA: 0x883640 Offset: 0x882A40 VA: 0x180883640
	public void ChangeMoney(int num, bool showInfo = True) { }

	// RVA: 0x87EA70 Offset: 0x87DE70 VA: 0x18087EA70
	public void ChangeFame(float num, bool showInfo = True) { }

	// RVA: 0x884CC0 Offset: 0x8840C0 VA: 0x180884CC0
	public void CheckHeroFameForceLv() { }

	// RVA: 0x88ABC0 Offset: 0x889FC0 VA: 0x18088ABC0
	public float GetFameRate() { }

	// RVA: 0x88C780 Offset: 0x88BB80 VA: 0x18088C780
	public float GetGameDifficultyFameRate() { }

	// RVA: 0x88C9A0 Offset: 0x88BDA0 VA: 0x18088C9A0
	public float GetGovernReduceBadFameRate() { }

	// RVA: 0x88C970 Offset: 0x88BD70 VA: 0x18088C970
	public float GetGovernExtraFameRate() { }

	// RVA: 0x88E250 Offset: 0x88D650 VA: 0x18088E250
	public float GetHornorReduceBadFameRate() { }

	// RVA: 0x88E250 Offset: 0x88D650 VA: 0x18088E250
	public float GetHornorAddFavorRate() { }

	// RVA: 0x889FA0 Offset: 0x8893A0 VA: 0x180889FA0
	public int GetBountyPirce() { }

	// RVA: 0x889170 Offset: 0x888570 VA: 0x180889170
	public int GetBadFameFineMoney() { }

	// RVA: 0x89B370 Offset: 0x89A770 VA: 0x18089B370
	public void ManageAIInPrison(bool showInfo) { }

	// RVA: 0x87E0B0 Offset: 0x87D4B0 VA: 0x18087E0B0
	public void ChangeBadFame(float num, bool showInfo = True, HeroData targetHero, bool changeDirect = False) { }

	// RVA: 0x8891D0 Offset: 0x8885D0 VA: 0x1808891D0
	public float GetBadFameRate(HeroData targetHero) { }

	// RVA: 0x8980D0 Offset: 0x8974D0 VA: 0x1808980D0
	public bool IsPlayerSameForce() { }

	// RVA: 0x88D360 Offset: 0x88C760 VA: 0x18088D360
	public string GetHeroGovernName() { }

	// RVA: 0x88D460 Offset: 0x88C860 VA: 0x18088D460
	public string GetHeroHornorName() { }

	// RVA: 0x88E280 Offset: 0x88D680 VA: 0x18088E280
	public int GetHornorChangeMaxArea() { }

	// RVA: 0x88E2A0 Offset: 0x88D6A0 VA: 0x18088E2A0
	public float GetHornorUpgradeCost() { }

	// RVA: 0x88C9D0 Offset: 0x88BDD0 VA: 0x18088C9D0
	public float GetGovernUpgradeCost() { }

	// RVA: 0x880250 Offset: 0x87F650 VA: 0x180880250
	public void ChangeGovernContribution(float num, bool showInfo = True) { }

	// RVA: 0x8804B0 Offset: 0x87F8B0 VA: 0x1808804B0
	public void ChangeGovernLv(int num) { }

	// RVA: 0x880EA0 Offset: 0x8802A0 VA: 0x180880EA0
	public void ChangeHornorLv(int num) { }

	// RVA: 0x89E110 Offset: 0x89D510 VA: 0x18089E110
	public float OutsideForceExtraContributionRate(int forceID = -1) { }

	// RVA: 0x8A2B60 Offset: 0x8A1F60 VA: 0x1808A2B60
	public float SelfForceContrituion() { }

	// RVA: 0x87F9A0 Offset: 0x87EDA0 VA: 0x18087F9A0
	public void ChangeForceContribution(float num, bool showInfo = True, int targetForce = -1) { }

	// RVA: 0x880570 Offset: 0x87F970 VA: 0x180880570
	public void ChangeHeroForceLv(int num, bool showInfo = True) { }

	// RVA: 0x89B8E0 Offset: 0x89ACE0 VA: 0x18089B8E0
	public void ManageHeroForceLvChangeMaxAttri(int num) { }

	// RVA: 0x89F880 Offset: 0x89EC80 VA: 0x18089F880
	public void RefreshHeroSalaryAndPopulation() { }

	// RVA: 0x8A3850 Offset: 0x8A2C50 VA: 0x1808A3850
	public void SetHeroForceLv(int _forceLv) { }

	// RVA: 0x890350 Offset: 0x88F750 VA: 0x180890350
	public float GetPlayerTeamMateExpRate(KungfuSkillLvData targetSkill) { }

	// RVA: 0x87D260 Offset: 0x87C660 VA: 0x18087D260
	public void BattleChangeSkillFightExp(float num, KungfuSkillLvData targetSkill, bool showInfo = False) { }

	// RVA: 0x87A960 Offset: 0x879D60 VA: 0x18087A960
	public void AddSkillFightExp(float num, KungfuSkillLvData targetSkill, bool showInfo = False) { }

	// RVA: 0x88B190 Offset: 0x88A590 VA: 0x18088B190
	public float GetFightExpRate(KungfuSkillLvData targetSkill) { }

	// RVA: 0x87A4C0 Offset: 0x8798C0 VA: 0x18087A4C0
	public void AddSkillBookExp(float num, KungfuSkillLvData targetSkill, bool showInfo = False) { }

	// RVA: 0x889D70 Offset: 0x889170 VA: 0x180889D70
	public float GetBookExpRate(KungfuSkillLvData targetSkill) { }

	// RVA: 0x88B890 Offset: 0x88AC90 VA: 0x18088B890
	public float GetForceBookStorageExpRate(int skillID) { }

	// RVA: 0x88C390 Offset: 0x88B790 VA: 0x18088C390
	public float GetGameDifficultyExpRate() { }

	// RVA: 0x884FE0 Offset: 0x8843E0 VA: 0x180884FE0
	public void CheckSkillUpgrade(KungfuSkillLvData targetSkill) { }

	// RVA: 0x8A9310 Offset: 0x8A8710 VA: 0x1808A9310
	public void UpgradeSkill(KungfuSkillLvData targetSkill) { }

	// RVA: 0x894310 Offset: 0x893710 VA: 0x180894310
	public KungfuSkillLvData GetSkill(KungfuSkillLvData skillLvData, bool showInfo = False, bool speShow = False) { }

	// RVA: 0x887C00 Offset: 0x887000 VA: 0x180887C00
	public KungfuSkillLvData FindSkill(int targetSkillID) { }

	// RVA: 0x88EB20 Offset: 0x88DF20 VA: 0x18088EB20
	public void GetItem(ItemListData _itemListData, bool showPopInfo, bool showSpeGetItem, int treasureChestClickTime = -1) { }

	// RVA: 0x88EAE0 Offset: 0x88DEE0 VA: 0x18088EAE0
	public void GetItem(ItemData itemData, bool showPopInfo) { }

	// RVA: 0x88E750 Offset: 0x88DB50 VA: 0x18088E750
	public void GetItem(ItemData itemData, bool showPopInfo, bool showSpeGetItem, int treasureChestClickTime = -1, bool skipManageItemPoison = False) { }

	// RVA: 0x89B740 Offset: 0x89AB40 VA: 0x18089B740
	public float ManageGetItemPoison(ItemData itemData, bool showInfo, float poisonRate = 1, bool reducePoisonNum = True) { }

	// RVA: 0x89B570 Offset: 0x89A970 VA: 0x18089B570
	public float ManageGetEquipPoison(ItemData equipData, bool showInfo, float poisonRate = 1, float injuryRate = 1, float reduceRate = 0.8) { }

	// RVA: 0x87CE20 Offset: 0x87C220 VA: 0x18087CE20
	public void AutoManageEquipPoison() { }

	// RVA: 0x89E390 Offset: 0x89D790 VA: 0x18089E390
	public void RandomAttriAndSkill() { }

	// RVA: 0x89AF40 Offset: 0x89A340 VA: 0x18089AF40
	public void LoseItem(ItemData itemData, bool showPopInfo) { }

	// RVA: 0x89AD30 Offset: 0x89A130 VA: 0x18089AD30
	public void LoseAllItem() { }

	// RVA: 0x89AE90 Offset: 0x89A290 VA: 0x18089AE90
	public void LoseAllSkill() { }

	// RVA: 0x89B0D0 Offset: 0x89A4D0 VA: 0x18089B0D0
	public void LoseSkill(KungfuSkillLvData targetSkill) { }

	// RVA: 0x886F00 Offset: 0x886300 VA: 0x180886F00
	public void EquipSkill(KungfuSkillLvData skillLvData, bool playSound = False) { }

	// RVA: 0x8A9160 Offset: 0x8A8560 VA: 0x1808A9160
	public void UnequipSkill(KungfuSkillLvData skillLvData, bool playSound = False) { }

	// RVA: 0x8869B0 Offset: 0x885DB0 VA: 0x1808869B0
	public void EquipHorse(ItemData itemData, bool playSound) { }

	// RVA: 0x8A8B50 Offset: 0x8A7F50 VA: 0x1808A8B50
	public void UnequipHorse(ItemData itemData, bool playSound, bool forceUnequip) { }

	// RVA: 0x886C70 Offset: 0x886070 VA: 0x180886C70
	public void EquipItem(ItemData itemData, bool playSound = False, bool showInfo = False) { }

	// RVA: 0x8A8DF0 Offset: 0x8A81F0 VA: 0x1808A8DF0
	public void UnequipItem(ItemData itemData, bool playSound = False, bool forceUnequip = False) { }

	// RVA: 0x88FE30 Offset: 0x88F230 VA: 0x18088FE30
	public float GetMedResist() { }

	// RVA: 0x885A00 Offset: 0x884E00 VA: 0x180885A00
	public void CosumeMedFood(ItemData itemData, bool showInfo = False, HeroData sourceHero, float percent = 1) { }

	// RVA: 0x8A99B0 Offset: 0x8A8DB0 VA: 0x1808A99B0
	public void UseMedFood(ItemData itemData, bool playSound = False, bool showInfo = False, HeroData sourceHero) { }

	// RVA: 0x88B650 Offset: 0x88AA50 VA: 0x18088B650
	public void GetFoodSpeBuff(ItemData itemData) { }

	// RVA: 0x896F30 Offset: 0x896330 VA: 0x180896F30
	public void GetWineSpeBuff(ItemData itemData) { }

	// RVA: 0x88A5F0 Offset: 0x8899F0 VA: 0x18088A5F0
	public void GetDebateSpeBuff(int buffLv) { }

	// RVA: 0x87BE10 Offset: 0x87B210 VA: 0x18087BE10
	public void AddTempTag(HeroTagDataBase tempTag, int time, bool showInfo) { }

	// RVA: 0x8A9460 Offset: 0x8A8860 VA: 0x1808A9460
	public void UpgradeTempTag(string tagName, HeroSpeAddDataType changeTarget, float changeNum, int setValue) { }

	// RVA: 0x897990 Offset: 0x896D90 VA: 0x180897990
	public bool HaveResource(List<ResourceData> resourceList) { }

	// RVA: 0x8977C0 Offset: 0x896BC0 VA: 0x1808977C0
	public bool HaveResource(ResourceData resource) { }

	// RVA: 0x8977F0 Offset: 0x896BF0 VA: 0x1808977F0
	public bool HaveResource(int id, float num) { }

	// RVA: 0x8857D0 Offset: 0x884BD0 VA: 0x1808857D0
	public void CostResource(List<ResourceData> resourceList) { }

	// RVA: 0x8859D0 Offset: 0x884DD0 VA: 0x1808859D0
	public void CostResource(ResourceData resource) { }

	// RVA: 0x8858A0 Offset: 0x884CA0 VA: 0x1808858A0
	public void CostResource(int id, float num) { }

	// RVA: 0x883F90 Offset: 0x883390 VA: 0x180883F90
	public void ChangeResource(int id, float num, bool showInfo, bool forceContribution = True, int targetForceID = -1, float contributionRate = 1) { }

	// RVA: 0x883E70 Offset: 0x883270 VA: 0x180883E70
	public void ChangeResource(List<float> num, bool showInfo, bool forceContribution = True) { }

	// RVA: 0x8973F0 Offset: 0x8967F0 VA: 0x1808973F0
	public bool HaveForce() { }

	// RVA: 0x897B50 Offset: 0x896F50 VA: 0x180897B50
	public bool HaveSkillForce() { }

	// RVA: 0x897A70 Offset: 0x896E70 VA: 0x180897A70
	public bool HaveServantForce() { }

	// RVA: 0x8971D0 Offset: 0x8965D0 VA: 0x1808971D0
	public bool HaveArea() { }

	// RVA: 0x888880 Offset: 0x887C80 VA: 0x180888880
	public AreaData GetArea() { }

	// RVA: 0x88C0B0 Offset: 0x88B4B0 VA: 0x18088C0B0
	public ForceData GetForce(bool includeServant = False) { }

	// RVA: 0x88BF40 Offset: 0x88B340 VA: 0x18088BF40
	public HeroData GetForceLeader() { }

	// RVA: 0x895520 Offset: 0x894920 VA: 0x180895520
	public string GetUpgradeForceLvNeedText() { }

	// RVA: 0x8953A0 Offset: 0x8947A0 VA: 0x1808953A0
	public int GetUpgradeForceLvNeedContribution(float rate = 1) { }

	// RVA: 0x8953E0 Offset: 0x8947E0 VA: 0x1808953E0
	public static int GetUpgradeForceLvNeedContribution(int _heroForceLv, float rate) { }

	// RVA: 0x894BE0 Offset: 0x893FE0 VA: 0x180894BE0
	public float GetTotalAttir() { }

	// RVA: 0x894CA0 Offset: 0x8940A0 VA: 0x180894CA0
	public float GetTotalFightSkill() { }

	// RVA: 0x894D80 Offset: 0x894180 VA: 0x180894D80
	public float GetTotalLivingSkill() { }

	// RVA: 0x88A4A0 Offset: 0x8898A0 VA: 0x18088A4A0
	public float GetDebateScore() { }

	// RVA: 0x88E660 Offset: 0x88DA60 VA: 0x18088E660
	public float GetItemFavorValue(ItemData targetItem, float maxLimit = 20) { }

	// RVA: 0x887F30 Offset: 0x887330 VA: 0x180887F30
	public bool FullState() { }

	// RVA: 0x89F860 Offset: 0x89EC60 VA: 0x18089F860
	public void RecoverState() { }

	// RVA: 0x88F330 Offset: 0x88E730 VA: 0x18088F330
	public float GetMaxBuyValue(float discount = 1) { }

	// RVA: 0x894F20 Offset: 0x894320 VA: 0x180894F20
	public float GetTradeValueRate(bool buy) { }

	// RVA: 0x895090 Offset: 0x894490 VA: 0x180895090
	public float GetTradeValueRate(bool buy, bool useLivingSkill) { }

	// RVA: 0x88B130 Offset: 0x88A530 VA: 0x18088B130
	public float GetFavorValueRate(bool buy) { }

	// RVA: 0x88C050 Offset: 0x88B450 VA: 0x18088C050
	public float GetForceStorageDiscount(ItemListData targetForceStorage) { }

	// RVA: 0x880E70 Offset: 0x880270 VA: 0x180880E70
	public void ChangeHeroMissionResult(MissionTargetType missionTargetType, float changeNum) { }

	// RVA: 0x8809E0 Offset: 0x87FDE0 VA: 0x1808809E0
	public void ChangeHeroMissionResult(MissionTargetType missionTargetType, string missionTargetID, float changeNum) { }

	// RVA: 0x880870 Offset: 0x87FC70 VA: 0x180880870
	public void ChangeHeroMissionResult(MissionTargetType missionTargetType, int resourceNeed, float changeNum) { }

	// RVA: 0x8808A0 Offset: 0x87FCA0 VA: 0x1808808A0
	public void ChangeHeroMissionResult(MissionTargetType missionTargetType, string missionTargetID, int resourceNeed, float changeNum) { }

	// RVA: 0x880A10 Offset: 0x87FE10 VA: 0x180880A10
	public void ChangeHeroMissionResult(MissionData targetMission, MissionTargetType missionTargetType, string missionTargetID, int resourceNeed, float changeNum) { }

	// RVA: 0x88D6C0 Offset: 0x88CAC0 VA: 0x18088D6C0
	public float GetHeroItemLv(bool useStrengthLv = False) { }

	// RVA: 0x8855F0 Offset: 0x8849F0 VA: 0x1808855F0 Slot: 4
	public object Clone() { }

	[CompilerGeneratedAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	// RVA: 0x8A8520 Offset: 0x8A7920 VA: 0x1808A8520
	private bool <RandomAttriAndSkill>b__556_0(int i) { }

	[CompilerGeneratedAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	// RVA: 0x8A8580 Offset: 0x8A7980 VA: 0x1808A8580
	private bool <RandomAttriAndSkill>b__556_1(int i) { }
}