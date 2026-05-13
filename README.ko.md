# Sts2HostObserver

Slay the Spire 2 멀티플레이 lobby 를 **dedicated host (전용 호스트)** 로 띄우는 mod — 세션만 열고 player slot 은 차지 안 함. client 4명이 자유롭게 join 해서 플레이.

🇺🇸 [English README](README.md)

> ⚠️ **v0.1.0 — pre-alpha scaffold.**
> 핵심 Harmony patch 는 작성됐지만 라이브 검증 안 됨. STS2 멀티플레이는 두 인스턴스 동시 필요해서 검증이 별도 환경 (PC 두 대 / 친구) 필요. lobby UI / character select 의 null-safety patch 가 실제 게임 실행 후 발견되는 NRE 따라 추가될 수 있어요. 이 release 는 scaffold publish 용 — 직접 켜는 건 본인 책임이에요.

## 동작 개요

STS2 멀티플레이는 호스트가 항상 player slot 0번을 차지. `StartRunLobby.AddLocalHostPlayer(UnlockState, int)` 가 lobby init 시 호스트를 `Players` 에 추가하는 명시적 진입점. 이 mod 는 Harmony Prefix 로 **`ObserverState.Enabled == true` 일 때 그 호출을 skip** — 결과적으로 0명 host + 4 slot 비어있는 채로 lobby 시작.

두 가지 patch:
- **`SkipAddLocalHostPlayerPatch`** — `StartRunLobby.AddLocalHostPlayer(UnlockState, int)` 와 `AddLocalHostPlayerInternal` 에 Prefix. `null` 반환하고 원본 skip.
- **`LocalContextGetMePatch`** — `LocalContext.GetMe` 에 Harmony Finalizer. `RunState.Players` 에 host 가 없으면 `LocalContext.GetMe(state)` 가 `InvalidOperationException("Local player not found")` throw — observer 모드에선 finalizer 가 그 exception 을 삼키고 caller 에게 `null` 반환.

나머지는 STS2 가 알아서 잘 처리:
- `NRemoteLobbyPlayerContainer._nodes` 가 dynamic list — 4 slot 자동 표시
- `LobbyBeginRunMessage.playersInLobby = Players` — host 없는 `Players` 가 그대로 `RunState.Players` 로
- `ActChangeSynchronizer._readyPlayers.Count = RunState.Players.Count` — 턴 gate 가 4명 ready 기다림 (5명 아님). host input 없어서 deadlock 발생 안 함
- 모든 in-game synchronizer (rest site / event / merchant / treasure) 가 netId-keyed — host netId 가 silent no-op

## 활성화 방법

1. mod 설치
2. STS2 한 번 실행 — mod 가 `<user_data>/Sts2HostObserver/observer.json` 을 `{ "enabled": false }` 로 생성
3. STS2 종료 → `observer.json` 을 `{ "enabled": true }` 로 편집 → 저장
4. STS2 재실행 → Standard 또는 Daily 멀티플레이 lobby host. client 4명까지 join 가능. host 화면엔 자기 캐릭터 slot 자리에 빈 UI 가 보일 수도 있는데 pre-alpha 단계의 기대 동작.

## 알려진 pre-alpha 한계

- **Lobby UI 의 character slot 깜빡임 / 빈칸**. `_lobby.LocalPlayer` 가 `default(LobbyPlayer)` (id=0, character=null) 반환 → `LocalPlayer.character.Name` 같은 UI 바인딩이 defensive null check 필요. 발견 즉시 patch 추가 예정.
- **`RunManager.UpdateRichPresence()`, room completion handler 들** — `LocalContext.GetMe(State).Character` 를 null-check 없이 호출. 이 mod 의 finalizer 가 throw 를 잡지만 caller 가 받는 건 `null Player` 라 그 다음 코드가 또 NRE 가능.
- **in-game 토글 UI 없음.** observer.json 편집 + 재시작 필요.
- **실제 2 인스턴스 환경에서 테스트 안 됨.** 코드 차원 검증만.

## 다른 mod 와의 호환성

- **[Sts2MultiplayerSync](https://github.com/ing-gom/Sts2MultiplayerSync) 와 양립.** MultiplayerSync 는 client 측 mismatch 우회, HostObserver 는 host 측 self-add skip. patch 대상 안 겹침. 일반적 사용 — host 에 둘 다 설치 + client 에 MultiplayerSync 만 설치.
- **다른 Sts2\* 자매 mod (SkinManager, CardAdvisor 등) 와 양립.**
- HostObserver 자체는 `affects_gameplay=false` — multiplayer mismatch 사유 안 됨.

## 주의사항

- host 측 mod 만. client 는 안 깔아도 됨.
- `affects_gameplay` mod-match 검증은 **우회 안 함** — MultiplayerSync 와 같이 써야 mismatched mod 셋 가능.
- host 의 `LocalContext.NetId` 는 여전히 set 되지만 매칭되는 `Player` 가 없어서 "내 player 로 뭔가 해" 류 코드는 silent no-op 또는 null 받음. host 를 진짜 spectator 로 취급해주세요.

## 라이선스

MIT. `LICENSE` 참조.
