using SQLite;

namespace CankiriKesifTakip;

public partial class MainPage : ContentPage
{
    private readonly SQLiteAsyncConnection db;

    public MainPage()
    {
        InitializeComponent();

        // Veritabanı bağlantısını kuruyoruz
        db = new SQLiteAsyncConnection(
            System.IO.Path.Combine(
                FileSystem.AppDataDirectory,
                "cankiri_gorevler.db3"));
    }

    // Sayfa her açıldığında/göründüğünde bu metod çalışır
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await IstatistikleriYukle();
    }

    private async Task IstatistikleriYukle()
    {
        try
        {
            // Tablo daha önce hiç oluşturulmadıysa hata vermemesi için oluştur
            await db.CreateTableAsync<TaskRecord>();

            // Tüm kayıtları listeye çek
            var tumGorevler = await db.Table<TaskRecord>().ToListAsync();

            // Sayıları hesapla
            int toplam = tumGorevler.Count;
            int devamEden = tumGorevler.Count(x => x.Durum == "Devam Ediyor" || x.Durum == "İhale Sürecinde");
            int tamamlanan = tumGorevler.Count(x => x.Durum == "Tamamlandı");
            int kabulBekleyen = tumGorevler.Count(x => x.Durum == "Geçici Kabul Yapıldı" || x.Durum == "Kesin Kabul Yapıldı");

            // Arayüzdeki (XAML) etiketlere sayıları yazdır
            lblToplam.Text = toplam.ToString();
            lblDevamEden.Text = devamEden.ToString();
            lblTamamlanan.Text = tamamlanan.ToString();
            lblKabulBekleyen.Text = kabulBekleyen.ToString();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Veritabanı Hatası", $"İstatistikler yüklenirken sorun oluştu:\n{ex.Message}", "Tamam");
        }
    }

    private async void NewTaskClicked(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PushModalAsync(
                new NavigationPage(new TaskPage()));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }

    private async void TaskListClicked(object sender, EventArgs e)
    {
        try
        {
            await Navigation.PushModalAsync(
                new NavigationPage(new TaskListPage()));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Hata", ex.Message, "Tamam");
        }
    }

    // --- HIZLI ERİŞİM MENÜSÜ TIKLAMA OLAYLARI ---

    private async void HizliGorevler_Tapped(object sender, TappedEventArgs e)
    {
        await Navigation.PushModalAsync(new NavigationPage(new TaskListPage()));
    }

    private async void HizliIlceler_Tapped(object sender, TappedEventArgs e)
    {
        // Yeni oluşturduğumuz İlçe Listesi sayfasına gider
        await Navigation.PushModalAsync(new NavigationPage(new IlceListPage()));
    }

    private async void HizliPersonel_Tapped(object sender, TappedEventArgs e)
    {
        // "Yapım Aşamasında" uyarısını kaldırdık, artık gerçek sayfaya gidiyor:
        await Navigation.PushModalAsync(new NavigationPage(new PersonelListPage()));
    }

    private async void HizliRaporlar_Tapped(object sender, TappedEventArgs e)
    {
        // Raporlar sayfasına yönlendirme
        await Navigation.PushModalAsync(new NavigationPage(new RaporlarPage()));
    }
}