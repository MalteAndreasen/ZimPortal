using Newtonsoft.Json.Linq;
using System.Buffers.Text;
using System.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ZimPortal.Data;
using ZimPortal.Models;

namespace ZimPortal.Services
{
    public class CachingServiceProvider : BackgroundService
    {
        private IServiceScopeFactory _scopeFactory;
        public CachingContext _cache;

        // Temporary food types until ZimSystem ApS update their database to contain it on the outlets that get sent over
        private string[] foodTypes = { "Pizza", "Burger", "Tyrkisk", "Italiensk", "Amerikansk", "Indisk" };

        public CachingServiceProvider(IServiceScopeFactory scopeFactory, CachingContext cache)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //throw new NotImplementedException();
            while (!stoppingToken.IsCancellationRequested)
            {
                using(var scope = _scopeFactory.CreateScope())
                {
                    // Get data and verwrite cache
                    try
                    {
                        List<Outlet> outlets = await GetOutlets();
                        _cache.AssignCache(outlets);
                    }
                    catch (NullReferenceException ex)
                    {
                        System.Diagnostics.Debug.WriteLine("API Error: " + ex.Message);
                    }
                    catch (ArgumentException ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Cache Error: " + ex.Message);
                    }
                }

                // Wait for an hour
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }

        }

        private async Task<List<Outlet>> GetOutlets()
        {
            using (var httpClient = new HttpClient())
            {
                // Get Outlets
                httpClient.Timeout = TimeSpan.FromMinutes(10);
                var response = await httpClient.GetAsync("https://localhost:44305/systemjob/PortalGetOutlets");
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                var decryptedContent = Decrypt(content);
                JArray outletArray = new JArray();
                outletArray = JArray.Parse(decryptedContent);

                // Get TimeSlots
                response = await httpClient.GetAsync("https://localhost:44305/systemjob/PortalGetTimeSlots");
                response.EnsureSuccessStatusCode();
                content = await response.Content.ReadAsStringAsync();
                decryptedContent = Decrypt(content);
                JArray timeSlotArray = new JArray();
                timeSlotArray = JArray.Parse(decryptedContent);

                // Prepare new cache list
                List <Outlet> outlets = new List<Outlet>();
                foreach(JObject item in outletArray)
                {
                    Outlet outlet = new Outlet();
                    outlet.Id = long.Parse(item.GetValue("ID").ToString());
                    outlet.Name = item.GetValue("Name").ToString();
                    outlet.Longitude = Double.Parse(item.GetValue("Long").ToString());
                    outlet.Latitude = Double.Parse(item.GetValue("Lat").ToString());
                    outlet.Address = item.GetValue("AddressStreet1").ToString();
                    outlet.PostalCode = item.GetValue("PostalCode").ToString();
                    outlet.City = item.GetValue("City").ToString();
                    outlet.PhoneNumber = item.GetValue("Phone").ToString();
                    outlet.Url = "https://www." + item.GetValue("UrlName").ToString() + ".zimsystem.dk?from=zimportal";
                    outlet.ShopDelayTakeAway = int.Parse(item.GetValue("ShopDelayTakeaway").ToString());
                    outlet.ShopDelayDelivery = int.Parse(item.GetValue("ShopDelayDelivery").ToString());
                    outlet.TimeSlots = new List<TimeSlot>();

                    // Get outlet logo
                    string jsonBody = $"{{\"outletID\": {outlet.Id}}}";
                    HttpContent imgRequestContent = new StringContent(Encrypt(jsonBody));
                    imgRequestContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                    var imgResponse = await httpClient.PostAsync("https://localhost:44305/systemjob/PortalGetOutletLogo", imgRequestContent);
                    imgResponse.EnsureSuccessStatusCode();
                    string imgResponseEncrypted = await imgResponse.Content.ReadAsStringAsync();
                    string imgResponseContent = Decrypt(imgResponseEncrypted);
                    string deserializedBase64 = JsonSerializer.Deserialize<string>(imgResponseContent);
                    // If no image for outlet
                    if (imgResponseContent == "\"\"")
                    {
                        string path = Directory.GetCurrentDirectory() + "/wwwroot/Images/defaultlogo.png";
                        byte[] imgBytes = File.ReadAllBytes(path);
                        outlet.ImageB64 = Convert.ToBase64String(imgBytes);
                    }
                    else 
                    {
                        outlet.ImageB64 = deserializedBase64;
                    }

                    // TEMPORARY UNTIL ZIMSYSTEM UPDATE
                    Random random = new Random();
                    outlet.FoodTypes = new List<string>();
                    outlet.FoodTypes.Add(foodTypes[random.Next(foodTypes.Length)]);
                    outlet.FoodTypes.Add(foodTypes[random.Next(foodTypes.Length)]);

                    // Find and add timeslot
                    foreach (JObject slot in timeSlotArray)
                    {
                        // Check for empty values
                        JToken outletId = slot["OutletID"];
                        if(outletId.ToString() != "")
                        {
                            // If matching outlet id
                            //System.Diagnostics.Debug.WriteLine(outletId.ToString());
                            if (long.Parse(outletId.ToString()) == outlet.Id)
                            {
                                TimeSlot ts = new TimeSlot();
                                ts.Weekday = int.Parse(slot.GetValue("Weekday").ToString());
                                ts.Time = TimeSpan.Parse(slot.GetValue("Time").ToString());
                                outlet.TimeSlots.Add(ts);
                            }
                        }
                    }

                    outlets.Add(outlet);
                }
                if (outlets[0] == null) throw new NullReferenceException("No outlets in cache.");
                else return outlets;
            }
        }


        /*
         * Decryption Method
         * Given by ZimSystem ApS
         */

        static string Decrypt(string encryptedKey)
        {
            try
            {
                string textToDecrypt = encryptedKey;
                string returnStr = "";
                string publicKey = "94631337";
                string secretKey = "13374747";
                byte[] privateKeyByte = { };
                privateKeyByte = System.Text.Encoding.UTF8.GetBytes(secretKey);
                byte[] publicKeybyte = { };
                publicKeybyte = System.Text.Encoding.UTF8.GetBytes(publicKey);
                MemoryStream ms = null;
                CryptoStream cs = null;
                byte[] inputbyteArray = new byte[textToDecrypt.Replace(" ", "+").Length];
                inputbyteArray = Convert.FromBase64String(textToDecrypt.Replace(" ", "+"));
                using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
                {
                    ms = new MemoryStream();
                    cs = new CryptoStream(ms, des.CreateDecryptor(publicKeybyte, privateKeyByte), CryptoStreamMode.Write);
                    cs.Write(inputbyteArray, 0, inputbyteArray.Length);
                    cs.FlushFinalBlock();
                    Encoding encoding = Encoding.UTF8;
                    returnStr = encoding.GetString(ms.ToArray());
                }
                return returnStr;
            }
            catch (Exception ae)
            {
                throw new Exception(ae.Message, ae.InnerException);
            }
        }
        static string Encrypt(string decryptedKey)
        {
            try
            {
                string textToEncrypt = decryptedKey;
                string ToReturn = "";
                string publickey = "94631337";
                string secretkey = "13374747";
                byte[] secretkeyByte = { };
                secretkeyByte = System.Text.Encoding.UTF8.GetBytes(secretkey);
                byte[] publickeybyte = { };
                publickeybyte = System.Text.Encoding.UTF8.GetBytes(publickey);
                MemoryStream ms = null;
                CryptoStream cs = null;
                byte[] inputbyteArray = System.Text.Encoding.UTF8.GetBytes(textToEncrypt);
                using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
                {
                    ms = new MemoryStream();
                    cs = new CryptoStream(ms, des.CreateEncryptor(publickeybyte, secretkeyByte), CryptoStreamMode.Write);
                    cs.Write(inputbyteArray, 0, inputbyteArray.Length);
                    cs.FlushFinalBlock();
                    ToReturn = Convert.ToBase64String(ms.ToArray());
                }
                return ToReturn;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message, ex.InnerException);
            }
        }
    }
}
