using SQLite;
using System.Globalization;
using System.IO;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace CankiriKesifTakip;

public class GeciciKabulKayit
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string IsinAdi { get; set; } = string.Empty;
    public string Yuklenici { get; set; } = string.Empty;
    public string SozlesmeBedeli { get; set; } = string.Empty;
    public string BelgeTarihi { get; set; } = string.Empty;
    public string DosyaYolu { get; set; } = string.Empty;

    [Ignore]
    public string OzetBilgi => $"Bedel: {SozlesmeBedeli}  |  Belge Tarihi: {BelgeTarihi}";
}

public partial class KabulEvrakiPage : ContentPage
{
    private readonly SQLiteAsyncConnection db;

    public KabulEvrakiPage()
    {
        InitializeComponent();
        db = new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, "cankiri_gorevler.db3"));
        BelgeTarihiEntry.Text = DateTime.Now.ToString("dd.MM.yyyy");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ListeyiYukle();
    }

    private async Task ListeyiYukle()
    {
        await db.CreateTableAsync<GeciciKabulKayit>();
        var kayitlar = await db.Table<GeciciKabulKayit>().OrderByDescending(x => x.Id).ToListAsync();
        KabulListesiView.ItemsSource = kayitlar;
    }

    private static double SayiOku(string? metin)
    {
        if (string.IsNullOrWhiteSpace(metin))
            return 0;

        string temiz = metin.Trim().Replace("TL", "", StringComparison.OrdinalIgnoreCase).Replace("₺", "").Replace(" ", "");
        if (temiz.Contains(',') && temiz.Contains('.'))
        {
            if (temiz.LastIndexOf(',') > temiz.LastIndexOf('.'))
                temiz = temiz.Replace(".", "").Replace(',', '.');
            else
                temiz = temiz.Replace(",", "");
        }
        else
        {
            temiz = temiz.Replace(',', '.');
        }

        return double.TryParse(temiz, NumberStyles.Any, CultureInfo.InvariantCulture, out double sonuc) ? sonuc : 0;
    }

    private static string TamSayiYaziya(long sayi)
    {
        if (sayi == 0) return "sıfır";

        string[] birler = { "", "bir", "iki", "üç", "dört", "beş", "altı", "yedi", "sekiz", "dokuz" };
        string[] onlar = { "", "on", "yirmi", "otuz", "kırk", "elli", "altmış", "yetmiş", "seksen", "doksan" };
        string[] binler = { "", "bin", "milyon", "milyar", "trilyon" };

        string sonuc = "";
        int grupIndex = 0;

        while (sayi > 0)
        {
            int grup = (int)(sayi % 1000);
            if (grup > 0)
            {
                int yuz = grup / 100;
                int on = (grup % 100) / 10;
                int bir = grup % 10;

                string grupYazi = "";
                if (yuz == 1) grupYazi += "yüz";
                else if (yuz > 1) grupYazi += birler[yuz] + "yüz";

                grupYazi += onlar[on];

                if (!(grupIndex == 1 && grup == 1))
                    grupYazi += birler[bir];

                grupYazi += binler[grupIndex];
                sonuc = grupYazi + " " + sonuc;
            }
            sayi /= 1000;
            grupIndex++;
        }

        return sonuc.Trim();
    }

    private static string TutariYaziyaCevir(double tutar)
    {
        if (tutar <= 0)
            return string.Empty;

        long tamKisim = (long)Math.Floor(tutar);
        int kurusKisim = (int)Math.Round((tutar - tamKisim) * 100);

        string yazi = $"{TamSayiYaziya(tamKisim)} türk lirası";
        if (kurusKisim > 0)
            yazi += $" {TamSayiYaziya(kurusKisim)} kuruş";

        return yazi;
    }

    private void SozlesmeBedeliEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        double bedel = SayiOku(SozlesmeBedeliEntry.Text);
        if (bedel > 0)
        {
            SozlesmeBedeliYaziylaEntry.Text = TutariYaziyaCevir(bedel);
        }
    }

    private void IsinBitirildigiTarihEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DilekceTarihiEntry.Text))
        {
            DilekceTarihiEntry.Text = IsinBitirildigiTarihEntry.Text;
        }
    }

    private async void WordOlustur_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(IsinAdiEntry.Text) || string.IsNullOrWhiteSpace(YukleniciEntry.Text))
        {
            await DisplayAlert("Eksik Bilgi", "Lütfen İşin Adı ve Yüklenici bilgilerini doldurunuz.", "Tamam");
            return;
        }

        try
        {
            string isinAdi = IsinAdiEntry.Text.Trim();
            string yuklenici = YukleniciEntry.Text.Trim();
            string sozlesmeTarihi = SozlesmeTarihiEntry.Text?.Trim() ?? "";

            double bedelSayi = SayiOku(SozlesmeBedeliEntry.Text);
            string formatliBedel = bedelSayi > 0
                ? $"{bedelSayi.ToString("N2", new CultureInfo("tr-TR"))} TL"
                : (SozlesmeBedeliEntry.Text?.Trim() ?? "");

            string bedelYaziyla = SozlesmeBedeliYaziylaEntry.Text?.Trim() ?? "";
            string bedelTamMetin = string.IsNullOrWhiteSpace(bedelYaziyla)
                ? formatliBedel
                : $"{formatliBedel}   ({bedelYaziyla})";

            string isinSuresi = IsinSuresiEntry.Text?.Trim() ?? "";
            string bitimTarihi = SozlesmeBitimTarihiEntry.Text?.Trim() ?? "";
            string sureUzatimlari = string.IsNullOrWhiteSpace(SureUzatimlariEntry.Text) ? "-" : SureUzatimlariEntry.Text.Trim();
            string sureUzatimliBitim = string.IsNullOrWhiteSpace(SureUzatimliBitimTarihiEntry.Text) ? "-" : SureUzatimliBitimTarihiEntry.Text.Trim();
            string bitirildigiTarih = IsinBitirildigiTarihEntry.Text?.Trim() ?? "";
            string dilekceTarihi = string.IsNullOrWhiteSpace(DilekceTarihiEntry.Text) ? bitirildigiTarih : DilekceTarihiEntry.Text.Trim();
            string belgeTarihi = string.IsNullOrWhiteSpace(BelgeTarihiEntry.Text) ? DateTime.Now.ToString("dd.MM.yyyy") : BelgeTarihiEntry.Text.Trim();
            string kurumAdi = string.IsNullOrWhiteSpace(KurumAdiEntry.Text) ? "Milli Eğitim Müdürlüğüne" : KurumAdiEntry.Text.Trim();

            string g1Ad = Gorevli1AdEntry.Text?.Trim() ?? "";
            string g1Unvan = Gorevli1UnvanEntry.Text?.Trim() ?? "";
            string g2Ad = Gorevli2AdEntry.Text?.Trim() ?? "";
            string g2Unvan = Gorevli2UnvanEntry.Text?.Trim() ?? "";
            string g3Ad = Gorevli3AdEntry.Text?.Trim() ?? "";
            string g3Unvan = Gorevli3UnvanEntry.Text?.Trim() ?? "";

            string klasor = Path.Combine(FileSystem.AppDataDirectory, "GeciciKabulBelgeleri");
            Directory.CreateDirectory(klasor);
            string yeniDosyaAdi = $"GeciciKabul_Ek1_{DateTime.Now:yyyyMMdd_HHmmss}.docx";
            string hedefYol = Path.Combine(klasor, yeniDosyaAdi);

            using (var doc = DocX.Create(hedefYol))
            {
                doc.MarginTop = 50f;
                doc.MarginBottom = 50f;
                doc.MarginLeft = 55f;
                doc.MarginRight = 55f;

                var pEk = doc.InsertParagraph("Ek - 1").Font("Times New Roman").FontSize(12).Bold();
                pEk.Alignment = Alignment.center;

                doc.InsertParagraph().SpacingAfter(6);

                var pBaslik = doc.InsertParagraph("YAPIM İŞLERİ GEÇİCİ KABUL TEKLİF BELGESİ").Font("Times New Roman").FontSize(12).Bold();
                pBaslik.Alignment = Alignment.center;

                doc.InsertParagraph().SpacingAfter(12);

                // Üst Bilgi Tablosu
                var satirlar = new (string Etiket, string Deger)[]
                {
                    ("İşin adı", isinAdi),
                    ("Yüklenicinin adı/ticari unvanı", yuklenici),
                    ("Sözleşme tarihi", sozlesmeTarihi),
                    ("Sözleşme bedeli", bedelTamMetin),
                    ("Sözleşmeye göre işin süresi (takvim günü)", isinSuresi),
                    ("Sözleşmeye göre işin bitirilmesi gereken tarih", bitimTarihi),
                    ("Varsa süre uzatımları", sureUzatimlari),
                    ("Süre uzatımı dahil işin bitirilmesi gereken tarih", sureUzatimliBitim),
                    ("İşin bitirildiği tarih", bitirildigiTarih)
                };

                var bilgiTablo = doc.AddTable(satirlar.Length, 3);
                bilgiTablo.Design = TableDesign.TableNormal;
                bilgiTablo.SetWidths(new float[] { 235f, 15f, 235f });

                for (int i = 0; i < satirlar.Length; i++)
                {
                    bilgiTablo.Rows[i].Cells[0].Paragraphs[0].Append(satirlar[i].Etiket).Font("Times New Roman").FontSize(11);
                    bilgiTablo.Rows[i].Cells[1].Paragraphs[0].Append(":").Font("Times New Roman").FontSize(11);
                    bilgiTablo.Rows[i].Cells[2].Paragraphs[0].Append(satirlar[i].Deger).Font("Times New Roman").FontSize(11);
                }

                doc.InsertTable(bilgiTablo);
                doc.InsertParagraph().SpacingAfter(16);

                // Ana Metin Paragrafı
                string anaMetin = $"\tÇankırı ili {isinAdi} nin bitirildiğine ilişkin yüklenici {yuklenici}’nin {kurumAdi} vermiş olduğu {dilekceTarihi} tarihli dilekçesi üzerine yukarıda yazılı işin ön incelemesi {belgeTarihi} tarihinde tarafımızdan yapılmış, işin sözleşmesine uygun olarak tamamlandığı ve geçici kabule hazır olduğu tespit edilmiştir.";
                var pMetin = doc.InsertParagraph(anaMetin).Font("Times New Roman").FontSize(11);
                pMetin.Alignment = Alignment.both;
                pMetin.LineSpacing = 16f;

                doc.InsertParagraph().SpacingAfter(6);

                var pArz = doc.InsertParagraph("\tGereğinin yapılmasını arz ederiz.").Font("Times New Roman").FontSize(11);
                pArz.Alignment = Alignment.left;

                doc.InsertParagraph().SpacingAfter(14);

                var pTarih = doc.InsertParagraph($"Tarih: {belgeTarihi}").Font("Times New Roman").FontSize(11);
                pTarih.Alignment = Alignment.right;

                doc.InsertParagraph().SpacingAfter(14);

                // Görevli ve İmza Tablosu
                var imzaTablo = doc.AddTable(4, 4);
                imzaTablo.Design = TableDesign.TableNormal;
                imzaTablo.SetWidths(new float[] { 105f, 125f, 125f, 130f });

                imzaTablo.Rows[0].Cells[0].Paragraphs[0].Append("Görevli veya görevlilerin").Font("Times New Roman").FontSize(11).Bold();

                imzaTablo.Rows[1].Cells[0].Paragraphs[0].Append("Adı Soyadı").Font("Times New Roman").FontSize(11);
                imzaTablo.Rows[1].Cells[1].Paragraphs[0].Append($": {g1Ad}").Font("Times New Roman").FontSize(11);
                imzaTablo.Rows[1].Cells[2].Paragraphs[0].Append(g2Ad).Font("Times New Roman").FontSize(11);
                imzaTablo.Rows[1].Cells[3].Paragraphs[0].Append(g3Ad).Font("Times New Roman").FontSize(11);

                imzaTablo.Rows[2].Cells[0].Paragraphs[0].Append("Görev unvanı").Font("Times New Roman").FontSize(11);
                imzaTablo.Rows[2].Cells[1].Paragraphs[0].Append($": {g1Unvan}").Font("Times New Roman").FontSize(10);
                imzaTablo.Rows[2].Cells[2].Paragraphs[0].Append(g2Unvan).Font("Times New Roman").FontSize(10);
                imzaTablo.Rows[2].Cells[3].Paragraphs[0].Append(g3Unvan).Font("Times New Roman").FontSize(10);

                imzaTablo.Rows[3].Cells[0].Paragraphs[0].Append("İmzası").Font("Times New Roman").FontSize(11);
                imzaTablo.Rows[3].Cells[1].Paragraphs[0].Append(":").Font("Times New Roman").FontSize(11);

                doc.InsertTable(imzaTablo);
                doc.Save();
            }

            await db.InsertAsync(new GeciciKabulKayit
            {
                IsinAdi = isinAdi,
                Yuklenici = yuklenici,
                SozlesmeBedeli = formatliBedel,
                BelgeTarihi = belgeTarihi,
                DosyaYolu = hedefYol
            });

            await ListeyiYukle();

            await Launcher.Default.OpenAsync(new OpenFileRequest("Geçici Kabul Teklif Belgesi", new ReadOnlyFile(hedefYol)));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Belge oluşturulurken bir hata oluştu:\n{ex.Message}", "Tamam");
        }
    }

    private async void KayitliWordAc_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string dosyaYolu && File.Exists(dosyaYolu))
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest("Geçici Kabul Teklif Belgesi", new ReadOnlyFile(dosyaYolu)));
        }
        else
        {
            await DisplayAlert("Uyarı", "Kayıtlı Word dosyası bulunamadı.", "Tamam");
        }
    }

    private async void KayitSil_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is GeciciKabulKayit kayit)
        {
            bool onay = await DisplayAlert("Silme Onayı",
                $"'{kayit.IsinAdi}' belgesini silmek istediğinize emin misiniz?",
                "Evet, Sil", "İptal");

            if (!onay)
                return;

            try
            {
                if (!string.IsNullOrWhiteSpace(kayit.DosyaYolu) && File.Exists(kayit.DosyaYolu))
                    File.Delete(kayit.DosyaYolu);
            }
            catch { }

            await db.DeleteAsync(kayit);
            await ListeyiYukle();
        }
    }

    private async void TumunuSil_Clicked(object sender, EventArgs e)
    {
        var kayitlar = await db.Table<GeciciKabulKayit>().ToListAsync();
        if (kayitlar.Count == 0)
        {
            await DisplayAlert("Bilgi", "Listede silinecek kayıt bulunmuyor.", "Tamam");
            return;
        }

        bool onay = await DisplayAlert("Listeyi Temizle",
            "Oluşturulan tüm geçici kabul kayıtları ve Word belgeleri silinecek. Emin misiniz?",
            "Evet, Tümünü Sil", "İptal");

        if (!onay)
            return;

        foreach (var kayit in kayitlar)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(kayit.DosyaYolu) && File.Exists(kayit.DosyaYolu))
                    File.Delete(kayit.DosyaYolu);
            }
            catch { }
        }

        await db.DeleteAllAsync<GeciciKabulKayit>();
        await ListeyiYukle();
    }
}