# S2a-0 — protocol codec (T0)

สถานะ: **commit แล้ว** · ไม่เปิดเกม · ไม่แตะเซฟ · ไม่ build/copy DLL เข้า `Mods/`

## ไฟล์

| ไฟล์ | เนื้อหา |
|---|---|
| `src/LongYinCoop/S2/Protocol/S2Types.cs` | `MessageType` (Hello 1 … Chunk 10, Auth 32, Ping 33, Resync 34), `ProtoVer=1`, cap 1100 B / 64 KB, allowlist guest→host |
| `src/LongYinCoop/S2/Protocol/CompactCodec.cs` | varint u32/u64, float32, bool, UTF-8 string (u16 len), bytes (varint + cap), reader fail-closed |
| `src/LongYinCoop/S2/Protocol/FrameCodec.cs` | `[u16 BE bodyLen][type][ver][u32 BE seq][payload]`, chunk split/reassembly |
| `src/LongYinCoop/S2/Protocol/S2Auth.cs` | HMAC-SHA256(nonce‖PROTO_VER), `FixedTimeEquals`, `FieldHash` |
| `src/LongYinCoop/S2/Protocol/ReplicaHash.cs` | canonical `s2|2` hash: hp(3), `isAlive`, `unitAlive`, เรียงตาม `stableId` |
| `tests/S2Protocol.Tests/` | net6.0 console, ไม่มี NuGet package, link source แบบ read-only |

## ผลทดสอบ

`dotnet run --project tests\S2Protocol.Tests` → `t0-protocol-tests.txt`

**66 checks PASS** ครอบ:
- frame: ขนาด, cap 1100 B, header เพี้ยน/ตัดขาด, type byte ไม่รู้จัก
- chunk: split, reassembly (ปกติ/out-of-order/duplicate/timeout/pending cap/total=0/nested chunk ปฏิเสธ)
- compact: varint ขอบเขต u32/u64, float, bool, string, bytes, อ่านเกินท้าย, blob เกิน cap
- auth: HMAC-SHA256 ตรง RFC 4231 case 1, tag 32 B, tamper/short tag/empty secret/nonce ผิดขนาด
- fieldHash: order-insensitive, anchor-sensitive
- replicaHash: order-insensitive, เปลี่ยนตาม hp / `isAlive` / `unitAlive` / moved / turnIndex / paused
- allowlist: Auth/Ping/Resync/Bye ผ่าน, Hello/Snapshot/BattleEvent/Chunk/0x41 ไม่ผ่าน

## ยังไม่ทำ (นอกขอบเขต S2a-0)

transport, host sampler/sender, guest store/overlay, การ build/copy DLL, การเปิดเกม
