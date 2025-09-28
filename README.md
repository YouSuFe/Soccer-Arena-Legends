# ⚽ Soccer Arena Legends
_A fast-paced Unity arena soccer game with combat abilities, unique characters, and hybrid first/third-person cameras._

---

## 🎮 Introduction

Soccer Arena Legends is a Unity-based prototype that blends **soccer mechanics** with **combat-inspired abilities** and **RPG-style progression**.  
It is designed for both **local practice** and **online multiplayer** using **Unity Netcode for GameObjects (NGO)** with **Unity Gaming Services (UGS) Relay + Lobby**.
⚙️ Dedicated Server: The project also supports a Unity Dedicated Server build (UGS), but this repo only contains the client/host version. The dedicated server logic lives in a separate project.

Players join matches, select unique characters and weapons, then compete in team-based soccer-combat matches with **power-ups, goals, assists, and environmental goal logic**.  

---

## ✨ Highlights

- ⚔️ **Abilities & Skills**: Each character and weapon combination offers a unique playstyle.  
- 📸 **Hybrid Camera**: Switch between first-person and third-person seamlessly with Cinemachine.  
- 🌐 **Multiplayer with Relay + Lobby**: Host or join matches through Unity’s Lobby system.  
- 🧩 **Scene-Based Flow**: From bootstrap to match, each scene drives part of the lifecycle.  
- 📦 **Content Pipeline**: ScriptableObjects for characters/weapons, Addressables for assets.  

---

## 🧰 Tech Stack

- **Engine**: Unity 6 LTS (URP)  
- **Core**: C#, Input System, TextMeshPro, Cinemachine  
- **Networking**: Unity Netcode for GameObjects (NGO) + UGS Relay + Lobby  
- **Scene Flow**: Bootstrap → NetBootstrap → Menu → Character Selection → Game  
- **Assets**: Addressables, ScriptableObjects  

---

## 🗺️ Scene Management & Flow

The game is structured around **five core scenes**, each responsible for part of the experience:

### 1️⃣ Bootstrap
- Entry point: shows game title/name.  
- Minimal scene that immediately transitions into **NetBootstrap**.  

### 2️⃣ NetBootstrap
- **Core “glue” scene**: sets up networking and application state.  
- Contains:
  - **NetworkManager** (NGO): manages network sessions, spawning, and authority.  
  - **ApplicationManager**: decides runtime mode: **Dedicated Server**, **Host**, or **Client**.  
- This ensures consistent startup logic whether running as a player client or dedicated host.  

### 3️⃣ Menu
- Acts as a **lobby hub** where players can:  
  - Select game modes  
  - Host or join lobbies  
  - Pick teams  
- Contains **LobbyManager**:
  - Handles lobby creation, joining, and team balance logic.  
  - Example: If one team has 4 and the other has 2, only players joining the larger team can be redirected to balance (making it 3–3).  
- ⚠️ **Important Note:** When a new lobby is created, it **does not appear automatically** in the list.  
  Players must **click the refresh button** to update the lobby list.  

### 4️⃣ Character Selection
- Each team chooses **unique characters and weapons**.  
- Once a character or weapon is picked, it becomes unavailable to other players on the same team.  
- Ensures variety and prevents duplicate playstyles within a team.  

### 5️⃣ Game
- The actual **arena soccer-combat match**.  
- Core gameplay includes:
  - Collectable **power-ups** on the field  
  - Two-team soccer with **goals, assists, and accidental goals**  
  - **Environmental hazards/goals** that affect scoring  
- All character/weapon abilities, ball mechanics, and damage mechanics are executed here.  

---

### 🗺️ Architecture Overview
- **Player Controller**: Rigidbody Movement, Rigidbody physics, and input-driven rotation.  
- **State Machine**: Handles all movement such as grounded, airborne, dashing, and landing states.  
- **Ability System**: ScriptableObject-driven definitions with cooldowns and effects.  
- **Camera System**: Cinemachine-based with a switch handler for FP/TP.  
- **Networking Layer**: Relay allocation and player spawn manager.  
- **UI**: Dynamic HUD cooldowns and stats.

---

# Document
https://drive.google.com/drive/folders/1UScvgWwXQ4Sil1jKkV-fDQ3D5OolXGcP?usp=sharing
