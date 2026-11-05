# Debug UI Builder — Thiết kế

Ngày: 2026-11-05 · Project: `/Users/nguyen/Working/Unity/DebugUI` (Unity 6000.6.0f1, 2D URP, Input System only)

## 1. Mục đích và lý do

Một thư viện để dev khai báo debug UI bằng code, chạy **trong game** (play mode trong Editor và trong bản build). Mục đích là **chỉnh (adjust) và xem trực tiếp (preview) giá trị trong game** khi đang chạy.

- **Người viết panel:** dev, viết bằng code C#.
- **Người dùng panel:** tester, chỉ mở ra và thao tác, không viết code.
- **Yêu cầu số 1:** API phải rõ ràng và đơn giản. Binding giá trị bằng hàm inline (getter/setter lambda).
- **Thiết bị:** những máy có chuột, hoặc điện thoại/tablet có cảm ứng.

## 2. Goals / Non-goals / Anti-goals

**Goals (v1)**
- Layout: `Vertical`, `Horizontal`, `Foldout`.
- Control: `Label`, `Button`, `Slider`, value `Field`.
- Binding bằng lambda: UI luôn phản ánh giá trị thật, kể cả khi game tự thay đổi giá trị đó.
- Dùng được thoải mái bằng chuột và bằng ngón tay trên mobile (control đủ to, cuộn được, có tính safe area).
- Không cần setup scene: không prefab, không kéo thả, không cần EventSystem.
- Đóng gói thành package để dùng lại ở nhiều project.

**Non-goals (v1)**
- Điều khiển bằng gamepad/console (focus navigation). Người dùng đã loại khỏi phạm vi.
- Editor window. Thư viện chỉ chạy runtime.
- Lưu giá trị đã chỉnh giữa các phiên chơi.
- Cho người dùng tự đổi giao diện (skin/theme tuỳ biến). Package có một theme cố định.
- Immediate-mode API kiểu `OnGUI`.

**Anti-goals (coi là thất bại, dù chạy được)**
- API dài dòng: phải tạo class, kế thừa, đánh attribute hay tạo asset mới dùng được.
- Tốn hiệu năng hoặc sinh GC đáng kể khi debug UI **đang đóng**.
- Lọt vào bản release cho người chơi.
- Tester trên mobile không bấm hoặc kéo được control vì quá nhỏ.

## 3. Ràng buộc

- Package hỗ trợ **Unity 6000.0 trở lên**, nên chỉ được dùng API và tính năng USS có từ 6000.0. Project này (6000.6) là nơi phát triển và thử nghiệm. *(Quyết định của Claude.)*
- Hệ UI: **UI Toolkit runtime**.
- Theme mặc định runtime (`UnityDefaultRuntimeTheme.tss`) là asset riêng của mỗi project, nên package **phải tự mang theo theme và USS** của mình.
- Phụ thuộc bắt buộc duy nhất là module `com.unity.modules.uielements`. **Input System là tuỳ chọn** và chỉ dùng cho phím tắt. Asmdef dùng `versionDefines` để chỉ compile phần phím tắt khi project có `com.unity.inputsystem`. Project không có Input System vẫn mở UI bằng nút nổi. *(Quyết định của Claude.)* Project này đặt `activeInputHandler: 1` (chỉ dùng Input System), nên không được dùng `UnityEngine.Input` cũ.

## 4. Các cách đã cân nhắc

| Cách | Ưu | Nhược | Kết luận |
|---|---|---|---|
| **A. UI Toolkit dựng bằng code** | Hệ UI chính của Unity 6; scale theo DPI qua `PanelSettings`; cảm ứng tốt; có sẵn slider/foldout/text field/scroll; nhận Input System không cần cấu hình; hợp với kiểu dựng một lần rồi chỉ refresh giá trị | Phải tự viết theme/USS | **Đã chọn** |
| B. IMGUI (`OnGUI`) | Dễ viết nhất, không cần setup | Kém trên mobile (control nhỏ, phải tự scale bằng `GUI.matrix`, cuộn không quán tính, nhập số lỗi), sinh GC, vẽ lại mỗi frame | Loại: trái với yêu cầu dùng được trên mobile |
| C. uGUI (Canvas) | Cảm ứng ổn định, quen thuộc | Mỗi control là một cây GameObject; dựng layout bằng code rất dài; cần sprite/prefab và EventSystem | Loại: nặng nhất và khó giữ API gọn |

Người dùng đã chọn **A**.

## 5. API (đã thống nhất về hình dạng)

```csharp
using DebugUI;  // namespace — tên cuối cùng xem mục 8

// trong một MonoBehaviour — `this` là owner, panel tự gỡ khi owner bị destroy
DebugUI.Panel(this, "Player", p =>
{
    p.Label("HP", () => player.hp);                          // read-only, tự cập nhật
    p.Label("Build 1.2.3");                                  // text tĩnh
    p.Slider("Speed", () => player.speed, v => player.speed = v, 0, 20);
    p.Field("Gold", () => save.gold, v => save.gold = v);    // value field
    p.Horizontal(h =>
    {
        h.Button("Heal", () => player.hp = player.maxHp);
        h.Button("Kill", () => player.hp = 0);
    });
    p.Foldout("Advanced", f =>
    {
        f.Slider("Gravity", () => Physics2D.gravity.y,
                 v => Physics2D.gravity = new(0, v), -30, 0);
    });
});

// hệ thống không phải MonoBehaviour: owner = null → panel sống suốt game
DebugUI.Panel(null, "Audio", p => { ... });
DebugUI.Remove("Audio");   // gỡ thủ công nếu cần
```

### Nguyên tắc
- **Mỗi hệ thống tự đăng ký panel của mình** ngay trong code của nó. Không có file trung tâm liệt kê mọi panel.
- **Lambda-based builder.** Mọi container (`Panel`, `Vertical`, `Horizontal`, `Foldout`) nhận `Action<IDebugBuilder>`, và bên trong gọi được đầy đủ các control và layout.
- **Binding = getter + setter tuỳ chọn.** Không có setter thì control ở trạng thái chỉ đọc.
- **Không có attribute, không có reflection**, không cần tạo asset.
- **Có owner, không có handle.** Panel gắn với một `UnityEngine.Object` owner và **tự gỡ chậm nhất ở frame kế tiếp** sau khi owner bị destroy, nên dev không phải nhớ gọi `Dispose`. Nếu owner là `null` thì panel sống đến khi gọi `Remove(name)`. *(Người dùng đã chọn phương án này ở bước review. Nó thay cho thiết kế trả về handle trước đó.)*
- **Xoá hẳn khỏi bản release.** `Panel` và `Remove` trả về `void` và được đánh `[Conditional]`. Khi build release không bật debug UI, compiler xoá toàn bộ lời gọi tại call site, gồm cả lambda, closure và các chuỗi tên. Xem mục 6.

### Bề mặt API v1

```csharp
public static class DebugUI
{
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD"), Conditional("DEBUG_UI_ENABLED")]
    void Panel(UnityEngine.Object owner, string name, Action<IDebugBuilder> build);
    [Conditional(/* như trên */)]
    void Remove(string name);

    // Không bị strip: trong bản release chúng chỉ trả false hoặc không làm gì
    bool IsOpen { get; }
    void Open(); void Close(); void Toggle();
    bool IsPointerOverUI { get; }   // để game bỏ qua click đã rơi vào debug UI
}

public interface IDebugBuilder
{
    // Layout
    void Vertical(Action<IDebugBuilder> build);
    void Horizontal(Action<IDebugBuilder> build);
    void Foldout(string title, Action<IDebugBuilder> build, bool open = false);

    // Controls
    void Label(string text);
    void Label<T>(string label, Func<T> getter);              // hiển thị getter().ToString()
    void Button(string text, Action onClick);
    void Slider(string label, Func<float> get, Action<float> set, float min, float max);
    void Slider(string label, Func<int>   get, Action<int>   set, int   min, int   max);
    void Field(string label, Func<int>    get, Action<int>    set = null);
    void Field(string label, Func<float>  get, Action<float>  set = null);
    void Field(string label, Func<string> get, Action<string> set = null);
    void Field(string label, Func<bool>   get, Action<bool>   set = null);   // hiển thị dạng toggle
}
```

Kiểu dữ liệu `Field` trong v1 gồm `int`, `float`, `string`, `bool`. Các kiểu `enum`, `Vector2/3`, `Color` để dành cho sau. *(Đây là quyết định của Claude, người dùng chưa xác nhận.)*

## 6. Hành vi runtime

### Khởi động
- Khởi tạo lười (lazy): lần đầu có lời gọi `DebugUI.Panel` hoặc `Open` thì thư viện mới tạo một GameObject ẩn, gắn `DontDestroyOnLoad`, có `UIDocument`.
- `PanelSettings` được tạo lúc chạy bằng `ScriptableObject.CreateInstance`, dùng theme và USS nằm trong package. Panel đặt `sortingOrder` cao để luôn nằm trên UI của game.
- Scale theo DPI để control có kích thước vật lý hợp lý trên cả màn hình PC lẫn điện thoại. Có tôn trọng `Screen.safeArea`.

### Giao diện tester thấy *(quyết định của Claude)*

```
┌───────────────────────────────┐
│ [Player] [Enemy] [Audio] ▸    │  ← tab bar, cuộn ngang được
├───────────────────────────────┤
│ HP                       87   │
│ Speed      ●────────── 5.0    │
│ Gold                  [ 120 ] │
│ [  Heal  ] [  Kill  ]         │
│ ▸ Advanced                    │
│                               │  ← nội dung cuộn dọc
└───────────────────────────────┘
                          [DBG]   ← nút nổi mở/đóng
```

- **Mở/đóng:** bằng một nút nổi nhỏ ở góc màn hình (bấm được bằng chuột hoặc tap), hoặc bằng phím tắt trên máy có bàn phím (mặc định phím `` ` ``, đọc qua Input System).
- **Mỗi lần hiện một panel**, chọn bằng tab bar ở trên. Thứ tự tab là thứ tự đăng ký panel.
- Cửa sổ có hai bố cục. Trên **màn hình hẹp** (điện thoại dọc), cửa sổ chiếm gần hết màn hình. Trên **màn hình rộng**, cửa sổ là một khung cố định một bên, để vẫn nhìn được game phía sau.

### Refresh giá trị
- UI được dựng **một lần** khi đăng ký panel. Hàm `build` không chạy lại mỗi frame.
- Mỗi frame, chỉ các control **đang hiển thị** mới được refresh: phải đúng panel đang mở và không nằm trong foldout đang đóng. Thư viện gọi getter, so với giá trị lần trước, và chỉ cập nhật UI khi giá trị thay đổi.
- **Khi debug UI đóng thì không gọi getter nào**, nên chi phí gần như bằng 0.
- Khi tester đang thao tác một control (đang kéo slider hoặc đang gõ trong field), thư viện không ghi đè control đó bằng giá trị từ getter.
- Setter được gọi mỗi khi giá trị trên UI đổi. Slider gọi setter liên tục trong lúc kéo. Field gọi setter khi tester xác nhận (Enter hoặc rời khỏi ô).
- Refresh không được sinh GC mỗi frame. Getter generic không bị boxing. Với `Label<T>`, `ToString()` chỉ được gọi khi giá trị đổi.

### Lỗi
- Nếu getter hoặc setter ném exception, thư viện bắt lại, đánh dấu control ở trạng thái lỗi (ví dụ chữ đỏ), và log **một lần** cho mỗi control, không spam log. Các control khác vẫn chạy bình thường.

### Bật/tắt theo bản build
- Mặc định thư viện bật trong Editor và trong bản **Development Build**.
- Muốn có trong bản release cho tester thì thêm scripting define `DEBUG_UI_ENABLED` cho cả project.
- **Bản release thường:** `[Conditional]` xoá mọi lời gọi `Panel`/`Remove` ngay tại call site. Assembly của thư viện vẫn có mặt (để code gọi `IsOpen` vẫn compile được), nhưng không bao giờ tạo GameObject hay UI.
- Define `DEBUG_UI_DISABLED` để tắt luôn trong Editor và Development Build. Trường hợp này lời gọi vẫn được compile, nhưng thư viện bỏ qua chúng (no-op), không tạo gì và không gọi lambda nào.
- Theme/USS của package có thể vẫn nằm trong build vì chỉ chứa style, không lộ thông tin gì về game. *(Quyết định của Claude.)*

## 7. Đóng gói và kiểm thử

- Package nhúng (embedded) đặt tại `Packages/com.ng.debugui/`, có `package.json`, `Runtime/` (asmdef `DebugUI.Runtime`), `Tests/Runtime/` và `Samples~/` (scene demo).
- Một scene demo trong `Assets/` dùng thử mọi control, để kiểm tra bằng tay trên Editor và trên điện thoại.
- Có test tự động (PlayMode, `com.unity.test-framework`) cho phần logic: đăng ký và gỡ panel, binding hai chiều, chỉ refresh control đang hiển thị, xử lý lỗi getter, chế độ bị tắt.

## 8. Để builder tự quyết

- Tên namespace và tên class cuối cùng. Riêng `DebugUI` trùng với tên namespace, nên cần chọn cách tránh xung đột, ví dụ namespace `DebugUI` và class tĩnh `DUI`, hoặc namespace `Ng.DebugUI`.
- Màu sắc, khoảng cách và font của theme, miễn là control dễ đọc và dễ chạm (vùng chạm khoảng 44pt trở lên).
- Cấu trúc class bên trong (cách lưu danh sách binding, cách xác định control đang hiển thị).
- Nhịp refresh: mặc định mỗi frame, có thể cho cấu hình.
- Cách hiện nút nổi khi panel đang đóng, và có cho kéo nút đi chỗ khác hay không.
- Xử lý khi đăng ký trùng tên panel, và khi project tắt domain reload (static state phải được reset đúng).
- Cách phát hiện owner đã bị destroy (ví dụ kiểm tra `owner == null` mỗi frame).
