using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

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
        if (line.Contains("2022"))
        {
          return "NoMoreLineE";
        }

        return line;
      })
      ;
    }
  }
}