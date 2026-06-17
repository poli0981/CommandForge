# CLAUDE.md

> File này là **ngữ cảnh + luật bắt buộc** cho AI coding agent (Claude Code) khi làm việc trong repo này. Đọc kỹ trước khi sinh/sửa code. Mọi tài liệu chi tiết nằm trong thư mục `docs/` (xem [Chỉ mục tài liệu](#chỉ-mục-tài-liệu)).

---

## 1. Dự án là gì

**CommandForge** *(tên tạm)* — ứng dụng desktop Windows giúp người dùng chạy các lệnh hệ thống phức tạp (winget, DISM, God Mode, registry tweak, network reset…) qua **giao diện trực quan, có phân loại**, không cần nhớ cú pháp.

- **Chỉ Windows 10 (22H2) và 11**, x64 (ARM64 nếu được).
- **Open-source, GPLv3.**
- Cài & cập nhật bằng **Velopack** (unpackaged, không qua Store).

---

## 2. Luật vàng (KHÔNG được vi phạm)

1. **An toàn lệnh:** Catalog lệnh là **tài nguyên nhúng, chỉ-đọc**. **TUYỆT ĐỐI KHÔNG** nạp lệnh tùy ý từ file/người dùng rồi tự nâng quyền Admin. Mọi lệnh chạy với quyền cao phải nằm trong catalog đã kiểm duyệt.
2. **Nâng quyền theo nhu cầu:** UI chính chạy `asInvoker` (không elevated). Lệnh cần Admin đi qua tiến trình phụ `CommandForge.Elevator` (xem `docs/architectures.md`). Không bắt cả app chạy Admin trừ khi bất khả kháng.
3. **Khởi động lại:** Phát hiện cần reboot bằng **exit code `3010`** (`ERROR_SUCCESS_REBOOT_REQUIRED`), không chỉ parse text.
4. **Ranh giới kiến trúc:** `Domain` không phụ thuộc gì. `Application` chỉ phụ thuộc `Domain`. `Infrastructure`/`Wpf` hiện thực các interface của `Application`. **Không bao giờ** để `Domain`/`Application` tham chiếu WPF hay Win32.
5. **Không obfuscate.** Mã GPLv3 vốn công khai; obfuscate vừa vô nghĩa vừa gây false-positive antivirus.
6. **Hiện chưa ký số** (sẽ thêm Azure Trusted Signing sau — xem `docs/Build.md`). Đừng thêm bước ký vào build/CI lúc này.
7. **Không telemetry.** Mạng duy nhất được phép: gọi GitHub để kiểm tra cập nhật. Bất kỳ thu thập dữ liệu nào khác đều bị cấm (vi phạm `docs/Privacy.md`).
8. **Xác nhận trước lệnh nguy hiểm.** Lệnh `Caution`/`Dangerous` phải có hộp thoại xác nhận; không cho phép tắt xác nhận với nhóm `Dangerous`.

---

## 3. Tech stack (tóm tắt — chi tiết ở `docs/tech.md`)

- **.NET 10 (LTS), C# 14** · **WPF** + **Material Design In XAML**
- **CommunityToolkit.Mvvm** (MVVM) · **Microsoft.Extensions.DependencyInjection/Hosting** (DI)
- **CsWin32** (P/Invoke) · **Serilog** (logging) · `System.Threading.Channels`
- **Velopack** (`vpk`) cho cài đặt + cập nhật qua GitHub Releases
- i18n: `.resx` (EN/VI)

---

## 4. Bố cục solution (chi tiết ở `docs/architectures.md`)

```
src/
  CommandForge.Domain/          # entities thuần
  CommandForge.Application/     # use-cases + interfaces (ports)
  CommandForge.Infrastructure/  # Process, registry, Velopack, GitHub, Serilog
  CommandForge.Wpf/             # View + ViewModel + DI bootstrap + .resx
helper/
  CommandForge.Elevator/        # tiến trình phụ chạy lệnh Admin (named pipe)
tests/
  CommandForge.Tests/           # xUnit
```

---

## 5. Lệnh build/run/test (chi tiết ở `docs/Build.md`)

```bash
dotnet restore
dotnet build -c Debug
dotnet run --project src/CommandForge.Wpf
dotnet test
# Phát hành + đóng gói:
dotnet publish src/CommandForge.Wpf -c Release -r win-x64 --self-contained
vpk pack --packId CommandForge --packVersion <ver> --packDir <publishDir>
```

---

## 6. Quy ước code (chi tiết ở `docs/codestyle.md`)

- Code, định danh, comment kỹ thuật: **tiếng Anh**. Chuỗi hiển thị cho người dùng: **`.resx` (EN/VI)** — không hardcode chuỗi UI.
- Nullable reference types **bật**; `TreatWarningsAsErrors` **bật**; analyzers **bật**.
- `async/await` xuyên suốt cho I/O; không `async void` (trừ event handler); luôn truyền `CancellationToken`.
- MVVM bằng source-generator của CommunityToolkit (`[ObservableProperty]`, `[RelayCommand]`).
- File-scoped namespace; một type/file (trừ ngoại lệ hợp lý).
- Đẩy output process về UI bằng `Channel<T>` + dispatcher; **không** chạm UI từ thread nền.

---

## 7. Bất biến quan trọng khi đụng tới các vùng nhạy cảm

- **Thêm lệnh mới:** chỉ thêm vào catalog JSON (`Infrastructure/Catalog/commands.json`) theo schema ở `docs/CatalogCommand.md`; thêm key chuỗi vào `.resx`; viết test validate catalog. **Catalog sẽ được bổ sung/cập nhật liên tục theo thời gian** — giữ schema ổn định, dễ mở rộng.
- **Sửa luồng nâng quyền:** đọc kỹ `docs/Security.md` trước.
- **Sửa luồng cập nhật:** xử lý đủ `404 / 403 / 429 / 5xx / offline` (KHÔNG có mã `419`).

---

## 8. Chỉ mục tài liệu

| File | Nội dung |
|---|---|
| `docs/tech.md` | Tech stack, third-party (font, update…), phiên bản |
| `docs/architectures.md` | Kiến trúc chi tiết, ports/adapters, elevation broker, threading |
| `docs/codestyle.md` | Chuẩn code C#/XAML, `.editorconfig`, analyzers |
| `docs/UI.md` | Đặc tả giao diện từng thành phần |
| `docs/CatalogCommand.md` | Toàn bộ lệnh + nhóm lệnh + schema |
| `docs/roadmap.md` | Lộ trình theo phase, milestone, version |
| `docs/note.md` | Ghi chú, quyết định, "gotcha", nhắc nhở |
| `docs/Extra.md` | Ý tưởng/tính năng tương lai (v2+) |
| `docs/Security.md` | Mô hình bảo mật, threat model, disclosure |
| `docs/Build.md` | Build, test, publish, release, CI |
| `docs/Testing.md` | Chiến lược kiểm thử |
| `docs/Changelog.md` | Nhật ký thay đổi |
| `LICENSE.md` / `Privacy.md` / `EULA.md` / `Disclaimer.md` | Pháp lý |

> ⚠️ Bộ tài liệu này nằm trong `.gitignore` (tài liệu nội bộ). Nếu sau này cần publish các file pháp lý (để Legal Gate trỏ link tới GitHub), tạo bản công khai riêng — xem `docs/note.md`.
