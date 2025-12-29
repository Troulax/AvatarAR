# Avatar AR Board Game

This project is a **mobile Augmented Reality (AR) board game** developed with **Unity**.  
It combines classic board-game mechanics with **Avatar-based buff systems**, designed for **1 human player + 3 AI bots**.

The game is built using **AR Foundation**, supporting **Android (ARCore)** and optionally **iOS (ARKit)**.

---

## 🧩 Technologies Used

- Unity (2021 LTS or newer recommended)
- AR Foundation
- ARCore XR Plugin (Android)
- (Optional) ARKit XR Plugin (iOS)
- C#
- Mobile touch input

---

## 📱 Supported Platforms

- ✅ Android (ARCore-supported devices)
- ⏳ iOS (ARKit – planned)

> ⚠️ AR features **cannot be tested in the Unity Editor**. A real device is required.

---

## 🚀 Setup – Step by Step

### 1️⃣ Install Required Packages
In Unity:

`Window > Package Manager`

Install:
- **AR Foundation**
- **XR Plug-in Management**
- **ARCore XR Plugin**
- (If targeting iOS) **ARKit XR Plugin**

---

### 2️⃣ Select Build Target (IMPORTANT)

`File > Build Settings`

- Select **Android**
- Click **Switch Platform**

> Android / iOS tabs in XR Plug-in Management appear **only after switching platform**.

---

### 3️⃣ XR Plug-in Management Setup

`Edit > Project Settings > XR Plug-in Management`

- Under **Android**:
  - ✅ Enable **ARCore**

---

### 4️⃣ Scene Setup (Core AR Objects)

In the Hierarchy, create:

- `AR Session`
- `XR Origin (AR)`  
  *(older versions may call this `AR Session Origin`)*

On **XR Origin (AR)**, add:
- `AR Plane Manager`
- `AR Raycast Manager`

---

### 5️⃣ Plane Detection (Surface Scanning)

- Assign a **Plane Prefab** to `AR Plane Manager`
- Recommended approach:
  - Open **AR Foundation** package
  - Import **Plane Detection** sample
  - Reuse the provided plane prefab

---

### 6️⃣ Android Player Settings

`Edit > Project Settings > Player > Android`

Recommended settings:
- Minimum API Level: **Android 7.0 (API 24)** or higher
- Scripting Backend: **IL2CPP**
- Target Architectures: **ARM64** enabled

---

## 🧱 Game Flow in AR

1. App launches
2. Camera scans the real-world surface (table / floor)
3. Player taps on a detected plane
4. **Board prefab** is placed at that position
5. Game starts:
   - Pawns move on the board
   - Avatar buffs activate
   - 1 Human + 3 AI bots play

---

## 🎯 Avatar Buff System (Overview)

- **Kyoshi**
  - Temporary capture protection (turn-based)
- **Roku**
  - Additional pawn deployment
- **Aang**
  - Extra movement using glider
- **Korra**
  - Capture-based advantages

> Aang and Roku do **not** use HUD elements  
> Kyoshi and Korra effects are shown directly on pawns via visual effects

---

## 🧪 Testing Notes

- AR features do **not** work in the Unity Editor
- Testing requires:
  - A real Android device
  - ARCore support enabled on the device

---

## 📌 Next Development Steps

- [ ] Board placement script (`ARBoardPlacer.cs`)
- [ ] AR touch input (pawn / tile selection)
- [ ] Board scaling and rotation adjustment
- [ ] AR-friendly lighting and shadows

---

## ℹ️ Notes

- UI is intentionally kept minimal
- Gameplay readability is handled via pawn-based visual effects
- Marker-based AR (image tracking) can be considered for higher stability

---

## 🧑‍💻 Developer Notes

This project is developed **step by step**,  
and each stage should be tested on a real device before moving forward.

---

## Developers

This project is developed by Yaşar Düzgün, Arda Ali Altıncı, Yüşa Emir Metin, Mert Kocuğlu for within the scope of ADA_410 course.

