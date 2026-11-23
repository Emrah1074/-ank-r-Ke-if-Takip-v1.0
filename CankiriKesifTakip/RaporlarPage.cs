using Microsoft.Maui.Controls.Shapes;
using SQLite;

namespace CankiriKesifTakip;

public class RaporlarPage : ContentPage
{
    private readonly SQLiteAsyncConnection db;
    private readonly VerticalStackLayout liste = new() { Spacing = 15 };
    private bool yukleniyor;

    public RaporlarPage()
    {
        Title = "Raporlar ve İstatistikler";
        BackgroundColor = Color.FromArgb("#F1F4F8");

        db = new SQLiteAsyncConnection(
            System.IO.Path.Combine(
                FileSystem.AppDataDirectory,
                "cankiri_gorevler.db3"));

        // 1. GERİ DÖN BUTONU
        var btnGeri = new Button
        {
            Text = "🔙 Ana Sayfaya Dön",
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#17365D"),
            HorizontalOptions = LayoutOptions.Start,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        };
        btnGeri.Clicked += async (s, e) => await Navigation.PopModalAsync();

        // 2. EXCEL DIŞA AKTAR BUTONU
        var btnExcel = new Button
        {
            Text = "📊 Kayıtları Excel (CSV) Olarak Paylaş",
            BackgroundColor = Color.FromArgb("#16834A"),
            TextColor = Colors.White,
            CornerRadius = 8,
            Margin = new Thickness(0, 0, 0, 15)
        };
        btnExcel.Clicked += async (s, e) => await ExcelDisaAktar();

        var baslik = new Label
        {
            Text = "GENEL İSTATİSTİKLER VE FİNANSAL ÖZET",
            FontSize = 21,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#17365D"),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var icerik = new VerticalStackLayout
        {
            Padding = 20,
            // Butonları en üste, başlığın hemen üstüne ekledik
            Children = { btnGeri, btnExcel, baslik, liste }
        };

        Content = new ScrollView { Content = icerik };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (yukleniyor) return;
        yukleniyor = true;

        try
        {
            await db.CreateTableAsync<TaskRecord>();
            await RaporlariYukle();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
        finally
        {
            yukleniyor = false;
        }
    }

    private async Task RaporlariYukle()
    {
        liste.Children.Clear();
        var tumGorevler = await db.Table<TaskRecord>().ToListAsync();

        if (tumGorevler.Count == 0)
        {
            liste.Children.Add(new Label
            {
                Text = "Sistemde raporlanacak görev bulunamadı.",
                TextColor = Colors.Gray,
                HorizontalTextAlignment = TextAlignment.Center
            });
            return;
        }

        // 1. GENEL DURUM ÖZETİ KARTI
        int toplam = tumGorevler.Count;
        int devamEden = tumGorevler.Count(x => x.Durum == "Devam Ediyor" || x.Durum == "İhale Sürecinde" || x.Durum == "Yeni Görev" || x.Durum == "Keşif Yapıldı");
        int tamamlanan = tumGorevler.Count(x => x.Durum == "Tamamlandı" || x.Durum == "Geçici Kabul Yapıldı" || x.Durum == "Kesin Kabul Yapıldı");
        int iptal = tumGorevler.Count(x => x.Durum == "İptal Edildi");

        var durumKarti = RaporKartiOlustur("📈 Görev Durum Dağılımı",
            $"Aktif İş Yükü (Devam Eden vs.): {devamEden}\n" +
            $"Sonuçlanan (Tamamlanan/Kabul): {tamamlanan}\n" +
            $"İptal Edilen İşler: {iptal}\n\n" +
            $"Tamamlanma Oranı: %{(toplam > 0 ? (tamamlanan * 100 / toplam) : 0)}");

        liste.Children.Add(durumKarti);

        // 2. FİNANSAL ÖZET KARTI
        double toplamMaliyet = 0;
        double toplamIhaleBedeli = 0;

        foreach (var gorev in tumGorevler)
        {
            if (double.TryParse(gorev.YaklasikMaliyet?.Replace(".", ","), out double maliyet))
                toplamMaliyet += maliyet;

            if (double.TryParse(gorev.IhaleBedeli?.Replace(".", ","), out double ihale))
                toplamIhaleBedeli += ihale;
        }

        var finansKarti = RaporKartiOlustur("💰 Finansal Özet",
            $"Toplam Yaklaşık Maliyet: {toplamMaliyet:N2} TL\n" +
            $"Toplam İhale Bedeli: {toplamIhaleBedeli:N2} TL\n\n" +
            $"Fark (Tasarruf/Aşım): {(toplamMaliyet - toplamIhaleBedeli):N2} TL");

        liste.Children.Add(finansKarti);
    }

    private View RaporKartiOlustur(string baslikMetni, string icerikMetni)
    {
        var kart = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            Padding = 15
        };
        kart.StrokeShape = new RoundRectangle { CornerRadius = 12 };

        var baslik = new Label
        {
            Text = baslikMetni,
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#17365D"),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var icerik = new Label
        {
            Text = icerikMetni,
            FontSize = 14,
            TextColor = Color.FromArgb("#475569"),
            LineHeight = 1.3
        };

        kart.Content = new VerticalStackLayout { Children = { baslik, icerik } };
        return kart;
    }

    // 3. EXCEL DIŞA AKTARMA FONKSİYONU
    private async Task ExcelDisaAktar()
    {
        try
        {
            var tumGorevler = await db.Table<TaskRecord>().ToListAsync();

            if (tumGorevler.Count == 0)
            {
                await DisplayAlert("Uyarı", "Dışa aktarılacak görev yok.", "Tamam");
                return;
            }

            var csv = new System.Text.StringBuilder();

            // Sütun Başlıkları
            csv.AppendLine("İş Adı;Kurum;İlçe;Durum;Görev Tarihi;Yaklaşık Maliyet;İhale Bedeli;Firma;Kontrol Mühendisi;Teknik Personel");

            foreach (var g in tumGorevler)
            {
                // İçinde noktalı virgül (;) varsa CSV'yi bozmaması için tire (-) yapıyoruz
                string isAdi = g.IsAdi?.Replace(";", "-") ?? "";
                string kurum = g.Kurum?.Replace(";", "-") ?? "";
                string ilce = g.Ilce?.Replace(";", "-") ?? "";
                string durum = g.Durum?.Replace(";", "-") ?? "";
                string tarih = g.GorevTarihi?.Replace(";", "-") ?? "";
                string maliyet = g.YaklasikMaliyet?.Replace(";", "-") ?? "";
                string ihale = g.IhaleBedeli?.Replace(";", "-") ?? "";
                string firma = g.Firma?.Replace(";", "-") ?? "";
                string muhendis = g.KontrolMuhendisi?.Replace(";", "-") ?? "";
                string teknik = g.TeknikPersonel?.Replace(";", "-") ?? "";

                csv.AppendLine($"{isAdi};{kurum};{ilce};{durum};{tarih};{maliyet};{ihale};{firma};{muhendis};{teknik}");
            }

            // Dosyayı oluştur ve MAUI Share ile paylaş
            string dosyaYolu = System.IO.Path.Combine(FileSystem.CacheDirectory, "Gorev_Raporu.csv");
            System.IO.File.WriteAllText(dosyaYolu, csv.ToString(), System.Text.Encoding.UTF8);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Görev Raporunu Paylaş",
                File = new ShareFile(dosyaYolu)
            });
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", "Dışa aktarılırken bir sorun oluştu: " + ex.Message, "Tamam");
        }
    }
}