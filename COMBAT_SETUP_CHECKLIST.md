# ✅ Combat System Setup Checklist

## 📋 Bước chuẩn bị (Preparation)

- [ ] Có player GameObject trong scene
- [ ] Player có **Animator** component
- [ ] Player có **CharacterController** component  
- [ ] Có **InputSystem_Actions.inputactions** file trong Assets root

---

## 🎮 Bước 1: Auto-Setup Components (5 giây)

**Menu: `BLINK → Combat → Quick Setup - Enable Combat System`**

Cái này sẽ tự động:
- [ ] Thêm **CombatController** component
- [ ] Thêm **PlayerInput** component + gán InputSystem_Actions
- [ ] Tạo **GameStartup** object (nếu chưa có)
- [ ] Tạo **InputManager** object (nếu chưa có)

---

## 🎬 Bước 2: Setup Animator (1 phút)

**Menu: `BLINK → Combat → Setup Combat Assets`**

Cái này sẽ tự động:
- [ ] Tạo **Combat** layer trên Animator
- [ ] Tạo 4 states: **Attack1, Attack2, Attack3, Attack4**
- [ ] Setup transitions: Idle → Attack1 → ... → Attack4 → Idle
- [ ] Tạo prefabs: **EnemyDummy, DamagePopup, HitEffect**

---

## 👹 Bước 3: Place Enemy (1 phút)

- [ ] Drag **Prefabs/EnemyDummy.prefab** từ Project vào Scene
- [ ] Đặt vị trí cách player ~2-3 units về phía trước
- [ ] Kiểm tra tag = **"Enemy"** ✓

---

## ⚙️ Bước 4: Gán Assets cho CombatController (30 giây)

Chọn **Player** GameObject → CombatController component:
- [ ] **Animator**: Tự động (hoặc gán Player's Animator)
- [ ] **damagePopupPrefab**: Kéo `Prefabs/DamagePopup` vào
- [ ] **hitEffectPrefab**: Kéo `Prefabs/HitEffect` vào
- [ ] **baseDamage**: Để mặc định 60 hoặc tuỳ chỉnh

---

## 🧪 Bước 5: Test Game (2 phút)

1. **Nhấn Play**
2. **Mở Console** (Ctrl+Shift+C)
3. **Ấn chuột trái** 4 lần nhanh chóng
   - Nhìn console → "[CombatController] OnAttackPerformed called"?
   - Nhìn player → chạy animation Attack1 → 2 → 3 → 4?
   - Nhìn enemy → nó bị đỏ (damage)?

---

## 🔍 Debug: Nếu không hoạt động

### ❌ Console không show "[CombatController] OnAttackPerformed"
**Kiểm tra:**
1. Có **PlayerInput** component trên player?
   - Menu: `BLINK → Combat → Quick Setup` sẽ thêm
2. PlayerInput có **InputSystem_Actions** được gán?
   - Check Inspector → Player component → Actions field
3. Input action map được enable?
   - Check Console → "[InputManager] Player action map enabled"?

### ❌ Animation không chạy
**Kiểm tra:**
1. Menu: `BLINK → Combat → Setup Combat Assets` đã chạy?
   - Nếu không → chạy ngay
2. Animator có **Combat** layer?
   - Inspector → Animator → Layers tab
3. Combat layer có **Attack1, Attack2, ...** states?
   - Inspector → Animator → Combat layer → states

### ❌ Enemy không bị damage
**Kiểm tra:**
1. Enemy tag = **"Enemy"**?
   - Inspector → Tags dropdown
2. Enemy có **EnemyDummy** component?
   - Được prefab tạo tự động
3. Animation clip có **OnAttackHit** event?
   - Setup Combat Assets sẽ tự động thêm

---

## 🎯 Tuỳ chỉnh (Optional)

### Thay đổi animation clips
1. Có animation file (.anim hay .fbx)
2. Drag vào **Combat layer** → **Attack1 state** trong Animator
3. Repeat cho Attack2, 3, 4

### Tuỳ sát thương
- CombatController → `baseDamage` = (số)

### Tuỳ combo timeout
- CombatController → `comboInputTimeout` = 4 (giây)
- Mặc định 4s giữa các lần bấm

### Tuỳ hit time
- CombatController → `hitNormalizedTimes` array
- Mặc định [0.35, 0.40, 0.45, 0.50]
- = Attack1 tại 35%, Attack2 tại 40%, ...

---

## ✨ Status

- **Input System**: ✅ Hoạt động với Attack button
- **Combo**: ✅ 4-hit combo trong 4 giây
- **Damage**: ✅ Mỗi hit gây 60 damage (tuỳ chỉnh)
- **Animation**: ✅ Blend giữa các attack
- **Movement**: ✅ Nhân vật di chuyển khi tấn công

---

**Tham khảo thêm**: `COMBAT_SETUP_GUIDE.md`

