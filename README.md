![Gif0](https://github.com/MidnightFork/xb-gifs/blob/main/XBurger0.gif?raw=true)

![GamePlayGif0]

![GamePlayGif1]https://github.com/MidnightFork/xb-gifs/blob/main/XBurger1.gif?raw=true
![GamePlayGif2]https://github.com/MidnightFork/xb-gifs/blob/main/XBurger2.gif?raw=true


# 🍔 X Burger - Advanced Gameplay Systems & Mechanics Showcase

This repository serves as a deep dive into advanced gameplay systems, architectural patterns, and accessibility features I've developed in Unity. Rather than just building a "game," I focused on creating robust, scalable systems that mirror production-grade development environments.

## 🚀 Key Technical Highlights

### 🕹️ Advanced Input & Aim Assist System (Console-Grade)
Moving beyond basic mouse input, I implemented a comprehensive Gamepad support system with a custom **Aim Assist** logic:
* **Dynamic Interaction:** Uses `SphereCast` instead of traditional `Raycast` for more forgiving interaction detection.
* **Sensitivity Scaling:** Implemented "Look Speed Friction" that automatically reduces sensitivity when the `SphereCast` detects interactable objects.
* **Developer Presets:** A dropdown-based preset system to fine-tune `SphereCast` radius and slowdown intensity on the fly.
* **Input Device Detection:** Real-time monitoring of the last input source (Mouse vs. Gamepad) to dynamically swap UI icons and tooltips.

### 🌍 Professional Localization Architecture
Built a 7-language localization system designed for performance and seamless user experience:
* **Observer Pattern:** Utilizes an `OnLanguageChanged` event system. UI elements subscribe to this event to update their text instantly without reloading scenes.
* **Decoupled Data:** Managed through a backend-ready structure, allowing for easy expansion to more languages.

### 🧠 Customer AI & State Machine
The shop simulation is powered by a robust **Finite State Machine (FSM)**:
* **Logic Flow:** Customers autonomously enter the shop -> Select a random order from the pool -> Navigate to available seating via Pathfinding after receiving their order.
* **Scalability:** The FSM allows for easy addition of new states (e.g., waiting, complaining, leaving) without breaking existing logic.

### ♿ Accessibility & UX Features
* **Customizable Keybindings:** Fully remappable input system for both Keyboard/Mouse and Gamepad.
* **UI Scaling & Visibility:** Adjustable UI size and toggleable tooltips to cater to different player needs.
* **Game "Juice":** Polished feedback loop using **Cinemachine** for dynamic camera work and **DOTween** for juicy, elastic UI and object animations.

## 🛠️ Tech Stack & Tools
* **Unity Engine** (C#)
* **Cinemachine** (Camera Systems)
* **DOTween** (Animations & Juice)
* * **Text Animator For Unity** (Typewriter Animations & Juice)
* **Unity Input System Package** (Cross-platform support)
* **NavMesh** (AI Navigation)

## 📈 Learning Journey
This project started as a self-improvement challenge. Throughout development, I mastered:
1.  **Event-Driven Programming:** Reducing class coupling for cleaner code.
2.  **State Management:** Handling complex AI behaviors efficiently.
3.  **UX Engineering:** Understanding how aim assist and input feel impact player retention.

---
*Note: This project is a testament to my growth as a developer—from basic scripts to complex, interconnected systems.*
