public class WorldData : ICloneable // TypeDefIndex: 8181
{
	// Fields
	public int chapter; // 0x10
	public List<int> cityAreaID; // 0x18
	public List<int> villageAreaID; // 0x20
	public List<int> forceAreaID; // 0x28
	public List<AreaData> Areas; // 0x30
	public List<InnData> Inns; // 0x38
	public List<ResourcePointData> ResourcePoints; // 0x40
	public List<ForceData> Forces; // 0x48
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public List<HeroData> Heros; // 0x50
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public List<HeroData> TempHeros; // 0x58
	public List<EventData> BigMapRandomEventDatas; // 0x60
	public List<EventData> AreaMapRandomEventDatas; // 0x68
	public int lastRandomWorldEventDay; // 0x70
	public List<int> WorldEventDatasSaveRecord; // 0x78
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public List<EventData> WorldEventDatas; // 0x80
	public List<WorldNewsData> WorldNewsDatas; // 0x88
	public List<MailData> MailDatas; // 0x90
	public bool cheating; // 0x98
	public bool cheated; // 0x99
	public GameMode gameMode; // 0x9C
	public int gameDifficulty; // 0xA0
	public bool relaxMode; // 0xA4
	public TimeData worldTime; // 0xA8
	public float TimeDifficulty; // 0xB0
	public float hour; // 0xB4
	public bool forceMeetingStarted; // 0xB8
	public bool forcePartyStarted; // 0xB9
	public int forceMeetingMissedTime; // 0xBC
	public int playerBetrayForceBadTime; // 0xC0
	public int playerGetTeacherTime; // 0xC4
	public int playerServantForceTime; // 0xC8
	public int playerReplaceForceTime; // 0xCC
	public List<InfoData> infos; // 0xD0
	public Dictionary<int, TimeData> plotHappened; // 0xD8
	public List<int> missionFinished; // 0xE0
	public PlotEventLogData PlotEventLog; // 0xE8
	public List<WorldPlotEventStartData> worldPlotEventStartData; // 0xF0
	public Dictionary<int, TimeData> worldPlotEventStartTime; // 0xF8
	public List<string> tutorialFinished; // 0x100
	public bool openLeaveForce; // 0x108
	public bool openForceBuilding; // 0x109
	public bool openForceAttackResource; // 0x10A
	public bool openForceAttackArea; // 0x10B
	public bool openForceAttackBasement; // 0x10C
	public int monthCatchBadFamePlayerTime; // 0x110
	public int monthGambleTime; // 0x114
	public int monthPartyTime; // 0x118
	public int monthForcePartyTime; // 0x11C
	public int monthDoctorTime; // 0x120
	public int monthPerformForMoneyTime; // 0x124
	public int monthCoachTime; // 0x128
	public int monthAttackMartialClubTime; // 0x12C
	public int monthSpeReduceBadFameTime; // 0x130
	public int monthSpeAddFameTime; // 0x134
	public int monthSpeGetTalentPointTime; // 0x138
	public int monthChallengeTime; // 0x13C
	public int monthBuyAreaInfoTime; // 0x140
	public int monthGiveMoneyToGovernTime; // 0x144
	public int monthBreakEquipTime; // 0x148
	public int monthKillTime; // 0x14C
	public int monthFreshBountyTime; // 0x150
	public int monthFreshAuctionTime; // 0x154
	public int monthLeaderInteractOtherForceTime; // 0x158
	public List<List<ItemData>> showRoomItems; // 0x160
	public float showRoomChangeFame; // 0x168
	public int nowWeather; // 0x16C
	public float weatherLastTime; // 0x170
	public List<SkinUnlockData> skinUnlockData; // 0x178
	public List<int> speBuildingUnlocked; // 0x180
	public int finishForceMissionCount; // 0x188
	public int totalFightCount; // 0x18C
	public int totalWinFightCount; // 0x190
	public int totalEnemyKilled; // 0x194
	public float totalBadFame; // 0x198
	public int studyFightWithGreatHeroSingleWinNum; // 0x19C
	public int studyFightWithGreatHeroMultiWinNum; // 0x1A0
	public int studyFightWithGreatHeroFinalWinNum; // 0x1A4
	public int totalHeroMeet; // 0x1A8
	public PrisonData prisonData; // 0x1B0
	public List<int> gameResultTriggered; // 0x1B8
	public List<BookWriterData> playerBookWriter; // 0x1C0
	public int thisYearExploreSpeEventNum; // 0x1C8
	public int thisYearExploreBigSpeEventNum; // 0x1CC
	public ItemListData governStorage; // 0x1D0
	public float battleTimeScale; // 0x1D8
	public List<HeroTagDataBase> tempTagDataBase; // 0x1E0
	public WeaponResearchData weaponResearchData; // 0x1E8
	public MeditationData meditationData; // 0x1F0
	public ForceSpeResearchData forceSpeResearchData; // 0x1F8
	public HeroSpeAddData forceSpeFunctionAddData; // 0x200
	public SpePoisonData getSpePoisonData; // 0x208
	public SpePoisonData combineSpePoisonData; // 0x210
	public ItemListData speBookStorage; // 0x218
	public HeroSpeAddData speBookStorageSpeAdd; // 0x220
	public SpeSummonResearchData speSummonResearchData; // 0x228
	public int speEnhanceStone; // 0x230
	public List<float> speSpellRate; // 0x238
	public bool autoResearch; // 0x240
	private ItemData playerAuctionItem; // 0x248
	public ItemSortType itemSortType; // 0x250
	public bool itemReverseOrder; // 0x254
	public SkillSortType skillSortType; // 0x258
	public bool skillReverseOrder; // 0x25C
	public CustomDifficultyData customDifficultyData; // 0x260
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public Dictionary<int, HeroData> HerosDict; // 0x268
	private readonly object herosDictLock; // 0x270
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public Dictionary<int, HeroData> TempHerosDict; // 0x278
	private readonly object tempHerosDictLock; // 0x280
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public Dictionary<int, ForceData> ForcesDict; // 0x288
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public Dictionary<int, AreaData> AreasDict; // 0x290
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public Dictionary<int, ResourcePointData> resourcePointDict; // 0x298
	[JsonIgnoreAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	public Dictionary<int, InnData> innDict; // 0x2A0

	// Properties
	public ItemData PlayerAuctionItem { get; set; }

	// Methods

	// RVA: 0x9D11F0 Offset: 0x9D05F0 VA: 0x1809D11F0
	public ItemData get_PlayerAuctionItem() { }

	// RVA: 0x9D1200 Offset: 0x9D0600 VA: 0x1809D1200
	public void set_PlayerAuctionItem(ItemData value) { }

	// RVA: 0x9D08D0 Offset: 0x9CFCD0 VA: 0x1809D08D0
	public void .ctor() { }

	// RVA: 0x9CE770 Offset: 0x9CDB70 VA: 0x1809CE770
	public int GetPlayerForceTotalArea() { }

	// RVA: 0x9CE740 Offset: 0x9CDB40 VA: 0x1809CE740
	public int GetPlayerForceMaxAttackTime() { }

	// RVA: 0x9CD480 Offset: 0x9CC880 VA: 0x1809CD480
	public float GetAIForceDevelopSpeed() { }

	// RVA: 0x9CD910 Offset: 0x9CCD10 VA: 0x1809CD910
	public string GetDifficlutyName() { }

	// RVA: 0x9CC470 Offset: 0x9CB870 VA: 0x1809CC470
	public bool AddTempTag(HeroTagDataBase tempTag) { }

	// RVA: 0x9CD120 Offset: 0x9CC520 VA: 0x1809CD120
	public HeroTagDataBase FindTempTag(string tagName) { }

	// RVA: 0x9CD040 Offset: 0x9CC440 VA: 0x1809CD040
	public int FindTempTagID(string tagName) { }

	// RVA: 0x9CC900 Offset: 0x9CBD00 VA: 0x1809CC900
	public void ClearTempTag(string tagName) { }

	// RVA: 0x9CBFD0 Offset: 0x9CB3D0 VA: 0x1809CBFD0
	public void AddGameResultTriggered(int resultID) { }

	// RVA: 0x9CEFC0 Offset: 0x9CE3C0 VA: 0x1809CEFC0
	public bool HaveGameResultTriggered(int resultID) { }

	// RVA: 0x9CEF70 Offset: 0x9CE370 VA: 0x1809CEF70
	public bool HaveGameResultTriggered() { }

	// RVA: 0x9CF5A0 Offset: 0x9CE9A0 VA: 0x1809CF5A0
	public HeroData Player() { }

	// RVA: 0x9CDA90 Offset: 0x9CCE90 VA: 0x1809CDA90
	public int GetEventSaveID(EventData targetEvent) { }

	// RVA: 0x9CD9D0 Offset: 0x9CCDD0 VA: 0x1809CD9D0
	public EventData GetEventSaveIDEvent(int eventSaveID) { }

	[OnSerializingAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	// RVA: 0x9CF2A0 Offset: 0x9CE6A0 VA: 0x1809CF2A0
	internal void OnSerializingMethod(StreamingContext context) { }

	[OnDeserializedAttribute] // RVA: 0x10A40 Offset: 0xFE40 VA: 0x180010A40
	// RVA: 0x9CF0A0 Offset: 0x9CE4A0 VA: 0x1809CF0A0
	internal void OnDeserializedMethod(StreamingContext context) { }

	// RVA: 0x9CFE40 Offset: 0x9CF240 VA: 0x1809CFE40
	public void SetPlayerMissionEventData() { }

	// RVA: 0x9CF5F0 Offset: 0x9CE9F0 VA: 0x1809CF5F0
	public void RecoverPlayerMissionEventData() { }

	// RVA: 0x9CD8E0 Offset: 0x9CCCE0 VA: 0x1809CD8E0
	public float GetChapterBadFameRate() { }

	// RVA: 0x9D0510 Offset: 0x9CF910 VA: 0x1809D0510
	public void UnlockSkin(int _skinID, int _skinLv, bool showInfo) { }

	// RVA: 0x9D0260 Offset: 0x9CF660 VA: 0x1809D0260
	public bool SkinUnlocked(int _skinID, int _skinLv) { }

	// RVA: 0x9CC680 Offset: 0x9CBA80 VA: 0x1809CC680
	public bool CanQuickTravel() { }

	// RVA: 0x9CC7C0 Offset: 0x9CBBC0 VA: 0x1809CC7C0
	public void ChangeSpeEnhanceStoneNum(int num, bool showInfo) { }

	// RVA: 0x9CC5D0 Offset: 0x9CB9D0 VA: 0x1809CC5D0
	public void AddWorldNews(string text, int time) { }

	// RVA: 0x9CF020 Offset: 0x9CE420 VA: 0x1809CF020
	public bool HaveWorldNews(bool includeWorldNews) { }

	// RVA: 0x9CEB80 Offset: 0x9CDF80 VA: 0x1809CEB80
	public string GetRandomWorldNews(bool includeWorldNews) { }

	// RVA: 0x9CEA70 Offset: 0x9CDE70 VA: 0x1809CEA70
	public int GetRandomEnemyCount() { }

	// RVA: 0x9CCCF0 Offset: 0x9CC0F0 VA: 0x1809CCCF0
	public int FindAvailableHeroID() { }

	// RVA: 0x9CCE70 Offset: 0x9CC270 VA: 0x1809CCE70
	public int FindAvailableTempHeroID() { }

	// RVA: 0x9CC280 Offset: 0x9CB680 VA: 0x1809CC280
	public void AddTempHero(HeroData target) { }

	// RVA: 0x9CFCC0 Offset: 0x9CF0C0 VA: 0x1809CFCC0
	public void RemoveTempHero(HeroData target) { }

	// RVA: 0x9CEEA0 Offset: 0x9CE2A0 VA: 0x1809CEEA0
	public int GetTempHeroCount() { }

	// RVA: 0x9CC0C0 Offset: 0x9CB4C0 VA: 0x1809CC0C0
	public void AddNewHero(HeroData target) { }

	// RVA: 0x9CFB30 Offset: 0x9CEF30 VA: 0x1809CFB30
	public void RemoveHero(HeroData target) { }

	// RVA: 0x9CDED0 Offset: 0x9CD2D0 VA: 0x1809CDED0
	public int GetHeroCount() { }

	// RVA: 0x9CD6B0 Offset: 0x9CCAB0 VA: 0x1809CD6B0
	public AreaData GetArea(string areaName) { }

	// RVA: 0x9CDB50 Offset: 0x9CCF50 VA: 0x1809CDB50
	public ForceData GetForce(string forceName) { }

	// RVA: 0x9CDFA0 Offset: 0x9CD3A0 VA: 0x1809CDFA0
	public ForceData GetHeroForce(int heroID) { }

	// RVA: 0x9CE110 Offset: 0x9CD510 VA: 0x1809CE110
	public HeroData GetHero(string heroName) { }

	// RVA: 0x9CD210 Offset: 0x9CC610 VA: 0x1809CD210
	public void GenerateHeroDict() { }

	// RVA: 0x9CE010 Offset: 0x9CD410 VA: 0x1809CE010
	public HeroData GetHero(int heroID) { }

	// RVA: 0x9CDD00 Offset: 0x9CD100 VA: 0x1809CDD00
	public ForceData GetForce(int forceID) { }

	// RVA: 0x9CD4F0 Offset: 0x9CC8F0 VA: 0x1809CD4F0
	public AreaData GetArea(int areaID) { }

	// RVA: 0x9CECE0 Offset: 0x9CE0E0 VA: 0x1809CECE0
	public ResourcePointData GetResourcePoint(int resourcePointID) { }

	// RVA: 0x9CE580 Offset: 0x9CD980 VA: 0x1809CE580
	public InnData GetInn(int innID) { }

	// RVA: 0x9CCB70 Offset: 0x9CBF70 VA: 0x1809CCB70 Slot: 4
	public object Clone() { }
}