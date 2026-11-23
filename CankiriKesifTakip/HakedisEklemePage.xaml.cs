using ClosedXML.Excel;
using SQLite;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using Xceed.Words.NET;

namespace CankiriKesifTakip;

public class ImalatKalem
{
    public int SiraNo { get; set; }
    public string PozNo { get; set; } = string.Empty;
    public string YapilanIs { get; set; } = string.Empty;
    public string Olcu { get; set; } = string.Empty;
    public double SozlesmedekiYuzde { get; set; }
    public double GerceklesenToplamYuzde { get; set; }
    public double OncekiHakedisYuzde { get; set; }
    public double BuHakedisteEklenenYuzde => GerceklesenToplamYuzde - OncekiHakedisYuzde;

    public string BaslikSatiri => $"{SiraNo}. {(string.IsNullOrWhiteSpace(PozNo) ? "" : $"[{PozNo}] ")}{YapilanIs} ({Olcu})";
    public string YuzdeDetay => $"Sözleşme: %{SozlesmedekiYuzde:N6}  |  Gerçekleşen: %{GerceklesenToplamYuzde:N6}  |  Önceki: %{OncekiHakedisYuzde:N6}  |  Bu Hakediş: %{BuHakedisteEklenenYuzde:N6}";
}

public class HakedisKayit
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }
    public string IsinAdi { get; set; } = string.Empty;
    public string Yuklenici { get; set; } = string.Empty;
    public double SozlesmeBedeli { get; set; }
    public string HakedisNo { get; set; } = string.Empty;
    public string HakedisTarihi { get; set; } = string.Empty;
    public string DosyaYolu { get; set; } = string.Empty;

    [Ignore]
    public string OzetBilgi => $"{SozlesmeBedeli.ToString("N2", new CultureInfo("tr-TR"))} TL  |  Hakediş No: {HakedisNo}  |  Tarih: {HakedisTarihi}";
}

public partial class HakedisEklemePage : ContentPage
{
    private readonly SQLiteAsyncConnection db;
    private readonly ObservableCollection<ImalatKalem> insaatKalemleri = new();
    private readonly ObservableCollection<ImalatKalem> elektrikKalemleri = new();
    private readonly ObservableCollection<ImalatKalem> mekanikKalemleri = new();

    public HakedisEklemePage()
    {
        InitializeComponent();
        db = new SQLiteAsyncConnection(Path.Combine(FileSystem.AppDataDirectory, "cankiri_gorevler.db3"));
        HakedisTarihiEntry.Text = DateTime.Now.ToString("dd.MM.yyyy");
        InsaatKalemlerView.ItemsSource = insaatKalemleri;
        ElektrikKalemlerView.ItemsSource = elektrikKalemleri;
        MekanikKalemlerView.ItemsSource = mekanikKalemleri;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ListeyiYukle();
        IcmalOzetiniGuncelle();
    }

    private async Task ListeyiYukle()
    {
        await db.CreateTableAsync<HakedisKayit>();
        var kayitlar = await db.Table<HakedisKayit>().OrderByDescending(x => x.Id).ToListAsync();
        HakedisListesiView.ItemsSource = kayitlar;
    }

    private static double SayiOku(string? metin)
    {
        if (string.IsNullOrWhiteSpace(metin))
            return 0;

        string temiz = metin.Trim().Replace("%", "").Replace(" ", "");
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

    private static double ExcelHucreSayiOku(IXLCell hucre)
    {
        if (hucre.Value.IsNumber)
            return (double)hucre.Value;

        return SayiOku(hucre.Value.ToString());
    }

    private static void TarihVeyaMetinYaz(IXLCell hucre, string? deger)
    {
        if (string.IsNullOrWhiteSpace(deger))
            return;

        if (DateTime.TryParseExact(deger.Trim(), new[] { "dd.MM.yyyy", "dd/MM/yyyy", "d.M.yyyy" },
            new CultureInfo("tr-TR"), DateTimeStyles.None, out DateTime tarih))
        {
            hucre.Value = tarih;
        }
        else
        {
            hucre.Value = deger.Trim();
        }
    }

    private void MaliyetEntry_TextChanged(object sender, TextChangedEventArgs e)
    {
        IcmalOzetiniGuncelle();
    }

    private void IcmalOzetiniGuncelle()
    {
        var tr = new CultureInfo("tr-TR");
        double bedel = SayiOku(MaliyetEntry.Text);

        double insaatYuzde = insaatKalemleri.Sum(x => x.BuHakedisteEklenenYuzde);
        double elkYuzde = elektrikKalemleri.Sum(x => x.BuHakedisteEklenenYuzde);
        double mekYuzde = mekanikKalemleri.Sum(x => x.BuHakedisteEklenenYuzde);

        double insaatTutar = (bedel * insaatYuzde) / 100.0;
        double elkTutar = (bedel * elkYuzde) / 100.0;
        double mekTutar = (bedel * mekYuzde) / 100.0;

        double toplamYuzde = insaatYuzde + elkYuzde + mekYuzde;
        double toplamTutar = insaatTutar + elkTutar + mekTutar;

        IcmalInsaatLabel.Text = $"1. İnşaat İmalatları: %{insaatYuzde:N4}  |  Bu Hakediş: {insaatTutar.ToString("N2", tr)} TL";
        IcmalElektrikLabel.Text = $"2. Elektrik İmalatları: %{elkYuzde:N4}  |  Bu Hakediş: {elkTutar.ToString("N2", tr)} TL";
        IcmalMekanikLabel.Text = $"3. Mekanik Tesisat: %{mekYuzde:N4}  |  Bu Hakediş: {mekTutar.ToString("N2", tr)} TL";
        IcmalToplamLabel.Text = $"ANA GRUP TOPLAMI: %{toplamYuzde:N4}  |  Bu Hakediş Tutarı: {toplamTutar.ToString("N2", tr)} TL";
    }

    private static void SiraNumaralariniYenile(ObservableCollection<ImalatKalem> liste, CollectionView view)
    {
        for (int i = 0; i < liste.Count; i++)
        {
            liste[i].SiraNo = i + 1;
        }
        view.ItemsSource = null;
        view.ItemsSource = liste;
    }

    // ==================== EXCEL & WORD TOPLU AKTARIM METOTLARI ====================

    private static bool YasakliSatirMi(string metin)
    {
        string buyuk = metin.Trim().ToUpper(new CultureInfo("tr-TR"));
        return buyuk == "TOPLAM"
            || buyuk.StartsWith("TOPLAM ")
            || buyuk.Contains("ANA GRUP")
            || buyuk.Contains("YÜKLENİCİ")
            || buyuk.Contains("KONTROL")
            || buyuk.Contains("YAPILAN İŞLER LİSTESİ")
            || buyuk.Contains("ANAHTAR TESLİMİ");
    }

    private static List<ImalatKalem> ExcelSayfasindanKalemOku(IXLWorksheet ws)
    {
        var sonuc = new List<ImalatKalem>();
        int sonSatir = ws.LastRowUsed()?.RowNumber() ?? 0;
        int sonSutun = ws.LastColumnUsed()?.ColumnNumber() ?? 0;
        if (sonSatir == 0 || sonSutun == 0)
            return sonuc;

        var tr = new CultureInfo("tr-TR");
        int baslikSatiri = -1;
        int colPoz = -1, colIs = -1, colOlcu = -1, colSozlesme = -1, colGerceklesen = -1, colOnceki = -1;

        for (int r = 1; r <= Math.Min(15, sonSatir); r++)
        {
            for (int c = 1; c <= sonSutun; c++)
            {
                string val = ws.Cell(r, c).Value.ToString().Trim().ToLower(tr);
                if (val.Contains("yapılan iş") || val.Contains("imalat") || val.Contains("işin cinsi") || val.Contains("açıklama"))
                {
                    baslikSatiri = r;
                    colIs = c;
                    break;
                }
            }
            if (baslikSatiri != -1)
            {
                for (int c = 1; c <= sonSutun; c++)
                {
                    string val = ws.Cell(baslikSatiri, c).Value.ToString().Trim().ToLower(tr);
                    if (val.Contains("poz")) colPoz = c;
                    else if (val.Contains("ölçü") || val.Contains("birim")) colOlcu = c;
                    else if (val.Contains("sözleşme") || val.Contains("yüzde") || val.Contains("oran"))
                    {
                        if (colSozlesme == -1) colSozlesme = c;
                    }
                    else if (val.Contains("gerçekleşen")) colGerceklesen = c;
                    else if (val.Contains("önceki")) colOnceki = c;
                }
                break;
            }
        }

        int veriBaslangic = baslikSatiri != -1 ? baslikSatiri + 1 : 1;

        if (colIs == -1)
        {
            // Başlık bulunamazsa standart Hakediş iç sayfa (B=Poz, C=Yapılan İş, D=Ölçü, E=Sözleşme, F=Gerçekleşen, G=Önceki)
            // veya 4 sütunlu tablo (A=Poz, B=Yapılan İş, C=Ölçü, D=Yüzde) düzenini uygula
            if (sonSutun >= 5)
            {
                colPoz = 2; colIs = 3; colOlcu = 4; colSozlesme = 5;
                colGerceklesen = sonSutun >= 6 ? 6 : 5;
                colOnceki = sonSutun >= 7 ? 7 : -1;
            }
            else
            {
                colPoz = 1; colIs = 2; colOlcu = 3; colSozlesme = 4;
                colGerceklesen = 4; colOnceki = -1;
            }
        }

        for (int r = veriBaslangic; r <= sonSatir; r++)
        {
            string ilkHucre = ws.Cell(r, 1).Value.ToString().Trim();
            string poz = colPoz > 0 ? ws.Cell(r, colPoz).Value.ToString().Trim() : "";
            string yapilanIs = colIs > 0 ? ws.Cell(r, colIs).Value.ToString().Trim() : "";

            if (YasakliSatirMi(ilkHucre) || YasakliSatirMi(poz) || YasakliSatirMi(yapilanIs))
            {
                if (ilkHucre.Equals("TOPLAM", StringComparison.OrdinalIgnoreCase))
                    break;
                continue;
            }

            if (string.IsNullOrWhiteSpace(yapilanIs))
                continue;

            string olcu = colOlcu > 0 ? ws.Cell(r, colOlcu).Value.ToString().Trim() : "";
            double sozlesme = colSozlesme > 0 ? ExcelHucreSayiOku(ws.Cell(r, colSozlesme)) : 0;
            double gerceklesen = colGerceklesen > 0 ? ExcelHucreSayiOku(ws.Cell(r, colGerceklesen)) : sozlesme;
            double onceki = colOnceki > 0 ? ExcelHucreSayiOku(ws.Cell(r, colOnceki)) : 0;

            sonuc.Add(new ImalatKalem
            {
                PozNo = poz,
                YapilanIs = yapilanIs,
                Olcu = olcu,
                SozlesmedekiYuzde = sozlesme,
                GerceklesenToplamYuzde = gerceklesen,
                OncekiHakedisYuzde = onceki
            });
        }

        return sonuc;
    }

    private static List<ImalatKalem> WordTablodanKalemOku(DocX doc)
    {
        var sonuc = new List<ImalatKalem>();
        if (doc.Tables == null || doc.Tables.Count == 0)
            return sonuc;

        var tr = new CultureInfo("tr-TR");

        // İçinde "Yapılan İş" veya "Poz" geçen en kapsamlı tabloyu seç
        var hedefTablo = doc.Tables
            .OrderByDescending(t => t.Rows.Any(r => r.Cells.Any(c =>
            {
                string txt = string.Join(" ", c.Paragraphs.Select(p => p.Text)).ToLower(tr);
                return txt.Contains("yapılan iş") || txt.Contains("poz") || txt.Contains("imalat");
            })))
            .ThenByDescending(t => t.RowCount)
            .First();

        int baslikSatiri = -1;
        int colPoz = -1, colIs = -1, colOlcu = -1, colSozlesme = -1, colGerceklesen = -1, colOnceki = -1;

        for (int r = 0; r < Math.Min(10, hedefTablo.RowCount); r++)
        {
            var row = hedefTablo.Rows[r];
            for (int c = 0; c < row.Cells.Count; c++)
            {
                string val = string.Join(" ", row.Cells[c].Paragraphs.Select(p => p.Text)).Trim().ToLower(tr);
                if (val.Contains("yapılan iş") || val.Contains("imalat") || val.Contains("işin cinsi") || val.Contains("açıklama"))
                {
                    baslikSatiri = r;
                    colIs = c;
                    break;
                }
            }
            if (baslikSatiri != -1)
            {
                for (int c = 0; c < row.Cells.Count; c++)
                {
                    string val = string.Join(" ", row.Cells[c].Paragraphs.Select(p => p.Text)).Trim().ToLower(tr);
                    if (val.Contains("poz")) colPoz = c;
                    else if (val.Contains("ölçü") || val.Contains("birim")) colOlcu = c;
                    else if (val.Contains("sözleşme") || val.Contains("yüzde") || val.Contains("oran"))
                    {
                        if (colSozlesme == -1) colSozlesme = c;
                    }
                    else if (val.Contains("gerçekleşen")) colGerceklesen = c;
                    else if (val.Contains("önceki")) colOnceki = c;
                }
                break;
            }
        }

        int veriBaslangic = baslikSatiri != -1 ? baslikSatiri + 1 : 0;

        for (int r = veriBaslangic; r < hedefTablo.RowCount; r++)
        {
            var cells = hedefTablo.Rows[r].Cells;
            if (cells.Count < 2)
                continue;

            int cPoz = colPoz, cIs = colIs, cOlcu = colOlcu, cSoz = colSozlesme, cGer = colGerceklesen, cOnc = colOnceki;
            if (cIs == -1)
            {
                if (cells.Count >= 5)
                {
                    cPoz = 1; cIs = 2; cOlcu = 3; cSoz = 4;
                    cGer = cells.Count >= 6 ? 5 : 4;
                    cOnc = cells.Count >= 7 ? 6 : -1;
                }
                else
                {
                    cPoz = 0; cIs = 1;
                    cOlcu = cells.Count >= 3 ? 2 : -1;
                    cSoz = cells.Count >= 4 ? 3 : -1;
                    cGer = cSoz; cOnc = -1;
                }
            }

            string ilk = string.Join(" ", cells[0].Paragraphs.Select(p => p.Text)).Trim();
            string poz = (cPoz >= 0 && cPoz < cells.Count) ? string.Join(" ", cells[cPoz].Paragraphs.Select(p => p.Text)).Trim() : "";
            string yapilanIs = (cIs >= 0 && cIs < cells.Count) ? string.Join(" ", cells[cIs].Paragraphs.Select(p => p.Text)).Trim() : "";

            if (YasakliSatirMi(ilk) || YasakliSatirMi(poz) || YasakliSatirMi(yapilanIs))
            {
                if (ilk.Equals("TOPLAM", StringComparison.OrdinalIgnoreCase))
                    break;
                continue;
            }

            if (string.IsNullOrWhiteSpace(yapilanIs))
                continue;

            string olcu = (cOlcu >= 0 && cOlcu < cells.Count) ? string.Join(" ", cells[cOlcu].Paragraphs.Select(p => p.Text)).Trim() : "";
            double sozlesme = (cSoz >= 0 && cSoz < cells.Count) ? SayiOku(string.Join(" ", cells[cSoz].Paragraphs.Select(p => p.Text))) : 0;
            double gerceklesen = (cGer >= 0 && cGer < cells.Count) ? SayiOku(string.Join(" ", cells[cGer].Paragraphs.Select(p => p.Text))) : sozlesme;
            double onceki = (cOnc >= 0 && cOnc < cells.Count) ? SayiOku(string.Join(" ", cells[cOnc].Paragraphs.Select(p => p.Text))) : 0;

            sonuc.Add(new ImalatKalem
            {
                PozNo = poz,
                YapilanIs = yapilanIs,
                Olcu = olcu,
                SozlesmedekiYuzde = sozlesme,
                GerceklesenToplamYuzde = gerceklesen,
                OncekiHakedisYuzde = onceki
            });
        }

        return sonuc;
    }

    private async Task ExceldenGrubaAktar(ObservableCollection<ImalatKalem> hedefListe, CollectionView hedefView, string grupAdi)
    {
        try
        {
            var excelType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".xlsx" } },
                { DevicePlatform.Android, new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } },
                { DevicePlatform.iOS, new[] { "org.openxmlformats.spreadsheetml.sheet" } },
                { DevicePlatform.MacCatalyst, new[] { "org.openxmlformats.spreadsheetml.sheet" } }
            });

            var dosya = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = $"{grupAdi} için Excel (.xlsx) Dosyası Seçin",
                FileTypes = excelType
            });

            if (dosya == null)
                return;

            using var stream = await dosya.OpenReadAsync();
            using var workbook = new XLWorkbook(stream);

            var tr = new CultureInfo("tr-TR");
            IXLWorksheet? secilenSayfa = null;

            foreach (var ws in workbook.Worksheets)
            {
                string ad = ws.Name.ToLower(tr);
                if ((grupAdi.Contains("İnşaat") && (ad.Contains("inşaat") || ad.Contains("insaat"))) ||
                    (grupAdi.Contains("Elektrik") && (ad.Contains("elk") || ad.Contains("elektrik"))) ||
                    (grupAdi.Contains("Mekanik") && (ad.Contains("mekanik"))))
                {
                    secilenSayfa = ws;
                    break;
                }
            }

            secilenSayfa ??= workbook.Worksheets.First();
            var okunanlar = ExcelSayfasindanKalemOku(secilenSayfa);

            if (okunanlar.Count == 0)
            {
                await DisplayAlert("Uyarı", $"'{secilenSayfa.Name}' sayfasında aktarılacak uygun imalat satırı bulunamadı.", "Tamam");
                return;
            }

            foreach (var kalem in okunanlar)
            {
                hedefListe.Add(kalem);
            }

            SiraNumaralariniYenile(hedefListe, hedefView);
            IcmalOzetiniGuncelle();
            await DisplayAlert("Başarılı", $"{grupAdi} listesine {okunanlar.Count} kalem toplu olarak eklendi.", "Tamam");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Excel dosyası okunurken hata oluştu:\n{ex.Message}", "Tamam");
        }
    }

    private async Task WorddenGrubaAktar(ObservableCollection<ImalatKalem> hedefListe, CollectionView hedefView, string grupAdi)
    {
        try
        {
            var wordType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".docx" } },
                { DevicePlatform.Android, new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document" } },
                { DevicePlatform.iOS, new[] { "org.openxmlformats.wordprocessingml.document" } },
                { DevicePlatform.MacCatalyst, new[] { "org.openxmlformats.wordprocessingml.document" } }
            });

            var dosya = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = $"{grupAdi} için Word (.docx) Dosyası Seçin",
                FileTypes = wordType
            });

            if (dosya == null)
                return;

            using var stream = await dosya.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var doc = DocX.Load(memoryStream);
            var okunanlar = WordTablodanKalemOku(doc);

            if (okunanlar.Count == 0)
            {
                await DisplayAlert("Uyarı", "Word belgesindeki tablolarda aktarılacak uygun imalat satırı bulunamadı.", "Tamam");
                return;
            }

            foreach (var kalem in okunanlar)
            {
                hedefListe.Add(kalem);
            }

            SiraNumaralariniYenile(hedefListe, hedefView);
            IcmalOzetiniGuncelle();
            await DisplayAlert("Başarılı", $"{grupAdi} listesine {okunanlar.Count} kalem Word belgesinden eklendi.", "Tamam");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Word dosyası okunurken hata oluştu:\n{ex.Message}", "Tamam");
        }
    }

    private async void TumGruplariExceldenAktar_Clicked(object sender, EventArgs e)
    {
        try
        {
            var excelType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".xlsx" } },
                { DevicePlatform.Android, new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" } },
                { DevicePlatform.iOS, new[] { "org.openxmlformats.spreadsheetml.sheet" } },
                { DevicePlatform.MacCatalyst, new[] { "org.openxmlformats.spreadsheetml.sheet" } }
            });

            var dosya = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Tüm Grupları İçeren Excel (.xlsx) Dosyasını Seçin",
                FileTypes = excelType
            });

            if (dosya == null)
                return;

            using var stream = await dosya.OpenReadAsync();
            using var workbook = new XLWorkbook(stream);
            var tr = new CultureInfo("tr-TR");

            int insaatSayi = 0, elkSayi = 0, mekSayi = 0;

            foreach (var ws in workbook.Worksheets)
            {
                string ad = ws.Name.ToLower(tr);
                if (ad.Contains("inşaat") || ad.Contains("insaat"))
                {
                    var liste = ExcelSayfasindanKalemOku(ws);
                    foreach (var k in liste) insaatKalemleri.Add(k);
                    insaatSayi += liste.Count;
                }
                else if (ad.Contains("elk") || ad.Contains("elektrik"))
                {
                    var liste = ExcelSayfasindanKalemOku(ws);
                    foreach (var k in liste) elektrikKalemleri.Add(k);
                    elkSayi += liste.Count;
                }
                else if (ad.Contains("mekanik"))
                {
                    var liste = ExcelSayfasindanKalemOku(ws);
                    foreach (var k in liste) mekanikKalemleri.Add(k);
                    mekSayi += liste.Count;
                }
            }

            SiraNumaralariniYenile(insaatKalemleri, InsaatKalemlerView);
            SiraNumaralariniYenile(elektrikKalemleri, ElektrikKalemlerView);
            SiraNumaralariniYenile(mekanikKalemleri, MekanikKalemlerView);
            IcmalOzetiniGuncelle();

            if (insaatSayi == 0 && elkSayi == 0 && mekSayi == 0)
            {
                await DisplayAlert("Bilgi", "Dosyada 'İNŞAATÖDE', 'elk' veya 'mekanik' adlı sayfa ya da veri bulunamadı. Grup içindeki 'Excel'den Toplu Ekle' butonlarını kullanabilirsiniz.", "Tamam");
            }
            else
            {
                await DisplayAlert("Toplu Aktarım Tamamlandı",
                    $"Excel dosyasından aktarılan kalemler:\n• İnşaat İmalatları: {insaatSayi} satır\n• Elektrik İmalatları: {elkSayi} satır\n• Mekanik Tesisat: {mekSayi} satır",
                    "Tamam");
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Excel dosyası okunurken hata oluştu:\n{ex.Message}", "Tamam");
        }
    }

    private async void InsaatExceldenEkle_Clicked(object sender, EventArgs e) =>
        await ExceldenGrubaAktar(insaatKalemleri, InsaatKalemlerView, "İnşaat İmalatları");

    private async void InsaatWorddenEkle_Clicked(object sender, EventArgs e) =>
        await WorddenGrubaAktar(insaatKalemleri, InsaatKalemlerView, "İnşaat İmalatları");

    private async void ElektrikExceldenEkle_Clicked(object sender, EventArgs e) =>
        await ExceldenGrubaAktar(elektrikKalemleri, ElektrikKalemlerView, "Elektrik İmalatları");

    private async void ElektrikWorddenEkle_Clicked(object sender, EventArgs e) =>
        await WorddenGrubaAktar(elektrikKalemleri, ElektrikKalemlerView, "Elektrik İmalatları");

    private async void MekanikExceldenEkle_Clicked(object sender, EventArgs e) =>
        await ExceldenGrubaAktar(mekanikKalemleri, MekanikKalemlerView, "Mekanik Tesisat İmalatları");

    private async void MekanikWorddenEkle_Clicked(object sender, EventArgs e) =>
        await WorddenGrubaAktar(mekanikKalemleri, MekanikKalemlerView, "Mekanik Tesisat İmalatları");

    private void InsaatListesiniTemizle_Clicked(object sender, EventArgs e)
    {
        insaatKalemleri.Clear();
        IcmalOzetiniGuncelle();
    }

    private void ElektrikListesiniTemizle_Clicked(object sender, EventArgs e)
    {
        elektrikKalemleri.Clear();
        IcmalOzetiniGuncelle();
    }

    private void MekanikListesiniTemizle_Clicked(object sender, EventArgs e)
    {
        mekanikKalemleri.Clear();
        IcmalOzetiniGuncelle();
    }

    // ==================== TEK SATIR EKLEME / SİLME METOTLARI ====================

    private async void InsaatKalemEkle_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(YapilanIsEntry.Text))
        {
            await DisplayAlert("Eksik Bilgi", "Lütfen İnşaat İmalatı için 'Yapılan İş' tanımını giriniz.", "Tamam");
            return;
        }

        insaatKalemleri.Add(new ImalatKalem
        {
            SiraNo = insaatKalemleri.Count + 1,
            PozNo = PozNoEntry.Text?.Trim() ?? "",
            YapilanIs = YapilanIsEntry.Text.Trim(),
            Olcu = OlcuEntry.Text?.Trim() ?? "",
            SozlesmedekiYuzde = SayiOku(SozlesmeYuzdeEntry.Text),
            GerceklesenToplamYuzde = SayiOku(GerceklesenYuzdeEntry.Text),
            OncekiHakedisYuzde = SayiOku(OncekiYuzdeEntry.Text)
        });

        PozNoEntry.Text = string.Empty;
        YapilanIsEntry.Text = string.Empty;
        OlcuEntry.Text = string.Empty;
        SozlesmeYuzdeEntry.Text = string.Empty;
        GerceklesenYuzdeEntry.Text = string.Empty;
        OncekiYuzdeEntry.Text = "0";
        IcmalOzetiniGuncelle();
    }

    private void InsaatKalemSil_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is ImalatKalem kalem)
        {
            insaatKalemleri.Remove(kalem);
            SiraNumaralariniYenile(insaatKalemleri, InsaatKalemlerView);
            IcmalOzetiniGuncelle();
        }
    }

    private async void ElektrikKalemEkle_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ElkYapilanIsEntry.Text))
        {
            await DisplayAlert("Eksik Bilgi", "Lütfen Elektrik İmalatı için 'Yapılan İş' tanımını giriniz.", "Tamam");
            return;
        }

        elektrikKalemleri.Add(new ImalatKalem
        {
            SiraNo = elektrikKalemleri.Count + 1,
            PozNo = ElkPozNoEntry.Text?.Trim() ?? "",
            YapilanIs = ElkYapilanIsEntry.Text.Trim(),
            Olcu = ElkOlcuEntry.Text?.Trim() ?? "",
            SozlesmedekiYuzde = SayiOku(ElkSozlesmeYuzdeEntry.Text),
            GerceklesenToplamYuzde = SayiOku(ElkGerceklesenYuzdeEntry.Text),
            OncekiHakedisYuzde = SayiOku(ElkOncekiYuzdeEntry.Text)
        });

        ElkPozNoEntry.Text = string.Empty;
        ElkYapilanIsEntry.Text = string.Empty;
        ElkOlcuEntry.Text = string.Empty;
        ElkSozlesmeYuzdeEntry.Text = string.Empty;
        ElkGerceklesenYuzdeEntry.Text = string.Empty;
        ElkOncekiYuzdeEntry.Text = "0";
        IcmalOzetiniGuncelle();
    }

    private void ElektrikKalemSil_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is ImalatKalem kalem)
        {
            elektrikKalemleri.Remove(kalem);
            SiraNumaralariniYenile(elektrikKalemleri, ElektrikKalemlerView);
            IcmalOzetiniGuncelle();
        }
    }

    private async void MekanikKalemEkle_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(MekYapilanIsEntry.Text))
        {
            await DisplayAlert("Eksik Bilgi", "Lütfen Mekanik Tesisat için 'Yapılan İş' tanımını giriniz.", "Tamam");
            return;
        }

        mekanikKalemleri.Add(new ImalatKalem
        {
            SiraNo = mekanikKalemleri.Count + 1,
            PozNo = MekPozNoEntry.Text?.Trim() ?? "",
            YapilanIs = MekYapilanIsEntry.Text.Trim(),
            Olcu = MekOlcuEntry.Text?.Trim() ?? "",
            SozlesmedekiYuzde = SayiOku(MekSozlesmeYuzdeEntry.Text),
            GerceklesenToplamYuzde = SayiOku(MekGerceklesenYuzdeEntry.Text),
            OncekiHakedisYuzde = SayiOku(MekOncekiYuzdeEntry.Text)
        });

        MekPozNoEntry.Text = string.Empty;
        MekYapilanIsEntry.Text = string.Empty;
        MekOlcuEntry.Text = string.Empty;
        MekSozlesmeYuzdeEntry.Text = string.Empty;
        MekGerceklesenYuzdeEntry.Text = string.Empty;
        MekOncekiYuzdeEntry.Text = "0";
        IcmalOzetiniGuncelle();
    }

    private void MekanikKalemSil_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is ImalatKalem kalem)
        {
            mekanikKalemleri.Remove(kalem);
            SiraNumaralariniYenile(mekanikKalemleri, MekanikKalemlerView);
            IcmalOzetiniGuncelle();
        }
    }

    // ==================== RAPOR OLUŞTURMA VE KAYIT YÖNETİMİ ====================

    private static void IcSayfaDoldur(IXLWorksheet ws, ObservableCollection<ImalatKalem> kalemler, int baslangicSatiri, int varsayilanSonSatir)
    {
        ws.Range($"A{baslangicSatiri}:H{varsayilanSonSatir}").Clear(XLClearOptions.Contents);

        int toplamSatir = varsayilanSonSatir + 1;
        if (kalemler.Count > (varsayilanSonSatir - baslangicSatiri + 1))
        {
            int eklenecekSatir = kalemler.Count - (varsayilanSonSatir - baslangicSatiri + 1);
            ws.Row(varsayilanSonSatir).InsertRowsBelow(eklenecekSatir);
            toplamSatir += eklenecekSatir;
        }

        for (int i = 0; i < kalemler.Count; i++)
        {
            int satir = baslangicSatiri + i;
            var kalem = kalemler[i];

            ws.Cell($"A{satir}").Value = i + 1;
            ws.Cell($"B{satir}").Value = kalem.PozNo;
            ws.Cell($"C{satir}").Value = kalem.YapilanIs;
            ws.Cell($"D{satir}").Value = kalem.Olcu;
            ws.Cell($"E{satir}").Value = kalem.SozlesmedekiYuzde;
            ws.Cell($"F{satir}").Value = kalem.GerceklesenToplamYuzde;
            ws.Cell($"G{satir}").Value = kalem.OncekiHakedisYuzde;
            ws.Cell($"H{satir}").FormulaA1 = $"F{satir}-G{satir}";
        }

        int sonVeriSatiri = toplamSatir - 1;
        ws.Cell($"E{toplamSatir}").FormulaA1 = $"SUM(E{baslangicSatiri}:E{sonVeriSatiri})";
        ws.Cell($"F{toplamSatir}").FormulaA1 = $"SUM(F{baslangicSatiri}:F{sonVeriSatiri})";
        ws.Cell($"G{toplamSatir}").FormulaA1 = $"SUM(G{baslangicSatiri}:G{sonVeriSatiri})";
        ws.Cell($"H{toplamSatir}").FormulaA1 = $"SUM(H{baslangicSatiri}:H{sonVeriSatiri})";
    }

    private async void RaporOlustur_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(IsinAdiEntry.Text))
        {
            await DisplayAlert("Eksik Bilgi", "Lütfen Yapılan İşin Adını giriniz.", "Tamam");
            return;
        }

        double maliyet = SayiOku(MaliyetEntry.Text);
        if (maliyet <= 0)
        {
            await DisplayAlert("Hata", "Lütfen geçerli bir Sözleşme Bedeli (Maliyet) giriniz.", "Tamam");
            return;
        }

        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync("HakedisSablon.xlsx");
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            using var workbook = new XLWorkbook(memoryStream);
            var ws = workbook.Worksheet("BAŞLIK");

            var trCulture = new CultureInfo("tr-TR");
            string temizIsinAdi = IsinAdiEntry.Text.Trim().TrimStart(':', ' ');

            ws.Cell("B2").Value = ": " + temizIsinAdi;
            ws.Cell("B3").Value = string.IsNullOrWhiteSpace(EtutProjeNoEntry.Text) ? "-" : EtutProjeNoEntry.Text.Trim();
            ws.Cell("B4").Value = YukleniciEntry.Text?.Trim() ?? "";
            ws.Cell("B6").Value = maliyet;

            double gecikmeCezasi = SayiOku(GecikmeCezasiEntry.Text);
            if (gecikmeCezasi > 0)
                ws.Cell("H6").Value = gecikmeCezasi;

            ws.Cell("B7").Value = IhaleTarihiEntry.Text?.Trim() ?? "";
            ws.Cell("B8").Value = string.IsNullOrWhiteSpace(IhaleKomKararEntry.Text) ? "-" : IhaleKomKararEntry.Text.Trim();

            TarihVeyaMetinYaz(ws.Cell("B9"), SozlesmeTarihiEntry.Text);
            TarihVeyaMetinYaz(ws.Cell("B10"), IsyeriTeslimTarihiEntry.Text);
            ws.Cell("B11").Value = IsinSuresiEntry.Text?.Trim() ?? "";
            TarihVeyaMetinYaz(ws.Cell("B12"), IsBitimTarihiEntry.Text);

            double verilenAvans = SayiOku(VerilenAvansEntry.Text);
            if (verilenAvans > 0)
                ws.Cell("B13").Value = verilenAvans;

            double mahsupAvans = SayiOku(MahsupAvansEntry.Text);
            if (mahsupAvans > 0)
                ws.Cell("B14").Value = mahsupAvans;

            TarihVeyaMetinYaz(ws.Cell("B15"), IsinBitirildigiTarihEntry.Text);

            ws.Cell("B16").Value = Gorevli1AdEntry.Text?.Trim() ?? "";
            ws.Cell("B17").Value = Gorevli1UnvanEntry.Text?.Trim() ?? "";
            ws.Cell("E16").Value = Gorevli2AdEntry.Text?.Trim() ?? "";
            ws.Cell("E17").Value = Gorevli2UnvanEntry.Text?.Trim() ?? "";
            ws.Cell("G16").Value = Gorevli3AdEntry.Text?.Trim() ?? "";
            ws.Cell("G17").Value = Gorevli3UnvanEntry.Text?.Trim() ?? "";

            TarihVeyaMetinYaz(ws.Cell("C20"), HakedisTarihiEntry.Text);
            string hakedisNo = HakedisNoEntry.Text?.Trim() ?? "1";
            ws.Cell("C21").Value = hakedisNo;

            if (int.TryParse(UygulamaYiliEntry.Text, out int yil))
                ws.Cell("C22").Value = yil;

            ws.Cell("B25").Value = $"Hakediş No: {hakedisNo}";
            ws.Cell("B27").Value = $"{maliyet.ToString("N2", trCulture)} TL Sözleşme Bedelli ";

            if (!string.IsNullOrWhiteSpace(IsyeriTeslimTarihiEntry.Text) && !string.IsNullOrWhiteSpace(IsBitimTarihiEntry.Text))
            {
                string baslangic = IsyeriTeslimTarihiEntry.Text.Trim().Replace('.', '/');
                string bitis = IsBitimTarihiEntry.Text.Trim().Replace('.', '/');
                ws.Cell("B29").Value = $"{baslangic} -{bitis}  TARİHLERİ ARASI YAPILAN İŞİN";
            }

            ws.Cell("B33").Value = OnayAdEntry.Text?.Trim() ?? "";
            ws.Cell("B34").Value = OnayKurumEntry.Text?.Trim() ?? "";
            ws.Cell("B35").Value = OnayUnvanEntry.Text?.Trim() ?? "";

            bool imalatGirildi = insaatKalemleri.Count > 0 || elektrikKalemleri.Count > 0 || mekanikKalemleri.Count > 0;
            if (imalatGirildi)
            {
                IcSayfaDoldur(workbook.Worksheet("İNŞAATÖDE"), insaatKalemleri, 6, 29);
                IcSayfaDoldur(workbook.Worksheet("elk"), elektrikKalemleri, 8, 44);
                IcSayfaDoldur(workbook.Worksheet("mekanik"), mekanikKalemleri, 8, 15);

                var wsIcmal = workbook.Worksheet("YAPI. İŞLER LİSTESİ İCMAL TAB.");
                int insaatToplamSatir = Math.Max(30, 6 + insaatKalemleri.Count);
                int elkToplamSatir = Math.Max(45, 8 + elektrikKalemleri.Count);
                int mekToplamSatir = Math.Max(16, 8 + mekanikKalemleri.Count);

                wsIcmal.Cell("B7").FormulaA1 = $"'İNŞAATÖDE'!E{insaatToplamSatir}";
                wsIcmal.Cell("E7").FormulaA1 = $"'İNŞAATÖDE'!F{insaatToplamSatir}";
                wsIcmal.Cell("G7").FormulaA1 = $"'İNŞAATÖDE'!G{insaatToplamSatir}";

                wsIcmal.Cell("B8").FormulaA1 = $"'elk'!E{elkToplamSatir}";
                wsIcmal.Cell("E8").FormulaA1 = $"'elk'!F{elkToplamSatir}";
                wsIcmal.Cell("G8").FormulaA1 = $"'elk'!G{elkToplamSatir}";

                wsIcmal.Cell("B9").FormulaA1 = $"'mekanik'!E{mekToplamSatir}";
                wsIcmal.Cell("E9").FormulaA1 = $"'mekanik'!F{mekToplamSatir}";
                wsIcmal.Cell("G9").FormulaA1 = $"'mekanik'!G{mekToplamSatir}";
            }

            workbook.CalculateMode = XLCalculateMode.Auto;
            try { workbook.RecalculateAllFormulas(); } catch { }

            string klasor = Path.Combine(FileSystem.AppDataDirectory, "HakedisRaporlari");
            Directory.CreateDirectory(klasor);
            string yeniDosyaAdi = $"Hakedis_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string hedefYol = Path.Combine(klasor, yeniDosyaAdi);

            workbook.SaveAs(hedefYol);

            await db.InsertAsync(new HakedisKayit
            {
                IsinAdi = temizIsinAdi,
                Yuklenici = YukleniciEntry.Text?.Trim() ?? "-",
                SozlesmeBedeli = maliyet,
                HakedisNo = hakedisNo,
                HakedisTarihi = HakedisTarihiEntry.Text?.Trim() ?? "",
                DosyaYolu = hedefYol
            });

            insaatKalemleri.Clear();
            elektrikKalemleri.Clear();
            mekanikKalemleri.Clear();
            IcmalOzetiniGuncelle();
            await ListeyiYukle();

            await Launcher.Default.OpenAsync(new OpenFileRequest("Hakediş Raporunu Görüntüle", new ReadOnlyFile(hedefYol)));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", $"Rapor oluşturulurken bir hata oluştu:\n{ex.Message}", "Tamam");
        }
    }

    private async void KayitliExcelAc_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is string dosyaYolu && File.Exists(dosyaYolu))
        {
            await Launcher.Default.OpenAsync(new OpenFileRequest("Hakediş Raporunu Görüntüle", new ReadOnlyFile(dosyaYolu)));
        }
        else
        {
            await DisplayAlert("Uyarı", "Kayıtlı Excel dosyası bulunamadı.", "Tamam");
        }
    }

    private async void KayitSil_Clicked(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is HakedisKayit kayit)
        {
            bool onay = await DisplayAlert("Silme Onayı",
                $"'{kayit.IsinAdi}' kaydını silmek istediğinize emin misiniz?",
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
        var kayitlar = await db.Table<HakedisKayit>().ToListAsync();
        if (kayitlar.Count == 0)
        {
            await DisplayAlert("Bilgi", "Listede silinecek kayıt bulunmuyor.", "Tamam");
            return;
        }

        bool onay = await DisplayAlert("Listeyi Temizle",
            "Oluşturulan tüm hakediş kayıtları ve Excel dosyaları silinecek. Emin misiniz?",
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

        await db.DeleteAllAsync<HakedisKayit>();
        await ListeyiYukle();
    }
}