using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Xml;
using СurrencyStream.Models;

namespace СurrencyStream.Controllers
{
    public class HomeController : Controller
    {
        private readonly IMemoryCache memoryCache;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, IMemoryCache memoryCache)
        {
            _logger = logger;
            this.memoryCache = memoryCache;
        }

        private const string UrlForCurrencies = "https://www.cbr.ru/scripts/XML_daily.asp";
        public async Task<List<CurrencyRate>> GetCurrencyRates()
        {
            // Регистрация провайдера кодировок
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (var httpClient = new HttpClient())
            {
                try
                {
                    var response = await httpClient.GetAsync(UrlForCurrencies);
                    response.EnsureSuccessStatusCode();

                    byte[] bytes = await response.Content.ReadAsByteArrayAsync();

                    Encoding encoding = Encoding.GetEncoding("windows-1251");
                    string xmlString = encoding.GetString(bytes, 0, bytes.Length);

                    var doc = new XmlDocument();
                    doc.LoadXml(xmlString);

                    var rates = new List<CurrencyRate>();

                    var usdNode = doc.SelectSingleNode("//Valute[CharCode='USD']");
                    var eurNode = doc.SelectSingleNode("//Valute[CharCode='EUR']");
                    var cnyNode = doc.SelectSingleNode("//Valute[CharCode='CNY']");

                    rates.Add(new CurrencyRate
                    {
                        CharCode = usdNode["CharCode"].InnerText,
                        Name = usdNode["Name"].InnerText,
                        Value = decimal.Parse(usdNode["Value"].InnerText.Replace(',', '.'), CultureInfo.InvariantCulture)
                    });

                    rates.Add(new CurrencyRate
                    {
                        CharCode = eurNode["CharCode"].InnerText,
                        Name = eurNode["Name"].InnerText,
                        Value = decimal.Parse(eurNode["Value"].InnerText.Replace(',', '.'), CultureInfo.InvariantCulture)
                    });

                    rates.Add(new CurrencyRate
                    {
                        CharCode = cnyNode["CharCode"].InnerText,
                        Name = cnyNode["Name"].InnerText,
                        Value = decimal.Parse(cnyNode["Value"].InnerText.Replace(',', '.'), CultureInfo.InvariantCulture)
                    });

                    return rates;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при получении данных из Центрального банка России: {ex.Message}");
                    throw;
                }
            }
        }

        public async Task<IActionResult> Index()
        {
            var currencyRates = await GetCurrencyRates();
            return View(currencyRates);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}