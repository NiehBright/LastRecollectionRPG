# Quick Start - Hướng Dẫn Nhanh

## Vấn Đề Đã Sửa ✅
- ✅ Character không di chuyển khi tấn công → **ĐÃ SỬA**
- ✅ Combat layer override Locomotion → **ĐÃ SỬA**
- ✅ Animation không blend mượt → **ĐÃ SỬA**

## Nguyên Nhân Vấn Đề Ban Đầu
1. CombatController sử dụng PlayableGraph override toàn bộ Animator
2. Combat layer được set weight cao làm Locomotion không hoạt động
3. Không có layer weight control tự động

## Giải Pháp Tôi Đã Thực Hiện
1. **Sửa CombatSetupUtility.cs**:
   - Combat layer được set `defaultWeight = 0` lúc tạo
   
2. **Sửa CombatController.cs**:
   - Bỏ PlayableGraph override
   - Thêm layer weight control (0 → 1 → 0)
   - Animation sẽ trigger qua Animator bình thường

3. **Cập nhật Guide**:
   - Chi tiết cách thay đổi animation clips
   - Troubleshooting

## Cách Sử Dụng Ngay

### 1️⃣ Setup lần đầu (nếu chưa làm)
```
Menu: BLINK → Combat → Setup Combat Assets
```

### 2️⃣ Thay đổi animation clips
- Mở **Animator Window**: `Window → Animation → Animator`
- Chọn layer **Combat**
- Click vào state `Attack1`
- Kéo animation clip vào field **Motion** trong Inspector
- Lặp lại cho Attack2, 3, 4

### 3️⃣ Test
- Nhấn **Play**
- Bấm chuột trái để tấn công
- Character sẽ:
  - Tấn công (Combat layer)
  - Quay lại bình thường (Locomotion layer)
  - Có thể di chuyển bình thường khi tấn công ✅

## Hệ Thống Layer Mới

```
┌─────────────────────────────────────────┐
│  TopDownWASDAnimator.controller         │
├─────────────────────────────────────────┤
│  Layer 0: Locomotion (Base Layer)       │
│  - Weight: 1 (luôn bật)                 │
│  - States: Idle, Walk, Run              │
│  - Điều khiển di chuyển                 │
│                                         │
│  Layer 1: Combat                        │
│  - Weight: 0 (bình thường)              │
│  - Weight: 1 (khi tấn công)             │
│  - States: Idle, Attack1-4              │
│  - Điều khiển tấn công                  │
└─────────────────────────────────────────┘
```

## Timeline Tấn Công (4-Hit Combo)

```
T=0s      T=0.35s      T=0.7s       T=1.05s      T=1.4s
Attack1   Attack2      Attack3      Attack4      Back to Locomotion
  │         │            │            │            │
  ├─────────┼────────────┼────────────┤            │
  │                                                 │
  └─────────────────────────────────────────────────┘
  Combat Layer weight = 1              weight = 0
```

## Files Cần Biết
- `TopDownWASDAnimator.controller` - Animator chính
- `CombatController.cs` - Quản lý Combat layer
- `TopDownWASDController.cs` - Quản lý Movement
- `ANIMATION_CLIP_GUIDE.md` - Guide chi tiết

## Next Steps
1. Test lại hệ thống
2. Gán animation clips đúng
3. Điều chỉnh `comboInputTimeout` nếu cần

---
**Nếu vẫn có vấn đề**, kiểm tra:
- [ ] Animator có Combat layer?
- [ ] Combat layer `defaultWeight = 0`?
- [ ] Locomotion layer `weight = 1` lúc không tấn công?
- [ ] Animation clips được gán chưa?

