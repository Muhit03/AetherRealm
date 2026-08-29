# AetherRealm ⚔️🛡️

A 3D **arena survival action-RPG** built in Unity 6 and C#, with a **Microsoft SQL
Server** database for accounts and the leaderboard.

Written for a C# / OOP course: every system is small, plain C# that shows
**encapsulation**, **inheritance**, **polymorphism** and **abstraction** — but it
is also a complete game you can actually play.

---

## ▶️ How to run it

1. Start the SQL Server database (see **Database** below).
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
| **E** | Talk to Elder Eldrin / open chests |
| **Tab** | Leaderboard |
| **Esc** | Pause |
| **F** | Skip the shop break between waves |

### The game

Survive **8 waves** of goblins and archers that path in from four portals. Kills
drop gold. Between waves, Elder Eldrin's shop lets you spend gold on permanent
upgrades (health, damage, speed, cooldown, lifesteal). Wave 8 is the **Ogre
Warlord** boss — beat it to win. Your score is saved to the SQL leaderboard.

---

## 🧠 OOP for the course

| Concept | Where to point |
|---|---|
| **Abstraction** | `IDamageable`, `IInteractable`, `IEnemyState`, `ISaveable` — callers use the interface and never check the real type |
| **Inheritance** | `Warrior` / `Mage` extend `PlayerController`; `MeleeGoblin` / `RangedArcher` / `BossOgre` extend `EnemyController` |
| **Polymorphism** | overridden `TakeDamage`, `UseAbility`, `DoAttack`, `OnDeath`; one attack-overlap hits players, enemies **and** barrels with the same code |
| **Encapsulation** | health / gold / score are private, read through properties and changed only through methods like `AddGold` and `SpendGold` |

Other things worth mentioning in a viva:
- **State machine** — `EnemyController` never checks "am I chasing or attacking";
  it just runs `Enter` / `Tick` / `Exit` on the current `IEnemyState`.
- **Events** — `EnemyController.Died` is a `static event`; the wave manager and
  HUD subscribe to it instead of everything referencing each other.
- **Database** — `DatabaseManager` calls SQL Server **stored procedures** with
  parameterised `SqlCommand`s; passwords are **SHA-256 hashed** in `AuthManager`.

---

## 🗄️ Database (Microsoft SQL Server)

Uses **`Microsoft.Data.SqlClient` 3.1.5** — the current Microsoft client library.
(`System.Data.SqlClient` is deprecated and throws `PlatformNotSupportedException`
on Windows ARM64, which is this machine.) The library plus its dependencies and
the native networking DLLs (`Microsoft.Data.SqlClient.SNI.arm64/x64/x86.dll`) are
all in `Assets/Plugins/SqlClient/`. `DatabaseManager`'s static constructor points
Windows at that folder so the native DLL is found.

Verified working on Windows ARM64: the client loads, the native TCP provider
engages, and it makes a real connection attempt.

### Windows
1. Open **Docker Desktop**.
2. Run `Database/setup_docker_sql.bat`.

### macOS
1. Open **Docker Desktop**.
2. `bash Database/setup_docker_sql.sh`

The connection string is on the `DatabaseManager` component
(`Server=localhost,1433; Database=AetherRealmDB; User Id=SA; …`). Schema and
stored procedures are in `Database/AetherRealmDB_Schema.sql`.

### Offline fallback

If the database can't be reached, `DatabaseManager` automatically switches to
`LocalStore` (Unity `PlayerPrefs`): accounts and the top-10 leaderboard are kept
on your PC and the game plays exactly the same. The HUD shows `(offline)` next
to your name so you know which one is active. The SQL code path is still the
real one whenever the server is up.

(The very first login/register attempt waits ~3 seconds for the server before
falling back — after that it's instant.)

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
