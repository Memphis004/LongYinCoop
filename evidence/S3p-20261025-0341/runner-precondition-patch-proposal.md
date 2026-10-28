# Runner precondition patch — PROPOSAL (ยังไม่ได้แก้ไฟล์ · รออนุมัติ)

- เป้าหมาย: `tools/negative-tests.py` (tracked) — เพิ่ม precondition validation อ่าน state สดก่อนยิงทุกเคส
- **ไฟล์ยังไม่ถูกแก้** — ตรวจยืนยันได้ด้วย `git status` (`tools/negative-tests.py` ยังไม่ modified) · patch ถูกนำไปใช้เมื่ออนุมัติเท่านั้น

## 1. พฤติกรรมปัจจุบัน (path/line จริง)

| จุด | บรรทัด | ปัญหาที่พบจาก incident |
|---|---|---|
| `status()` | `negative-tests.py:51-55` | exception ลอยขึ้น ฆ่า runner ทั้งตัว (Gate 2: traceback ก่อนเขียน output) |
| `wait_stable()` | `:78-87` | อ่านแค่ stateHash และทำเฉพาะตอนเริ่มรัน — ไม่มี per-case check |
| หลัก loop | `:134` | `h0 = status()...["stateHash"]` — อ่าน state มาแล้ว**ทิ้งทุก field ยกเว้น hash** — ไม่มีการตรวจ premise เลย |
| ยิง | `:148` | ยิง**ไม่มีเงื่อนไข** — เคส 7 (Gate 2) ยิงการโจมตีจริงที่เกมรับเพราะเชื่อพรีมิส "นอกระยะ" จากศึกเก่า |
| evidence | `:161-170` | ไม่มี snapshot ของ state ก่อนยิง, ไม่มี precondition record, ไม่มี NOT-RUN state |
| crash | — | ไม่มี guard: crash ระหว่างรัน = หายทั้งไฟล์ผล |

## 2. Precondition matrix (ครบทุกเคส 20 ข้อ — ตรวจกับ state สดจาก `spike status` ทุกเคส)

รายการตรวจร่วมทุกเคส: **state อ่านได้** · **stateHash นิ่ง 2 ครั้ง** · **inBattle=true** · **battleState=Fighting** — ข้อใดล้ม = `NOT-RUN(reason)` ไม่ยิง

| เคส | กลุ่ม | premise ที่ต้องยืนยันจาก state สด | ตรวจได้จาก surface ปัจจุบัน? |
|---|---|---|---|
| 1 | DISARMED (move, ไม่ arm) | ไม่มี arm ค้างใน session (runner-local fact: `arm=0` ไม่เคยยิง arm) | บางส่วน — arm state ไม่ถูก expose (UNKNOWN-from-surface, บันทึกใน detail) |
| 2 | DISARMED (attack, arm 1s → รอ 2.2s) | arm window หมดอายุ (wall-clock: `arm_active_until_ms <= now`) | บางส่วนเช่นเดียวกัน |
| 3,4 | NOT_ACTIVE (move) | `args.id` **มีจริงใน live units[]** · `args.id != active` | ✓ ครบ |
| 5,6 | NOT_ACTIVE (attack) | เช่นเดียวกับ 3,4 | ✓ ครบ |
| 7–10 | OUT_OF_RANGE (attack) | attacker = active + มีชีวิต (`unitAlive=true`) · target มีจริง + มีชีวิต · **target อยู่นอกระยะ** | **ไม่ครบ — surface ให้แค่ `attackRangeCount` ไม่ให้ cells** ⇒ `range-membership-verifiable=false` ⇒ **NOT-RUN(premise-unverifiable-range) ทุกเคสจนกว่า mod จะ expose cells** (การแก้ mod = อนุมัติแยก) |
| 11–14 | TARGET_DEAD (attack) | `args.target` มีจริงใน live units[] · **`unitAlive=false`** · attacker = active | ✓ ครบ (v2 `unitAlive` = `IsAlive` [RUNTIME S1.5f §3]) |
| 15,16 | STATE (move ขณะเล็ง) | `nowActiveStateName == "WaitAttacking"` | ✓ ครบ |
| 17,18 | STATE (attack ขณะรอ input) | `nowActiveStateName == "WaitPlayerInput"` | ✓ ครบ |
| 19 | SELF (attack ตัวเอง) | `active == args.id` และ `args.target == args.id` | ✓ ครบ |
| 20 | GRID (move ไป (99,99)) | attacker = active · **(99,99) อยู่นอก grid จริง** (`row >= gridW or col >= gridH` จากค่า gridW/H สด) | ✓ ครบ |

กติกา ID (#8): ทุก group ตรวจ ID กับ `units[]` ของ state สดเท่านั้น — ID จาก plan ที่ไม่พบในศึกปัจจุบัน = `NOT-RUN(id-not-found)` (precondition `attacker-in-live-units` / `target-in-live-units`) — ไม่มีการอ้าง S1.5f คงเหลืออยู่ใน runner

## 3. Diff ที่เสนอ (unified — ยังไม่ได้แก้ไฟล์)

```diff
--- a/tools/negative-tests.py
+++ b/tools/negative-tests.py
@@ -2,6 +2,7 @@
 """S1.5-f: negative-test runner for exit criterion 3 (+ criterion 9 panic switch).
 
 Sends `spike` commands straight to the mod pipe through `coopctl.call`, i.e. it
 bypasses coopctl's prefire checks on purpose: those checks refuse client-side
 exactly the commands whose server-side guards we must prove here.
@@ -8,6 +9,30 @@
 Every case must satisfy all three:
   1) the mod answers with the expected error code (its own guard fired),
   2) `stateHash` is identical before/after (nothing applied, nothing moved),
   3) the reply is a failure (no ok=true).
+
+Gate-3 follow-up (2026-10-25): before firing each case the runner re-reads live
+state (`spike status`, read-only) and validates the case premise against THAT
+read:
+  - state unreadable / stateHash unstable          -> NOT-RUN (nothing fired)
+  - plan IDs absent from the live unit list        -> NOT-RUN (never trust IDs
+    from S1.5f or an older battle; verify against the current status)
+  - group premise mismatch                         -> NOT-RUN (reason recorded)
+  - OUT_OF_RANGE: the status surface exposes only attackRangeCount, not the
+    range CELLS -> the out-of-range premise is unverifiable from live state
+    -> NOT-RUN (premise-unverifiable-range) until the mod exposes the cells
+  - IPC error / connection lost / heartbeat loss   -> ABORT the whole run at
+    once (no auto-retry); partial results are still written to --out
+
+Rows record: result = PASS | FAIL | NOT-RUN | UNKNOWN, a pre_state snapshot
+(live), prechecks (each check ok/detail), expected, actual and hash
+before/after (hash_after=null when not fired or unreadable).
 
 Exit codes: 0 = every case PASS, 1 = a case FAILED (stopped there), 2 = aborted
-before firing (state not settled / IPC error) - nothing was sent in that case.
+before firing (state not settled / IPC error), 3 = aborted mid-run (connection
+lost / heartbeat lost - partial results written).
 """
@@ -78,13 +103,112 @@ def wait_stable(pid, settle_ms, tries=6):
-    """Two reads of stateHash `settle_ms` apart must agree, else the game is moving."""
-    prev = status(pid)["stateHash"]
-    for _ in range(tries):
-        time.sleep(settle_ms / 1000.0)
-        cur = status(pid)["stateHash"]
-        if cur == prev:
-            return prev
-        prev = cur
-    return None
+def read_status(pid):
+    """Read spike status; returns (state, None) or (None, error_text)."""
+    try:
+        return status(pid), None
+    except Exception as e:
+        return None, "%s: %s" % (type(e).__name__, e)
+
+
+def wait_stable_state(pid, settle_ms, tries=6):
+    """Read the FULL state (not only the hash) twice, settle_ms apart; returns
+    (state, None) when the two stateHash values agree, else (None, reason)."""
+    s0, err = read_status(pid)
+    if err:
+        return None, "state-unreadable: %s" % err
+    prev = s0.get("stateHash")
+    for _ in range(tries):
+        time.sleep(settle_ms / 1000.0)
+        s1, err = read_status(pid)
+        if err:
+            return None, "state-unreadable: %s" % err
+        cur = s1.get("stateHash")
+        if cur == prev:
+            return s1, None
+        prev = cur
+    return None, "hash-unstable"
+
+
+def unit_by_id(state, uid):
+    for u in state.get("units") or []:
+        if u.get("id") == uid:
+            return u
+    return None
+
+
+def pre_snapshot(state):
+    """Live-state evidence subset recorded with every row."""
+    keys = ("inBattle", "battleStateName", "nowActiveStateName", "active",
+            "isPlayerTurn", "unitCount", "turnIndex", "gridW", "gridH",
+            "attackRangeCount", "damageRangeCount", "paused", "stateHash")
+    snap = {k: state.get(k) for k in keys}
+    snap["units"] = [
+        {k: u.get(k) for k in ("id", "team", "row", "col", "hp", "alive",
+                               "unitAlive", "playerControl")}
+        for u in (state.get("units") or [])
+    ]
+    return snap
+
+
+def check_preconditions(group, cmd, case, state, session_facts):
+    """Validate the case premise against the freshly read live state.
+    Returns (can_fire, checks, notrun_reason)."""
+    checks = []
+
+    def add(name, ok, detail=""):
+        checks.append({"check": name, "ok": bool(ok), "detail": detail})
+        return bool(ok)
+
+    if not add("in-battle", state.get("inBattle") is True):
+        return False, checks, "not-in-battle"
+    if not add("battle-fighting", state.get("battleStateName") == "Fighting",
+               "battleState=%s" % state.get("battleStateName")):
+        return False, checks, "precondition-mismatch"
+
+    active = state.get("active")
+    args = case.get("args") or {}
+    cid, tid = args.get("id"), args.get("target")
+
+    if group == "DISARMED":
+        # arm state is not exposed by the status surface: case 1 never arms,
+        # case 2 relies on the wall-clock arm window having lapsed.
+        add("arm-window-lapsed(session-fact)",
+            session_facts.get("arm_active_until_ms", 0) <= now_ms(),
+            "arm=%ss waited=%sms (arm state not exposed by surface)"
+            % (case.get("arm"), case.get("arm_wait_ms")))
+        return True, checks, None
+
+    if group == "NOT_ACTIVE":
+        a = add("attacker-in-live-units", unit_by_id(state, cid) is not None, str(cid))
+        b = add("attacker-is-not-active", cid is not None and cid != active,
+                "active=%s" % (active or "-"))
+        ok = a and b
+        return ok, checks, None if ok else "precondition-mismatch"
+
+    if group == "OUT_OF_RANGE":
+        au, tu = unit_by_id(state, cid), unit_by_id(state, tid)
+        add("attacker-is-active", au is not None and active == cid,
+            "active=%s" % (active or "-"))
+        add("attacker-alive", bool(au and au.get("unitAlive")), str(cid))
+        add("target-exists", tu is not None, str(tid))
+        add("target-alive", bool(tu and tu.get("unitAlive")), str(tid))
+        add("range-membership-verifiable", False,
+            "attackRangeGridUnits cells are not exposed by the spike status "
+            "surface - the out-of-range premise cannot be confirmed from live "
+            "state (needs a mod-surface change; separate approval)")
+        return False, checks, "premise-unverifiable-range"
+
+    if group == "TARGET_DEAD":
+        tu = unit_by_id(state, tid)
+        a = add("target-in-live-units", tu is not None, str(tid))
+        b = add("target-dead-unitAlive-false",
+                bool(tu and tu.get("unitAlive") is False),
+                "unitAlive=%s" % (None if tu is None else tu.get("unitAlive")))
+        c = add("attacker-is-active", active == cid, "active=%s" % (active or "-"))
+        ok = a and b and c
+        return ok, checks, None if ok else "precondition-mismatch"
+
+    if group == "STATE":
+        want = "WaitAttacking" if cmd == "move" else "WaitPlayerInput"
+        ok = add("current-phase-%s" % want,
+                 state.get("nowActiveStateName") == want,
+                 "nowActiveState=%s" % state.get("nowActiveStateName"))
+        return ok, checks, None if ok else "phase-mismatch"
+
+    if group == "SELF":
+        a = add("attacker-is-active", active == cid, "active=%s" % (active or "-"))
+        b = add("target-equals-attacker", tid is not None and tid == cid,
+                "%s vs %s" % (tid, cid))
+        ok = a and b
+        return ok, checks, None if ok else "precondition-mismatch"
+
+    if group == "GRID":
+        gw, gh = state.get("gridW"), state.get("gridH")
+        row, col = int(args.get("row", -1)), int(args.get("col", -1))
+        a = add("attacker-is-active", active == cid, "active=%s" % (active or "-"))
+        b = add("coords-outside-live-grid",
+                gw is not None and gh is not None and (row >= gw or col >= gh),
+                "grid=%sx%s target=(%s,%s)" % (gw, gh, row, col))
+        ok = a and b
+        return ok, checks, None if ok else "precondition-mismatch"
+
+    add("unknown-group", False, str(group))
+    return False, checks, "unknown-group"
```

(main loop — hunk 3: base state + per-case flow)

```diff
@@ -113,2 +232,3 @@
-    base_hash = wait_stable(pid, settle_ms)
-    if base_hash is None:
-        print("ABORT: stateHash keeps changing (state not settled) - nothing fired")
+    base_state, err = wait_stable_state(pid, settle_ms)
+    if base_state is None:
+        print("ABORT: %s - nothing fired" % err)
         return 2
+    base_hash = base_state.get("stateHash")
+    session_facts = {"arm_active_until_ms": 0}
@@ -121,6 +247,7 @@
     rows = []
     failed = False
+    aborted = None
     for c in cases:
         n = c.get("n")
         group = c.get("group", "")
         cmd = c["cmd"]
         cpref = c.get("pref", pref)
-        if cpref != pref:
-            set_pref(pid, "allowRemoteInput", cpref)
-            pref = cpref
-            if not pref:
-                coopctl.call(pid, "spike", extra={"sub": "disarm"})
-
-        h0 = status(pid)["stateHash"]
+        try:
+            if cpref != pref:
+                set_pref(pid, "allowRemoteInput", cpref)
+                pref = cpref
+                if not pref:
+                    coopctl.call(pid, "spike", extra={"sub": "disarm"})
+        except Exception as e:
+            aborted = "connection-lost during set_pref: %s" % e
+            break
+
+        ts = time.strftime("%Y-%m-%d %H:%M:%S")
+        try:
+            state, err = wait_stable_state(pid, settle_ms)
+        except Exception as e:
+            state, err = None, "exception: %s: %s" % (type(e).__name__, e)
+        if state is None:
+            if "state-unreadable" in (err or ""):
+                aborted = err            # heartbeat/crash candidate: stop the run
+                break
+            rows.append({"n": n, "group": group, "cmd": cmd, "args": c.get("args"),
+                         "result": "NOT-RUN", "notrun_reason": err,
+                         "prechecks": [], "pre_state": None,
+                         "hash_before": None, "hash_after": None,
+                         "expect": c["expect"], "ts": ts})
+            print("%-4s %-14s %-7s %-8s %-6s %-6s %s" % (n, group, cmd, "NOT-RUN", "-", "-", (err or "")[:90]))
+            continue
+
+        pre = pre_snapshot(state)
+        can_fire, prechecks, notrun = check_preconditions(group, cmd, c, state, session_facts)
+        if not can_fire:
+            rows.append({"n": n, "group": group, "cmd": cmd, "args": c.get("args"),
+                         "result": "NOT-RUN", "notrun_reason": notrun,
+                         "prechecks": prechecks, "pre_state": pre,
+                         "hash_before": pre.get("stateHash"), "hash_after": None,
+                         "expect": c["expect"], "ts": ts})
+            print("%-4s %-14s %-7s %-8s %-6s %-6s %s" % (n, group, cmd, "NOT-RUN", "-", "-", (notrun or "precondition-mismatch")[:90]))
+            continue
+
+        h0 = pre.get("stateHash")
         armed = None
         arm_s = int(c.get("arm", 0) or 0)
         if arm_s > 0:
-            res = coopctl.call(pid, "spike", extra={"sub": "arm", "seconds": arm_s})
-            armed = bool((res.get("result") or {}).get("armed")) if res.get("ok") else False
-            if not res.get("ok"):
-                armed = False
-            wait_ms = int(c.get("arm_wait_ms", 0) or 0)
-            if wait_ms > 0:
-                time.sleep(wait_ms / 1000.0)
-
-        t0 = now_ms()
-        res = send(pid, cmd, c["args"])
-        cost = now_ms() - t0
-
-        time.sleep(settle_ms / 1000.0)
-        h1 = status(pid)["stateHash"]
+            try:
+                res = coopctl.call(pid, "spike", extra={"sub": "arm", "seconds": arm_s})
+                armed = bool((res.get("result") or {}).get("armed")) if res.get("ok") else False
+                session_facts["arm_active_until_ms"] = now_ms() + arm_s * 1000
+            except Exception as e:
+                aborted = "connection-lost during arm: %s" % e
+                break
+            wait_ms = int(c.get("arm_wait_ms", 0) or 0)
+            if wait_ms > 0:
+                time.sleep(wait_ms / 1000.0)
+
+        try:
+            t0 = now_ms()
+            res = send(pid, cmd, c["args"])
+            cost = now_ms() - t0
+        except Exception as e:
+            aborted = "connection-lost during fire: %s" % e
+            break
+
+        try:
+            time.sleep(settle_ms / 1000.0)
+            post, err = read_status(pid)
+        except Exception as e:
+            post, err = None, "exception: %s: %s" % (type(e).__name__, e)
+        if post is None:
+            # fired but post-state unreadable (crash/heartbeat loss): record what
+            # we have as UNKNOWN - never claim success from incomplete data
+            aborted = "state-unreadable after fire: %s" % err
+            rows.append({"n": n, "group": group, "cmd": cmd, "args": c.get("args"),
+                         "result": "UNKNOWN", "aborted_reason": aborted,
+                         "prechecks": prechecks, "pre_state": pre,
+                         "hash_before": h0, "hash_after": None,
+                         "reply": res if isinstance(res, dict) else str(res),
+                         "expect": c["expect"], "latency_ms": cost, "ts": ts})
+            print("%-4s %-14s %-7s %-8s %-6s %-6s %s" % (n, group, cmd, "UNKNOWN", "?", "?", aborted[:90]))
+            break
 
         err = err_text(res)
         okflag = bool((res.get("result") or {}).get("Ok"))
         expect = c["expect"]
         hash_same = (h0 == h1)
         code_ok = expect.lower() in err.lower()
         passed = (not res.get("ok")) and (not okflag) and hash_same and code_ok
 
         row = {
             "n": n, "group": group, "cmd": cmd, "args": c["args"],
             "arm": arm_s, "armed": armed,
             "pref": cpref,
             "hash_before": h0, "hash_after": h1, "hash_same": hash_same,
             "reply_ok": res.get("ok"), "applied_flag": okflag,
             "error": err, "expect": expect, "code_ok": code_ok,
             "latency_ms": cost, "pass": passed,
             "ts": time.strftime("%Y-%m-%d %H:%M:%S"),
         }
+        row["result"] = "PASS" if passed else "FAIL"
+        row["prechecks"] = prechecks
+        row["pre_state"] = pre
         rows.append(row)
@@ -179,3 +320,20 @@
-    coopctl.call(pid, "spike", extra={"sub": "disarm"})
-    set_pref(pid, "allowRemoteInput", False)
+    cleanup_ok = True
+    try:
+        coopctl.call(pid, "spike", extra={"sub": "disarm"})
+        set_pref(pid, "allowRemoteInput", False)
+    except Exception:
+        cleanup_ok = False          # connection may already be dead after a crash
@@ -189,3 +346,10 @@
         "base_hash": base_hash, "cases": rows,
-        "pass": sum(1 for r in rows if r["pass"]), "fail": sum(1 for r in rows if not r["pass"]),
+        "pass": sum(1 for r in rows if r.get("result") == "PASS"),
+        "fail": sum(1 for r in rows if r.get("result") == "FAIL"),
+        "notrun": sum(1 for r in rows if r.get("result") == "NOT-RUN"),
+        "unknown": sum(1 for r in rows if r.get("result") == "UNKNOWN"),
+        "aborted": aborted, "cleanup_ok": cleanup_ok,
     }
@@ -197,3 +376,4 @@
-    return 1 if failed else 0
+    if aborted:
+        return 3
+    return 1 if failed else 0
```

**สรุปพฤติกรรมใหม่:** ทุกเคส = อ่าน state สด (รอ hash นิ่ง) → ตรวจ premise รายกลุ่ม → ผ่านค่อยยิง (พฤติกรรมเดิม) · ไม่ผ่าน = `NOT-RUN(reason)` ไม่ยิง · ขาด connection หลังยิง = `UNKNOWN` + abort (exit 3) ไม่ retry · ทุกแถวมี `pre_state` + `prechecks` + hash ก่อน/หลัง (หลัง = null ถ้าไม่ยิง/อ่านไม่ได้)

## 4. UNKNOWN

1. **OUT_OF_RANGE premise ยืนยันไม่ได้จาก surface ปัจจุบัน** (`TurnGate.cs:505-514` expose แค่ count) — ภายใต้ patch นี้เคส 7–10 จะเป็น `NOT-RUN(premise-unverifiable-range)` ทุกครั้ง · เพื่อรันจริงต้องเพิ่ม cells ใน status (แก้ mod source + rebuild + redeploy — **อนุมัติแยก**) หรือตัดเคสกลุ่มนี้ออกจากชุดตามมติ
2. **arm state ไม่ถูก expose** — DISARMED (เคส 1–2) ยึด runner-local fact + wall-clock เท่านั้น
3. **ต้นเหตุ crash ของ Gate 2 ยัง UNKNOWN** — patch นี้กัน "ยิงด้วยพรีมิสผิด" แต่**ไม่สามารถกัน crash ของเกมเองจาก action ที่เกมรับ** ได้ (ถ้าเกม crash กับ attack ที่ in-range อีก จะเจอที่เคสอื่นแทน)
4. `moved`/`attacked` ใน status คืน None (turn-flag property อ่านไม่ได้ใน build นี้) — ไม่ถูกใช้ใน preconditions แต่บันทึกไว้เพื่อความชัด
5. ไม่ได้ตรวจ: พฤติกรรมจริงของ runner ใหม่บน runtime (ห้ามยิงในรอบนี้) — unit-test ฝั่ง runner ไม่มีในโครงสร้างเดิม (pytest ไม่มีใน repo) · เสนอตรวจด้วย dry-run mode หรือตรวจด้วยมือหลังอนุมัติ
