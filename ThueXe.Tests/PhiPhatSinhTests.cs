using ThueXe.Services;
using Xunit;

namespace ThueXe.Tests;

/// <summary>
/// Phí phát sinh là hàm thuần nên test được mà không cần CSDL, không cần backend chạy.
/// Mỗi ca ở đây bám đúng một dòng trong bảng phí của kế hoạch.
/// </summary>
public class PhiPhatSinhTests
{
    // Đơn mẫu: 600.000/ngày, thuê 3 ngày, giới hạn 300 km/ngày.
    private const long GiaMotNgay = 600_000;
    private const int SoNgay = 3;
    private const int GioiHanKm = 300;

    private static PhiPhatSinh Tinh(
        int odoGiao = 10_000, int odoTra = 10_000,
        int nlGiao = 8, int nlTra = 8, int treGio = 0)
        => PhiPhatSinhService.Tinh(GiaMotNgay, SoNgay, GioiHanKm,
                                   odoGiao, odoTra, nlGiao, nlTra, treGio);

    [Fact]
    public void Tra_dung_gio_dung_km_du_xang_thi_khong_mat_them_dong_nao()
    {
        var phi = Tinh(odoTra: 10_900, nlTra: 8);
        Assert.Equal(0, phi.Tong);
    }

    // ── Quá giờ ──────────────────────────────────────────────────────

    [Theory]
    [InlineData(1, 100_000)]
    [InlineData(3, 300_000)]
    [InlineData(4, 400_000)]
    public void Tre_duoi_5_tieng_thi_tinh_100k_moi_gio(int gio, long mongDoi)
    {
        Assert.Equal(mongDoi, Tinh(treGio: gio).QuaGio);
    }

    [Fact]
    public void Tre_dung_5_tieng_thi_nhay_sang_tron_mot_ngay()
    {
        // Mốc biên: 4 giờ tính 400.000, nhưng 5 giờ tính nguyên một ngày 600.000.
        Assert.Equal(400_000, Tinh(treGio: 4).QuaGio);
        Assert.Equal(GiaMotNgay, Tinh(treGio: 5).QuaGio);
    }

    [Fact]
    public void Tre_rat_nhieu_van_chi_tinh_mot_ngay()
    {
        Assert.Equal(GiaMotNgay, Tinh(treGio: 20).QuaGio);
    }

    [Fact]
    public void Tra_som_khong_duoc_hoan_nhung_cung_khong_bi_phat()
    {
        Assert.Equal(0, Tinh(treGio: -5).QuaGio);
    }

    // ── Quá km ───────────────────────────────────────────────────────

    [Fact]
    public void Chay_dung_gioi_han_thi_khong_mat_phi()
    {
        // 3 ngày × 300 km = 900 km.
        Assert.Equal(0, Tinh(odoTra: 10_900).QuaKm);
    }

    [Fact]
    public void Vuot_mot_km_da_tinh_tien()
    {
        Assert.Equal(5_000, Tinh(odoTra: 10_901).QuaKm);
    }

    [Fact]
    public void Vuot_30km_thi_150k()
    {
        Assert.Equal(150_000, Tinh(odoTra: 10_930).QuaKm);
    }

    [Fact]
    public void Odo_luc_tra_nho_hon_luc_giao_thi_coi_nhu_khong_chay()
    {
        // Người lập biên bản gõ nhầm số thì không được đẻ ra phí âm.
        Assert.Equal(0, Tinh(odoGiao: 10_000, odoTra: 9_000).QuaKm);
    }

    // ── Nhiên liệu ───────────────────────────────────────────────────

    [Fact]
    public void Tra_du_xang_thi_khong_mat_phi_nhien_lieu()
    {
        Assert.Equal(0, Tinh(nlGiao: 8, nlTra: 8).NhienLieu);
    }

    [Fact]
    public void Thieu_mot_nac_thi_120k_cong_100k_phi_do_ho()
    {
        Assert.Equal(220_000, Tinh(nlGiao: 8, nlTra: 7).NhienLieu);
    }

    [Fact]
    public void Thieu_ba_nac_thi_360k_cong_100k()
    {
        Assert.Equal(460_000, Tinh(nlGiao: 8, nlTra: 5).NhienLieu);
    }

    [Fact]
    public void Do_day_hon_luc_nhan_thi_khong_hoan_tien_nhung_cung_khong_tinh_phi()
    {
        Assert.Equal(0, Tinh(nlGiao: 5, nlTra: 8).NhienLieu);
    }

    // ── Cộng dồn ─────────────────────────────────────────────────────

    [Fact]
    public void Ba_loai_phi_cong_lai_dung_bang_tong()
    {
        var phi = Tinh(odoTra: 10_930, nlGiao: 8, nlTra: 7, treGio: 2);
        Assert.Equal(200_000, phi.QuaGio);
        Assert.Equal(150_000, phi.QuaKm);
        Assert.Equal(220_000, phi.NhienLieu);
        Assert.Equal(570_000, phi.Tong);
    }

    [Fact]
    public void Tien_luon_la_so_nguyen_dong_khong_bao_gio_ra_so_le()
    {
        var phi = Tinh(odoTra: 10_937, nlTra: 6, treGio: 3);
        Assert.Equal(phi.QuaGio + phi.QuaKm + phi.NhienLieu, phi.Tong);
        Assert.True(phi.Tong % 1000 == 0, $"Tổng {phi.Tong} phải chẵn nghìn");
    }
}
