using Microsoft.Maui.Controls.Shapes;
using SQLite;

namespace CankiriKesifTakip;

public class IlceListPage : ContentPage
{
    private readonly SQLiteAsyncConnection db;
    private readonly VerticalStackLayout liste = new() { Spacing = 15 };
    private bool yukleniyor;

    public IlceListPage()
    {
        Title = "İlçe ve Görev Yerleri";
        BackgroundColor = Color.FromArgb("#F1F4F8");

        db = new SQLiteAsyncConnection(
            System.IO.Path.Combine(
                FileSystem.AppDataDirectory,
                "cankiri_gorevler.db3"));

        // GERİ DÖN BUTONU
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

        var baslik = new Label
        {
            Text = "İLÇELERE GÖRE İŞ DAĞILIMI",
            FontSize = 21,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#17365D"),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var icerik = new VerticalStackLayout
        {
            Padding = 20,
            Children = { btnGeri, baslik, liste }
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
        liste.Children.Clear();
        var tumGorevler = await db.Table<TaskRecord>().ToListAsync();

        var ilceler = new List<string>
        {
            "Çankırı Merkez", "Atkaracalar", "Bayramören", "Çerkeş",
            "Eldivan", "Ilgaz", "Kızılırmak", "Korgun", "Kurşunlu",
            "Orta", "Şabanözü", "Yapraklı"
        };

        foreach (var ilce in ilceler)
        {
            var ilceGorevleri = tumGorevler.Where(x => x.Ilce == ilce).ToList();
            liste.Children.Add(IlceKartiOlustur(ilce, ilceGorevleri));
        }
    }

    private View IlceKartiOlustur(string ilceAdi, List<TaskRecord> gorevler)
    {
        int toplam = gorevler.Count;
        int devamEden = gorevler.Count(x => x.Durum == "Devam Ediyor" || x.Durum == "İhale Sürecinde" || x.Durum == "Yeni Görev" || x.Durum == "Keşif Yapıldı");
        int biten = gorevler.Count(x => x.Durum == "Tamamlandı" || x.Durum == "Geçici Kabul Yapıldı" || x.Durum == "Kesin Kabul Yapıldı");

        var kart = new Border
        {
            BackgroundColor = Colors.White,
            StrokeThickness = 0,
            Padding = 15
        };

        kart.StrokeShape = new RoundRectangle { CornerRadius = 12 };

        var baslik = new Label
        {
            Text = $"📍 {ilceAdi}",
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#17365D")
        };

        var lblToplam = new Label { Text = $"Toplam Görev: {toplam}", TextColor = Colors.Black, FontSize = 13 };
        var lblDevam = new Label { Text = $"Devam Eden/Süreçte: {devamEden}", TextColor = Color.FromArgb("#2476B9"), FontSize = 13 };
        var lblBiten = new Label { Text = $"Tamamlanan/Kabul: {biten}", TextColor = Color.FromArgb("#16834A"), FontSize = 13 };

        var detay = new VerticalStackLayout
        {
            Spacing = 5,
            Margin = new Thickness(0, 10, 0, 0),
            Children = { lblToplam, lblDevam, lblBiten }
        };

        kart.Content = new VerticalStackLayout
        {
            Children = { baslik, detay }
        };

        return kart;
    }
}