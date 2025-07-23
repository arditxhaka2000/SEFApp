using SEFApp.Models.Fiscal;
using SEFApp.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SEFApp.Services
{
    public class FiscalCertificateService : IFiscalCertificateService
    {
        private readonly HttpClient _httpClient;
        private readonly IPreferencesService _preferences;
        private readonly string _testBaseUrl = "https://fiskalizimi-test.atk-ks.org";
        private readonly string _prodBaseUrl = "https://fiskalizimi.atk-ks.org";
        // Hardcoded PEM key
        private const string HARDCODED_PEM_KEY = @"-----BEGIN EC PRIVATE KEY-----
MHcCAQEEIBfHKZuDiuZDWeDR4lTTOcPKrI6qech0skrqzA4VVa/toAoGCCqGSM49
AwEHoUQDQgAEawBIIFQ1YaJ2ZhdHYOMQ4u8JzV9QpzDrCJn6af6oZ5AdamEiEIXJ
TipI7wDQCrKJUJ3+EOxb8XD3DK79BbLk7g==
-----END EC PRIVATE KEY-----";

        private ECDsa _privateKey;
        private X509Certificate2 _certificate;

        public FiscalCertificateService(HttpClient httpClient, IPreferencesService preferences)
        {
            _httpClient = httpClient;
            _preferences = preferences;

            // Load the hardcoded key immediately
            _privateKey = LoadPrivateKeyFromPem(HARDCODED_PEM_KEY);
        }

        public async Task<bool> IsOnboardedAsync()
        {
            return true;
        }

        public async Task<bool> OnboardBusinessAsync(OnboardingRequest request)
        {
            try
            {
                // Step 1: Generate ECDSA key pair
                var privateKey = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                var privateKeyPem = ConvertPrivateKeyToPem(privateKey);

                // Step 2: Get verification code
                var verificationResponse = await GetVerificationCodeAsync(request);
                if (verificationResponse == null)
                    return false;

                // Step 3: Generate CSR
                var csr = GenerateCertificateSigningRequest(privateKey, verificationResponse.BusinessName, request);

                // Step 4: Submit CSR for signing
                var certificateResponse = await SubmitCsrAsync(new CsrRequest
                {
                    BusinessName = verificationResponse.BusinessName,
                    BusinessId = request.BusinessId,
                    BranchId = request.BranchId,
                    VerificationCode = verificationResponse.VerificationCode,
                    PosId = request.PosId,
                    ApplicationId = request.ApplicationId,
                    Csr = csr
                });

                if (certificateResponse != null)
                {
                    // Store private key and certificate
                    await _preferences.SetAsync("fiscal_private_key", privateKeyPem);
                    await _preferences.SetAsync("fiscal_certificate", certificateResponse.SignedCertificate);

                    // Store business information
                    await _preferences.SetAsync("fiscal_business_id", request.BusinessId.ToString());
                    await _preferences.SetAsync("fiscal_pos_id", request.PosId.ToString());
                    await _preferences.SetAsync("fiscal_branch_id", request.BranchId.ToString());
                    await _preferences.SetAsync("fiscal_application_id", request.ApplicationId.ToString());

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Onboarding failed: {ex.Message}");
                return false;
            }
        }

        public async Task<string> SignDataAsync(byte[] data)
        {
            try
            {
                if (_privateKey == null)
                    throw new InvalidOperationException("Private key not available");

                var hash = SHA256.HashData(data);
                var signature = _privateKey.SignHash(hash, DSASignatureFormat.Rfc3279DerSequence);

                return Convert.ToBase64String(signature);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Signing failed: {ex.Message}", ex);
            }
        }

        public async Task<string> GenerateSimpleQrCodeAsync(decimal totalAmount, string transactionNumber, DateTime transactionDate)
        {
            try
            {
                var simpleReceipt = new
                {
                    TransactionNumber = transactionNumber,
                    Date = transactionDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    Total = totalAmount,
                    Timestamp = ((DateTimeOffset)transactionDate).ToUnixTimeSeconds()
                };

                var receiptJson = JsonSerializer.Serialize(simpleReceipt);
                var receiptData = Encoding.UTF8.GetBytes(receiptJson);
                var base64Data = Convert.ToBase64String(receiptData);

                var signature = await SignDataAsync(Encoding.UTF8.GetBytes(base64Data));

                return $"{base64Data}|{signature}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"QR generation failed: {ex.Message}");
                return $"TXN:{transactionNumber}|€{totalAmount:F2}|{transactionDate:yyyy-MM-dd HH:mm}";
            }
        }

        public async Task<(string details, string signature)> SignPosCouponAsync(PosCoupon coupon)
        {
            try
            {
                // Serialize POS coupon to protobuf
                var couponData = SerializePosCoupon(coupon);
                var base64Data = Convert.ToBase64String(couponData);

                // Sign the data
                var signature = await SignDataAsync(Encoding.UTF8.GetBytes(base64Data));

                return (base64Data, signature);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"POS coupon signing failed: {ex.Message}", ex);
            }
        }

        private async Task EnsureKeysLoadedAsync()
        {
            if (_privateKey == null)
            {
                var privateKeyPem = await _preferences.GetAsync("fiscal_private_key", string.Empty);
                if (!string.IsNullOrEmpty(privateKeyPem))
                {
                    _privateKey = LoadPrivateKeyFromPem(privateKeyPem);
                }
            }

            if (_certificate == null)
            {
                var certificatePem = await _preferences.GetAsync("fiscal_certificate", string.Empty);
                if (!string.IsNullOrEmpty(certificatePem))
                {
                    var certBytes = Convert.FromBase64String(certificatePem.Replace("-----BEGIN CERTIFICATE-----", "").Replace("-----END CERTIFICATE-----", "").Replace("\n", ""));
                    _certificate = new X509Certificate2(certBytes);
                }
            }
        }

        private async Task<VerificationResponse> GetVerificationCodeAsync(OnboardingRequest request)
        {
            var url = $"{GetBaseUrl()}/ca/verify/{request.BusinessId}";
            var content = new
            {
                fiscalization_no = request.FiscalizationNo,
                pos_id = request.PosId,
                branch_id = request.BranchId,
                application_id = request.ApplicationId
            };

            var json = JsonSerializer.Serialize(content);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, httpContent);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<VerificationResponse>(responseJson);
            }

            return null;
        }

        private async Task<CertificateResponse> SubmitCsrAsync(CsrRequest request)
        {
            var url = $"{GetBaseUrl()}/ca/signcsr";
            var json = JsonSerializer.Serialize(request);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, httpContent);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<CertificateResponse>(responseJson);
            }

            return null;
        }

        private string GetBaseUrl()
        {
            // You can make this configurable
            return _testBaseUrl; // Use _prodBaseUrl for production
        }

        private string ConvertPrivateKeyToPem(ECDsa privateKey)
        {
            var keyBytes = privateKey.ExportECPrivateKey();
            return $"-----BEGIN EC PRIVATE KEY-----\n{Convert.ToBase64String(keyBytes)}\n-----END EC PRIVATE KEY-----";
        }

        private ECDsa LoadPrivateKeyFromPem(string pem)
        {
            var base64 = pem.Replace("-----BEGIN EC PRIVATE KEY-----", "").Replace("-----END EC PRIVATE KEY-----", "").Replace("\n", "");
            var keyBytes = Convert.FromBase64String(base64);

            var ecdsa = ECDsa.Create();
            ecdsa.ImportECPrivateKey(keyBytes, out _);
            return ecdsa;
        }

        private string GenerateCertificateSigningRequest(ECDsa privateKey, string businessName, OnboardingRequest request)
        {
            // Create CSR using .NET built-in functionality
            var subject = new X500DistinguishedName($"C=RKS, O={request.BusinessId}, OU={request.PosId}, L={request.BranchId}, CN={businessName}");

            var csrRequest = new CertificateRequest(subject, privateKey, HashAlgorithmName.SHA256);

            // Create the CSR
            var csr = csrRequest.CreateSigningRequest();

            // Convert to PEM format
            return $"-----BEGIN CERTIFICATE REQUEST-----\n{Convert.ToBase64String(csr)}\n-----END CERTIFICATE REQUEST-----";
        }

        //private byte[] SerializeCitizenCoupon(CitizenCoupon coupon)
        //{
        //    return ProtobufSerializer.SerializeCitizenCoupon(coupon);
        //}

        private byte[] SerializePosCoupon(PosCoupon coupon)
        {
            return ProtobufSerializer.SerializePosCoupon(coupon);
        }

        // Response models
        private class VerificationResponse
        {
            public string business_name { get; set; } = string.Empty;
            public string verification_code { get; set; } = string.Empty;

            public string BusinessName => business_name;
            public string VerificationCode => verification_code;
        }

        private class CertificateResponse
        {
            public string signed_certificate { get; set; } = string.Empty;
            public string SignedCertificate => signed_certificate;
        }

        private class CsrRequest
        {
            public string BusinessName { get; set; } = string.Empty;
            public long BusinessId { get; set; }
            public long BranchId { get; set; }
            public string VerificationCode { get; set; } = string.Empty;
            public long PosId { get; set; }
            public long ApplicationId { get; set; }
            public string Csr { get; set; } = string.Empty;
        }
    }
}
