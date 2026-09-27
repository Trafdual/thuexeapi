# Hợp đồng API — sàn thuê xe

Bản này mô tả đúng những gì backend đang trả, đo bằng cách gọi thật ngày 22/09/2026.
Đổi bất kỳ trường nào ở đây thì báo cả nhóm trong ngày — Trà (web) cùng Quỳnh và Vũ (app) dựng lớp gọi API theo
đúng tệp này.

Gốc: `http://<máy chạy backend>:5160`

## Quy ước chung

**Tiền** là số nguyên đồng: `480000`, không phải `"480.000đ"`. Định dạng là việc của giao diện.

**Ngày thuê** là chuỗi `"2026-09-12"` (`DateOnly`). Không bao giờ gửi mốc thời gian cho ngày thuê.

**Mốc thời gian hệ thống** là chuỗi ISO có múi giờ: `"2026-09-21T10:30:00+00:00"`.

**Tên trường** là camelCase.

**Đơn định danh bằng MÃ ĐƠN, không phải id.** Đường dẫn dùng `{maDon}` dạng `KNMAAAA8F`:
`KNM` + 6 ký tự lấy từ bảng `ABCDEFGHJKMNPQRSTUVWXYZ23456789` (đã bỏ `0 O 1 I L` cho khỏi
nhầm khi gõ nội dung chuyển khoản). Id chạy số cho người ngoài đếm được sàn có bao nhiêu đơn;
mã đơn có 31⁶ ≈ 887 triệu tổ hợp nên dò không nổi.

Backend **vẫn nhận id số** ở các đường dẫn này để bộ Postman cũ và nhóm web đang làm dở không
gãy ngang. Mã mới là đường chính thức; id số sẽ bỏ khi web xong.

Biên bản, phiếu thu và lệnh chi vẫn dùng id số — chúng chưa có mã, và chỉ người trong cuộc
hoặc người vận hành gọi tới được.

**Xác thực**: mọi đường dẫn trừ `/auth/**` và `/news` đều cần header

```
Authorization: Bearer <token>
```

Token sống 1440 phút (1 ngày), mang sẵn `nameidentifier` = id người dùng và `is_owner`.

**Mọi phản hồi đều là HTTP 200 và cùng một khuôn bốn trường** `status`, `data`, `code`, `message`:

Thành công — `data` mang dữ liệu, `code` và `message` là `null`:

```json
{ "status": 200, "data": { ... }, "code": null, "message": null }
```

Lỗi — `data` là `null`, `code` và `message` mang lỗi:

```json
{ "status": 422, "data": null, "code": "WRONG_STATE",
  "message": "Đơn đang ở CHO_THANH_TOAN, không nhận được" }
```

Mọi thân mẫu ở các mục dưới đây là **nội dung của `data`**. Danh sách thì `data` là mảng, thao tác
không có dữ liệu trả về thì `data` là `null`.

`status` trong lỗi: 422 lỗi nghiệp vụ hoặc dữ liệu sai, 401 chưa đăng nhập, 403 sai vai, 404, 405,
415, 500. Client phân biệt bằng `status === 200` (hoặc `code == null`) và bắt lỗi theo `code`,
**không bắt theo câu chữ**. Lỗi không lường trước trả `status: 500, code: "INTERNAL_ERROR"`.
Thêm các mã: `UNAUTHORIZED` (401), `NOT_FOUND` (404), `METHOD_NOT_ALLOWED` (405),
`UNSUPPORTED_MEDIA_TYPE` (415).

### Bảng mã lỗi

| code | Nghĩa |
|---|---|
| `INVALID_INPUT` | Thiếu trường hoặc dữ liệu không hợp lệ |
| `PHONE_TAKEN` | Số điện thoại đã có tài khoản |
| `OTP_INVALID` | Mã OTP sai hoặc đã hết hạn (OTP sống 5 phút) |
| `LOGIN_FAILED` | Sai số điện thoại hoặc mật khẩu |
| `ACCOUNT_LOCKED` | Tài khoản ở trạng thái `KHOA` |
| `USER_NOT_FOUND` | Không tìm thấy người dùng |
| `FORBIDDEN` | Không đúng vai, hoặc sai khoá bí mật webhook |
| `NOT_FOUND` | Không tìm thấy. **Đơn của người khác cũng trả mã này**, không trả `FORBIDDEN` — phân biệt hai ca là biến API thành máy dò id |
| `MA_DON_KHONG_KHOP` | Nội dung chuyển khoản mang mã đơn khác với phiếu thu đang xác nhận |
| `CAR_UNAVAILABLE` | Không tìm thấy xe, hoặc xe không cho thuê |
| `PLATE_TAKEN` | Biển số đã có người đăng |
| `OWNER_AGREEMENT_REQUIRED` | Chưa ký cam kết chủ xe |
| `SLOT_TAKEN` | Ngày muốn khoá đang có đơn giữ |
| `WRONG_STATE` | Trạng thái hiện tại không cho phép thao tác này |
| `KYC_REQUIRED` | Giấy tờ của khách chưa được duyệt (chặn chủ xe nhận đơn) |
| `DEBT_OUTSTANDING` | Khách còn nợ từ chuyến trước, chưa đặt chuyến mới được |
| `HANDOVER_INCOMPLETE` | Biên bản thiếu ảnh hoặc thiếu chữ ký |
| `EMPTY_FILE` / `FILE_TOO_LARGE` / `UNSUPPORTED_FILE_TYPE` | Lỗi tải ảnh |

### Bảng trạng thái

| Nhóm | Giá trị |
|---|---|
| Đơn | `CHO_CHU_XE` `CHO_THANH_TOAN` `DA_XAC_NHAN` `DANG_THUE` `CHO_QUYET_TOAN` `CHO_CHI_TRA` `HOAN_TAT` `BI_TU_CHOI` `HET_HAN` `DA_HUY` |
| Xe | `NHAP` `CHO_DUYET` `DANG_BAN` `AN` |
| Giấy tờ | `CHUA_NOP` `CHO_DUYET` `DAT` `TU_CHOI` |
| Biên bản | `CHO_SOI` `DA_KY` `PHAN_DOI` |
| Lệnh chi | `CHO` `DA_CHI` `LOI` |
| Hộp số | `SO_SAN` `SO_TU_DONG` |
| Nhiên liệu | `XANG` `DAU` `DIEN` |
| Loại biên bản | `GIAO` `TRA` |
| Bên | `CHU_XE` `KHACH` |
| Khung ảnh | `TRUOC` `SAU` `TRAI` `PHAI` `TAPLO` `ODO` `KHAC` |
| Giấy tờ xe | `DANG_KY` `DANG_KIEM` `BAO_HIEM` `UY_QUYEN` |

---

# 1 · Xác thực

## POST /auth/register

Không cần token.

```json
{ "phone": "0364184928", "password": "test", "fullName": "Bùi Thanh Long", "email": "long@thuexe.vn" }
```

`email` cho phép null. Trả:

```json
{ "message": "Đã gửi OTP, kiểm tra log ở môi trường dev" }
```

Môi trường dev in OTP ra log backend: `OTP cho 0364184928: 763128`.

Lỗi: `INVALID_INPUT`, `PHONE_TAKEN`.

## POST /auth/verify-otp

```json
{ "phone": "0364184928", "otp": "763128" }
```

Trả `AuthResponse` (xem dưới). Lỗi: `OTP_INVALID`, `USER_NOT_FOUND`.

## POST /auth/login

```json
{ "phone": "0364184928", "password": "test" }
```

Trả **AuthResponse**:

```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "profile": {
    "id": 1,
    "phone": "0364184928",
    "fullName": "Bùi Thanh Long",
    "email": "long@thuexe.vn",
    "isOwner": true,
    "status": "HOAT_DONG",
    "documentStatus": "DAT"
  }
}
```

> ⚠ Trường tên là **`profile`**, không phải `user`.

Lỗi: `LOGIN_FAILED`, `ACCOUNT_LOCKED`.

---

# 2 · Hồ sơ và tệp

## GET /me

Không có thân. Trả **phẳng**, đúng hình dạng `profile` ở trên:

```json
{
  "id": 1, "phone": "0364184928", "fullName": "Bùi Thanh Long",
  "email": "long@thuexe.vn", "isOwner": true,
  "status": "HOAT_DONG", "documentStatus": "DAT",
  "bankAccount": "0123456789", "bankName": "Vietcombank"
}
```

> ⚠ Chỉ có **một chuỗi** `documentStatus`, không kèm số CCCD hay hạn GPLX. Cần chi tiết thì
> phải bổ sung đường dẫn mới.

## POST /me/documents

```json
{
  "cccdNo": "079201000123",
  "gplxNo": "790123456789",
  "gplxClass": "B2",
  "gplxExpiry": "2031-04-18",
  "frontUrl": "/files/ab12.jpg",
  "backUrl": "/files/cd34.jpg",
  "selfieUrl": null
}
```

Mọi trường trừ `frontUrl` và `backUrl` cho phép null. Trả:

```json
{ "id": 7, "status": "CHO_DUYET" }
```

> ⚠ Chỉ trả id và trạng thái, **không trả lại cả bản ghi**.

Lỗi: `INVALID_INPUT`.

## PUT /me/bank-account

Nơi nhận tiền hoàn cọc. Chủ xe đã khai trong bản cam kết; khách thì phải khai ở đây, nếu không
lệnh hoàn ghi `CHUA_CO` và người vận hành phải đi hỏi từng người.

```json
{ "bankAccount": "9704221234567", "bankName": "Techcombank" }
```

Trả lại `MeResponse`. Thiếu một trong hai trường trả `INVALID_INPUT`.

⚠ Cần migration `ThemTaiKhoanNganHangNguoiDung`. Kéo mã về phải chạy
`dotnet ef database update --project ThueXe/ThueXe.csproj`, quên là backend chết ngay lần gọi
`/me` đầu tiên.

## POST /files

`multipart/form-data`, một phần tên **`file`**.

- Tối đa **8 MB**
- Chỉ nhận `.jpg` `.jpeg` `.png` `.webp`

```json
{ "url": "/files/9f2c4a1b8e7d4f0a.jpg" }
```

`url` là **đường dẫn tương đối**. Muốn hiện ảnh thì phải ghép gốc vào:
`http://<host>:5160/files/9f2c4a1b8e7d4f0a.jpg`. Ảnh phục vụ tĩnh ở `/files/**`.

Lỗi: `EMPTY_FILE`, `FILE_TOO_LARGE`, `UNSUPPORTED_FILE_TYPE`.

---

# 3 · Cam kết chủ xe

## GET /owner/agreement

```json
{
  "version": "1.0",
  "content": "CAM KẾT CHỦ XE — bản 1.0\n\n1. Xe đăng lên sàn phải...",
  "signed": false,
  "signedAt": null
}
```

## POST /owner/agreement/accept

```json
{
  "version": "1.0",
  "cccdNo": "079201000123",
  "bankAccount": "0071000123456",
  "bankName": "Vietcombank",
  "signatureUrl": "/files/chu-ky.png"
}
```

Trả cùng hình dạng `GET`, với `signed: true` và `signedAt`. Ký xong thì `isOwner` thành `true`.
Ký lại bản đã ký thì trả nguyên trạng, không tạo bản mới.

Lỗi: `INVALID_INPUT`.

---

# 4 · Xe

## GET /owner/cars

Trả mảng **Car**, mới nhất trước:

```json
[{
  "id": 1, "ownerId": 1, "plate": "51A-123.45",
  "brand": "Toyota", "model": "Vios", "year": 2021, "seats": 5,
  "transmission": "SO_TU_DONG", "fuel": "XANG", "odo": 42000,
  "district": "Quận 1", "pickupAddress": "12 Nguyễn Huệ, Quận 1",
  "pricePerDay": 620000, "maxKmDay": 300, "deposit": 3000000,
  "description": "Toyota Vios 2021, 5 chỗ.",
  "status": "DANG_BAN", "rejectReason": null,
  "createdAt": "2026-09-21T12:00:00+00:00",
  "photos": [{ "id": 1, "url": "/files/mau/xe-1.jpg", "sortOrder": 0 }],
  "documents": [{ "id": 1, "type": "DANG_KIEM", "url": "/files/...", "expiryDate": "2027-03-01" }]
}]
```

## POST /owner/cars

**CarUpsertRequest** — dùng chung cho cả tạo và sửa:

```json
{
  "plate": "51A-123.45", "brand": "Toyota", "model": "Vios",
  "year": 2021, "seats": 5,
  "transmission": "SO_TU_DONG", "fuel": "XANG", "odo": 42000,
  "district": "Quận 1", "pickupAddress": "12 Nguyễn Huệ, Quận 1",
  "pricePerDay": 620000, "maxKmDay": 300, "deposit": 3000000,
  "description": "Xe gia đình, mới bảo dưỡng.",
  "photoUrls": ["/files/a.jpg", "/files/b.jpg"],
  "documents": [{ "type": "DANG_KIEM", "url": "/files/c.jpg", "expiryDate": "2027-03-01" }]
}
```

`maxKmDay` để 0 thì mặc định 300; `deposit` để 0 thì mặc định 15.000.000.
Biển số tự chuyển hoa và cắt khoảng trắng. Xe mới vào `CHO_DUYET`.

Trả **Car**. Lỗi: `OWNER_AGREEMENT_REQUIRED`, `PLATE_TAKEN`, `INVALID_INPUT`.

## PUT /owner/cars/{id}

Cùng thân với `POST`. `plate` bị bỏ qua — không đổi biển số được.
`photoUrls` hoặc `documents` rỗng thì **giữ nguyên** ảnh cũ; có phần tử thì **thay hết**.
Xe đang `DANG_BAN` mà sửa thì tự quay về `CHO_DUYET`.

Trả **Car**. Lỗi: `CAR_UNAVAILABLE`, `FORBIDDEN`.

## PUT /owner/cars/{id}/calendar

```json
{ "days": ["2026-10-05", "2026-10-06"], "blocked": true }
```

`blocked: true` khoá ngày, `false` mở lại. Thân trả về rỗng, chỉ xem mã 200.

Chỉ mở lại được ngày **do chủ xe tự khoá**; ngày đang có đơn giữ thì không đụng tới.
Khoá đè lên ngày đang có đơn thì trả `SLOT_TAKEN` kèm danh sách ngày vướng.

Lỗi: `CAR_UNAVAILABLE`, `FORBIDDEN`, `SLOT_TAKEN`.

## GET /cars/{id}

Trả **Car**. Người không phải chủ xe thì `pickupAddress` là `null`, trừ khi họ có đơn
đã qua bước xác nhận cho chính xe đó.

Lỗi: `CAR_UNAVAILABLE`.

## GET /cars/{id}/availability?from=&to=

`from` và `to` là `"2026-09-01"`. Bỏ trống thì mặc định hôm nay → +30 ngày.

```json
{
  "carId": 1,
  "from": "2026-09-01", "to": "2026-09-30",
  "busyDays": ["2026-09-23", "2026-09-24", "2026-09-25", "2026-09-26"],
  "blockedDays": ["2026-10-05"]
}
```

`busyDays` là ngày có đơn giữ, `blockedDays` là ngày chủ xe tự khoá.
**Đơn khoá cả ngày trả**: thuê 23→26 thì bận bốn ngày 23, 24, 25, 26.

Lỗi: `CAR_UNAVAILABLE`, `INVALID_INPUT`.

---

# 4b · Người thuê: tìm xe và đặt xe

## GET /cars?district=&from=&to=&seats=&transmission=&maxPrice=&page=&size=

Mọi tham số đều tuỳ chọn. Chỉ trả xe `DANG_BAN`. Có cả `from` và `to` thì bỏ hết xe bận dù chỉ
một ngày trong khoảng. Sắp theo giá tăng dần. `page` từ 1, `size` mặc định 20, tối đa 50.

```json
{
  "items": [{
    "id": 1, "plate": "51A-123.45", "brand": "Toyota", "model": "Vios", "year": 2021,
    "seats": 5, "transmission": "AT", "fuel": "XANG", "district": "Quận 1",
    "pricePerDay": 620000, "maxKmDay": 300, "deposit": 3000000,
    "photoUrl": "/files/mau/xe-1.jpg"
  }],
  "page": 1, "size": 20, "total": 12, "totalPages": 1
}
```

Bản rút gọn, **không có `pickupAddress`**. Lỗi: `INVALID_INPUT` (`to` không sau `from`).

## POST /bookings/quote

Tính giá, **không tạo đơn**.

```json
{ "carId": 1, "startDate": "2026-10-10", "endDate": "2026-10-13" }
```

```json
{
  "carId": 1, "startDate": "2026-10-10", "endDate": "2026-10-13", "days": 3,
  "pricePerDay": 620000, "rentTotal": 1860000, "deposit": 3000000,
  "commission": 279000, "tongPhaiTra": 4860000,
  "conTrong": true, "ngayBanTrongKhoang": []
}
```

Ngày trả không tính tiền: 10→13 là 3 ngày. `tongPhaiTra` = tiền thuê + cọc.
Lỗi: `CAR_UNAVAILABLE`, `INVALID_INPUT`.

## POST /bookings

Cùng body với `/bookings/quote`. Tạo đơn `CHO_CHU_XE`, giữ lịch cả ngày trả, hẹn chủ xe 30 phút
(`holdExpiresAt`). Chưa thu tiền ở bước này. Trả **Booking**.

Điều kiện: GPLX (nếu đã được duyệt) còn hạn tới ngày trả, không phải xe của chính mình, ngày không
ở quá khứ, và khách **không còn nợ** từ chuyến trước.

**Không còn chặn theo giấy tờ ở bước này**: khách mới đăng ký đặt trước được. Giấy tờ phải `DAT`
thì chủ xe mới nhận được đơn, xem `POST /owner/bookings/{maDon}/confirm`.

Lỗi: `DEBT_OUTSTANDING`, `CAR_UNAVAILABLE`, `SLOT_TAKEN`, `INVALID_INPUT`.

## GET /bookings?status=

Đơn của tôi (phía khách), mới nhất trước. `status` để trống thì lấy hết. Trả mảng **Booking**.

---

# 5 · Đơn

## GET /owner/bookings?status=

`status` để trống thì lấy hết. Trả mảng **Booking**, mới nhất trước:

```json
[{
  "id": 1, "code": "KNM4F2C9", "carId": 1,
  "car": { "id": 1, "plate": "51A-123.45", "brand": "Toyota", "model": "Vios",
           "photoUrl": "/files/mau/xe-1.jpg" },
  "renter": { "id": 101, "fullName": "Nguyễn Văn An",
              "phone": null, "idDocumentStatus": "DAT" },
  "startDate": "2026-09-23", "endDate": "2026-09-26", "days": 3,
  "pricePerDay": 620000, "rentTotal": 1860000,
  "deposit": 3000000, "commission": 279000,
  "status": "CHO_CHU_XE",
  "holdExpiresAt": "2026-09-22T03:30:00+00:00",
  "cancelReason": null,
  "createdAt": "2026-09-21T12:00:00+00:00",
  "debtAmount": 0,
  "debtPaidAt": null
}]
```

> `renter.phone` là **`null`** khi đơn còn ở `CHO_CHU_XE` hoặc `CHO_THANH_TOAN`.
> Chỉ mở sau khi chủ xe nhận đơn.

Chủ xe nhận = `rentTotal − commission`. Hoa hồng sàn là 15% tiền thuê.

## GET /bookings/{maDon}

```json
{
  "booking": { ... Booking ... },
  "payment": {
    "id": 1, "bookingId": 1, "amount": 4860000,
    "transferCode": "KNM4F2C9", "qrUrl": "https://img.vietqr.io/...",
    "status": "CHO", "receivedAmount": null,
    "confirmedAt": null, "bankNote": null
  },
  "handovers": [ ... Handover ... ],
  "charges": [ { "id": 1, "type": "QUA_KM", "amount": 150000, "note": "quá 30km" } ]
}
```

`payment` có thể là `null`. Cả chủ xe lẫn khách của đơn đều xem được.

Lỗi: `WRONG_STATE` (không tìm thấy), `FORBIDDEN`.

## POST /owner/bookings/{maDon}/confirm

Không có thân. Đơn phải đang ở `CHO_CHU_XE`.

Chuyển sang `CHO_THANH_TOAN`, hẹn tiếp 30 phút, và **mở số điện thoại khách**.
Trả **Booking**.

**Giấy tờ của khách phải `DAT`** thì mới nhận được; chưa duyệt trả `KYC_REQUIRED` (chủ xe chờ sàn
duyệt hoặc từ chối đơn).

Lỗi: `WRONG_STATE`, `KYC_REQUIRED`, `FORBIDDEN`.

## POST /owner/bookings/{maDon}/reject

```json
{ "reason": "Xe bận lịch bảo dưỡng" }
```

`reason` bắt buộc. Đơn phải đang ở `CHO_CHU_XE`. Chuyển sang `BI_TU_CHOI` và **nhả lịch**.
Chưa thu tiền nên không sinh hoàn tiền. Trả **Booking**.

Lỗi: `INVALID_INPUT`, `WRONG_STATE`, `FORBIDDEN`.

## POST /bookings/{maDon}/cancel

```json
{ "reason": "Khách đổi kế hoạch" }
```

Chuyển sang `DA_HUY`, nhả lịch. **Đã có biên bản GIAO thì không huỷ được** — phải đi qua
đường trả xe. Trả **Booking**.

Lỗi: `INVALID_INPUT`, `WRONG_STATE`, `FORBIDDEN`.

---

# 6 · Biên bản hai chiều

## POST /bookings/{maDon}/handovers

```json
{
  "kind": "GIAO",
  "odo": 42150,
  "fuelLevel": 8,
  "note": "Xước nhẹ cản sau, đã có sẵn",
  "signature": "/files/chu-ky.png",
  "photos": [
    { "slot": "TRUOC", "url": "/files/1.jpg", "note": null },
    { "slot": "SAU",   "url": "/files/2.jpg", "note": "xước cản" },
    { "slot": "TRAI",  "url": "/files/3.jpg", "note": null },
    { "slot": "PHAI",  "url": "/files/4.jpg", "note": null },
    { "slot": "TAPLO", "url": "/files/5.jpg", "note": null },
    { "slot": "ODO",   "url": "/files/6.jpg", "note": null }
  ]
}
```

Luật:

- **`GIAO` do chủ xe lập**, đơn phải ở `DA_XAC_NHAN`
- **`TRA` do khách lập**, đơn phải ở `DANG_THUE`
- Phải đủ **sáu khung** `TRUOC SAU TRAI PHAI TAPLO ODO`, thiếu thì `HANDOVER_INCOMPLETE`
  kèm danh sách khung còn thiếu trong `message`
- `fuelLevel` trong thang **0..8 nấc**
- Mỗi đơn chỉ một biên bản mỗi loại

Trả **Handover**:

```json
{
  "id": 1, "bookingId": 4, "kind": "GIAO", "createdBy": "CHU_XE",
  "odo": 42150, "fuelLevel": 8, "note": "Xước nhẹ cản sau",
  "status": "CHO_SOI",
  "signCreator": "/files/chu-ky.png", "signReviewer": null,
  "objection": null, "reviewedAt": null,
  "createdAt": "2026-09-22T01:00:00+00:00",
  "photos": [{ "id": 1, "slot": "TRUOC", "url": "/files/1.jpg",
               "takenBy": "CHU_XE", "note": null }]
}
```

Đơn **chưa đổi trạng thái** ở bước này — phải đợi bên kia ký.

Lỗi: `HANDOVER_INCOMPLETE`, `WRONG_STATE`, `FORBIDDEN`, `INVALID_INPUT`.

## POST /handovers/{id}/photos

```json
{ "photos": [ { "slot": "KHAC", "url": "/files/7.jpg", "note": "vết mới ở cửa" } ] }
```

Chỉ **bên soi** được bổ sung, và chỉ khi biên bản còn `CHO_SOI`. Trả **Handover**.

Lỗi: `WRONG_STATE`, `FORBIDDEN`.

## POST /handovers/{id}/review

Ký:

```json
{ "agreed": true, "objection": null, "signature": "/files/khach-ky.png" }
```

Phản đối:

```json
{ "agreed": false, "objection": "Xe xước nhiều hơn ảnh, không nhận", "signature": null }
```

Chỉ **bên soi** được gọi, và chỉ khi biên bản còn `CHO_SOI`.

| | Biên bản `GIAO` | Biên bản `TRA` |
|---|---|---|
| Ký | biên bản `DA_KY`, đơn → **`DANG_THUE`** | biên bản `DA_KY`, đơn → **`CHO_QUYET_TOAN`** |
| Phản đối | biên bản `PHAN_DOI`, đơn → **`DA_HUY`**, nhả lịch | biên bản `PHAN_DOI`, đơn **đứng yên** chờ người vận hành |

Trả **Handover**. Lỗi: `WRONG_STATE`, `FORBIDDEN`, `HANDOVER_INCOMPLETE`, `INVALID_INPUT`.

---

# 7 · Chi trả

## GET /owner/payouts

```json
[{
  "id": 1, "bookingId": 6, "bookingCode": "KNM8W7Z4",
  "payeeType": "CHU_XE",
  "bankAccount": "0071000123456", "bankName": "Vietcombank",
  "amount": 3570000, "status": "DA_CHI",
  "transferRef": "FT2609210012345",
  "paidAt": "2026-09-21T12:00:00+00:00",
  "createdAt": "2026-09-21T12:00:00+00:00"
}]
```

Chỉ lệnh chi có `payeeType = CHU_XE` và người nhận là chính bạn.

---

# 8 · Ngoài hợp đồng

## GET /news

⚠ **Không có trong bảng 36 đường dẫn của kế hoạch (§04).** App chủ xe đang gọi để dựng màn
Tin tức. Nhóm phải chốt: hoặc bổ sung chính thức và làm bảng `news` trong CSDL, hoặc bỏ màn
Tin tức và xoá `NewsController.cs`. Hiện nội dung cắm cứng trong controller.

Không cần token.

```json
[{
  "id": 1, "tag": "CHÍNH SÁCH",
  "title": "Từ 01/10: chi trả cho chủ xe rút xuống trong 12 giờ",
  "summary": "Sàn rút hạn chuyển tiền từ 24 giờ còn 12 giờ...",
  "imageUrl": "/files/mau/tin-1.jpg",
  "publishedAt": "2026-09-21"
}]
```

## POST /dev/seed

Chỉ chạy khi `ASPNETCORE_ENVIRONMENT=Development`. Dựng 12 xe, 6 đơn ở các trạng thái khác
nhau, **4 biên bản**, 2 khoản phí và 2 lệnh chi cho người gọi. Gọi lại thì xoá sạch rồi dựng lại.

```json
{ "message": "Đã dựng dữ liệu mẫu", "xe": 12, "don": 6, "bienBan": 4, "lenhChi": 2 }
```

Biển số và mã đơn **sinh theo từng người gọi** (`51A-009.01`, `KNMAAAA8B`). Trước đây dùng giá
trị cố định, mà `Plate` và `Code` có ràng buộc duy nhất toàn bảng còn seed chỉ xoá dữ liệu của
người gọi — nên người thứ hai seed trên backend dùng chung luôn nhận 500. Giờ mỗi người một dải
riêng, seed lại vẫn ra đúng mã cũ.

Trong 6 đơn có sẵn **một đơn `DANG_THUE` kèm biên bản TRA đang `CHO_SOI`** — đó là trạng thái
màn "Soi biên bản trả xe" của app chủ xe cần để mở ra có thứ mà xem. App khách chưa có nên
không ai lập được biên bản TRA từ giao diện.

Seed cũng cấp vai `VAN_HANH` cho người gọi để thử các đường `/admin/**`.

⚠ Mỗi lần gọi là **id đổi hết** — nó xoá bản ghi cũ rồi chèn mới. Ca kiểm thử đừng cắm cứng
`carId: 1`; lấy id từ `GET /owner/cars` rồi mới dùng.

## GET /dev/so-tien-phieu-thu/{maDon}

Chỉ ở môi trường dev, không cần token. Trả số tiền phải thu của một mã đơn — để script
`ban-tien-ve.sh` biết phải bắn bao nhiêu, đúng như khách quét mã QR và không sửa được số.

---

# 9 · Quản trị — vai `VAN_HANH`

Mọi đường dẫn `/admin/**` đòi token có vai `VAN_HANH`, sai vai trả `FORBIDDEN` (403).
Ở môi trường dev, `POST /dev/seed` cấp luôn vai này cho người gọi.

## GET /admin/documents?status=CHO_DUYET · POST /admin/documents/{id}/review

Hàng chờ duyệt CCCD và GPLX. `review` nhận `{ "approved": true }`, hoặc
`{ "approved": false, "reason": "Ảnh mờ, chụp lại" }` — từ chối mà thiếu lý do trả
`INVALID_INPUT`.

## GET /admin/cars?status=CHO_DUYET · POST /admin/cars/{id}/review

Như trên, cho xe chờ duyệt. Phản hồi kèm CCCD của chủ xe để đối chiếu giấy tờ.

## POST /admin/payments/{id}/confirm

Xác nhận tiền đã về ví treo.

```json
{ "receivedAmount": 5600000, "bankNote": "CT DEN KNMAAAA8D THUE XE" }
```

`receivedAmount` là **số tiền của LẦN CHUYỂN NÀY**, không phải tổng. Khách chuyển thiếu rồi
bù thêm thì backend cộng dồn.

`bankNote` nên dán nguyên nội dung trên sao kê. Backend dò mã đơn trong đó rồi đối chiếu với
phiếu thu; lệch thì trả `MA_DON_KHONG_KHOP` — đây là lá chắn duy nhất chống chọn nhầm phiếu
thu, lỗi tốn tiền nhất của khâu đối soát tay. Không dán thì vẫn cho qua nhưng phản hồi trả
`daDoiChieuNoiDung: false`.

Ba nhánh: **thiếu** thì không xác nhận, đơn giữ nguyên `CHO_THANH_TOAN`, trả `khopSoTien: false`;
**đủ** thì ghi 2 bút toán và đơn sang `DA_XAC_NHAN`; **thừa** thì vẫn xác nhận, sinh thêm lệnh
hoàn phần dư kèm 2 bút toán nữa.

## POST /admin/bookings/{maDon}/settle

Chốt phí, ghi sổ, sinh 2 lệnh chi, đơn sang `CHO_CHI_TRA`.

Thân rỗng `{}` thì backend **tự tính phí từ hai biên bản** — so ODO, so mức nhiên liệu, so giờ
trả. Muốn ghi đè thì gửi `{ "charges": [{ "type": "QUA_KM", "amount": 420000, "note": "..." }] }`;
loại phí ngoài `QUA_GIO` `QUA_KM` `NHIEN_LIEU` trả `INVALID_INPUT`.

Ghi **sáu bút toán** — ba cặp NỢ/CÓ, bỏ qua cặp nào có số tiền bằng 0:

```
VI_TREO NỢ  →  CHU_XE CÓ     tiền thuê − hoa hồng + phí trừ vào cọc
VI_TREO NỢ  →  SAN    CÓ     hoa hồng
VI_TREO NỢ  →  KHACH  CÓ     cọc − phí
```

Phí trừ vào cọc của khách rồi cộng cho chủ xe; phí lớn hơn cọc thì chỉ trừ hết cọc, sàn không
ứng phần vượt. **Phần vượt ghi thành nợ của khách**: `Booking.debtAmount` > 0 và `debtPaidAt` là
`null`. Khách còn nợ thì `POST /bookings` trả `DEBT_OUTSTANDING`.

## POST /admin/bookings/{maDon}/debt-paid

Khách đã chuyển khoản trả nợ. Ghi `debtPaidAt`, ghi sổ tiền vào ví treo và ra cho chủ xe (hai cặp
bút toán, `refType` = `DEBT` cho phần tiền vào), sinh một lệnh chi bù cho chủ xe. Gọi lần hai trả
`WRONG_STATE` nên không ghi sổ hai lần. Trả **SettleResult**. Lỗi: `NOT_FOUND`, `WRONG_STATE`.

## POST /dev/gia-lap-chuyen-khoan/{maDon}

**Chỉ môi trường dev.** Giả lập khách vừa chuyển khoản đủ số tiền còn thiếu của đơn, đi qua đúng
luồng của webhook ngân hàng. Chỉ khách đặt hoặc chủ xe của đơn gọi được. Dùng cho demo khi không
có ngân hàng thật.

## GET /admin/payouts?status=CHO · POST /admin/payouts/{id}/paid

Hàng chờ chuyển khoản, và ghi nhận đã chuyển. `paid` **bắt buộc** `transferRef` — mã giao dịch
trên sao kê; thiếu trả `INVALID_INPUT`. Chỉ khi **mọi** lệnh chi của đơn đã `DA_CHI` thì đơn mới
sang `HOAN_TAT`.

Số tài khoản người nhận được **chụp lại lúc sinh lệnh**: chủ xe lấy từ bản cam kết đã ký, khách
lấy từ hồ sơ (xem `PUT /me/bank-account`). Chưa khai thì ghi `CHUA_CO` và người vận hành phải
tự hỏi.

## GET /admin/ledger?bookingId=

Sổ cái một đơn, kèm tổng NỢ, tổng CÓ và cờ `canBang`. Dùng khi đối soát lệch.

## GET /admin/doi-soat?soDuNganHang=

Đối soát cuối ngày: so sổ cái với số dư thật trong tài khoản ngân hàng.

```json
{ "tongDaThu": 36800000, "tongDaChi": 9180000, "soDuKyVong": 27620000,
  "soDuNganHang": 27620000, "lech": 0, "viTreoTheoSoCai": 14000000,
  "conPhaiChi": 9360000, "dat": true,
  "loi": "Khớp. Sổ cái và tài khoản ngân hàng bằng nhau." }
```

Bỏ trống `soDuNganHang` thì chỉ tính số dư kỳ vọng. Lệch **dương** là có tiền vào chưa ghi
nhận; lệch **âm** nguy hiểm hơn — sổ nói còn tiền mà tài khoản không có, phải dừng lại tra
trước khi chi tiếp. Điểm cuối này không lưu gì, chỉ tính rồi so.

---

# 10 · Webhook ngân hàng

## POST /webhooks/bank

Nhận thông báo tiền về. Không dùng JWT vì bên gọi là máy — xác thực bằng header

```
X-Bank-Secret: <khoá>
```

Khoá đọc từ biến môi trường `Bank__WebhookSecret`. **Không đặt trong `appsettings.json`** —
ai đọc được repo là xác nhận được đơn mà không chuyển đồng nào. Chưa cấu hình thì điểm cuối
từ chối mọi yêu cầu.

Thân đặt tên trường theo đúng cái SePay và Casso gửi ra, để cắm dịch vụ thật vào là chạy:

```json
{ "amount": 5600000, "content": "CT DEN FT2609 KNMAAAA8D CHUYEN TIEN THUE XE",
  "transferRef": "FT260926193855", "occurredAt": "2026-09-26T12:38:55Z" }
```

Backend dò mã đơn trong `content` theo mẫu (ngân hàng hay viết hoa và cắt bớt), tìm phiếu thu
rồi chạy **đúng luật của `/admin/payments/{id}/confirm`** — hai đường dùng chung một service,
không có hai bản luật lệch nhau.

Trả về `tinhHuong` một trong: `DA_GHI_NHAN`, `THIEU_TIEN`, `DA_XU_LY_TRUOC_DO`,
`KHONG_THAY_MA_DON`, `MA_DON_LA`. Ba cái sau không tự quyết được nên ghi log chờ người vận
hành, nhưng vẫn trả HTTP 200 để dịch vụ ngân hàng khỏi gửi lại mãi một thông báo hỏng.

**Chống bắn trùng**: `transferRef` đã ghi thì bỏ qua. So khớp cả token chứ không dùng
`Contains` — mã giao dịch ngắn rất dễ lọt vào trong mã dài đã lưu rồi chặn oan lần chuyển bù.

Script giả lập lúc demo: `docs/ban-tien-ve.sh KNMAAAA8D`.

---

# 11 · Tác vụ nền

`DonTreoService` chạy mỗi phút, cảnh báo quá hạn mỗi 6 giờ. Năm việc: hết hạn chờ chủ xe,
hết hạn chờ thanh toán, khách không tới nhận xe, tự chốt sau 24 giờ, cảnh báo chưa trả xe.
Thiếu chúng thì đơn treo vĩnh viễn và lịch xe bị khoá chết — lỗi kín, không ai thấy cho tới
lúc chủ xe hỏi vì sao xe mình không ai đặt nữa.

## POST /me/device-token · DELETE /me/device-token?token=
Thông báo đẩy (Firebase Cloud Messaging). App gọi `POST` sau khi đăng nhập với `{ "token": "<FCM token>", "platform": "android" }`; gọi lại cùng token thì chỉ cập nhật, token chuyển sang người đăng nhập sau. `DELETE` khi đăng xuất.

Backend gửi thông báo ở các sự kiện: đơn mới (cho chủ xe), chủ xe nhận hoặc từ chối đơn, đã nhận tiền (cả hai bên), huỷ đơn, biên bản cần soi và đã ký hoặc bị phản đối, quyết toán, xe được duyệt hoặc bị từ chối. Payload có `data.bookingCode` khi thông báo gắn với một đơn.

Cấu hình: đặt khoá dịch vụ Firebase (Project settings → Service accounts → Generate new private key) vào `ThueXe/firebase-service-account.json`, hoặc đổi đường dẫn ở `Firebase:ServiceAccountPath`. Thiếu tệp này thì backend vẫn chạy bình thường nhưng không gửi thông báo, chỉ ghi cảnh báo vào log.
