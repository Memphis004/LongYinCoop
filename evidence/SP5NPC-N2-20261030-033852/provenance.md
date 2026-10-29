# provenance — SP5-NPC stage N2 artifact (สถานะ: **READY — build ผ่าน 0W/0E · เทสต์ ≥ เดิมทุกชุด · ยังไม่ deploy**)

> stage N2 (มติผู้ใช้ 2026-10-30: อนุมัติเฉพาะ N2 offline = hook ต่อ predicate + เทสต์ — N3–N6 ยังไม่
> อนุมัติ ห้าม deploy ห้ามเปิดเกม) · label `SP5NPC-N2-20261030-033852` · **`Mods\LongYinCoop.dll`
> ยังเป็น `6a94abb5…` ไม่ถูกแตะ** (ยืนยัน hash ปลาย session)

## งานที่ทำ (ตามแผน §7 แถว N2: "implement hooks G1/G4/G5 (+G2 ถ้าอนุมัติ) + negative tests")

| ตัว | เนื้อหา | สถานะ |
|---|---|---|
| **G1** (A1 ย่อ heroNum) | `Sp5NpcGate.GenerateHeroPrefix/Finalizer` — prefix ย่อ `forceLevelData[*].heroNum[i]` ด้วย `Sp5NpcRules.ShrinkHeroNum` (typed write บน interop List\<int\>), **finalizer คืนค่าเดิมทุกทางออกรวม exception** ⇒ การเขียนถูกจำกัดใน window ของ creation round · กัน re-entry · Instance null = ไม่แตะ | implement + compile |
| **G4** (C2 clamp เพดาน) | postfix บน `GetRecommendHeroNum()`/`GetMaxHeroNum()` (`ref int __result` = round(old×0.5), ห้ามเพิ่มเหนือเดิม) — **เฉพาะ Mid** | implement + compile |
| **G5** (C1 deny เติม) | prefix บน `WorldAddNewHero(int,int,bool,bool=True)` — **เฉพาะ Strong** · pattern `bool` prefix + `ref HeroData __result = null, return false` (skip original ด้วย null — prefix คืน reference non-null เท่านั้นที่ skip; แก้จากร่างแรกที่คืน null แล้วกลายเป็นผ่านต่อ) · fail-closed: exception ⇒ รัน original | implement + compile |
| **G2** (A2 budget) | **ไม่ implement** — แผน §7 กำหนด "+G2 ถ้าอนุมัติ" และยังไม่มีมติ | คงรอ |
| ตรรกะ pure | `Sp5NpcRules` (ShrinkHeroNum round half-up / ClampCeiling / activate ต่อระดับ / HeroScaleFor) + ค่า scale ประกาศใน `Sp5NpcScales` (Light 0.5 · Mid 0.25 · Strong 0.05 · MidCeiling 0.5) | เทสต์ offline ครบ |
| wiring | `CoopMod`: `Sp5NpcGate.Install()` หลัง Sp5T0Probe ตอน init · `Uninstall()` ทั้ง OnDeinitialize และกิ่ง `CoopPrefs.Enabled=false` | implement + compile |
| snapshot | gate อ่าน net.cfg เอง (S2NetConfig poll ≤1 s) ผ่าน provider delegate — fail-closed Off เมื่ออ่านไม่ได้ | implement + compile |

## พารามิเตอร์ที่ implement เลือก (บันทึกตามจริง — แก้ได้ก่อน runtime ต้องมติ)

1. **rounding ของการย่อ = round half-up (AwayFromZero)** — band ค่า 1 ที่ scale 0.5 ยังคงเป็น 1
   (ไม่ zero ทั้ง band) · ผลสุทธิ: ตาราง T0 88→50 (เอ่อเหนือ linear 44) — **ต้องวัดจริง T4**
2. **restore ท้ายรอบ GenerateHero** (ไม่ทิ้งค่าย่อค้างใน RAM) — กัน consumer อื่นของตาราง
   (N1-4 candidates) เห็นค่าย่อนอก window
3. **G4 เฉพาะ Mid / G5 เฉพาะ Strong / G1 ทุกระดับ** — ตาม design §4 ตาราง L1/L2/L3
4. **install ตอนโหลดโมด (unconditional) + Decide ต่อ invocation** — npcPolicy อ่านสดจาก net.cfg
   (poll ≤1 s); Off = no-op เกมเดิม 100%

## บั๊กที่จับได้ระหว่างเขียน (แก้แล้วก่อน build ผ่าน)

- ร่างแรก G5 คืน `HeroData null` จาก prefix — ตาม contract ของ Harmony prefix ที่คืน reference
  **null = ผ่านต่อ (ไม่ skip)** ⇒ เปลี่ยนเป็น pattern `bool` + `ref __result` (skip = return false)
- `M()` helper คืน HarmonyMethod แต่ ctor `new HarmonyMethod(x)` ต้องการ MethodInfo — แก้คืน MethodInfo
- `using System.Threading` ที่ตก (Interlocked)

## build + เทสต์ (exit code จริง)

| รายการ | ค่า |
|---|---|
| build | `dotnet build src/LongYinCoop -c Release --no-incremental` — **0 Warning / 0 Error** · determinism ×2 |
| **artifact** | DLL **`2b59e46c53bb2f415d2e6f6f526eafe2409612315c08158ac733642c8028396f`** (371,712 B) · PDB **`857cd1b86aa3bb679ceac6825f66d614dc4520a193029d42e2456d5ade6a74fc`** (102,068 B) — สำเนาใน evidence โฟลเดอร์นี้ (gitignored) |
| เทียบ build ก่อนหน้า | N1 `b6d24d34…` (367,104 B — ไม่ deploy) · prep `865b4a87…` (366,592 B — ไม่ deploy) · `Mods\` = `6a94abb5…` (ไม่แตะ) |
| C# suite | **693 PASS / 0 FAIL** (N1 = 673 → +20 rules tests) |
| python | **53 OK** + **discover 110 OK** (คงเดิม) — บรรทัด "FAILED pass=0…" = stdout ของเทสต์ negative-path เอง |
| static | **CLEAN (0 violations)** |

## ข้อจำกัด / คง UNKNOWN

- hook shells (`Sp5NpcGate.cs`) เป็น Il2Cpp+Harmony — **ไม่ถูกลิงก์เทสต์ offline** (compile-only +
  static check + code audit) ตาม idiom S2WriteGuardHook · พฤติกรรมจริงบนเกม = stage N3/N4 (ต้อง
  deploy/split/เปิดเกม = อนุมัติแยก)
- ค่า scale/rounding = พารามิเตอร์ที่ยังไม่ผ่านการวัด — ผลลดจริงรอ T4
- G2/A2, ระดับที่จะใช้จริง, นโยบาย refill ของโลกที่ลดแล้ว = **ยังรอมติผู้ใช้** (bracket ในมติรอบ N2
  ยังไม่ได้เลือก — บันทึกใน decisions-pending §7.1)
