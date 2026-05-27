# Combat Setup Guide - Hướng Dẫn Thiết Lập Combat

## Vấn Đề & Giải Pháp

### ⚠️ Vấn Đề: Nhân vật không di chuyển khi tấn công
- **Nguyên nhân**: Combat layer được set weight cao hơn Locomotion layer, làm cho movement không hoạt động
- **Giải pháp**: Combat layer hiện được set weight=0 lúc đầu, chỉ được kích hoạt khi tấn công, rồi tự động vô hiệu hóa sau tấn công

### ✅ Cách Hoạt Động (Giải Pháp Mới)
1. **Locomotion Layer** (Layer 1) - Luôn hoạt động với weight=1, điều khiển di chuyển
2. **Combat Layer** (Layer 2) - Khởi động với weight=0
3. Khi bạn **ấn tấn công** (Left Click):
   - Combat Layer weight → 1 (animation tấn công chạy)
   - Locomotion Layer vẫn hoạt động phía dưới nhưng không override
4. Khi tấn công **kết thúc**:
   - Combat Layer weight → 0
   - Quay lại Locomotion Layer bình thường

## Cách Sử Dụng

### Bước 1: Thiết Lập Animation Combat
1. Mở Unity Editor
2. Vào menu: **BLINK → Combat → Setup Combat Assets**
3. Script sẽ tạo:
   - **Combat Layer** trong TopDownWASDAnimator.controller (weight=0 mặc định)
   - **4 Attack States** (Attack1, Attack2, Attack3, Attack4)
   - **Animation Clips** mặc định (Attack1.anim, Attack2.anim, Attack3.anim, Attack4.anim)
   - **Animator Parameters**: Attack1, Attack2, Attack3, Attack4 (Trigger type)
   - **Enemy Dummy Prefab** để test
   - **DamagePopup Prefab** và **HitEffect Prefab**

### Bước 2: Thay Đổi Animation Clips
Bạn có hai cách:

#### Cách 1: Trực tiếp trong Animator Window (Dễ nhất)
1. Chọn Player hoặc Character có Animator
2. Mở **Animator Window** (Window → Animation → Animator)
3. Chuyển sang layer **Combat**
4. Click vào state **Attack1, Attack2, Attack3, Attack4**
5. Trong Inspector, kéo animation clip của bạn vào field **Motion**

#### Cách 2: Qua Prefabs Folder
1. Đi tới: `Assets/Blink/Controllers/TopDownWASD/Prefabs/`
2. Tìm các file: `Attack1.anim`, `Attack2.anim`, `Attack3.anim`, `Attack4.anim`
3. Mở từng file trong Animation window
4. Chỉnh sửa animation theo ý muốn

### Bước 3: Cấu Trúc Combat Layer

```
Combat Layer (weight controlled by CombatController)
├── Idle (Initial State)
├── Attack1 ──→ Attack2 (nếu ấn Attack2 trong thời gian cho phép)
│    ↓
│   Idle (nếu không ấn tiếp)
├── Attack2 ──→ Attack3 (nếu ấn Attack3)
│    ↓
│   Idle
├── Attack3 ──→ Attack4 (nếu ấn Attack4)
│    ↓
│   Idle
└── Attack4
     ↓
    Idle
```

### Layer Weight System
- **Locomotion Layer**: weight = 1 (luôn hoạt động)
- **Combat Layer**: 
  - weight = 0 (bình thường)
  - weight = 1 (khi đang tấn công)
  - Tự động chuyển đổi qua CombatController

### Animation Parameters
- **Attack1**: Trigger để kích hoạt đòn tấn công thứ 1
- **Attack2**: Trigger để kích hoạt đòn tấn công thứ 2
- **Attack3**: Trigger để kích hoạt đòn tấn công thứ 3
- **Attack4**: Trigger để kích hoạt đòn tấn công thứ 4

### Cách Gọi Attack từ Script
Script `CombatController.cs` tự động xử lý:
- Bấm chuột trái để tấn công
- Combo tự động chạy 4 đòn
- Layer weight tự động được điều khiển

### Animation Events
Mỗi Attack animation có sẵn **"OnAttackHit"** event được kích hoạt ở giữa animation.
Bạn có thể custom thời gian event bằng cách:
1. Mở Attack animation clip
2. Click chuột phải tại thanh timeline
3. Chọn **Add Event**
4. Gọi function: `OnAttackHit` (đã được setup sẵn)

### Troubleshooting

**Problem**: Attack animations không xuất hiện trong Animator
- **Solution**: Chạy "Setup Combat Assets" lại từ menu BLINK

**Problem**: Combat Layer không tìm thấy
- **Solution**: Kiểm tra file: `Assets/Blink/Controllers/TopDownWASD/Character/TopDownWASDAnimator.controller` có tồn tại không

**Problem**: Animation clips không animate
- **Solution**: 
  1. Mở animation clip
  2. Thiết lập keyframes cho transform hoặc properties
  3. Lưu animation clip

**Problem**: Nhân vật vẫn không di chuyển khi tấn công
- **Solution**:
  1. Kiểm tra Locomotion layer vẫn còn weight=1
  2. Kiểm tra `CombatController` đã gán Animator chưa
  3. Xem log console có lỗi gì không

**Problem**: Combo không chain
- **Solution**: Kiểm tra `CombatController.comboInputTimeout` > 0

## File Quan Trọng
- `TopDownWASDAnimator.controller` - Animator Controller với Locomotion + Combat layers
- `Attack1.anim`, `Attack2.anim`, `Attack3.anim`, `Attack4.anim` - Animation clips
- `CombatSetupUtility.cs` - Script setup utility (Editor-only)
- `CombatController.cs` - Combat logic (manages layer weights tự động)
- `TopDownWASDController.cs` - Movement controller (Locomotion layer)

