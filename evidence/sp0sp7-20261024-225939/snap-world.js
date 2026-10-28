
const A = Il2Cpp.domain.assembly("Assembly-CSharp");
const wd = Il2Cpp.gc.choose(A.image.class("GameController"))[0].field("worldData").value;
const obj = wd;
const out = {};
function v2s(v) {
  try {
    if (v === null || v === undefined) return null;
    if (typeof v === "number" || typeof v === "boolean") return v;
    try { const c = v.content; if (typeof c === "string") return c; } catch (e) {}
    try { const n = v.field("_size").value; return { listSize: n }; } catch (e) {}
    try { return v.toString(); } catch (e) {}
    return "OBJ";
  } catch (e) { return "ERR"; }
}
function innerOf(v, names) {
  try { const o = {}; for (const n of names) { try { o[n] = v2s(v.field(n).value); } catch (e) { o[n] = "ERR"; } } return o; } catch (e) { return "ERR"; }
}
const NAMES = ["chapter", "cityAreaID", "villageAreaID", "forceAreaID", "Areas", "Inns", "ResourcePoints", "Forces", "Heros", "TempHeros", "BigMapRandomEventDatas", "AreaMapRandomEventDatas", "lastRandomWorldEventDay", "WorldEventDatasSaveRecord", "WorldEventDatas", "WorldNewsDatas", "MailDatas", "cheating", "cheated", "gameMode", "gameDifficulty", "relaxMode", "worldTime", "TimeDifficulty", "hour", "forceMeetingStarted", "forcePartyStarted", "forceMeetingMissedTime", "playerBetrayForceBadTime", "playerGetTeacherTime", "playerServantForceTime", "playerReplaceForceTime", "infos", "plotHappened", "missionFinished", "PlotEventLog", "worldPlotEventStartData", "worldPlotEventStartTime", "tutorialFinished", "openLeaveForce", "openForceBuilding", "openForceAttackResource", "openForceAttackArea", "openForceAttackBasement", "monthCatchBadFamePlayerTime", "monthGambleTime", "monthPartyTime", "monthForcePartyTime", "monthDoctorTime", "monthPerformForMoneyTime", "monthCoachTime", "monthAttackMartialClubTime", "monthSpeReduceBadFameTime", "monthSpeAddFameTime", "monthSpeGetTalentPointTime", "monthChallengeTime", "monthBuyAreaInfoTime", "monthGiveMoneyToGovernTime", "monthBreakEquipTime", "monthKillTime", "monthFreshBountyTime", "monthFreshAuctionTime", "monthLeaderInteractOtherForceTime", "showRoomItems", "showRoomChangeFame", "nowWeather", "weatherLastTime", "skinUnlockData", "speBuildingUnlocked", "finishForceMissionCount", "totalFightCount", "totalWinFightCount", "totalEnemyKilled", "totalBadFame", "studyFightWithGreatHeroSingleWinNum", "studyFightWithGreatHeroMultiWinNum", "studyFightWithGreatHeroFinalWinNum", "totalHeroMeet", "prisonData", "gameResultTriggered", "playerBookWriter", "thisYearExploreSpeEventNum", "thisYearExploreBigSpeEventNum", "governStorage", "battleTimeScale", "tempTagDataBase", "weaponResearchData", "meditationData", "forceSpeResearchData", "forceSpeFunctionAddData", "getSpePoisonData", "combineSpePoisonData", "speBookStorage", "speBookStorageSpeAdd", "speSummonResearchData", "speEnhanceStone", "speSpellRate", "autoResearch", "playerAuctionItem", "itemSortType", "itemReverseOrder", "skillSortType", "skillReverseOrder", "customDifficultyData", "HerosDict", "herosDictLock", "TempHerosDict", "tempHerosDictLock", "ForcesDict", "AreasDict", "resourcePointDict", "innDict"];
for (const n of NAMES) { try { out[n] = v2s(obj.field(n).value); } catch (e) { out[n] = "READ_ERR"; } }
out.__extra = {};
out.__extra.worldTime = innerOf(wd.field("worldTime").value, ["year", "month", "day"]);
JSON.stringify(out)
