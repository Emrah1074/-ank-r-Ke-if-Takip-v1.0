using Microsoft.Maui.Controls.Shapes;
using SQLite;

namespace CankiriKesifTakip;

public class TaskListPage : ContentPage
{
    private readonly SQLiteAsyncConnection db;

    private readonly Entry arama = new()
    {
        Placeholder = "İş adı, firma veya personel ara...",
        BackgroundColor = Colors.White
    };

    private readonly Picker ilceSec = new();
    private readonly Picker durumSec = new();

    private readonly VerticalStackLayout liste = new()
    {
        Spacing = 12
    };

    private readonly Label sonuc = new()
    {
        TextColor = Colors.Gray,
        FontSize = 13
    };

    private bool yukleniyor;

    public TaskListPage()
    {
        Title = "Görev Kayıtları";
        BackgroundColor = Color.FromArgb("#F1F4F8");

        db = new SQLiteAsyncConnection(
            System.IO.Path.Combine(
                FileSystem.AppDataDirectory,
                "cankiri_gorevler.db3"));

        ilceSec.Title = "İlçe seçiniz";
        ilceSec.ItemsSource = new List<string>
        {
            "Tüm ilçeler", "Çankırı Merkez", "Atkaracalar",
            "Bayramören", "Çerkeş", "Eldivan", "Ilgaz",
            "Kızılırmak", "Korgun", "Kurşunlu",
            "Orta", "Şabanözü", "Yapraklı"
        };
        ilceSec.SelectedIndex = 0;

        durumSec.Title = "İş durumu";
        durumSec.ItemsSource = new List<string>
        {
            "Tüm durumlar", "Yeni Görev", "Keşif Yapıldı",
            "İhale Sürecinde", "Devam Ediyor",
            "Geçici Kabul Yapıldı", "Kesin Kabul Yapıldı",
            "Tamamlandı", "İptal Edildi"
        };
        durumSec.SelectedIndex = 0;

        arama.TextChanged += async (s, e) =>
            await Yukle();

        ilceSec.SelectedIndexChanged += async (s, e) =>
            await Yukle();

        durumSec.SelectedIndexChanged += async (s, e) =>
            await Yukle();

        // GERİ DÖN BUTONU EKLENDİ
        var btnGeri = new Button
        {
            Text = "🔙 Ana Sayfaya Dön",
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#17365D"),
            HorizontalOptions = LayoutOptions.Start,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 0, 0, 10)
        };

        btnGeri.Clicked += async (s, e) =>
            await Navigation.PopModalAsync();

        var baslik = new Label
        {
            Text = "GÖREV KAYITLARI",
            FontSize = 23,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#17365D")
        };

        var temizle = new Button
        {
            Text = "Filtreleri Temizle",
            BackgroundColor = Colors.LightGray,
            TextColor = Colors.Black
        };

        temizle.Clicked += async (s, e) =>
        {
            arama.Text = "";
            ilceSec.SelectedIndex = 0;
            durumSec.SelectedIndex = 0;
            await Yukle();
        };

        var icerik = new VerticalStackLayout
        {
            Padding = 18,
            Spacing = 12
        };

        // Geri butonu en üste eklendi
        icerik.Children.Add(btnGeri);
        icerik.Children.Add(baslik);
        icerik.Children.Add(new Label
        {
            Text = "Kayıtlı işleri arayabilir ve filtreleyebilirsin.",
            TextColor = Colors.Gray
        });

        icerik.Children.Add(arama);

        icerik.Children.Add(new Label
        {
            Text = "İlçeye göre filtrele",
            TextColor = Color.FromArgb("#17365D")
        });
        icerik.Children.Add(ilceSec);

        icerik.Children.Add(new Label
        {
            Text = "İş durumuna göre filtrele",
            TextColor = Color.FromArgb("#17365D")
        });
        icerik.Children.Add(durumSec);

        icerik.Children.Add(temizle);
        icerik.Children.Add(sonuc);
        icerik.Children.Add(liste);

        Content = new ScrollView
        {
            Content = icerik
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (yukleniyor)
            return;

        yukleniyor = true;

        try
        {
            await db.CreateTableAsync<TaskRecord>();
            await Yukle();
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

    private async Task Yukle()
    {
        try
        {
            var kayitlar = await db.Table<TaskRecord>()
                .OrderByDescending(x => x.Id)
                .ToListAsync();

            string aranan = arama.Text?.Trim() ?? "";

            if (!string.IsNullOrWhiteSpace(aranan))
            {
                kayitlar = kayitlar.Where(x =>
                    (x.IsAdi != null && x.IsAdi.Contains(aranan, StringComparison.OrdinalIgnoreCase)) ||
                    (x.Kurum != null && x.Kurum.Contains(aranan, StringComparison.OrdinalIgnoreCase)) ||
                    (x.Firma != null && x.Firma.Contains(aranan, StringComparison.OrdinalIgnoreCase)) ||
                    (x.KontrolMuhendisi != null && x.KontrolMuhendisi.Contains(aranan, StringComparison.OrdinalIgnoreCase)) ||
                    (x.TeknikPersonel != null && x.TeknikPersonel.Contains(aranan, StringComparison.OrdinalIgnoreCase)) ||
                    (x.GorevliPersoneller != null && x.GorevliPersoneller.Contains(aranan, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            if (ilceSec.SelectedIndex > 0)
            {
                string secilen = ilceSec.SelectedItem?.ToString() ?? "";

                kayitlar = kayitlar
                    .Where(x => x.Ilce == secilen)
                    .ToList();
            }

            if (durumSec.SelectedIndex > 0)
            {
                string secilen = durumSec.SelectedItem?.ToString() ?? "";

                kayitlar = kayitlar
                    .Where(x => x.Durum == secilen)
                    .ToList();
            }

            liste.Children.Clear();

            sonuc.Text = $"Bulunan görev sayısı: {kayitlar.Count}";

            if (kayitlar.Count == 0)
            {
                liste.Children.Add(new Label
                {
                    Text = "Henüz bu kriterlere uygun görev bulunamadı.",
                    TextColor = Colors.Gray,
                    HorizontalTextAlignment = TextAlignment.Center,
                    Margin = 20
                });

                return;
            }

            foreach (var kayit in kayitlar)
            {
                liste.Children.Add(GorevKarti(kayit));
            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Listeleme Hatası", ex.Message, "Tamam");
        }
    }

    private View GorevKarti(TaskRecord kayit)
    {
        var kart = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            Padding = 15
        };

        kart.StrokeShape = new RoundRectangle
        {
            CornerRadius = 12
        };

        var baslik = new Label
        {
            Text = kayit.IsAdi,
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#17365D")
        };

        var bilgiler = new Label
        {
            Text =
                $"Kurum: {kayit.Kurum}\n" +
                $"İlçe: {kayit.Ilce}\n" +
                $"Firma: {kayit.Firma}\n" +
                $"Durum: {kayit.Durum}\n" +
                $"Görev Tarihi: {kayit.GorevTarihi}",
            FontSize = 13,
            TextColor = Color.FromArgb("#475569")
        };

        var detay = new Button
        {
            Text = "DETAY",
            BackgroundColor = Color.FromArgb("#2476B9"),
            TextColor = Colors.White,
            CornerRadius = 8
        };

        detay.Clicked += async (s, e) =>
            await DetayGoster(kayit);

        var sil = new Button
        {
            Text = "SİL",
            BackgroundColor = Color.FromArgb("#B42318"),
            TextColor = Colors.White,
            CornerRadius = 8
        };

        sil.Clicked += async (s, e) =>
            await Sil(kayit);

        var butonlar = new HorizontalStackLayout
        {
            Spacing = 10,
            Children = { detay, sil }
        };

        kart.Content = new VerticalStackLayout
        {
            Spacing = 9,
            Children = { baslik, bilgiler, butonlar }
        };

        return kart;
    }

    private async Task DetayGoster(TaskRecord x)
    {
        string bilgi =
            $"İş: {x.IsAdi}\n" +
            $"Kurum: {x.Kurum}\n" +
            $"İlçe: {x.Ilce}\n" +
            $"Durum: {x.Durum}\n\n" +
            $"Görev tarihi: {x.GorevTarihi}\n" +
            $"Keşif tarihi: {x.KesifTarihi}\n\n" +
            $"Yaklaşık maliyet: {x.YaklasikMaliyet} TL\n" +
            $"İhale bedeli: {x.IhaleBedeli} TL\n" +
            $"İhale tarihi: {x.IhaleTarihi}\n" +
            $"İhale süresi: {x.IhaleSuresi} gün\n" +
            $"Firma: {x.Firma}\n\n" +
            $"Sözleşme tarihi: {x.SozlesmeTarihi}\n" +
            $"İşe başlama: {x.BaslamaTarihi}\n" +
            $"İş bitiş: {x.BitisTarihi}\n" +
            $"Geçici kabul: {x.GeciciKabul}\n" +
            $"Kesin kabul: {x.KesinKabul}\n\n" +
            $"Kontrol mühendisi: {x.KontrolMuhendisi}\n" +
            $"Teknik personel: {x.TeknikPersonel}\n" +
            $"Diğer personeller: {x.GorevliPersoneller}\n\n" +
            $"Açıklama: {x.Aciklama}";

        await DisplayAlert("Görev Detayları", bilgi, "Kapat");
    }

    private async Task Sil(TaskRecord kayit)
    {
        bool onay = await DisplayAlert(
            "Görev Sil",
            $"'{kayit.IsAdi}' kaydını silmek istiyor musun?",
            "Evet, Sil",
            "Vazgeç");

        if (!onay)
            return;

        await db.DeleteAsync(kayit);

        await DisplayAlert(
            "Bilgi",
            "Görev kaydı silindi.",
            "Tamam");

        await Yukle();
    }
}