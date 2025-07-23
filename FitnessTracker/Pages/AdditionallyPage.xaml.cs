namespace FitnessTrackingApp.Pages;

public partial class AdditionallyPage : ContentPage
{
    private const string YandexMapUrl = "https://yandex.ru/map-widget/v1/?um=constructor%3Aea8d087a688008fa332401b2c7db432fe71dd2f93f742f35742b96731d516d1c&amp";

    public AdditionallyPage()
    {
        InitializeComponent();
        LoadYandexMap();
    }

    private void LoadYandexMap()
    {
        var webViewSource = new UrlWebViewSource { Url = YandexMapUrl };
        YandexMapWebView.Source = webViewSource;
    }

    private async void OnAboutAppTapped(object sender, EventArgs e)
    {
        await DisplayAlert("� ����������",
            "FitnessTrackingApp v1.0\n\n" +
            "��� �������� �������� � ���� �������\n\n" +
            "������������: ������ ����, ��������� ����\n" +
            "��������: https://github.com/IvanZhutyaev",
            "�������");
    }

    private async void OnSupportTapped(object sender, EventArgs e)
    {
        try
        {
            await Launcher.OpenAsync("https://t.me/FitnessTrackingAppBot");
        }
        catch
        {
            await DisplayAlert("������", "�� ������� ������� Telegram", "OK");
        }
    }

    private async void OnBackupTapped(object sender, EventArgs e)
    {
        await DisplayAlert("��������� �����������",
            "��� ������� �������� � ��������� ����������",
            "��� � �����������");
    }

    private async void OnCommunityTapped(object sender, EventArgs e)
    {
        try
        {
            await Launcher.OpenAsync("https://vk.com/fitnesstrackingappVK");
        }
        catch
        {
            await DisplayAlert("������", "�� ������� ������� ����������", "OK");
        }
    }
}