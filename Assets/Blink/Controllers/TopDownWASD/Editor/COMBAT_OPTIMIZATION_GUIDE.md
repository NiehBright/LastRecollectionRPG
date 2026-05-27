# Combat Optimization Guide - Hướng Dẫn Tối Ưu

## ✅ Những Gì Đã Được Cải Thiện

### 1. **Movement During Attack** (Di chuyển khi tấn công)
- Character giờ sẽ di chuyển **forward một chút** khi tấn công
- Di chuyển mượt từ ví trí hiện tại đến vị trí mục tiêu

### 2. **Improved Combo System** (Cải thiện Combo)
- **Input Queue**: Lưu input được nhấn khi đang tấn công
- **Không bỏ qua đòn**: 1→2→3→4 luôn tuần tự (không nhảy cóc)
- **Khắc phục**: Bug 1→2→4 hoặc 1→3→4

---

## 🎮 Cách Sử Dụng Settings

### **Bước 1: Chọn Character**
- Chọn Player/Character trong Scene có **CombatController**

### **Bước 2: Inspector - Combat Settings**

#### **Attack Movement Settings** (Mới)
```
┌─ Movement During Attack
│  ├─ Attack Move Distance: 0.3
│  │   (Khoảng cách di chuyển forward - 0 = không di chuyển)
│  │   (0.3 = di chuyển 0.3m về phía trước)
│  │   (Bạn có thể sửa sang 0.5, 0.1, v.v.)
│  │
│  └─ Attack Move Durations: [0.35, 0.35, 0.35, 0.35]
│      (Thời gian di chuyển cho mỗi attack)
│      (0.35 = 0.35 giây)
└─
```

### **Điều Chỉnh Movement**

#### ❌ Nếu character di chuyển **quá xa**:
- Giảm `Attack Move Distance`: 0.3 → 0.15

#### ❌ Nếu character di chuyển **quá gần**:
- Tăng `Attack Move Distance`: 0.3 → 0.5

#### ❌ Nếu di chuyển **quá nhanh/quá chậm**:
- Sửa `Attack Move Durations`:
  - Nhanh hơn: 0.35 → 0.25
  - Chậm hơn: 0.35 → 0.45

### **Combo Settings** (Vẫn còn)
```
┌─ Attack Settings
│  ├─ Combo Input Timeout: 0.8
│  │   (Thời gian cho phép input đòn tiếp theo)
│  │   (0.8 giây = 800ms)
│  │   (Nếu muốn combo dễ hơn: 0.8 → 1.0)
│  │   (Nếu muốn combo khó hơn: 0.8 → 0.6)
│  │
│  └─ Hit Times: [0.18, 0.22, 0.26, 0.32]
│      (Thời gian hit được apply cho mỗi attack)
└─
```

---

## 📊 Recommended Settings

### **Balanced (Cân Bằng)**
```
Attack Move Distance: 0.3
Attack Move Durations: [0.35, 0.35, 0.35, 0.35]
Combo Input Timeout: 0.8
```

### **Aggressive (Tấn Công Mạnh)**
```
Attack Move Distance: 0.5
Attack Move Durations: [0.30, 0.30, 0.30, 0.30]
Combo Input Timeout: 0.9
```

### **Conservative (Bảo Thủ)**
```
Attack Move Distance: 0.1
Attack Move Durations: [0.40, 0.40, 0.40, 0.40]
Combo Input Timeout: 0.7
```

---

## 🧪 Test & Verify

### **Combo Test**
1. Nhấn **Play**
2. Bấm chuột trái **liên tục nhanh**
3. Kiểm tra:
   - ✅ Combo chạy: 1→2→3→4 (không nhảy cóc)
   - ✅ Character di chuyển forward mỗi lần tấn công
   - ✅ Không bỏ qua đòn nào

### **Movement Test**
1. Tấn công 1 đòn
2. Kiểm tra:
   - ✅ Character di chuyển ~0.3m về phía trước
   - ✅ Di chuyển mượt (không giật)
   - ✅ Di chuyển dừng lại sau animation

### **Timing Test**
1. Ấn combo với các tốc độ khác nhau:
   - Ấn nhanh (spam)
   - Ấn chậm
   - Ấn không đều
2. Kiểm tra combo luôn tuần tự: 1→2→3→4

---

## 🔧 Technical Details

### **Cải Thiện Combo System**
```csharp
// TRƯỚC (Có bug - bỏ qua đòn)
_comboIndex = Mathf.Clamp(_comboIndex + 1, 0, 3);

// SAU (Lưu input - không bỏ qua)
_queuedComboIndex = Mathf.Clamp(_comboIndex + 1, 0, 3);
```

### **Movement During Attack**
```csharp
// Di chuyển từ vị trí hiện tại đến target position
Vector3 startPosition = transform.position;
Vector3 targetPosition = startPosition + transform.forward * moveDistance;

// Sử dụng CharacterController để di chuyển
CharacterController cc = GetComponent<CharacterController>();
cc.Move(moveDir * moveDistance * Time.deltaTime / moveDuration);
```

---

## ⚠️ Troubleshooting

### **Q: Combo vẫn bỏ qua đòn?**
A: 
- Kiểm tra `comboInputTimeout` ≥ 0.7
- Kiểm tra Attack Move Duration không quá dài
- Thử tăng `comboInputTimeout` lên 1.0

### **Q: Character không di chuyển?**
A:
- Kiểm tra `Attack Move Distance` > 0
- Kiểm tra Character có **CharacterController** component
- Kiểm tra TopDownWASDController không disable movement

### **Q: Di chuyển quá far?**
A:
- Giảm `Attack Move Distance`: 0.3 → 0.15

### **Q: Combo quá khó/dễ?**
A:
- Khó: Tăng `comboInputTimeout` (0.8 → 1.0)
- Dễ: Giảm `comboInputTimeout` (0.8 → 0.6)

---

## 📝 Default Values

| Setting | Default | Min | Max | Recommendation |
|---------|---------|-----|-----|-----------------|
| Attack Move Distance | 0.3 | 0.0 | 1.0 | 0.2-0.5 |
| Attack Move Duration | 0.35 | 0.1 | 1.0 | 0.3-0.5 |
| Combo Input Timeout | 0.8 | 0.5 | 1.5 | 0.7-0.9 |

---

## 🚀 Next Steps

1. **Test combo**: Bấm nhanh xem có bỏ qua đòn không
2. **Adjust movement**: Tuỳ chỉnh `Attack Move Distance` cho phù hợp
3. **Fine-tune timing**: Điều chỉnh `comboInputTimeout` nếu cần
4. **Polish**: Thêm visual effects, sounds, v.v.

---

**Giờ combo system đã được tối ưu và character có movement khi tấn công!** 🎉

