# Tiến độ thực tế và phân việc theo kế hoạch

Đối chiếu 69 story trong `phan-cong-trello.md` với mã nguồn (backend, web quản trị Laravel, app Android). Cập nhật ngày 27/09/2026.

Trạng thái lấy từ đọc mã và các lần chạy thử backend/web. **App Android build được nhưng chưa được chạy thử trên máy**, nên ✅ ở các story app nghĩa là *có mã và biên dịch được*.

Ký hiệu: ✅ có mã và đáp ứng story · 🟡 làm một phần hoặc chưa xác minh hết · ❌ chưa có.

## Tổng quan

| | Điểm | Tỉ lệ |
|---|---|---|
| ✅ Đã xong | 235 | 76% |
| 🟡 Một phần | 46 | 14% |
| ❌ Chưa có | 28 | 9% |
| Tổng | 309 | 100% |

## Theo người phụ trách (đúng phân công)

| Người | Điểm được giao | ✅ | 🟡 | ❌ |
|---|---|---|---|---|
| Bùi Quang Long | 70 | 54 | 16 | 0 |
| Cao Tuấn Linh | 64 | 64 | 0 | 0 |
| Nguyễn Hoàng Trà | 63 | 31 | 14 | 18 |
| Bùi Đức Quỳnh | 56 | 51 | 5 | 0 |
| Nguyễn Quang Vũ | 56 | 35 | 11 | 10 |

## Theo sprint (đúng kế hoạch)

| Sprint | Điểm cam kết | ✅ | 🟡 | ❌ |
|---|---|---|---|---|
| Sprint 1 | 63 | 42 | 16 | 5 |
| Sprint 2 | 74 | 53 | 11 | 10 |
| Sprint 3 | 85 | 72 | 5 | 8 |
| Sprint 4 | 87 | 68 | 14 | 5 |

## Chi tiết từng story

| Story | Người | Sprint | Điểm | Trạng thái | Bằng chứng / ghi chú |
|---|---|---|---|---|---|
| US-01 Là cả nhóm, tôi muốn kho mã và CSDL chạy bằng một lệnh, để ai cũng bắt | Long | 1 | 5 | 🟡 | dotnet run + migration có sẵn; CSDL SQL Server phải tự dựng, chưa có docker-compose |
| US-02 Là cả nhóm, tôi muốn 14 bảng chốt trong một migration, để không ai phả | Long | 1 | 8 | 🟡 | Có migration; migration đầu tạo đủ bảng, sau đó có thêm 3 migration nhỏ (vai người dùng, tài khoản ngân hàng, khoản nợ) nên tổng 4, chưa đúng 'một migration' |
| US-03 Là cả nhóm, tôi muốn docs/api.md đủ 36 đường dẫn, để web và app dựng l | Long | 1 | 5 | ✅ | docs/api.md + docs/openapi.json |
| US-04 Là khách, tôi muốn đăng ký bằng số điện thoại và nhận OTP, để tạo được | Long | 2 | 5 | ✅ | POST /auth/register, /auth/verify-otp (đã chạy thử); app có màn đăng ký + OTP |
| US-05 Là khách, tôi muốn đăng nhập và giữ phiên, để không phải nhập lại mỗi  | Long | 2 | 3 | ✅ | POST /auth/login, token 1 ngày, tự đăng xuất khi hết hạn (đã chạy thử) |
| US-06 Là khách, tôi muốn xem hồ sơ và trạng thái duyệt giấy tờ của mình | Long | 2 | 3 | ✅ | GET /me (đã chạy thử) |
| US-07 Là người dùng, tôi muốn tải ảnh lên một chỗ duy nhất, để mọi loại ảnh  | Long | 2 | 5 | ✅ | POST /files |
| US-08 Là khách, tôi muốn nộp CCCD và GPLX, để được duyệt và đặt xe | Long | 2 | 3 | ✅ | POST /me/documents; app có màn Nộp giấy tờ |
| US-09 Là người vận hành, tôi muốn duyệt hoặc từ chối giấy tờ kèm lý do | Long | 3 | 3 | ✅ | POST /admin/documents/{id}/review (đã chạy thử) |
| US-10 Là chủ xe, tôi muốn ký cam kết với sàn, để được đăng xe | Long | 3 | 3 | ✅ | GET, POST /owner/agreement |
| US-11 Là lập trình viên, tôi muốn mọi lỗi nghiệp vụ về một dạng {code, messa | Long | 3 | 3 | ✅ | Mọi lỗi dạng {status,data,code,message} (đã chạy thử) |
| US-12 Là cả nhóm, tôi muốn dữ liệu mẫu dựng bằng một lệnh, để test lại từ đầ | Long | 4 | 2 | ✅ | POST /dev/seed |
| US-13 Là chủ xe, tôi muốn đăng xe kèm ảnh và giấy tờ xe | Linh | 1 | 5 | ✅ | POST /owner/cars |
| US-14 Là chủ xe, tôi muốn sửa giá, mô tả và ảnh xe của mình | Linh | 1 | 3 | ✅ | PUT /owner/cars/{id} |
| US-15 Là chủ xe, tôi muốn mở hoặc khoá từng ngày trong lịch xe | Linh | 1 | 5 | ✅ | PUT /owner/cars/{id}/calendar |
| US-16 Là khách, tôi muốn tìm xe theo khu vực, ngày, số chỗ và giá | Long | 3 | 5 | ✅ | GET /cars có lọc (đã chạy thử) |
| US-17 Là khách, tôi muốn xem chi tiết xe và những ngày còn trống | Long | 3 | 3 | ✅ | GET /cars/{id}, /availability (đã chạy thử) |
| US-18 Là người vận hành, tôi muốn duyệt xe và đối chiếu tên đăng ký xe với C | Long | 4 | 3 | 🟡 | POST /admin/cars/{id}/review; phần đối chiếu tên đăng ký xe với CCCD chưa xác minh |
| US-19 Là khách, tôi muốn xem giá trước khi đặt, để biết phải trả bao nhiêu | Linh | 2 | 3 | ✅ | POST /bookings/quote (đã chạy thử) |
| US-20 Là khách, tôi muốn đặt xe mà không bao giờ trùng ngày với người khác | Linh | 2 | 8 | ✅ | POST /bookings, khoá (car_id, day), SLOT_TAKEN (đã chạy thử) |
| US-21 Là khách, tôi muốn có mã QR đúng số tiền và đúng nội dung, để khỏi gõ  | Linh | 2 | 3 | ✅ | QrService + payment.qrUrl |
| US-22 Là chủ xe, tôi muốn nhận hoặc từ chối đơn trong 30 phút | Linh | 3 | 3 | ✅ | POST /owner/bookings/{id}/confirm, /reject; nhận đơn chặn khi giấy tờ khách chưa duyệt |
| US-23 Là người vận hành, tôi muốn xác nhận đã nhận tiền và bắt được chuyển t | Long | 4 | 5 | ✅ | POST /admin/payments/{id}/confirm + webhook ngân hàng |
| US-24 Là khách, tôi muốn huỷ đơn và được nhả lịch ngay | Quỳnh | 4 | 3 | ✅ | POST /bookings/{id}/cancel |
| US-25 Là hai bên giao nhận, tôi muốn lập biên bản có 6 ảnh, ODO, nhiên liệu  | Linh | 3 | 8 | ✅ | POST /bookings/{id}/handovers (đã chạy thử) |
| US-26 Là bên soi, tôi muốn bổ sung ảnh vết chưa ghi rồi mới ký | Linh | 4 | 5 | ✅ | POST /handovers/{id}/photos |
| US-27 Là bên soi, tôi muốn phản đối kèm lý do thay vì buộc phải ký | Vũ | 4 | 3 | ✅ | Backend review agreed=false; app có nút phản đối ở màn soi biên bản (cho cả khách và chủ xe) |
| US-28 Là lập trình viên, tôi muốn hàm tính phí phát sinh thuần, để test khôn | Linh | 3 | 5 | ✅ | PhiPhatSinhService + 17 unit test đạt |
| US-29 Là người vận hành, tôi muốn chốt đơn, ghi sổ cái và sinh 2 lệnh chi | Linh | 4 | 8 | ✅ | POST /admin/bookings/{id}/settle + sổ cái + ghi nợ phần vượt cọc (đã chạy thử) |
| US-30 Là người vận hành, tôi muốn danh sách lệnh chi phải chuyển mỗi tối | Long | 4 | 3 | ✅ | GET /admin/payouts |
| US-31 Là người vận hành, tôi muốn tra sổ cái một đơn khi đối soát lệch | Long | 4 | 3 | ✅ | GET /admin/ledger (đã chạy thử, cân bằng) |
| US-32 Là hệ thống, tôi muốn tác vụ nền dọn đơn treo, để lịch xe không bị kho | Linh | 4 | 8 | ✅ | Jobs/DonTreoService chạy nền mỗi phút |
| US-33 Là cả nhóm, tôi muốn khung web và lớp gọi API dựng theo api.md | Trà | 1 | 8 | ✅ | webadmin Laravel, ThueXeApi.php gọi API |
| US-34 Là người dùng web, tôi muốn đăng nhập và phân quyền theo vai | Trà | 1 | 3 | ✅ | Đăng nhập, middleware admin.auth (đã chạy thử) |
| US-35 Là chủ xe, tôi muốn ký cam kết và đăng xe trên web | Trà | 2 | 5 | ❌ | Web chưa có cổng chủ xe |
| US-36 Là chủ xe, tôi muốn mở khoá ngày trên lịch trực quan | Trà | 2 | 5 | ❌ | Web chưa có cổng chủ xe |
| US-37 Là chủ xe, tôi muốn xem đơn của xe mình và nhận hoặc từ chối | Trà | 3 | 5 | ❌ | Web chưa có cổng chủ xe |
| US-38 Là chủ xe, tôi muốn xem tiền đã nhận và tiền đang chờ | Trà | 3 | 3 | ❌ | Web chưa có cổng chủ xe |
| US-39 Là người vận hành, tôi muốn duyệt giấy tờ và duyệt xe | Trà | 3 | 5 | ✅ | Trang /documents, /cars (đã mở thử) |
| US-40 Là người vận hành, tôi muốn xác nhận tiền và dán nội dung sao kê | Trà | 4 | 5 | ✅ | /bookings/{id}/confirm-payment |
| US-41 Là người vận hành, tôi muốn xem biên bản và so ảnh giao với ảnh trả | Trà | 3 | 5 | 🟡 | /bookings/{id} có xem đơn; so ảnh giao với ảnh trả chưa xác minh |
| US-42 Là người vận hành, tôi muốn nhập phí phát sinh và chốt đơn | Trà | 4 | 5 | ✅ | /bookings/{id}/settle |
| US-43 Là người vận hành, tôi muốn danh sách lệnh chi và đánh dấu đã chuyển | Trà | 4 | 3 | ✅ | /payouts, /payouts/{id}/paid; /bookings/{id}/debt-paid (thu nợ) |
| US-44 Là cả nhóm, tôi muốn trang thống kê đơn giản cho buổi bảo vệ | Trà | 4 | 2 | ✅ | Trang tổng quan / |
| US-45 Là cả nhóm, tôi muốn dự án Compose và tầng gọi API dựng theo api.md | Quỳnh | 1 | 5 | ✅ | Compose + Hilt + Retrofit + BocConverterFactory, dùng chung cho hai chế độ |
| US-46 Là khách, tôi muốn đăng ký, nhập OTP và đăng nhập trên app | Quỳnh | 1 | 5 | ✅ | Màn đăng ký + OTP + đăng nhập |
| US-47 Là khách, tôi muốn tìm xe theo khu vực và ngày | Quỳnh | 2 | 8 | ✅ | SearchCarsScreen: lọc khu vực, ngày, số chỗ, hộp số, giá |
| US-48 Là khách, tôi muốn xem chi tiết xe, ảnh và lịch trống | Quỳnh | 2 | 3 | ✅ | CarDetailScreen: ảnh, thông số, lịch xe |
| US-49 Là khách, tôi muốn chọn ngày và thấy giá trước khi đặt | Quỳnh | 3 | 5 | ✅ | BookCarScreen: chọn ngày, tự báo giá |
| US-50 Là khách, tôi muốn xác nhận đặt và tạo đơn | Quỳnh | 3 | 3 | ✅ | BookCarScreen: đặt xe, tạo đơn |
| US-51 Là khách, tôi muốn thấy QR và app tự biết khi sàn đã nhận tiền | Quỳnh | 3 | 8 | ✅ | RenterBookingDetailScreen: QR + tự cập nhật khi chủ xe nhận hoặc tiền vào |
| US-52 Là khách, tôi muốn theo dõi đơn và xem lịch sử chuyến | Quỳnh | 4 | 5 | ✅ | MyBookingsScreen 5 tab + chi tiết chuyến + thanh tiến trình |
| US-53 Là khách, tôi muốn thấy lỗi bằng tiếng Việt dễ hiểu thay vì mã lỗi | Quỳnh | 4 | 3 | ✅ | nhanLoi + ErrorBanner ở mọi màn |
| US-54 Là khách, tôi muốn chụp CCCD và GPLX, app tự đọc số bằng ML Kit | Vũ | 1 | 5 | ❌ | Chưa thấy ML Kit trong app; nhập số CCCD/GPLX bằng tay |
| US-55 Là người dùng, tôi muốn ảnh được nén trước khi gửi, để không chờ 20 gi | Vũ | 1 | 3 | ✅ | data/AnhNen.kt: cạnh dài 1600px, JPEG 80% |
| US-56 Là bên lập, tôi muốn chụp đủ 6 ảnh theo khung bằng CameraX | Vũ | 2 | 8 | 🟡 | Có màn biên bản GIAO/TRẢ; ảnh chọn từ thư viện, chưa dùng CameraX |
| US-57 Là bên lập, tôi muốn nhập ODO và mức nhiên liệu 8 nấc | Vũ | 2 | 3 | ✅ | HandoverGiaoScreen |
| US-58 Là bên lập, tôi muốn ký tay bằng ngón thay vì tải ảnh chữ ký | Vũ | 3 | 5 | ✅ | widget ChuKyPad |
| US-59 Là bên soi, tôi muốn xem từng ảnh to hết cỡ rồi mới được ký | Vũ | 3 | 8 | ✅ | HandoverSoiScreen vuốt ngang 6 ảnh, phải xem đủ mới ký |
| US-60 Là chủ xe, tôi muốn xem hồ sơ, cam kết và tiền của mình trên app | Vũ | 2 | 3 | ✅ | ProfileScreen, AgreementScreen, DashboardScreen |
| US-61 Là chủ xe, tôi muốn xem xe của mình và lịch xe trên app | Vũ | 3 | 5 | ✅ | OwnerCarsScreen, CarCalendarScreen |
| US-62 Là cả nhóm, tôi muốn APK ký sẵn có biểu tượng và màn chào | Vũ | 4 | 3 | 🟡 | Có onboarding, biểu tượng, signingConfig; APK ký sẵn chưa xác minh |
| US-63 Là người demo, tôi muốn app chạy được cả khi không có máy chủ | Vũ | 4 | 5 | ❌ | Không thấy chế độ demo trong mã, dù báo cáo có ghi |
| US-64 Là người dùng web, tôi muốn thấy trạng thái rỗng và đang tải thay vì m | Trà | 1 | 3 | 🟡 | Chưa xác minh trạng thái rỗng, đang tải |
| US-65 Là người dùng web, tôi muốn thông báo lỗi bằng tiếng Việt thay vì mã l | Trà | 4 | 3 | 🟡 | Có ApiException dịch lỗi; phủ hết mã lỗi chưa xác minh |
| US-66 Là người vận hành, tôi muốn bảng biểu lọc và sắp xếp được, để tìm nhan | Trà | 2 | 3 | 🟡 | Có tab trạng thái và tìm đơn; sắp xếp chưa xác minh |
| US-67 Là khách, tôi muốn huỷ đơn trên app và thấy rõ chính sách hoàn tiền tr | Quỳnh | 4 | 5 | 🟡 | Có nút huỷ đơn; chưa hiện chính sách hoàn tiền trước khi bấm |
| US-68 Là khách, tôi muốn app nhớ phiên đăng nhập, không phải nhập lại mỗi lầ | Quỳnh | 2 | 3 | ✅ | TokenStore + tự đăng xuất khi hết hạn |
| US-69 Là khách, tôi muốn lập biên bản trả xe khi kết thúc chuyến | Vũ | 4 | 5 | ✅ | HandoverGiaoScreen kind=TRA cho khách |
