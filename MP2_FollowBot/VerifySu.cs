using DreamPoeBot.Loki.Bot;
using MP2.EXtensions;
using System;
using System.Management;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;

namespace MP2
{
    public class VerifySuApiClient
    {
        public static VerifySuApiClient VerifySu = new VerifySuApiClient();
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string ApiUrl = "https://verify.su/api/v1/verify";
        private const string _apiKey = "6c544b4ed6226506a264cae6252eaf68"; // API Key fornecida pela Verify.SU

        // Parâmetros de configuração do produto
        private const int SecretNumber = 3506;
        private const int EncryptPos = 30;
        private const int AnsPos1 = 28;
        private const int AnsPos2 = 24;
        private const int AnsPos3 = 31;
        private const int AnsPos4 = 17;
        private const int TickPos1 = 0;
        private const int TickPos2 = 21;
        private const int TickPos3 = 16;
        private const string ProductId = "156";  // ID do seu produto na Verify.SU

        public VerifySuApiClient()
        {

        }

        public static string GetHardwareId()
        {
            try
            {
                string hwid = "";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");

                foreach (ManagementObject obj in searcher.Get())
                {
                    hwid = obj["SerialNumber"]?.ToString().Trim();
                    break; // Pega apenas o primeiro disco
                }

                return hwid ?? System.Environment.MachineName;
            }
            catch (Exception ex)
            {
                return "Erro ao obter HWID: ";
            }
        }

        public static async Task<bool> VerifyUserKey()
        {
            string userKey = Mp2Settings.Instance.UserKey;  // A chave precisa ser capturada da UI ou de um arquivo
            string hwid = GetHardwareId();
            bool isValid = await VerifySu.VerifyKey(userKey, hwid);
            if (!isValid)
            {
                GlobalLog.Error("[Mp2] Acess Denied!.");
                BotManager.Stop();
                return false;
            }
            return true;
        }

        public async Task<bool> VerifyKey(string key, string hwid)
        {
            try
            {
                // Definir um valor dinâmico para tick (pode ser tempo ou random)
                int tick = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string hexTick = ((tick % SecretNumber) + 256).ToString("X3"); // Gera um HEX de 3 caracteres

                // Criar os parâmetros da requisição
                var requestUri = $"{ApiUrl}/?id={ProductId}&ver=0&key={key}&hwid={hwid}&tick={tick}";

                // Enviar a requisição para o servidor
                HttpResponseMessage response = await _httpClient.GetAsync(requestUri);
                string responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Mp2Settings.Instance.DaysLeft = "Access denied.";
                    GlobalLog.Error($"[Mp2] Acess Denied");
                    return false;
                }

                // Verificar a resposta
                if (!await ValidateResponse(responseText, hexTick))
                {
                    Mp2Settings.Instance.DaysLeft = "Access denied.";
                    GlobalLog.Error("[Mp2] Acess Denied.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                GlobalLog.Error($"[Verify.SU] Key error");
                return false;
            }
        }

        private async Task<bool> ValidateResponse(string response, string expectedHexTick)
        {
            // Verifica se os caracteres nas posições TickPos1, TickPos2 e TickPos3 correspondem ao hex esperado
            string tickResponse = $"{response[TickPos1]}{response[TickPos2]}{response[TickPos3]}";
            if (tickResponse != expectedHexTick)
            {
                return false;
            }

            // Determinar a posição de criptografia no alfabeto
            string alphabet = "0123456789ABCDEF";
            int n = alphabet.IndexOf(response[EncryptPos]);

            // Recuperar o código de resposta deslocado
            string code = $"{response[AnsPos1]}{response[AnsPos2]}{response[AnsPos3]}{response[AnsPos4]}";

            // Deslocar os caracteres para recuperar o código real
            string decryptedCode = string.Empty;
            foreach (char c in code)
            {
                int newIndex = (alphabet.IndexOf(c) - n + 16) % 16;
                decryptedCode += alphabet[newIndex];
            }

            // Comparar com códigos conhecidos
            if (decryptedCode == "97D3")
            {
                var DaysLeft = await GetKeyExpirationDate(Mp2Settings.Instance.UserKey);
                if (DaysLeft == null) DaysLeft = "1";
                Mp2Settings.Instance.DaysLeft = DaysLeft;
                GlobalLog.Info("[Verify.SU] Access sucessuful.");
                return true;
            }
            if (decryptedCode == "572E")
            {
                var DaysLeft = await GetKeyExpirationDate(Mp2Settings.Instance.UserKey);
                if (DaysLeft == null) DaysLeft = "1";
                Mp2Settings.Instance.DaysLeft = DaysLeft;
                GlobalLog.Info("[Verify.SU] Access sucessuful.");
                return true;
            }
            if (decryptedCode == "4F4B")
            {
                GlobalLog.Info("[Verify.SU] Please update your Mp2 Bot.");
                return false;
            }
            if (decryptedCode == "0A14")
            {
                Mp2Settings.Instance.DaysLeft = "Your key is expired.";
                GlobalLog.Info("[Verify.SU] Your Key is expired.");
                return false;
            }
            if (decryptedCode == "3FC9")
            {
                Mp2Settings.Instance.DaysLeft = "Cant find your key.";
                GlobalLog.Info("[Verify.SU] Cant find your key.");
                return false;
            }
            if (decryptedCode == "903F")
            {
                Mp2Settings.Instance.DaysLeft = "Key from another product.";
                GlobalLog.Info("[Verify.SU] Key from another product.");
                return false;
            }

            if (decryptedCode == "7345")
            {
                Mp2Settings.Instance.DaysLeft = "Access denied.";
                GlobalLog.Info("[Verify.SU] Private access denied.");
                return false;
            }

            if (decryptedCode == "3AEE")
            {
                Mp2Settings.Instance.DaysLeft = "HWID banned.";
                GlobalLog.Info("[Verify.SU] HWID banned");
                return false;
            }

            return false;
        }
        public async Task<string?> GetKeyExpirationDate(string userKey)
        {
            try
            {
                string requestUrl = $"https://verify.su/api/v1/getdate/?api={_apiKey}&key={userKey}";

                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);
                string responseText = await response.Content.ReadAsStringAsync();
                // Extraindo os dias restantes do formato esperado: (N) dd.mm.yy, H:m
                Match match = Regex.Match(responseText, @"\(([-]?\d+)\) (\d{2}\.\d{2}\.\d{4}), (\d{2}:\d{2})");
                if (match.Success)
                {
                    string daysLeft = match.Groups[1].Value;
                    string ExpirationDate = match.Groups[2].Value;
                    string ExpirationTime = match.Groups[3].Value;
                    daysLeft = ExpirationDate + "/" + ExpirationTime;
                    return daysLeft;
                }

                GlobalLog.Warn("[Verify.SU] Error");
                return null;
            }
            catch (Exception ex)
            {
                GlobalLog.Warn($"[Verify.SU] Error");
                return null;
            }
        }

        public async Task<bool> SendMessageToVerifySu(string message, string color = "")
        {
            try
            {
                // Certifica-se de que a mensagem está corretamente codificada para URL
                string encodedMessage = HttpUtility.UrlEncode(message);

                // Criar a URL da requisição
                string requestUrl = $"{ApiUrl}sendmsg/?api={_apiKey}&id={ProductId}&hwid={GetHardwareId}&key={Mp2Settings.Instance.UserKey}&msg={encodedMessage}";

                // Se o usuário especificar uma cor, adiciona à URL
                if (!string.IsNullOrEmpty(color))
                {
                    requestUrl += $"&color={color}";
                }

                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);
                string responseText = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[Verify.SU] Erro ao enviar mensagem");
                    return false;
                }

                Console.WriteLine($"[Verify.SU] Mensagem enviada com sucesso");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Verify.SU] Erro ao enviar mensagem");
                return false;
            }
        }
    }
}
