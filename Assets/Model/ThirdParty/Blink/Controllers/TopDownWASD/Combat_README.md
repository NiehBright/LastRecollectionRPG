# Hướng dẫn thiết lập hệ thống combat (TopDownWASD)

Tệp này mô tả cách thiết lập bản demo combat đơn giản tôi đã thêm vào dự án. Nó tạo một hệ thống combo 4 đòn, một enemy prefab (cube) có thanh máu trên đầu, và thêm animation tấn công vào Animator Controller `TopDownWASDAnimator` (nếu có). Nếu không tìm thấy controller đó, công cụ sẽ tạo một controller mới trong thư mục Prefabs.

Các file đã thêm
- Scripts:
  - `Assets/Blink/Controllers/TopDownWASD/Scripts/Combat/CombatController.cs` — logic combat của nhân vật
  - `Assets/Blink/Controllers/TopDownWASD/Scripts/Combat/EnemyDummy.cs` — quái thử nghiệm, kèm thanh máu
  - `Assets/Blink/Controllers/TopDownWASD/Scripts/Combat/DamagePopup.cs` — popup sát thương TextMeshPro + pooling
  - `Assets/Blink/Controllers/TopDownWASD/Scripts/Combat/HitEffectAutoDestroy.cs` — hiệu ứng hit mẫu
- Công cụ Editor:
  - `Assets/Blink/Controllers/TopDownWASD/Editor/CombatSetupUtility.cs` — tạo prefab và thêm animation vào Animator Controller có sẵn / tạo mới nếu cần

Prefabs / assets sẽ được tạo (nếu bạn chạy công cụ)
- `Assets/Blink/Controllers/TopDownWASD/Prefabs/EnemyDummy.prefab` — cube với component `EnemyDummy`, tag `Enemy`
- `Assets/Blink/Controllers/TopDownWASD/Prefabs/DamagePopup.prefab` — popup sát thương để bạn chỉnh UI dễ dàng
- `Assets/Blink/Controllers/TopDownWASD/Prefabs/HitEffect.prefab` — hiệu ứng hit mẫu
- Nếu dự án có sẵn `Assets/Blink/Controllers/TopDownWASD/Character/TopDownWASDAnimator.controller`, công cụ sẽ thêm 4 animation (Attack1..Attack4) và 4 trigger tương ứng vào controller đó. Nếu không, công cụ sẽ tạo `TopDownWASDAnimator.controller` trong thư mục Prefabs.

Cách tạo prefab và thêm animation (một lần)
1. Mở Unity Editor và chờ compile xong.
2. (Khuyên dùng) Chọn GameObject player trong Hierarchy trước khi chạy tool.
3. Trên thanh menu, chọn: `BLINK -> Combat -> Setup Combat Assets`.
   - Công cụ sẽ tạo `EnemyDummy.prefab`, `DamagePopup.prefab`, `HitEffect.prefab`.
   - Công cụ sẽ thêm 4 animation (Attack1..Attack4), 4 trigger và Animation Events `OnAttackHit(int)` vào `TopDownWASDAnimator.controller` nếu controller đó tồn tại; nếu không sẽ tạo controller mới.
   - Nếu bạn đã chọn player trước đó, tool sẽ tự gán controller + popup prefab + hit prefab vào component phù hợp trên player.

Cách cấu hình scene để test
1. Đặt GameObject nhân vật của bạn (object đang dùng `TopDownWASDController`) vào scene.
2. Thêm component `CombatController` vào nhân vật (từ thư mục Scripts/Combat).
   - Kéo component `Animator` của nhân vật (không phải asset controller) vào trường `animator` của `CombatController`.
   - Gán `damagePopupPrefab` = `Assets/Blink/Controllers/TopDownWASD/Prefabs/DamagePopup.prefab`.
   - Gán `hitEffectPrefab` = `Assets/Blink/Controllers/TopDownWASD/Prefabs/HitEffect.prefab`.
   - Nếu bạn muốn sử dụng controller tự động, đảm bảo Animator component của nhân vật có `Controller` được đặt là `TopDownWASDAnimator.controller` (nếu công cụ đã thêm animation vào controller này).
3. Kéo `EnemyDummy.prefab` từ `Assets/Blink/Controllers/TopDownWASD/Prefabs` vào scene ở vị trí bạn muốn thử.
4. Nhấn Play và click trái để tấn công. Nếu quái nằm trong phạm vi, nó sẽ mất máu, hiện popup sát thương và thanh máu sẽ giảm.

Ghi chú
- Animator Controller mà công cụ thêm animation chỉ chứa các animation mẫu (chuyển động nhỏ). Để cảm giác combat tốt hơn, thay bằng các clip tấn công thực tế và giữ tên trigger `Attack1`..`Attack4`.
- `CombatController` đã hỗ trợ Animation Events `OnAttackHit(int index)` (event-driven) và có fallback theo `hitTimes[]` nếu event bị thiếu.
- Popup sát thương đã dùng prefab + TextMeshPro + pooling.
- Physics hiện dùng `Physics.OverlapSphere`. Để tối ưu, có thể chuyển sang `OverlapSphereNonAlloc` và tái sử dụng mảng buffer.

Tôi có thể tiếp tục giúp nếu bạn muốn:
- Chuyển popup sang TextMeshPro và thêm pooling.
- Thay `hitTimes` bằng Animation Events (tôi sẽ thêm hàm sự kiện và sửa `CombatController`).
- Thêm chỉ số nhân vật (STR, crit chance) và công thức tính sát thương nâng cao.

Hãy nói tôi biết bạn muốn làm theo phương án nào tiếp theo (ví dụ: dùng Animation Events — tôi sẽ cập nhật code để hỗ trợ). 


