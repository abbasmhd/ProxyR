using System.Net.Http.Headers;

namespace ProxyR.Core.Tests
{
    [TestClass]
    public class UsersQueryTests : VerifyBase
    {

        private const string baseUrl = "https://localhost:44368/";

        [Ignore]
        [TestMethod]
        public async Task GetUserGrid()
        {
            var apiUrl = "users/grid?$take=2&$skip=1&$select=firstname,lastname&$filter=lastname%20contains%20%27pear%27";

            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            HttpResponseMessage response = await client.GetAsync(apiUrl);
            var res = await response.Content.ReadAsStringAsync();

            //Assert.IsTrue(response.IsSuccessStatusCode);
            //Assert.IsTrue(res.Contains("results"));
            //Assert.IsTrue(res.Contains("Spearing"));

            //await Verify(res)
            await Verify(new
            {
                response,
                res
            })
            .ScrubLinesWithReplace(replaceLine: line =>
            {
                var currentYear = DateTime.Now.Year.ToString();
                if (line.Contains(currentYear))
                {
                    return "2022-02-02"; // Twosday.
                }

                return line;
            })
            ;
        }
    }
}