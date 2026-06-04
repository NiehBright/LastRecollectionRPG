# 🎮 Combat System Setup Guide

## Prerequisites
- Unity 2021.3+ với Input System package
- Animator với Combat layer đã được setup

## ⚙️ Quick Setup Steps

### 1️⃣ **Create PlayerInput Component**
- Chọn player GameObject
- Add component: `PlayerInput`
- Gán InputSystem_Actions.inputactions vào `Actions` field
- Chọn `Behavior: Send Messages` (hoặc `Invoke Unity Events`)

### 2️⃣ **Add CombatController**
- Chọn player GameObject (cùng object có Animator)
- Add component: `CombatController`
- **Auto-setup** sẽ xảy ra ở Awake()

### 3️⃣ **Setup Combat Animator**
**Menu: `BLINK → Combat → Setup Combat Assets`**
- Tự động tạo Combat layer
- Tạo Attack1..4 states
- Tạo EnemyDummy prefab
- Tạo DamagePopup & HitEffect prefabs

### 4️⃣ **Place Enemy Dummy**
- Drag `Prefabs/EnemyDummy.prefab` vào scene
- Đặt cách player ~2-3 units
- Tag: `Enemy` ✓

### 5️⃣ **Test Game**
- Play scene
- Ấn chuột trái (hoặc Gamepad Y button)
- Nhìn console logs để debug

---

## 🔧 Key Components

### CombatController
- **attackAction**: InputActionReference → auto-find từ PlayerInput
- **baseDamage**: Sát thương của mỗi đòn (60 mặc định)
- **comboInputTimeout**: Thời gian tối đa giữa lần bấm (4s)

### Input System Actions
- **Player ActionMap**: Phải được enable
- **Attack Action**: Button type, bind `Mouse Left Button`, `Gamepad Y`, `Touch Tap`

---

## 🐛 Troubleshooting

### ❌ "Attack action not found!"
✅ Giải pháp:
1. Đảm bảo PlayerInput component tồn tại
2. Đảm bảo InputSystem_Actions.inputactions được assign
3. Đảm bảo "Player" ActionMap tồn tại và có "Attack" action

### ❌ Nhấn chuột không gì xảy ra
✅ Debug checks:
1. Xem Console → "[CombatController] OnAttackPerformed called" ?
2. Xem Console → "[CombatController] Starting new combo from Attack1" ?
3. Nếu không, PlayerInput không nhận input → kiểm tra Input System

### ❌ Animation không chạy
✅ Checks:
1. Animator có Combat layer? (BLINK → Combat → Setup Combat Assets)
2. Attack1..4 states tồn tại?
3. Transitions Idle → Attack1 → ... → Idle được setup?

### ❌ Enemy không bị damage
✅ Checks:
1. Enemy tag = "Enemy"?
2. EnemyDummy component tồn tại?
3. Hit range/radius hợp lý? (Inspector gizmo)

---

## 📝 Combat Logic

**Combo Timeline (4 giây tối đa):**
```
Click 1   → Attack1 animation start
Click 2 (trong animation)   → Attack1 layer transition → Attack2
Click 3 (trong animation)   → Attack2 transition → Attack3
Click 4 (trong animation)   → Attack3 transition → Attack4
(không click)              → Attack4 END → back to Idle
(click sau 4s)             → Reset → Attack1 lại
```

**Damage:**
- Tất cả 4 đòn gây **60 damage** (mặc định)
- Được trigger ở ~35-50% vào animation

---

## 🎯 Next Steps (Tuỳ chỉnh)

1. **Thay animation clips**: Kéo animation mới vào Attack1..4 state
2. **Tuỳ hit time**: CombatController → `hitNormalizedTimes`
3. **Tuỳ sát thương**: `baseDamage` field
4. **Tuỳ combo timeout**: `comboInputTimeout` (giây)
5. **Tuỳ movement lunge**: `attackMoveDistances` array

---

**Status**: ✅ Combat system ready! Run game & test.

