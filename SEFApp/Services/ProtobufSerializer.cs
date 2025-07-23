using Google.Protobuf;
using SEFApp.Models.Fiscal;
using SEFApp.Proto;
using System;
using System.Diagnostics;

namespace SEFApp.Services
{
    public static class ProtobufSerializer
    {
        public static byte[] SerializePosCoupon(Models.Fiscal.PosCoupon coupon)
        {
            Debug.WriteLine($"=== ATK PROTOBUF SERIALIZATION (PosCoupon) ===");
            Debug.WriteLine($"Input coupon has {coupon.Items?.Count ?? 0} items");
            Debug.WriteLine($"Input coupon has {coupon.Payments?.Count ?? 0} payments");
            Debug.WriteLine($"Input coupon has {coupon.TaxGroups?.Count ?? 0} tax groups");

            var protoCoupon = new SEFApp.Proto.PosCoupon
            {
                BusinessId = 810151580,
                CouponId = (ulong)coupon.CouponId,
                BranchId = (ulong)coupon.BranchId,
                Location = coupon.Location,
                OperatorId = coupon.OperatorId,
                PosId = (ulong)coupon.PosId,
                ApplicationId = 1235,
                VerificationNo = coupon.VerificationNo,
                Type = (SEFApp.Proto.CouponType)coupon.Type,
                Time = coupon.Time, // Should already be Unix timestamp
                Total = coupon.Total, // Keep as long (fiscal amount)
                TotalTax = coupon.TotalTax, // Keep as long (fiscal amount)
                TotalNoTax = coupon.TotalNoTax, // Keep as long (fiscal amount)
                TotalDiscount = coupon.TotalDiscount, // Keep as long (fiscal amount)
                ReferenceNo = (ulong)coupon.ReferenceNo
            };

            Debug.WriteLine($"Basic fields set - BusinessId: {protoCoupon.BusinessId}, Total: {protoCoupon.Total}");

            // Add items - keep original amounts (don't convert from fiscal)
            if (coupon.Items != null)
            {
                Debug.WriteLine($"Processing {coupon.Items.Count} items...");
                foreach (var item in coupon.Items)
                {
                    Debug.WriteLine($"Adding item: {item.Name}, Price: {item.Price}, Qty: {item.Quantity}, TaxRate: {item.TaxRate}");

                    var protoItem = new SEFApp.Proto.CouponItem
                    {
                        Name = item.Name,
                        Price = item.Price, // Keep as long (fiscal amount)
                        Unit = item.Unit,
                        Quantity = (float)item.Quantity,
                        Total = item.Total, // Keep as long (fiscal amount)
                        TaxRate = item.TaxRate, // Keep as string ("C", "D", "E", etc.)
                        Type = item.Type
                    };

                    protoCoupon.Items.Add(protoItem);
                }
                Debug.WriteLine($"Added {protoCoupon.Items.Count} items to protobuf");
            }
            else
            {
                Debug.WriteLine("WARNING: coupon.Items is null!");
            }

            // Add payments - keep original amounts
            if (coupon.Payments != null)
            {
                Debug.WriteLine($"Processing {coupon.Payments.Count} payments...");
                foreach (var payment in coupon.Payments)
                {
                    Debug.WriteLine($"Adding payment: Type {payment.Type}, Amount: {payment.Amount}");
                    protoCoupon.Payments.Add(new SEFApp.Proto.Payment
                    {
                        Type = (SEFApp.Proto.PaymentType)payment.Type,
                        Amount = payment.Amount // Keep as long (fiscal amount)
                    });
                }
                Debug.WriteLine($"Added {protoCoupon.Payments.Count} payments to protobuf");
            }

            // Add tax groups - keep original amounts
            if (coupon.TaxGroups != null)
            {
                Debug.WriteLine($"Processing {coupon.TaxGroups.Count} tax groups...");
                foreach (var taxGroup in coupon.TaxGroups)
                {
                    Debug.WriteLine($"Adding tax group: Rate {taxGroup.TaxRate}, TotalForTax: {taxGroup.TotalForTax}, TotalTax: {taxGroup.TotalTax}");
                    protoCoupon.TaxGroups.Add(new SEFApp.Proto.TaxGroup
                    {
                        TaxRate = taxGroup.TaxRate, // Keep as string
                        TotalForTax = taxGroup.TotalForTax, // Keep as long (fiscal amount)
                        TotalTax = taxGroup.TotalTax // Keep as long (fiscal amount)
                    });
                }
                Debug.WriteLine($"Added {protoCoupon.TaxGroups.Count} tax groups to protobuf");
            }

            Debug.WriteLine($"Final protobuf summary:");
            Debug.WriteLine($"  Items: {protoCoupon.Items.Count}");
            Debug.WriteLine($"  Payments: {protoCoupon.Payments.Count}");
            Debug.WriteLine($"  TaxGroups: {protoCoupon.TaxGroups.Count}");
            Debug.WriteLine($"  Total: {protoCoupon.Total}");

            var bytes = protoCoupon.ToByteArray();
            Debug.WriteLine($"Generated protobuf bytes length: {bytes.Length}");
            Debug.WriteLine($"Base64: {Convert.ToBase64String(bytes)}");

            return bytes;
        }

        // Alternative method for simplified citizen coupon
        public static byte[] SerializeCitizenCoupon(Models.Fiscal.PosCoupon coupon)
        {
            Debug.WriteLine($"=== ATK PROTOBUF SERIALIZATION (CitizenCoupon) ===");

            var protoCoupon = new SEFApp.Proto.CitizenCoupon
            {
                BusinessId = (ulong)coupon.BusinessId,
                PosId = (ulong)coupon.PosId,
                CouponId = (ulong)coupon.CouponId,
                BranchId = (ulong)coupon.BranchId,
                Type = (SEFApp.Proto.CouponType)coupon.Type,
                Time = coupon.Time,
                Total = coupon.Total,
                TotalTax = coupon.TotalTax,
                TotalNoTax = coupon.TotalNoTax,
                TotalDiscount = coupon.TotalDiscount
            };

            // Add tax groups only
            if (coupon.TaxGroups != null)
            {
                foreach (var taxGroup in coupon.TaxGroups)
                {
                    protoCoupon.TaxGroups.Add(new SEFApp.Proto.TaxGroup
                    {
                        TaxRate = taxGroup.TaxRate,
                        TotalForTax = taxGroup.TotalForTax,
                        TotalTax = taxGroup.TotalTax
                    });
                }
            }

            var bytes = protoCoupon.ToByteArray();
            Debug.WriteLine($"CitizenCoupon protobuf bytes: {bytes.Length}");
            Debug.WriteLine($"Base64: {Convert.ToBase64String(bytes)}");

            return bytes;
        }
    }
}