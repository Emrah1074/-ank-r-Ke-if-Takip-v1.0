
using SQLite;

namespace CankiriKesifTakip;

public class TaskPage : ContentPage
{
    private readonly Entry isAdi = new() { Placeholder = "Örn: Okul bakım ve onarım işi" };
    private readonly Entry kurum = new() { Placeholder = "Örn: Çankırı Lisesi" };
    private readonly Picker ilce = new();
    private readonly Entry gorevTarihi = new() { Placeholder = "YYYY-AA-GG" };
    private readonly Entry kesifTarihi = new() { Placeholder = "YYYY-AA-GG" };
    private readonly Entry yaklasikMaliyet = new() { Placeholder = "TL", Keyboard = Keyboard.Numeric };
    private readonly Entry ihaleBedeli = new() { Placeholder = "TL", Keyboard = Keyboard.Numeric };
    private readonly Entry ihaleTarihi = new() { Placeholder = "YYYY-AA-GG" };
    private readonly Entry ihaleSuresi = new() { Placeholder = "Gün", Keyboard = Keyboard.Numeric };
    private readonly Entry firma = new() { Placeholder = "Yüklenici firma adı" };
    private readonly Entry sozlesmeTarihi = new() { Placeholder = "YYYY-AA-GG" };
    private readonly Entry baslamaTarihi = new() { Placeholder = "YYYY-AA-GG" };
    private readonly Entry bitisTarihi = new() { Placeholder = "YYYY-AA-GG" };
    private readonly Entry geciciKabul = new() { Placeholder = "YYYY-AA-GG" };
    private readonly Entry kesinKabul = new() { Placeholder = "YYYY-AA-GG" };
    private readonly Entry kontrolMuhendisi = new() { Placeholder = "Adı Soyadı" };
    private readonly Entry teknikPersonel = new() { Placeholder = "Adı Soyadı" };
    private readonly Entry gorevliPersoneller = new() { Placeholder = "Diğer görevli personeller" };
    private readonly Picker durum = new();
    private readonly Editor aciklama = new()
    {
        Placeholder = "Görevle ilgili açıklamalar...",
        AutoSize = EditorAutoSizeOption.TextChanges,
        MinimumHeightRequest = 100
    };

    private static readonly string DbPath =
        Path.Combine(FileSystem.AppDataDirectory, "cankiri_gorevler.db3");

    private static SQLiteAsyncConnection? database;

    public TaskPage()
    {
        Title = "Yeni Görev Kaydı";
        BackgroundColor = Color.FromArgb("#F1F4F8");

        ilce.ItemsSource = new List<string>
        {
            "Çankırı Merkez", "Atkaracalar", "Bayramören",
            "Çerkeş", "Eldivan", "Ilgaz", "Kızılırmak",
            "Korgun", "Kurşunlu", "Orta",
            "Şabanözü", "Yapraklı"
        };

        durum.ItemsSource = new List<string>
        {
            "Yeni Görev",
            "Keşif Yapıldı",
            "İhale Sürecinde",
            "Devam Ediyor",
            "Geçici Kabul Yapıldı",
            "Kesin Kabul Yapıldı",
            "Tamamlandı",
            "İptal Edildi"
        };

        var form = new VerticalStackLayout
        {
            Padding = 20,
            Spacing = 12
        };

        form.Children.Add(new Label
        {
            Text = "YENİ GÖREV KAYDI",
            FontSize = 23,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#17365D")
        });

        form.Children.Add(new Label
        {
            Text = "Görev ve yapım işi bilgilerini doldurunuz.",
            TextColor = Colors.Gray
        });

        AddSection(form, "1. İŞİN GENEL BİLGİLERİ");
        AddField(form, "İşin Adı *", isAdi);
        AddField(form, "Kurum / Okul Adı", kurum);
        AddField(form, "İlçe *", ilce);
        AddField(form, "İşin Durumu", durum);

        AddSection(form, "2. GÖREV VE KEŞİF BİLGİLERİ");
        AddField(form, "Göreve Gidilen Tarih", gorevTarihi);
        AddField(form, "Keşif Tarihi", kesifTarihi);

        AddSection(form, "3. MALİYET VE İHALE BİLGİLERİ");
        AddField(form, "Yaklaşık Maliyet (TL)", yaklasikMaliyet);
        AddField(form, "İhale Bedeli (TL)", ihaleBedeli);
        AddField(form, "İhale Tarihi", ihaleTarihi);
        AddField(form, "İhale Süresi (Gün)", ihaleSuresi);
        AddField(form, "İhaleyi Alan Firma", firma);

        AddSection(form, "4. SÖZLEŞME VE YAPIM SÜRECİ");
        AddField(form, "Sözleşme Tarihi", sozlesmeTarihi);
        AddField(form, "İşe Başlama Tarihi", baslamaTarihi);
        AddField(form, "İşin Bitiş Tarihi", bitisTarihi);

        AddSection(form, "5. KABUL BİLGİLERİ");
        AddField(form, "Geçici Kabul Tarihi", geciciKabul);
        AddField(form, "Kesin Kabul Tarihi", kesinKabul);

        AddSection(form, "6. TEKNİK PERSONEL BİLGİLERİ");
        AddField(form, "Kontrol Mühendisi", kontrolMuhendisi);
        AddField(form, "Kontrol Teknik Personeli", teknikPersonel);
        AddField(form, "Diğer Görevli Personeller", gorevliPersoneller);

        AddSection(form, "7. AÇIKLAMALAR");
        AddField(form, "Görev Açıklaması", aciklama);

        var kaydet = new Button
        {
            Text = "KAYDI KAYDET",
            BackgroundColor = Color.FromArgb("#17365D"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            HeightRequest = 55,
            CornerRadius = 12
        };

        kaydet.Clicked += KaydetClicked;
        form.Children.Add(kaydet);

        var vazgec = new Button
        {
            Text = "VAZGEÇ",
            BackgroundColor = Colors.Gray,
            TextColor = Colors.White
        };

        vazgec.Clicked += async (s, e) =>
            await Navigation.PopModalAsync();

        form.Children.Add(vazgec);

        Content = new ScrollView { Content = form };
    }

    private void AddSection(VerticalStackLayout form, string title)
    {
        form.Children.Add(new Label
        {
            Text = title,
            FontSize = 16,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#17365D"),
            Margin = new Thickness(0, 18, 0, 3)
        });
    }

    private void AddField(
        VerticalStackLayout form,
        string title,
        View control)
    {
        form.Children.Add(new Label
        {
            Text = title,
            FontSize = 13,
            TextColor = Color.FromArgb("#475569")
        });

        control.BackgroundColor = Colors.White;
        control.Margin = new Thickness(0, 0, 0, 5);

        form.Children.Add(control);
    }

    private async void KaydetClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(isAdi.Text) ||
            ilce.SelectedItem == null)
        {
            await DisplayAlert(
                "Eksik Bilgi",
                "İşin adı ve ilçe bilgisi zorunludur.",
                "Tamam");
            return;
        }

        try
        {
            database ??= new SQLiteAsyncConnection(DbPath);

            await database.CreateTableAsync<TaskRecord>();

            var kayit = new TaskRecord
            {
                IsAdi = isAdi.Text ?? "",
                Kurum = kurum.Text ?? "",
                Ilce = ilce.SelectedItem.ToString() ?? "",
                Durum = durum.SelectedItem?.ToString() ?? "Yeni Görev",
                GorevTarihi = gorevTarihi.Text ?? "",
                KesifTarihi = kesifTarihi.Text ?? "",
                YaklasikMaliyet = yaklasikMaliyet.Text ?? "",
                IhaleBedeli = ihaleBedeli.Text ?? "",
                IhaleTarihi = ihaleTarihi.Text ?? "",
                IhaleSuresi = ihaleSuresi.Text ?? "",
                Firma = firma.Text ?? "",
                SozlesmeTarihi = sozlesmeTarihi.Text ?? "",
                BaslamaTarihi = baslamaTarihi.Text ?? "",
                BitisTarihi = bitisTarihi.Text ?? "",
                GeciciKabul = geciciKabul.Text ?? "",
                KesinKabul = kesinKabul.Text ?? "",
                KontrolMuhendisi = kontrolMuhendisi.Text ?? "",
                TeknikPersonel = teknikPersonel.Text ?? "",
                GorevliPersoneller = gorevliPersoneller.Text ?? "",
                Aciklama = aciklama.Text ?? "",
                KayitTarihi = DateTime.Now.ToString("yyyy-MM-dd HH:mm")
            };

            await database.InsertAsync(kayit);

            await DisplayAlert(
                "Başarılı",
                "Görev kaydı başarıyla kaydedildi.",
                "Tamam");

            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert(
                "Kayıt Hatası",
                ex.Message,
                "Tamam");
        }
    }
}

public class TaskRecord
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string IsAdi { get; set; } = "";
    public string Kurum { get; set; } = "";
    public string Ilce { get; set; } = "";
    public string Durum { get; set; } = "";
    public string GorevTarihi { get; set; } = "";
    public string KesifTarihi { get; set; } = "";
    public string YaklasikMaliyet { get; set; } = "";
    public string IhaleBedeli { get; set; } = "";
    public string IhaleTarihi { get; set; } = "";
    public string IhaleSuresi { get; set; } = "";
    public string Firma { get; set; } = "";
    public string SozlesmeTarihi { get; set; } = "";
    public string BaslamaTarihi { get; set; } = "";
    public string BitisTarihi { get; set; } = "";
    public string GeciciKabul { get; set; } = "";
    public string KesinKabul { get; set; } = "";
    public string KontrolMuhendisi { get; set; } = "";
    public string TeknikPersonel { get; set; } = "";
    public string GorevliPersoneller { get; set; } = "";
    public string Aciklama { get; set; } = "";
    public string KayitTarihi { get; set; } = "";
}
