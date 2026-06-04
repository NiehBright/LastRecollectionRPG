# 🗡️ Combat System - Complete Setup

## 📖 Overview

Combat system đã được cập nhật theo yêu cầu:
- ✅ **Input System**: Dùng `Attack` action thay vì chuột trực tiếp
- ✅ **Equal Damage**: Mỗi đòn (1, 2, 3, 4) gây **60 damage** bằng nhau
- ✅ **4-Hit Combo**: Nhấn trong 4 giây sẽ nối combo (không tự động chuyển)
- ✅ **Stop at Input**: Nếu ấn 1→2→3, sẽ dừng ở đòn 3 (không tự động chạy 4)

---

## 🚀 Quick Start (5 phút)

### Step 1: Run Auto-Setup Menu
1. Chọn **Player** GameObject trong scene
2. Menu → `BLINK → Combat → Quick Setup - Enable Combat System`
   - ✅ Tự động thêm CombatController
   - ✅ Tự động thêm PlayerInput
   - ✅ Tự động tạo GameStartup + InputManager

### Step 2: Run Combat Assets Setup  
1. Menu → `BLINK → Combat → Setup Combat Assets`
   - ✅ Tạo Combat animator layer
   - ✅ Tạo Attack1..4 states
   - ✅ Tạo enemy prefab + damage popup + hit effect

### Step 3: Place Enemy + Test
1. Drag `Prefabs/EnemyDummy.prefab` vào scene
2. Nhấn Play
3. Ấn chuột trái 4 lần để test combo

---

## 📁 Files Modified/Created

### Combat System
- ✅ `CombatController.cs` - Main combat logic (Input System, equal damage, 4s timeout)
- ✅ `InputManager.cs` - Input System manager (find + enable Player action map)
- ✅ `GameStartup.cs` - Scene initialization (ensure InputManager exists)
- ✅ `CombatQuickSetup.cs` - One-click setup menu

### Animator Assets  
- ✅ `TopDownWASDAnimator.controller` - Combat layer + Attack1..4 + transitions
- ✅ `Attack1/2/3/4.anim` - Animation clips

### Guides
- ✅ `COMBAT_SETUP_GUIDE.md` - Detailed setup guide
- ✅ `COMBAT_SETUP_CHECKLIST.md` - Step-by-step checklist
- ✅ `README.md` (this file)

---

## 🎮 How It Works

### Input Flow
```
Player ➜ Input System (left click / Gamepad Y)
         ➜ PlayerInput component
         ➜ Attack action
         ➜ CombatController.OnAttackPerformed()
         ➜ Combo logic
```

### Combo Logic
```
T=0s: Click 1 → Start Attack1 animation
T=0.5s: Click 2 (during Attack1, combo window) → Queue Attack2
T=0.3s: Attack1 ends ➜ Start Attack2
T=0.7s: Click 3 (during Attack2) → Queue Attack3
T=0.3s: Attack2 ends ➜ Start Attack3
T=0.8s: (No click) → Attack3 ends ➜ Return to Idle
T=5s+: (If click after 4s timeout) → Reset combo, Start Attack1 again
```

### Damage Logic
```
All attacks = 60 damage
No scaling
No streak bonus
Hit detection: ~35-50% into animation (tuỳ chỉnh hitNormalizedTimes)
```

---

## 🔧 Configuration

### CombatController Inspector

| Field | Default | Purpose |
|-------|---------|---------|
| **attackAction** | null | InputActionReference (auto-find) |
| **baseDamage** | 60 | Sát thương mỗi đòn |
| **attackRange** | 1.6 | Khoảng cách detect enemy |
| **attackRadius** | 0.8 | Bán kính detect |
| **comboInputTimeout** | 4 | Max giây giữa lần bấm |
| **hitNormalizedTimes** | [0.35, 0.40, 0.45, 0.50] | Hit time % trong animation |
| **comboWindowStarts** | [0.45, ...] | Khi nào có thể queue combo tiếp |
| **comboWindowEnds** | [0.90, ...] | Hết window combo |

---

## 🧪 Testing Checklist

- [ ] **Input**: Ấn chuột → console "[CombatController] OnAttackPerformed"?
- [ ] **Animation**: Player chạy animation Attack1?
- [ ] **Combo**: Ấn nhanh 2x → chạy Attack1 → Attack2?
- [ ] **Stop**: Ấn 3 lần → dừng ở Attack3, không tự chạy Attack4?
- [ ] **Damage**: Enemy bị 60 damage popup?
- [ ] **Timeout**: Ấn sau 4s → reset combo, chạy Attack1?

---

## ⚠️ Common Issues

### Combat không hoạt động
**Problem**: Nhấn chuột không gì xảy ra  
**Solution**: 
1. Kiểm tra console logs (Ctrl+Shift+C)
2. Xác định "OnAttackPerformed called"?
   - Không? → PlayerInput / Input System không setup → chạy Quick Setup menu
3. Chạy `BLINK → Combat → Setup Combat Assets`

### Enemy không bị damage  
**Problem**: Nhấn attack, animation chạy, nhưng enemy health không giảm  
**Solution**:
1. Kiểm tra enemy tag = "Enemy"
2. Kiểm tra EnemyDummy component tồn tại
3. Kiểm tra hit range → gizmo trong Scene view

### Animation không blend mượt  
**Problem**: Transition giữa attack bị lag/jump  
**Solution**:
1. Animator → Combat layer → transition duration = 0.08
2. CombatController → comboTransitionDuration = 0.08
3. Verify animation clips được gán đúng

---

## 🔍 Debug Logs

Khi game chạy, xem console để trace:
```
[InputManager] Found InputActionAsset from PlayerInput in scene
[InputManager] Player action map enabled
[CombatController] Using InputManager.GetAction fallback
[CombatController] Attack action subscribed successfully
[CombatController] OnAttackPerformed called from Input System
[CombatController] Starting new combo from Attack1
[CombatController] Queuing next combo input (current=0)
[CombatController] Hit detected! 60 damage to enemy
```

---

## 📚 References

- **InputSystem Docs**: https://docs.unity3d.com/Packages/com.unity.inputsystem
- **Animator Workflow**: https://docs.unity3d.com/Manual/AnimatorOverview.html
- **Combat Design**: RPG standard 4-hit combo with input buffering

---

## ✅ Final Status

| Component | Status | Notes |
|-----------|--------|-------|
| Input System | ✅ | Attack action configured |
| Combo Logic | ✅ | 4-hit, 4s timeout |
| Damage | ✅ | Equal 60 per hit |
| Animation | ✅ | Combat layer + 4 states |
| Enemy Damage | ✅ | EnemyDummy prefab |
| UI | ✅ | DamagePopup + HitEffect |

**Ready to play! 🎮**

---

**Last Updated**: 2026-06-04  
**System Version**: Combat v2.0 (Input System + Equal Damage + Timeout Combo)

