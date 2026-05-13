# Sts2HostObserver

Slay the Spire 2 멀티플레이 lobby 를 **dedicated host (전용 호스트)** 로 띄우는 mod — 세션만 열고 player slot 은 차지 안 함. client 4명이 자유롭게 join 해서 플레이.

🇺🇸 [English README](README.md)

> ⚠️ **v0.1.0 — pre-alpha scaffold.**
> 핵심 Harmony patch 는 작성됐지만 라이브 검증 안 됨. STS2 멀티플레이는 두 인스턴스 동시 필요해서 검증이 별도 환경 (PC 두 대 / 친구) 필요. lobby UI / character select 의 null-safety patch 가 실제 게임 실행 후 발견되는 NRE 따라 추가될 수 있어요. 이 release 는 scaffold publish 용 — 직접 켜는 건 본인 책임이에요.

## 동작 개요

STS2 멀티플레이는 호스트가 항상 player slot 0번을 차지. `StartRunLobby.AddLocalHostPlayer(UnlockState, int)` 가 lobby init 시 호스트를 `Players` 에 추가하는 명시적 진입점. 이 mod 는 (a) 그 호출을 skip 하고 (b) **`LocalContext.NetId` 를 실제 client 중 한 명의 NetId 로 redirect** — 그러면 host 의 in-game UI 가 그 client 의 view 로 자동 동작. combat / hand / intent / event / rest site / merchant / treasure / map 가 전부 `LocalContext.GetMe(...)` 기반이라 우리가 한 곳에서 redirect 하면 전체 UI 가 spectator view 로 따라옴.

네 가지 patch + Godot input/UI layer:
- **`SkipAddLocalHostPlayerPatch`** — `StartRunLobby.AddLocalHostPlayer(UnlockState, int)` 와 `AddLocalHostPlayerInternal` 에 Prefix. `null` 반환하고 원본 skip → `Players` 비어 있음.
- **`LocalContextGetMePatch`** — `LocalContext.GetMe(...)` 에 Harmony Finalizer. `LocalContext.NetId` 가 redirect 안 됐을 때 (lobby 단계, client 아직 join 안 함) `"Local player not found"` throw 를 swallow 하고 caller 에게 `null` 반환.
- **`LocalContextNetIdRedirectPatch` (핵심)** — `LocalContext.NetId` 의 getter Prefix. `SpectatorState.SpectatedPlayerNetId` 가 set 되어 있으면 host 자신의 NetId 대신 그 값 반환. 게임 코드의 모든 `GetMe()` 가 이 property 통과 → spectator client view 자동 적용.
- **`LocalContextNetIdSetterCapturePatch`** — setter Postfix. `RunManager.Launch` 가 host NetId 를 쓰는 순간 그 값을 `SpectatorState.RealHostNetId` 에 기록 → spectatable client 목록에서 host 자신을 제외.
- **`SpectatorInputHandler`** — `tree.Root` 에 attach 된 Godot Node. **Tab 키** 누르면 다음 non-host client 로 cycle.
- **`SpectatorStatusOverlay`** — 화면 우측 상단 작은 라벨로 현재 spectated client 표시.

`SpectatorState` 가 **lazy auto-spectate** 수행 — client 가 join 한 후 첫 번째 `LocalContext.NetId` 읽기 시점에 자동으로 첫 non-host player 선택. 즉 host 가 Tab 안 눌러도 기본적으로 Player 1 의 view 가 보이고, 거기서 Tab 으로 cycle 가능.

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

## 조작

- **Tab 키** — 다음 non-host client 로 cycle.
- 화면 우측 상단에 `[HostObserver] watching: <캐릭터 이름>  (Tab to cycle)` 표시.

## 알려진 pre-alpha 한계

- **Lobby 단계에서 client 아직 join 안 했을 때**. client 가 들어오기 전엔 redirect 할 NetId 가 없어서 `GetMe` 가 null 반환 → lobby UI 가 `_lobby.LocalPlayer.character.Name` 류 binding 에서 빈칸/깜빡 가능. client 한 명이라도 들어오면 lazy auto-spectate 가 발동해서 host view 안정화.
- **`_lobby.LocalPlayer` 는 `LocalContext.NetId` 와 별개**. 우리 redirect 가 `StartRunLobby.LocalPlayer` 는 안 건드림 (그쪽은 `LocalContext` 안 거치고 `NetService.NetId` 직접 사용). 어떤 화면이 `_lobby.LocalPlayer` 를 직접 보고 `.character` 에 null-check 없이 접근하면 NRE. 실제 2-인스턴스 테스트에서 발견되면 patch 추가.
- **Tab 키 충돌 가능성**. unhandled-input 으로 받으니까 STS2 의 UI 가 Tab 을 먼저 소비하면 우리 handler 안 호출. 추후 rebindable 로 개선 가능.
- **실제 2 STS2 인스턴스 환경에서 테스트 안 됨**. 코드 읽기 + single-instance 부팅 검증만 있음.

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
