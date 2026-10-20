# S1-b — BigMap cross-check (Frida vs IPC)

Build ที่วัด: `LongYinCoop 0.2.0` (ก่อนแก้ schema view) · pid 26312 · 2026-10-20 06:13
> หมายเหตุ: หลังวัดรอบนี้พบว่า view ยังไม่ตรง schema §3 (`worldTime` nested, `npcs[].id`,
> `resources.contribution`, `nowActiveUnit`, `cell{x,y}`) จึงแก้โค้ดและ build ใหม่
> — ตัวเลขของ sample นี้ยังใช้ได้ (เส้นทางอ่านเดียวกัน) แต่จะวัดซ้ำบน build สุดท้าย

## เส้นทาง

- mod: `BigMapController.Instance.playerArmy → GetComponent<BigmapNpcController>().heroData.bigMapPos`
- Frida: `GameController.Instance.worldData → WorldData.Player().bigMapPos`
  (คนละเส้นทางกับ mod; ไม่ได้เรียก method ที่เปลี่ยน state)

## Sample 1 (ปาร์ตี้หยุดนิ่ง)

| ฝั่ง | x | y | heroID | atAreaID |
|---|---|---|---|---|
| Frida (`WorldData.Player()`) | 1886.2742919921875 | 283.280517578125 | 0 | -1 |
| IPC (`get_player_pos`) | 1886.2743 | 283.28052 | 0 | -1 |
| Δ | 0 | 0 | 0 | 0 |

ผล: **1/1 ตรง** — ยังต้องเก็บ sample 2 และ 3 โดยให้ x,y เปลี่ยนระหว่างครั้ง

## Patch inventory ที่ BigMap (06:11)

`compat-notes-bigmap.md` — LongYinThai patch setter 4 ชนิด:
`Il2Cpp.UILabel.set_text`, `Il2CppTMPro.TMP_Text.set_text`, `UnityEngine.TextMesh.set_text`,
`UnityEngine.UI.Text.set_text` (prefix 1 + postfix 1 ต่อตัว)
**ไม่มีการ patch `BattleController.AddInfoText` โดย mod อื่น** — hook ของเราเป็นเจ้าของเดียว
