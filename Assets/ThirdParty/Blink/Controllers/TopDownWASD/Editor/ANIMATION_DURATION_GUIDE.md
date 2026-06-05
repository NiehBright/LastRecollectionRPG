# Animation Duration Fix - Hướng Dẫn Sửa Animation

## ✅ Vấn Đề Đã Sửa

### ❌ Trước (Animation đè nhau):
```
Đòn 1 bắt đầu
Đòn 1 chưa xong → Đòn 2 kích hoạt ngay
Result: Animation nhảy tùm lum, không mượt
```

### ✅ Sau (Animation xong mới kích hoạt đòn tiếp):
```
Đòn 1 bắt đầu
Đòn 1 chạy hết animation (0.6s)
Đòn 1 XONG hoàn toàn → Đòn 2 kích hoạt
Result: Animation mượt, không đè nhau
```

---

## 🎮 Cách Sửa Ngay

### **Bước 1: Đo Animation Duration**

Bạn cần biết **độ dài** của mỗi attack animation:

1. **Mở Animation Window**: `Window → Animation → Animation`
2. Chọn animation clip (ví dụ: `Attack1.anim`)
3. **Nhìn timeline** → xem độ dài (ở góc phải)

```
Ví dụ:
Attack1.anim: 0 --- 0.6 giây (tổng cộng 0.6s)
Attack2.anim: 0 --- 0.65 giây (tổng cộng 0.65s)
Attack3.anim: 0 --- 0.7 giây (tổng cộng 0.7s)
Attack4.anim: 0 --- 0.75 giây (tổng cộng 0.75s)
```

### **Bước 2: Ghi Lại Độ Dài**

| Animation | Duration | Ghhi chú |
|-----------|----------|----------|
| Attack1 | 0.6 | Đo từ timeline |
| Attack2 | 0.65 | Đo từ timeline |
| Attack3 | 0.7 | Đo từ timeline |
| Attack4 | 0.75 | Đo từ timeline |

### **Bước 3: Cập Nhật Settings**

1. **Chọn Character** trong Scene
2. **Inspector** → tìm **CombatController**
3. Tìm section: **"Attack Settings"**
4. Tìm: **"Animation Durations"** (array)

```
┌─ Attack Settings
│  ├─ Animation Durations
│  │  ├─ Element 0: 0.6  (Attack1)
│  │  ├─ Element 1: 0.65 (Attack2)
│  │  ├─ Element 2: 0.7  (Attack3)
│  │  └─ Element 3: 0.75 (Attack4)
```

5. **Sửa giá trị** theo animation duration bạn đo được

### **Bước 4: Test**

1. **Nhấn Play**
2. **Bấm chuột trái liên tục**
3. **Kiểm tra**:
   - ✅ Animation smooth, không nhảy
   - ✅ Combo: 1→2→3→4 tuần tự
   - ✅ Đòn tiếp theo kích hoạt khi đòn trước XONG

---

## 📏 Cách Đo Animation Duration

### **Cách 1: Xem Trong Animation Window** (Dễ nhất)

1. Mở: `Window → Animation → Animation`
2. Double-click animation clip
3. Timeline sẽ show:

```
┌─────────────────────────────┐
│ Attack1.anim                │
│ 0s ─────── 0.6s ─────────→  │
│           ▲                 │
│        Khung cuối           │
│     (Duration = 0.6s)       │
└─────────────────────────────┘
```

### **Cách 2: Kiểm Tra Animation Clip Properties**

1. Chọn animation clip (ví dụ: `Attack1.anim`)
2. **Inspector** → thấy:
   - `Clip Length: 0.6` ← Đây là duration

---

## 💡 Ví Dụ Cài Đặt

### **Ví Dụ 1: Tất cả animation dài 0.6 giây**
```
Animation Durations: [0.6, 0.6, 0.6, 0.6]
```

### **Ví Dụ 2: Animation dài hơn**
```
Animation Durations: [0.5, 0.6, 0.7, 0.8]
```

### **Ví Dụ 3: Animation khác nhau**
```
Animation Durations: [0.55, 0.62, 0.68, 0.75]
```

---

## 🔧 Sự Khác Biệt So Với Trước

### **Trước (Lỗi)**
```csharp
// Chỉ đợi 0.35s rồi kích hoạt đòn tiếp theo
yield return new WaitForSeconds(0.35f);
// → Animation chưa xong nhưng đòn mới đã kích hoạt
```

### **Sau (Sửa)**
```csharp
// Đợi animation HOÀN TOÀN xong (theo animationDurations)
while (Time.time - animStartTime < animDuration)
{
    yield return null;
}
// → Đòn tiếp theo chỉ kích hoạt khi animation xong
```

---

## ✅ Verification Checklist

- [ ] Đo độ dài animation cho tất cả 4 đòn
- [ ] Ghi lại các giá trị
- [ ] Nhập vào Inspector → Animation Durations
- [ ] Play → Bấm chuột trái liên tục
- [ ] Kiểm tra animation smooth, không đè nhau

---

## ⚠️ Common Issues

### **Q: Animation vẫn nhảy tùm lum**
A:
- Kiểm tra Animation Durations có đúng không
- Tăng giá trị lên một chút (0.6 → 0.65)

### **Q: Combo quá chậm**
A:
- Giảm Animation Durations xuống
- Kiểm tra Combo Input Timeout (tăng lên 0.9-1.0)

### **Q: Không biết animation bao lâu**
A:
1. Mở Animation window
2. Chọn animation clip
3. Xem timeline → khung cuối là duration

---

## 📝 Default Settings

```
Attack Settings:
├─ Hit Times: [0.18, 0.22, 0.26, 0.32]
└─ Animation Durations: [0.6, 0.6, 0.6, 0.6] ← ĐÂY CẦN CHỈNH
```

**Lưu ý**: `Animation Durations` cần **phù hợp với animation thực tế**!

---

## 🚀 Summary

| Bước | Hành Động |
|------|----------|
| 1 | Đo độ dài animation (0.6s, 0.65s, v.v.) |
| 2 | Mở Inspector → CombatController |
| 3 | Sửa "Animation Durations" array |
| 4 | Test → animation smooth ✅ |

**Xong! Animation sẽ không còn đè nhau** 🎉

