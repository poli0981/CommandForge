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
6. **Hiện chưa ký số Authenticode** (sẽ thêm Azure Trusted Signing/SignPath sau — xem `docs/Build.md`). Đừng thêm bước ký Authenticode vào build/CI lúc này. Attestation build-provenance (Sigstore, `actions/attest-build-provenance`), SBOM và checksums trong `release.yml` là được phép và phải giữ.
7. **Không telemetry.** Mạng duy nhất được phép: gọi GitHub để kiểm tra cập nhật. Bất kỳ thu thập dữ liệu nào khác đều bị cấm (vi phạm `docs/Privacy.md`).
8. **Xác nhận trước lệnh nguy hiểm.** Lệnh `Caution`/`Dangerous` phải có hộp thoại xác nhận; không cho phép tắt xác nhận với nhóm `Dangerous`.

---

## 3. Tech stack (tóm tắt — chi tiết ở `docs/tech.md`)

- **.NET 10 (LTS), C# 14** · **WPF** + **WPF-UI 4.3** (Fluent, lepoco) — đã thay Material Design từ v2.0.0
- **CommunityToolkit.Mvvm** (MVVM) · **Microsoft.Extensions.DependencyInjection** (`ServiceCollection` + `ValidateOnBuild`; **không** dùng Generic Host)
- **CsWin32** (P/Invoke) · **Serilog** (logging) · `System.Threading.Channels`
- **Velopack** (`vpk` pin làm local tool trong `.config/dotnet-tools.json`) cho cài đặt + cập nhật qua GitHub Releases
- Test: **xUnit v3** (`xunit.v3.mtp-off`, chạy qua VSTest) · **SonarAnalyzer.CSharp** chạy trong build
- i18n: `.resx` 5 ngôn ngữ (EN/VI/JA/zh-Hans/ES)

---

## 4. Bố cục solution (chi tiết ở `docs/architectures.md`)

```
src/
  CommandForge.Domain/          # entities thuần
  CommandForge.Application/     # use-cases + interfaces (ports)
  CommandForge.Infrastructure/  # Process, registry, Velopack, GitHub, Serilog
  CommandForge.Wpf/             # View + ViewModel + DI bootstrap + .resx (namespace: CommandForge.Desktop)
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
dotnet format CommandForge.slnx --verify-no-changes
# Phát hành + đóng gói (publish ReadyToRun + vpk pack → Releases/):
dotnet tool restore
pwsh ./scripts/pack.ps1 -Version <ver>
```

Phát hành thật: tag ký GPG `vX.Y.Z` trên `main` → `release.yml` (xem `RELEASING.md`).

---

## 6. Quy ước code (chi tiết ở `docs/codestyle.md`)

- Code, định danh, comment kỹ thuật: **tiếng Anh**. Chuỗi hiển thị cho người dùng: **`.resx` (EN/VI/JA/zh-Hans/ES)** — không hardcode chuỗi UI; mỗi key phải có ở cả 5 file (test `ResxKeyTests` quét UI để kiểm tra).
- Namespace tầng giao diện là `CommandForge.Desktop` (project/assembly/exe vẫn tên `CommandForge.Wpf`): namespace `CommandForge.Wpf` che namespace gốc `Wpf` của WPF-UI trong code XAML sinh ra.
- WPF-UI: mỗi trang (UserControl trong NavigationView) phải đặt `Foreground="{DynamicResource TextFillColorPrimaryBrush}"` ở gốc (Frame không truyền màu chữ xuống); lớp con của `ContentDialog` phải gọi `SetResourceReference(StyleProperty, typeof(ContentDialog))` — `XamlConventionsTests` kiểm tra cả hai. Cỡ chữ dùng `DynamicResource AppFontSize*`.
- Version package đặt trong **từng `.csproj`** (không dùng Central Package Management — dependency graph của GitHub không đọc `Directory.Packages.props`).
- Nullable reference types **bật**; `TreatWarningsAsErrors` **bật**; analyzers **bật**.
- `async/await` xuyên suốt cho I/O; không `async void` (trừ event handler); luôn truyền `CancellationToken`.
- MVVM bằng source-generator của CommunityToolkit (`[ObservableProperty]`, `[RelayCommand]`).
- File-scoped namespace; một type/file (trừ ngoại lệ hợp lý).
- Đẩy output process về UI bằng `Channel<T>` + dispatcher; **không** chạm UI từ thread nền.

---

## 7. Bất biến quan trọng khi đụng tới các vùng nhạy cảm

- **Thêm lệnh mới:** chỉ thêm vào catalog JSON (`Infrastructure/Catalog/commands.json`) theo schema ở `docs/CatalogCommand.md`; thêm key chuỗi vào cả 5 file `.resx`; icon phải có trong `Converters/IconMap.cs`; PowerShell luôn `-NoProfile -NonInteractive`; dùng token `{SystemDrive}`/`{SystemRoot}`/`{UserProfile}` thay vì đường dẫn cứng; viết test validate catalog. **Catalog sẽ được bổ sung/cập nhật liên tục theo thời gian** — giữ schema ổn định, dễ mở rộng.
- **Sửa luồng nâng quyền:** đọc kỹ `docs/Security.md` trước.
- **Sửa luồng cập nhật:** xử lý đủ `404 / 403 / 429 / 5xx / offline` (KHÔNG có mã `419`).

---

## 8. Chỉ mục tài liệu

| File | Nội dung |
|---|---|
| `docs/tech.md` | Tech stack, third-party (font, update…), phiên bản |
| `docs/architectures.md` | Kiến trúc chi tiết, ports/adapters, elevation broker, threading |
| `docs/codestyle.md` | Chuẩn code C#/XAML, `.editorconfig`, analyzers |
| `docs/UI.md` | Đặc tả giao diện từng thành phần (Fluent / WPF-UI) |
| `docs/CatalogCommand.md` | Toàn bộ lệnh + nhóm lệnh + schema |
| `docs/roadmap.md` | Lộ trình theo phase, milestone, version |
| `docs/note.md` | Ghi chú, quyết định, "gotcha", nhắc nhở |
| `docs/Extra.md` | Ý tưởng/tính năng tương lai (v2+) |
| `docs/Security.md` | Mô hình bảo mật, threat model, disclosure |
| `docs/Build.md` | Build, test, publish, release, CI |
| `docs/Testing.md` | Chiến lược kiểm thử |
| `docs/Changelog.md` | Chỉ trỏ về `CHANGELOG.md` (gốc repo) |
| `docs/LICENSE.md` / `Privacy.md` / `EULA.md` / `Disclaimer.md` | Bản pháp lý nội bộ (bản công khai ở gốc repo là chuẩn) |

> ⚠️ Thư mục `docs/` nằm trong `.gitignore` (tài liệu nội bộ, chỉ có trên máy).

**Tài liệu công khai (ở gốc repo, được commit):** `README.md`, `USAGE.md` (hướng dẫn người dùng — link "User guide" trong app), `CHANGELOG.md`, `BUILD.md`, `RELEASING.md`, `UPDATES.md`, `PRIVACY.md`, `EULA.md`, `DISCLAIMER.md`, `THIRD-PARTY-NOTICES.md`, `MAINTAINERS.md`, `LICENSE`, và trong `.github/`: `SECURITY.md`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`. Legal Gate trỏ tới EULA/PRIVACY/DISCLAIMER/LICENSE công khai.

**Email liên hệ (@poli0981.dev):** `security@` (lỗ hổng, cùng GitHub private vulnerability reporting) · `privacy@` · `legal@` · `copyright@` / `dmca@` / `takedown@` (bản quyền, DMCA, yêu cầu gỡ bỏ khác) · `code@` (mã nguồn, đóng góp, báo cáo Code of Conduct) · `collab@` · `press@` · `sponsor@`. Không dùng `games@` cho dự án này.
