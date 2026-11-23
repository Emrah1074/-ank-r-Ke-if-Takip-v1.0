using Microsoft.Maui.Controls.Shapes;
using SQLite;

namespace CankiriKesifTakip;

public class PersonelListPage : ContentPage
{
    private readonly SQLiteAsyncConnection db;
    private readonly VerticalStackLayout liste = new() { Spacing = 15 };
    private bool yukleniyor;

    public PersonelListPage()
    {
        Title = "Teknik Personel İş Yükü";
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

        // 2. PERSONEL EKLE BUTONU
        var btnEkle = new Button
        {
            Text = "➕ Yeni Personel Ekle",
            BackgroundColor = Color.FromArgb("#2476B9"),
            TextColor = Colors.White,
            CornerRadius = 8,
            Margin = new Thickness(0, 0, 0, 15)
        };
        btnEkle.Clicked += async (s, e) =>
        {
            await DisplayAlert("Bilgi", "Mevcut yapıda personeller, görev kaydı eklerken yazdığınız isimlerden otomatik olarak toplanmaktadır. \n\nYeni bir personel eklemek için 'Yeni Görev Ekle' ekranında Kontrol Mühendisi veya Teknik Personel alanına yeni personelin adını yazmanız yeterlidir.", "Anladım");
        };

        var baslik = new Label
        {
            Text = "PERSONEL İŞ YÜKÜ DAĞILIMI",
            FontSize = 21,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#17365D"),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var aciklama = new Label
        {
            Text = "Görevlere atanan personellerin güncel iş sayıları aşağıda listelenmiştir.",
            TextColor = Colors.Gray,
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 15)
        };

        var icerik = new VerticalStackLayout
        {
            Padding = 20,
            // Butonları ve başlıkları yukarıdan aşağıya doğru sıraladık
            Children = { btnGeri, btnEkle, baslik, aciklama, liste }
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

        // Veritabanındaki tüm personellerin isimlerini toplayıp tekilleştiriyoruz (boş olanları eliyoruz)
        var personelListesi = tumGorevler
            .Select(x => x.KontrolMuhendisi)
            .Concat(tumGorevler.Select(x => x.TeknikPersonel))
            .Where(isim => !string.IsNullOrWhiteSpace(isim))
            .Distinct()
            .OrderBy(isim => isim)
            .ToList();

        if (personelListesi.Count == 0)
        {
            liste.Children.Add(new Label
            {
                Text = "Henüz sistemde kayıtlı bir personel bulunmuyor. Görevlere personel ekledikçe burada görünecektir.",
                TextColor = Colors.Gray,
                HorizontalTextAlignment = TextAlignment.Center,
                Margin = 20
            });
            return;
        }

        foreach (var personel in personelListesi)
        {
            // Bu personelin geçtiği görevleri bul
            var personelinGorevleri = tumGorevler.Where(x =>
                (x.KontrolMuhendisi != null && x.KontrolMuhendisi.Contains(personel)) ||
                (x.TeknikPersonel != null && x.TeknikPersonel.Contains(personel))
            ).ToList();

            liste.Children.Add(PersonelKartiOlustur(personel, personelinGorevleri));
        }
    }

    private View PersonelKartiOlustur(string personelAdi, List<TaskRecord> gorevler)
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

        var isimBaslik = new Label
        {
            Text = $"👷 {personelAdi}",
            FontSize = 17,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#17365D")
        };

        var lblToplam = new Label { Text = $"Üstlendiği Toplam İş: {toplam}", TextColor = Colors.Black, FontSize = 13 };
        var lblDevam = new Label { Text = $"Devam Eden İşleri: {devamEden}", TextColor = Color.FromArgb("#2476B9"), FontSize = 13 };
        var lblBiten = new Label { Text = $"Tamamlanan İşleri: {biten}", TextColor = Color.FromArgb("#16834A"), FontSize = 13 };

        var detay = new VerticalStackLayout
        {
            Spacing = 5,
            Margin = new Thickness(0, 10, 0, 0),
            Children = { lblToplam, lblDevam, lblBiten }
        };

        kart.Content = new VerticalStackLayout
        {
            Children = { isimBaslik, detay }
        };

        return kart;
    }
}