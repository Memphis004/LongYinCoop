
const A = Il2Cpp.domain.assembly("Assembly-CSharp");
const wd = Il2Cpp.gc.choose(A.image.class("GameController"))[0].field("worldData").value;
const obj = wd.method("Player").invoke();
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
const NAMES = ["isSummon", "summonID", "summonLv", "summonMoveRange", "summonControlable", "summonSourceHero", "interestingStar", "manageAiHour", "dailyAIManaged", "heroAIData", "heroAIDataArriveTargetRecord", "heroAISettingData", "heroID", "speHero", "tempPlotHero", "recruitAble", "loveAble", "hide", "dead", "heroName", "heroFamilyName", "heroNickName", "isFemale", "belongForceID", "skillForceID", "outsideForce", "replacedForce", "forceJobType", "forceJobID", "forceJobCD", "branchLeaderAreaID", "thisMonthContribution", "lastMonthContribution", "thisYearContribution", "lastYearContribution", "lastFightContribution", "isLeader", "heroForceLv", "heroStrengthLv", "atAreaID", "bigMapPos", "inSafeArea", "inPrison", "age", "generation", "voicePitch", "faceData", "skinColorDark", "defaultSkinID", "skinID", "skinLv", "changeSkinCd", "playerSetSkin", "setSkinID", "setSkinLv", "kungfuSkillFocus", "livingSkillFocus", "goodKungfuSkillName", "haveMeet", "favor", "baseAttri", "maxAttri", "totalAttri", "baseFightSkill", "maxFightSkill", "totalFightSkill", "baseLivingSkill", "maxLivingSkill", "totalLivingSkill", "expLivingSkill", "hp", "maxhp", "realMaxHp", "power", "maxPower", "realMaxPower", "mana", "maxMana", "realMaxMana", "armor", "externalInjury", "internalInjury", "poisonInjury", "isGovern", "governLv", "governContribution", "isHornord", "hornorLv", "forceContribution", "fame", "badFame", "loyal", "evil", "chaos", "nature", "talent", "hobby", "rest", "cureType", "salary", "population", "nowEquipment", "horseSaveRecord", "horse", "horseArmorSaveRecord", "horseArmor", "itemListData", "selfStorage", "partPosture", "selfCureTime", "restCureTotalRate", "medResist", "fightProtectTarget", "fightForceEnter", "fightSpeControl", "battlePrepareSpellData", "skillMaxPracticeExpData", "skillCount", "kungfuSkills", "internalSkillSaveRecord", "internalSkill", "dodgeSkillSaveRecord", "dodgeSkill", "uniqueSkillSaveRecord", "uniqueSkill", "attackSkillSaveRecord", "attackSkills", "nowActiveSkill", "baseAddData", "totalAddData", "heroBuff", "heroBuffLevel", "autoSetting", "heroDetailDirty", "heroBuffDirty", "heroIconDirtyCount", "forceMission", "missions", "inTeam", "teamLeader", "teamMates", "autoLeaveTeamDay", "autoLeaveTeamDestroy", "bodyGuard", "playerInteractionTimeData", "plotNumCount", "missionNumCount", "playerBecomeTeacherTime", "Teacher", "Students", "Lover", "PreLovers", "Relatives", "Brothers", "Friends", "Haters", "recordLog", "selfHouseTotalAdd", "heroTagPoint", "heroTagData", "bookWriteWorking", "wantRemove", "equipLock", "skillLock", "studyNewSkillSetting", "settingName", "servantForceID", "recruitByPlayer", "shownHpBarRoot", "shownHp", "shownMaxHp", "shownRealMaxHp", "shownMpBarRoot", "shownMana", "shownMaxMana", "shownRealMaxMana", "shownPowerBarRoot", "shownPower", "shownMaxPower", "shownRealMaxPower", "isTempHero", "isRandomEnemy", "needRemove", "inWater", "inMountain", "inHill", "fightScore"];
for (const n of NAMES) { try { out[n] = v2s(obj.field(n).value); } catch (e) { out[n] = "READ_ERR"; } }
out.__extra = {};
out.__extra.bigMapPos = innerOf(obj.field("bigMapPos").value, ["x", "y"]);out.__extra.worldTime = innerOf(wd.field("worldTime").value, ["year", "month", "day"]);out.__extra.itemListData = innerOf(obj.field("itemListData").value, ["heroID", "forceID", "money", "weight", "maxWeight", "allItem", "itemTypeList"]);
JSON.stringify(out)
