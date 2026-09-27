# Kế hoạch Agile — sàn thuê xe tự lái

Bốn sprint, mỗi sprint một tuần. **Năm người đều viết code** — không ai chỉ làm tài liệu hay
chỉ bấm test, mỗi người có một mảng mã nguồn đứng tên để chấm điểm riêng.

---

## Vai trò

| Người | Vai Scrum | Mảng mã nguồn |
|---|---|---|
| **Bùi Quang Long** | Product Owner kiêm Dev | Backend A — nền tảng, CSDL, tài khoản, giấy tờ |
| **Cao Tuấn Linh** | Dev | Backend B — xe, đặt xe, tiền, biên bản, tác vụ nền |
| **Nguyễn Hoàng Trà** | Scrum Master kiêm Dev | Web — cổng chủ xe + trang quản trị |
| **Bùi Đức Quỳnh** | Dev | App — tìm xe, đặt xe, thanh toán, theo dõi đơn |
| **Nguyễn Quang Vũ** | Dev | App — giấy tờ, biên bản, chủ xe, đóng gói |

Nhóm năm người thì không đủ để có PO và SM chuyên trách. Long giữ Product Backlog vì nắm lược
đồ và hợp đồng API; Trà chạy các buổi họp và gỡ vướng, vì mảng web đứng sau cả backend lẫn
app nên nhìn được toàn cảnh.

---

## Bảng Trello theo Scrum

Sáu cột:

```
Product Backlog · Sprint Backlog · Doing · Code Review · Testing · Done
```

**Nhãn**: `Long` `Linh` `Trà` `Quỳnh` `Vũ` · `Sprint 1..4` · `Blocker` · `Tiền — 2 người duyệt` ·
`Nợ kỹ thuật`

**Quy tắc WIP**: mỗi người **không quá 2 thẻ** ở cột Doing cùng lúc. Muốn nhận thẻ mới thì
đẩy thẻ cũ sang Code Review trước. Đây là quy tắc hay bị bỏ qua nhất và cũng là thứ giữ cho
cuối sprint không có mười thẻ làm dở.

Thẻ chỉ sang **Done** khi đủ Definition of Done ở cuối tệp.

---

## Nhịp làm việc

| Nghi thức | Khi nào | Bao lâu | Làm gì |
|---|---|---|---|
| Sprint Planning | Thứ Hai đầu sprint | 60 phút | Chọn story từ Product Backlog, chia task, ước lượng |
| Daily Standup | Mỗi ngày 20:00 | 10 phút | Hôm qua làm gì, hôm nay làm gì, đang vướng gì |
| Sprint Review | Thứ Bảy cuối sprint | 45 phút | Demo chạy thật, không demo bằng ảnh chụp |
| Retrospective | Ngay sau Review | 30 phút | Giữ gì, bỏ gì, thử gì sprint sau |

Standup là **đứng báo vướng**, không phải báo cáo tiến độ. Ai vướng quá một ngày thì nêu ra
ngay, đừng để tới Review mới nói.

---

## Ước lượng

Dùng dãy Fibonacci: **1 · 2 · 3 · 5 · 8 · 13**

| Điểm | Nghĩa |
|---|---|
| 1 | Sửa vặt, dưới nửa buổi |
| 2 | Một màn đơn giản, một đường dẫn CRUD |
| 3 | Một màn có gọi API và xử lý lỗi |
| 5 | Một luồng nhiều bước, hoặc logic nghiệp vụ có bẫy |
| 8 | Nhiều thành phần cùng đổi, hoặc chỗ dễ sai như chống đặt trùng |
| 13 | **Quá to — phải chẻ nhỏ trước khi đưa vào sprint** |

Sức chứa mỗi người khoảng **14–18 điểm một sprint** (sinh viên, làm ngoài giờ học).
Sprint 1 cố ý nhẹ hơn vì ai cũng đang dựng khung và học công cụ.

Tổng cả đợt **309 điểm**, chia: Long 70 · Linh 64 · Trà 63 · Quỳnh 56 · Vũ 56. Lệch 14 điểm trên bốn
sprint là chấp nhận được — Long nặng hơn vì hai sprint đầu gánh phần chặn cả nhóm.

---

# Product Backlog

Ưu tiên từ trên xuống. Story nào chưa đủ Definition of Ready thì để ở Backlog, đừng kéo vào
sprint.

## Epic A — Nền tảng và tài khoản

```
US-01 [Long] (5) Là cả nhóm, tôi muốn kho mã và CSDL chạy bằng một lệnh, để ai cũng bắt đầu được
US-02 [Long] (8) Là cả nhóm, tôi muốn 14 bảng chốt trong một migration, để không ai phải sửa lược đồ giữa chừng
US-03 [Long] (5) Là cả nhóm, tôi muốn docs/api.md đủ 36 đường dẫn, để web và app dựng lớp gọi API không cần đợi backend
US-04 [Long] (5) Là khách, tôi muốn đăng ký bằng số điện thoại và nhận OTP, để tạo được tài khoản
US-05 [Long] (3) Là khách, tôi muốn đăng nhập và giữ phiên, để không phải nhập lại mỗi lần mở app
US-06 [Long] (3) Là khách, tôi muốn xem hồ sơ và trạng thái duyệt giấy tờ của mình
US-07 [Long] (5) Là người dùng, tôi muốn tải ảnh lên một chỗ duy nhất, để mọi loại ảnh dùng chung một đường
US-08 [Long] (3) Là khách, tôi muốn nộp CCCD và GPLX, để được duyệt và đặt xe
US-09 [Long] (3) Là người vận hành, tôi muốn duyệt hoặc từ chối giấy tờ kèm lý do
US-10 [Long] (3) Là chủ xe, tôi muốn ký cam kết với sàn, để được đăng xe
US-11 [Long] (3) Là lập trình viên, tôi muốn mọi lỗi nghiệp vụ về một dạng {code, message}
US-12 [Long] (2) Là cả nhóm, tôi muốn dữ liệu mẫu dựng bằng một lệnh, để test lại từ đầu nhanh
```

## Epic B — Xe và lịch

```
US-13 [Linh] (5) Là chủ xe, tôi muốn đăng xe kèm ảnh và giấy tờ xe
US-14 [Linh] (3) Là chủ xe, tôi muốn sửa giá, mô tả và ảnh xe của mình
US-15 [Linh] (5) Là chủ xe, tôi muốn mở hoặc khoá từng ngày trong lịch xe
US-16 [Long] (5) Là khách, tôi muốn tìm xe theo khu vực, ngày, số chỗ và giá
US-17 [Long] (3) Là khách, tôi muốn xem chi tiết xe và những ngày còn trống
US-18 [Long] (3) Là người vận hành, tôi muốn duyệt xe và đối chiếu tên đăng ký xe với CCCD
```

## Epic C — Đặt xe và thanh toán

```
US-19 [Linh] (3) Là khách, tôi muốn xem giá trước khi đặt, để biết phải trả bao nhiêu
US-20 [Linh] (8) Là khách, tôi muốn đặt xe mà không bao giờ trùng ngày với người khác
US-21 [Linh] (3) Là khách, tôi muốn có mã QR đúng số tiền và đúng nội dung, để khỏi gõ tay
US-22 [Linh] (3) Là chủ xe, tôi muốn nhận hoặc từ chối đơn trong 30 phút
US-23 [Long] (5) Là người vận hành, tôi muốn xác nhận đã nhận tiền và bắt được chuyển thiếu thừa
US-24 [Quỳnh] (3) Là khách, tôi muốn huỷ đơn và được nhả lịch ngay
```

## Epic D — Biên bản hai chiều

```
US-25 [Linh] (8) Là hai bên giao nhận, tôi muốn lập biên bản có 6 ảnh, ODO, nhiên liệu và chữ ký
US-26 [Linh] (5) Là bên soi, tôi muốn bổ sung ảnh vết chưa ghi rồi mới ký
US-27 [Vũ] (3) Là bên soi, tôi muốn phản đối kèm lý do thay vì buộc phải ký
```

## Epic E — Tiền và quyết toán

```
US-28 [Linh] (5) Là lập trình viên, tôi muốn hàm tính phí phát sinh thuần, để test không cần CSDL
US-29 [Linh] (8) Là người vận hành, tôi muốn chốt đơn, ghi sổ cái và sinh 2 lệnh chi
US-30 [Long] (3) Là người vận hành, tôi muốn danh sách lệnh chi phải chuyển mỗi tối
US-31 [Long] (3) Là người vận hành, tôi muốn tra sổ cái một đơn khi đối soát lệch
US-32 [Linh] (8) Là hệ thống, tôi muốn tác vụ nền dọn đơn treo, để lịch xe không bị khoá chết
```

## Epic F — Web

```
US-33 [Trà] (8) Là cả nhóm, tôi muốn khung web và lớp gọi API dựng theo api.md
US-34 [Trà] (3) Là người dùng web, tôi muốn đăng nhập và phân quyền theo vai
US-35 [Trà] (5) Là chủ xe, tôi muốn ký cam kết và đăng xe trên web
US-36 [Trà] (5) Là chủ xe, tôi muốn mở khoá ngày trên lịch trực quan
US-37 [Trà] (5) Là chủ xe, tôi muốn xem đơn của xe mình và nhận hoặc từ chối
US-38 [Trà] (3) Là chủ xe, tôi muốn xem tiền đã nhận và tiền đang chờ
US-39 [Trà] (5) Là người vận hành, tôi muốn duyệt giấy tờ và duyệt xe
US-40 [Trà] (5) Là người vận hành, tôi muốn xác nhận tiền và dán nội dung sao kê
US-41 [Trà] (5) Là người vận hành, tôi muốn xem biên bản và so ảnh giao với ảnh trả
US-42 [Trà] (5) Là người vận hành, tôi muốn nhập phí phát sinh và chốt đơn
US-43 [Trà] (3) Là người vận hành, tôi muốn danh sách lệnh chi và đánh dấu đã chuyển
US-44 [Trà] (2) Là cả nhóm, tôi muốn trang thống kê đơn giản cho buổi bảo vệ
US-64 [Trà] (3) Là người dùng web, tôi muốn thấy trạng thái rỗng và đang tải thay vì màn trắng
US-65 [Trà] (3) Là người dùng web, tôi muốn thông báo lỗi bằng tiếng Việt thay vì mã lỗi
US-66 [Trà] (3) Là người vận hành, tôi muốn bảng biểu lọc và sắp xếp được, để tìm nhanh một đơn
```

## Epic G — App: tìm và đặt xe

```
US-45 [Quỳnh] (5) Là cả nhóm, tôi muốn dự án Compose và tầng gọi API dựng theo api.md
US-46 [Quỳnh] (5) Là khách, tôi muốn đăng ký, nhập OTP và đăng nhập trên app
US-47 [Quỳnh] (8) Là khách, tôi muốn tìm xe theo khu vực và ngày
US-48 [Quỳnh] (3) Là khách, tôi muốn xem chi tiết xe, ảnh và lịch trống
US-49 [Quỳnh] (5) Là khách, tôi muốn chọn ngày và thấy giá trước khi đặt
US-50 [Quỳnh] (3) Là khách, tôi muốn xác nhận đặt và tạo đơn
US-51 [Quỳnh] (8) Là khách, tôi muốn thấy QR và app tự biết khi sàn đã nhận tiền
US-52 [Quỳnh] (5) Là khách, tôi muốn theo dõi đơn và xem lịch sử chuyến
US-53 [Quỳnh] (3) Là khách, tôi muốn thấy lỗi bằng tiếng Việt dễ hiểu thay vì mã lỗi
US-67 [Quỳnh] (5) Là khách, tôi muốn huỷ đơn trên app và thấy rõ chính sách hoàn tiền trước khi bấm
US-68 [Quỳnh] (3) Là khách, tôi muốn app nhớ phiên đăng nhập, không phải nhập lại mỗi lần mở
```

## Epic H — App: giấy tờ và biên bản

```
US-54 [Vũ] (5) Là khách, tôi muốn chụp CCCD và GPLX, app tự đọc số bằng ML Kit
US-55 [Vũ] (3) Là người dùng, tôi muốn ảnh được nén trước khi gửi, để không chờ 20 giây
US-56 [Vũ] (8) Là bên lập, tôi muốn chụp đủ 6 ảnh theo khung bằng CameraX
US-57 [Vũ] (3) Là bên lập, tôi muốn nhập ODO và mức nhiên liệu 8 nấc
US-58 [Vũ] (5) Là bên lập, tôi muốn ký tay bằng ngón thay vì tải ảnh chữ ký
US-59 [Vũ] (8) Là bên soi, tôi muốn xem từng ảnh to hết cỡ rồi mới được ký
US-60 [Vũ] (3) Là chủ xe, tôi muốn xem hồ sơ, cam kết và tiền của mình trên app
US-61 [Vũ] (5) Là chủ xe, tôi muốn xem xe của mình và lịch xe trên app
US-62 [Vũ] (3) Là cả nhóm, tôi muốn APK ký sẵn có biểu tượng và màn chào
US-63 [Vũ] (5) Là người demo, tôi muốn app chạy được cả khi không có máy chủ
US-69 [Vũ] (5) Là khách, tôi muốn lập biên bản trả xe khi kết thúc chuyến
```

---

# Bốn sprint

## Sprint 1 — Khung chạy được đầu–cuối

> **Sprint Goal**: chủ xe đăng được xe trên web, người vận hành duyệt, và xe hiện lên trong
> app trên điện thoại thật.

| | Story | Điểm |
|---|---|---|
| **Long** | US-01 US-02 US-03 | **18** |
| **Linh** | US-13 US-14 US-15 | **13** |
| **Trà** | US-33 US-34 US-64 | **14** |
| **Quỳnh** | US-45 US-46 | **10** |
| **Vũ** | US-54 US-55 | **8** |

Long nặng nhất sprint này vì **US-02 và US-03 chặn cả bốn người còn lại**. Bốn người kia hai
ngày đầu dựng khung dự án và làm giao diện với dữ liệu giả — không ai ngồi chơi, nhưng cũng
đừng nhận thêm story.

**Rủi ro lớn nhất**: lược đồ CSDL chốt muộn thì cả sprint trượt. Hết ngày 2 mà US-02 chưa
xong thì Standup hôm đó phải gọi người sang phụ, đừng đợi tới Review mới nói.

## Sprint 2 — Đặt đơn và thanh toán

> **Sprint Goal**: đặt được một đơn thật, quét QR bằng app ngân hàng ra đúng số tiền và đúng
> nội dung, người vận hành xác nhận, đơn đổi trạng thái.

| | Story | Điểm |
|---|---|---|
| **Long** | US-04 US-05 US-06 US-07 US-08 | **19** |
| **Linh** | US-19 US-20 US-21 | **14** |
| **Trà** | US-35 US-36 US-66 | **13** |
| **Quỳnh** | US-47 US-48 US-68 | **14** |
| **Vũ** | US-56 US-57 US-60 | **14** |

**US-20 là story nguy hiểm nhất cả dự án.** Nếu nó trượt thì US-50, US-37 và toàn bộ Epic E
đứng theo. Làm nó ngày đầu sprint, đừng để cuối — 8 điểm nhưng là 8 điểm dễ đội lên gấp đôi.

## Sprint 3 — Giao xe, trả xe, quyết toán

> **Sprint Goal**: một chuyến đi trọn vẹn từ tìm xe tới chốt đơn, không phải sửa CSDL bằng
> tay ở bất kỳ bước nào.

| | Story | Điểm |
|---|---|---|
| **Long** | US-09 US-10 US-11 US-16 US-17 | **17** |
| **Linh** | US-22 US-25 US-28 | **16** |
| **Trà** | US-37 US-38 US-39 US-41 | **18** |
| **Quỳnh** | US-49 US-50 US-51 | **16** |
| **Vũ** | US-58 US-59 US-61 | **18** |

Sprint nặng nhất cả đợt (85 điểm), nhưng chia đều 16–18 mỗi người. Đây là lúc ba mảng phải
khớp nhau: biên bản chạy được thì cả web lẫn app mới demo được vòng đời một chuyến.

## Sprint 4 — Hoàn thiện, không thêm tính năng

> **Sprint Goal**: demo hai lượt không vấp, APK ký sẵn, video quay xong.

| | Story | Điểm |
|---|---|---|
| **Long** | US-12 US-18 US-23 US-30 US-31 | **16** |
| **Linh** | US-26 US-29 US-32 | **21** ⚠️ |
| **Trà** | US-40 US-42 US-43 US-44 US-65 | **18** |
| **Quỳnh** | US-24 US-52 US-53 US-67 | **16** |
| **Vũ** | US-27 US-62 US-63 US-69 | **16** |

**Linh vẫn 21 điểm — vượt sức chứa.** Trong Planning phải xử: hoặc kéo US-29 lên sprint 3,
hoặc Long nhận đỡ US-32. Đừng nhận rồi làm không hết; đó là cách nhanh nhất để Review không có
gì demo.

Hai story tiền nặng nhất (US-29 chốt đơn, US-32 tác vụ nền) nằm ở sprint cuối là rủi ro đã
biết. Nếu sprint 3 ai xong sớm thì kéo chúng lên.

**Từ giữa sprint 4 không nhận story mới**, kể cả story "chỉ mất một tiếng". Cái bị chấm là
buổi demo, không phải số dòng code.

## Vì sao có story backend nằm ở tay người làm app

US-24 (huỷ đơn) thuộc Quỳnh và US-27 (phản đối biên bản) thuộc Vũ — hai người này làm app, nhưng
nhận luôn đường dẫn backend của chính tính năng mình dựng giao diện.

Cắt **lát dọc** như vậy đúng tinh thần Agile hơn cắt theo tầng: một người làm trọn một tính
năng từ CSDL tới màn hình thì không phải chờ ai, và lúc hỏng cũng biết ngay hỏng ở đâu.
Đổi lại, hai người đó phải đọc được mã C# ở mức sửa một controller — Linh kèm trong sprint đầu.

---

# Tiêu chí chấp nhận — các story dễ sai

Dán vào ô Description của thẻ.

## US-02 · 14 bảng chốt trong một migration `Blocker`

- [ ] Đủ 14 bảng theo §03 của kế hoạch
- [ ] **Tiền lưu số nguyên đồng**, không bao giờ dùng số thực
- [ ] **Ngày thuê lưu kiểu date**, không phải mốc thời gian — thuê theo ngày thì giờ không
      có nghĩa, mà lại đẻ ra bệnh múi giờ
- [ ] Mốc hệ thống lưu UTC, hiển thị mới đổi sang giờ Việt Nam
- [ ] `car_availability` khoá chính `(car_id, day)` — đây chính là cơ chế chống đặt trùng
- [ ] Người khác kéo mã về, chạy một lệnh là có CSDL giống hệt

## US-20 · Đặt xe không bao giờ trùng ngày `Blocker`

Đừng kiểm tra rồi mới ghi — giữa hai bước đó là chỗ đơn thứ hai chen vào. Dùng thẳng khoá
chính `(car_id, day)` làm trọng tài: ai chèn được thì thắng, ai đụng khoá trùng thì thua.

- [ ] Chèn từng ngày, **khoá cả ngày trả**: thuê 12→15 là khoá bốn ngày 12, 13, 14, 15
- [ ] Đụng khoá trùng thì cuộn giao dịch, đơn biến mất, trả `SLOT_TAKEN`
- [ ] Đơn vào `CHO_CHU_XE`, hẹn 30 phút, **chưa thu tiền**
- [ ] Chặn khách đặt chính xe của mình
- [ ] Chưa nộp giấy tờ thì trả `KYC_REQUIRED`
- [ ] Huỷ đơn thì xoá dòng lịch bằng một câu, không có trạng thái trung gian nào để quên

**Demo ở Review**: hai người bấm đặt cùng lúc cùng ngày — một người thắng, một người nhận
`SLOT_TAKEN`, bảng lịch không có dòng mồ côi.

Chỉ khoá tới ngày 14 thì người khác đặt được ngày 15 trong khi xe còn chưa về.

## US-29 · Chốt đơn, ghi sổ cái, sinh 2 lệnh chi `Tiền — 2 người duyệt`

- [ ] **Số dư không lưu thành một cột** — nó là tổng của sổ cái
- [ ] Bút toán kép, chỉ ghi thêm, không sửa không xoá
- [ ] Sinh **hai** lệnh chi: một cho chủ xe, một hoàn cọc khách
- [ ] Đơn chỉ đóng khi cả hai lệnh đã chuyển xong

**Demo ở Review**: chạy ví dụ §05.5 ra đúng tổng nợ = tổng có = 4.800.000.

Cột số dư là chỗ mọi hệ thống tiền đều lệch sau vài tháng chạy, và không cách nào tìm lại
được lệch từ đâu.

## US-32 · Tác vụ nền dọn đơn treo `Blocker`

Thiếu là đơn treo vĩnh viễn và lịch xe bị khoá chết — lỗi kín, không ai thấy cho tới lúc chủ
xe hỏi vì sao xe mình không ai đặt nữa.

- [ ] Chủ xe im lặng quá 30 phút → hết hạn, nhả lịch
- [ ] Khách không chuyển tiền quá 30 phút → hết hạn, nhả lịch ngay để xe còn bán được
- [ ] Quá giờ nhận 24 giờ chưa có biên bản → huỷ, mất tiền thuê, **hoàn đủ cọc**
- [ ] Khách im lặng 24 giờ sau quyết toán → tự chốt, trừ khi đang có phản đối mở
- [ ] Mỗi 6 giờ: cảnh báo quá hạn chưa trả, nhắc bên soi chưa ký

## US-59 · Bên soi xem hết ảnh rồi mới được ký

Biên bản một chiều thì vô giá trị: bên nào lập cũng chỉ chụp cái mình muốn chụp.

- [ ] Ảnh hiện **to hết cỡ**, vuốt từng tấm
- [ ] **Nút ký chỉ bật sau khi đã xem hết 6 tấm** — nghe vụn vặt nhưng đây chính là chỗ
      quyết định biên bản có giá trị hay không; người ta ký bừa vì nút ký nằm ngay màn đầu
- [ ] Bên soi chụp bổ sung được vết bên lập chưa ghi
- [ ] Phản đối thì bắt buộc ghi lý do
- [ ] Nói rõ trên màn: ký là hết quyền khiếu nại về vết đó

## US-23 · Xác nhận đã nhận tiền `Tiền — 2 người duyệt`

- [ ] Hiện số tiền phải nhận và số **thực** nhận
- [ ] Chuyển thiếu thì **không tự xác nhận** — cờ lệch số tiền, gọi khách chuyển bù
- [ ] Chuyển thừa thì xác nhận đơn và sinh thêm lệnh hoàn phần thừa
- [ ] Ghi sai nội dung chuyển khoản → hàng chờ đối chiếu tay
- [ ] **Không bao giờ tự huỷ đơn khi đã có tiền vào**

---

# Definition of Ready

Story chỉ được kéo vào Sprint Backlog khi:

1. Viết đúng dạng "Là … tôi muốn … để …", ai đọc cũng hiểu vì sao cần
2. Có tiêu chí chấp nhận, đếm được, không mơ hồ
3. Đã ước lượng, và **không quá 8 điểm** — 13 điểm thì phải chẻ
4. Phụ thuộc đã xong hoặc nằm cùng sprint và có người nhận
5. Hợp đồng API liên quan đã có trong `docs/api.md`

# Definition of Done

Thẻ chỉ sang Done khi đủ cả sáu:

1. Chạy được trên nhánh chính sau khi kéo mã mới, không cần sửa gì thêm
2. Có ca kiểm thử của chính story đó, chạy đạt
3. Trường hợp lỗi có xử lý: mất mạng, dữ liệu rỗng, ảnh hỏng, bấm hai lần
4. Không còn TODO hay dữ liệu cắm cứng trong đường đi chính
5. Một người khác đã duyệt mã. **Đụng tới đơn hoặc tiền thì hai người duyệt**
6. Có ảnh chụp màn hoặc log dán vào thẻ làm bằng chứng

---

# Theo dõi tiến độ

Cuối mỗi sprint, Trà ghi vào thẻ **"Retro Sprint N"**:

```
Cam kết:     __ điểm
Hoàn thành:  __ điểm
Velocity:    __
Story trượt: US-__ vì __

Giữ:   
Bỏ:    
Thử:   
```

Velocity sprint 1 là con số để ước lượng sprint 2, không phải chỉ tiêu để so ai hơn ai.
Ước lượng lệch là chuyện bình thường ở sprint đầu — điều chỉnh, đừng đổ lỗi.

**Nợ kỹ thuật** phát sinh giữa sprint thì tạo thẻ nhãn `Nợ kỹ thuật` đưa vào Product Backlog,
đừng sửa lén trong story đang làm. Mỗi sprint dành khoảng 10% điểm để trả nợ.

---

# Quy ước kỹ thuật

**Nhánh**: `be/ten-viec`, `web/ten-viec`, `app/ten-viec`. Nhánh chính luôn chạy được.
Không đẩy thẳng lên nhánh chính, kể cả sửa một dòng.

**Commit** tiếng Việt không dấu, có phạm vi và mã story:
`be(booking): chan dat trung ngay [US-20]`

**Chia thư mục theo người** để không ai sửa cùng một tệp. Chỉ `docs/api.md` là tệp chung —
sửa nó thì báo nhóm ngay trong ngày, đừng đổi lặng lẽ rồi để người khác phát hiện lúc hợp nhất.

## Tài liệu — chia đều, không dồn cho ai

```
[Long] Sơ đồ CSDL + mô tả 14 bảng
[Linh] Sơ đồ tuần tự: đặt xe, thanh toán, quyết toán
[Trà] Sơ đồ ca sử dụng + ảnh chụp màn web
[Quỳnh] Sơ đồ lớp tầng dữ liệu app
[Vũ] Kịch bản demo 10 phút + quay video
```

## Kỷ luật bắt buộc khi giữ tiền người khác

Giữ 4–5 triệu của mỗi khách nặng hơn hẳn giữ vài trăm nghìn. Bốn thứ này không phải khuyến
nghị mà là điều kiện để chạy:

- Tài khoản riêng chỉ dùng cho dự án, có biên bản nội bộ ký giữa 5 thành viên
- Chi trả trong 24 giờ sau khi chốt đơn, không giữ tiền qua đêm lâu hơn
- Đối soát sao kê mỗi tối, lệch một đồng thì dừng lại tìm
- Chính sách hoàn tiền in rõ trong app trước khi khách bấm thanh toán

Và giới hạn quy mô ở mức người quen giới thiệu trong giai đoạn đầu — không phải vì phần mềm
yếu, mà vì đây là tiền thật của người thật.
