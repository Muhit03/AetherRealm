# AetherRealm ⚔️🛡️

A 3D **arena survival action-RPG** built in Unity 6 and C#, with a **Microsoft SQL
Server** database for accounts and the leaderboard.

Written for a C# / OOP course: every system is small, plain C# that shows
**encapsulation**, **inheritance**, **polymorphism** and **abstraction** — but it
is also a complete game you can actually play.

---

## ▶️ How to run it

1. *(Optional)* set up SQL Server — see **Database** below. The game runs fine
   without it (everything saves locally instead).
2. Open the project in Unity Hub (**Unity 6000.5.10f1**).
3. Press **Play**.

That's it. The game builds its whole world, UI, lighting and sound from code when
it starts, so there is nothing to set up in the editor.

*(Optional: the menu item **AetherRealm ▸ Set Up Play Scene** tidies the scene file
and removes an old "missing script" warning. It is not required.)*

### Controls

| Key | Action |
|---|---|
| **WASD** | Move |
| **Left Mouse** | Attack (3-hit combo) |
| **Space** | Dodge roll (short invincibility) |
| **Q** or **Right Mouse** | Class ability |
| **E** | Talk to Elder Eldrin / open chests / skip dialogue |
| **Tab** | Leaderboard (also a button on the main menu) |
| **Esc** | Pause |
| **F** | Skip the shop break between waves |

### The game

Survive **8 waves** that pour in from four portals. Each wave has more enemies,
tougher stats and new enemy types:

| Enemy | Behaviour |
|---|---|
| **Goblin** (wave 1+) | Rushes in. A group spreads out to *surround* the player instead of queueing. After a swing it often **raises a guard** — hits from the front only do a quarter damage. |
| **Archer** (wave 3+) | Hangs back, moves to a spot **behind a cover wall**, and only shoots when it has a clear line to the player. If you close in it **runs**; if the shot is blocked it shuffles or finds new cover. |
| **Brute** (wave 4+) | Big, slow, lots of health. Guards more than half the time and its hit **shoves you backwards**. |
| **Ogre Warlord** (wave 8) | Boss. Ground-slam AoE, never blocks. Beat it to win. |

A big unblocked hit **staggers** any non-boss enemy for a moment. Kills drop
gold; spend it at Elder Eldrin's shop between waves. Every run is saved to the
leaderboard (on death, victory, or quitting), and the leaderboard lists **every
registered player** — a new account shows up immediately with zeros.

### Classes

| | Basic attack (Left Mouse) | Ability (Q / Right Mouse) | Toughness |
|---|---|---|---|
| **Warrior** | Melee sword swing (3-hit combo) | Shield-bash shockwave | Armour soaks 4 off every hit |
| **Mage** | **Ranged** magic bolt toward the cursor | Fan of 5 bolts (costs mana) | Mana soaks 3 off every hit until it runs out |

---

## 🧠 OOP for the course

| Concept | Where to point |
|---|---|
| **Abstraction** | `IDamageable`, `IInteractable`, `IEnemyState`, `ISaveable` — callers use the interface and never check the real type |
| **Inheritance** | `Warrior` / `Mage` extend `PlayerController`; `MeleeGoblin` / `RangedArcher` / `BruteGoblin` / `BossOgre` extend `EnemyController` |
| **Polymorphism** | overridden `TakeDamage`, `UseAbility`, `DoAttack`, `OnDeath`; one attack-overlap hits players, enemies **and** barrels with the same code |
| **Encapsulation** | health / gold / score are private, read through properties and changed only through methods like `AddGold` and `SpendGold` |

Other things worth mentioning in a viva:
- **State machine** — `EnemyController` never checks "am I chasing, attacking or
  blocking"; it just runs `Enter` / `Tick` / `Exit` on the current `IEnemyState`.
  The states live in `EnemyStates.cs` (`MeleeApproachState`, `BlockState`,
  `ArcherRepositionState`, `ArcherShootState`, `StaggerState`, ...).
- **Events** — `EnemyController.Died` is a `static event`; the wave manager and
  HUD subscribe to it instead of everything referencing each other.
- **NavMesh** — enemies path with `NavMeshAgent`; archers use `Physics.Raycast`
  for line-of-sight and read the cover-wall positions from `ArenaLayout`.
- **Database** — `DatabaseManager` calls SQL Server **stored procedures** with
  parameterised `SqlCommand`s; passwords are **SHA-256 hashed** in `AuthManager`.

### Memory / performance notes

- Shared `Collider[]` buffer for attack overlap checks (`CombatUtil`) instead of
  allocating a new array every swing.
- Hard caps on live damage-numbers and sparks (`Effects`).
- `WaveManager.maxEnemiesAtOnce` keeps at most 12 enemies alive; the rest spawn
  as those die.
- Projectiles use one short raycast per frame instead of an overlap sphere.
- Sound clips are generated at 16 kHz mono.

---

## 🗄️ Database (Microsoft SQL Server — no Docker)

Uses **`Microsoft.Data.SqlClient` 3.1.5** (the current Microsoft client library —
`System.Data.SqlClient` is deprecated and doesn't run on Windows ARM64). The
library + native networking DLLs are in `Assets/Plugins/SqlClient/`.

### Set it up

1. Install **SQL Server Express** (or Developer). LocalDB also works.
   *(sqlcmd / SSMS gives you the command-line tools.)*
2. Create the database:
   ```bat
   Database\setup_database.bat
   ```
   If your instance isn't the default one, open that file and change
   `set SERVER=localhost` to e.g. `set SERVER=localhost\SQLEXPRESS`.
3. Tell the game where the server is — edit the one connection-string line in
   **`Assets/StreamingAssets/db_config.txt`** (no recompile needed). Examples are
   in that file. The default is `Server=localhost;…;Trusted_Connection=True`.

Schema + stored procedures: `Database/AetherRealmDB_Schema.sql` (re-running it is
safe and adds any new columns).

### Offline fallback

If the database can't be reached, `DatabaseManager` automatically uses
`LocalStore` (Unity `PlayerPrefs`) instead — accounts and the leaderboard are
kept on your PC and the game plays identically. The HUD shows `(offline)` next
to your name. The SQL code path is the real one whenever the server is up.

(The first login attempt waits up to ~3 s for the server before falling back.)

---

## 📁 Where the code lives

```
Assets/Scripts/
  Core/       GameBootstrap (builds everything), Palette, Effects, Fonts
  Characters/ PlayerController + Warrior/Mage,
              EnemyController + MeleeGoblin/RangedArcher/BossOgre,
              CharacterBuilder, ProceduralAnimator, EnemyFactory
  Combat/     Projectile, Knockback, DamageFlash, HealthBar, FloatingText,
              PickupOrb, RingExpand
  World/      ArenaBuilder (also bakes the NavMesh), WaveManager, Torch, CameraFollow
  UI/         UIBuilder + UIFactory, HUDController, MainMenuPanel, LoginPanel,
              ShopPanel, PauseMenu, EndScreen, DialoguePanel, LeaderboardPanel,
              ScreenEffects
  Managers/   GameManager, AuthManager, UIManager, LeaderboardManager,
              AudioManager, DatabaseManager, LocalStore
```

### Notes

- **Enemies path with a baked NavMesh.** `ArenaBuilder` adds a `NavMeshSurface`
  and calls `BuildNavMesh()` at runtime; every enemy has a `NavMeshAgent`.
- **No art or audio assets.** Characters are built from cubes and spheres,
  animated by `ProceduralAnimator`; every sound is a short tone generated in
  `AudioManager`; every material is made in `Palette`.
- `Assets/Editor/AutoPlay.cs` enters Play Mode automatically after each recompile.
  Delete it if you'd rather press Play yourself.
- `Assets/Editor/AetherRealmCI.cs` is a command-line smoke test
  (`Unity.exe -batchmode -executeMethod AetherRealmCI.Run`) — it was used to
  verify the game builds and runs with no errors on this machine.
- `dev-compilecheck.ps1` (repo root) is a quick `dotnet build` compile check.
  Both files are dev-only; delete them if you don't want them.
