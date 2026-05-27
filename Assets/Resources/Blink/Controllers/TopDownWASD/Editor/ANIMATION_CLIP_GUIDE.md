# Thay Đổi Combat Animation Clips - Hướng Dẫn Chi Tiết

## Tổng Quan
Hệ thống Combat hiện tại sử dụng **Animator Layers** để blending giữa Locomotion (di chuyển) và Combat (tấn công).

**Quan trọng**: Combat Layer sẽ tự động được kích hoạt (weight=1) khi bạn tấn công, và vô hiệu hóa (weight=0) khi tấn công kết thúc.

## Cách 1: Thay Đổi Animation Qua Animator Window (Dễ Nhất)

### Bước 1: Chuẩn Bị
1. Chọn **Player hoặc Character** trong Scene có component **TopDownWASDController**
2. Mở **Animator Window**: `Window → Animation → Animator`

### Bước 2: Điều Hướng đến Combat Layer
1. Trong Animator Window, tìm **Layers panel** (thường ở góc trái dưới)
2. Bạn sẽ thấy 2 layers:
   - **Base Layer** (Locomotion)
   - **Combat** (Attack layer)
3. Click vào layer **Combat**

### Bước 3: Thay Đổi Animation Clips
1. Khi ở Combat layer, bạn sẽ thấy các states:
   - `Idle`
   - `Attack1`
   - `Attack2`
   - `Attack3`
   - `Attack4`

2. Click vào state `Attack1` (hoặc Attack2, 3, 4)

3. Trong **Inspector panel** bên phải, tìm field **Motion**

4. Kéo animation clip của bạn vào field **Motion**
   - Hoặc click vào hình tròn selector → tìm animation clip bạn muốn

### Bước 4: Lặp Lại cho các Attack khác
- Lặp lại bước 3 cho Attack2, Attack3, Attack4

## Cách 2: Thay Đổi qua Prefabs Folder

### Nếu bạn chỉ muốn chỉnh sửa animation keyframes:

1. Đi tới: `Assets/Blink/Controllers/TopDownWASD/Prefabs/`

2. Tìm và double-click vào file:
   - `Attack1.anim`
   - `Attack2.anim`
   - `Attack3.anim`
   - `Attack4.anim`

3. Animation window sẽ mở, giờ bạn có thể:
   - Thêm keyframes
   - Chỉnh sửa animation timeline
   - Thêm animation events

4. Nhấn **Ctrl+S** (hoặc File → Save) để lưu

## Cách 3: Gán Animation Clips Từ Đối Tượng Khác

Nếu bạn đã có animation clips từ model khác (ví dụ từ Suriyun folder):

1. Vào **Animator Window**
2. Chọn Combat layer
3. Click vào state `Attack1`
4. Trong Inspector, tìm field **Motion**
5. Kéo animation clip từ **Suriyun** folder vào field Motion
6. Lặp lại cho Attack2, Attack3, Attack4

## Kiểm Tra Animation Đã Chạy Đúng

### Test trong Play Mode:
1. Nhấn **Play**
2. Bấm chuột trái để tấn công
3. Nhân vật sẽ:
   - Thực hiện animation Attack1
   - Sau đó quay về Locomotion layer để di chuyển

### Nếu animation không chạy:
- Kiểm tra **Combat layer weight** có bằng 0 lúc không tấn công không
- Kiểm tra **Locomotion layer weight** vẫn = 1
- Xem log console có error không

## Layer Weight Logic (Tự động)

### CombatController sẽ tự động:
```
Khi bấm tấn công:
  - Tìm Combat layer
  - Set weight = 1
  - Trigger Attack1/2/3/4
  
Khi tấn công kết thúc:
  - Set Combat layer weight = 0
  - Character quay lại Locomotion layer
```

## Animation Events (Tùy Chọn)

Mỗi Attack animation đã có sẵn **OnAttackHit** event.

### Để thay đổi thời gian hit:
1. Mở animation clip (`Attack1.anim`, v.v.)
2. Trong Animation window, tìm **Events** (dòng đỏ phía dưới timeline)
3. Click vào event red marker
4. Trong Inspector, có thể điều chỉnh thời gian event

## Troubleshooting

### Q: Animation không xuất hiện trên Combat layer
A: 
- Chạy lại `BLINK → Combat → Setup Combat Assets`
- Đảm bảo TopDownWASDAnimator.controller được load đúng

### Q: Nhân vật không thể di chuyển khi tấn công
A: 
- Kiểm tra **Locomotion layer** vẫn còn `weight = 1`
- Kiểm tra **TopDownWASDController** có `movementEnabled = true`

### Q: Combat animation chạy nhưng không chạy Locomotion
A: 
- Đảm bảo Combat layer `defaultWeight = 0` trong Animator
- Xem CombatController có set weight = 0 sau khi tấn công không

### Q: Combo không chain
A: 
- Kiểm tra `CombatController.comboInputTimeout` (nên > 0.6)
- Đảm bảo animation duration > 0.35 giây

## Tips & Best Practices

1. **Animation Duration**: Nên giữ Attack animation từ 0.35 đến 0.6 giây để combo hoạt động smooth

2. **Hit Event**: Đặt OnAttackHit event ở khoảng **50-60%** của animation

3. **Root Motion**: Nếu muốn nhân vật di chuyển khi tấn công, hãy:
   - Bỏ check **Apply Root Motion** ở Animator
   - Hoặc để TopDownWASDController xử lý movement

4. **Layer Blending**: Combat layer sẽ blend mượt với Locomotion layer nhờ additive blending

## Files Liên Quan
- `TopDownWASDAnimator.controller` - Main animator
- `CombatController.cs` - Quản lý Combat layer weight
- `TopDownWASDController.cs` - Quản lý Locomotion layer

