using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityDriver.Models.Mapping;

namespace TraceabilityDriver.Tests.Models.Mapping
{
    [TestFixture]
    public class CommonEventMergeTests
    {
        [Test]
        public void CommonEvent_Merge_WithNullTargetProperties_ShouldCopyFromSource()
        {
            // Arrange
            var source = new CommonEvent
            {
                EventTime = DateTime.Now,
                Location = new CommonLocation { Name = "Source Site" },
                Certificates = new CommonCertificates { FishingAuthorization = new CommonCertificate { Identifier = "Cert1" } },
                Products = new List<CommonProduct> { new CommonProduct { ProductId = "Product1" } }
            };

            var target = new CommonEvent();

            // Act
            target.Merge(source);

            // Assert
            Assert.That(target.EventTime, Is.EqualTo(source.EventTime));
            Assert.That(target.Location, Is.EqualTo(source.Location));
            Assert.That(target.Certificates, Is.EqualTo(source.Certificates));
            Assert.That(target.Products, Is.EqualTo(source.Products));
        }

        [Test]
        public void CommonEvent_Merge_WithNonNullNestedObjects_ShouldMergeNestedObjects()
        {
            // Arrange
            var source = new CommonEvent
            {
                Location = new CommonLocation { Name = "Source Site", LocationId = "SiteId1" },
                Certificates = new CommonCertificates { FishingAuthorization = new CommonCertificate { Identifier = "SourceCert" } },
            };

            var target = new CommonEvent
            {
                Location = new CommonLocation { Name = "Target Site" },
                Certificates = new CommonCertificates { FishingAuthorization = new CommonCertificate { Identifier = null } },
            };

            // Act
            target.Merge(source);

            // Assert
            Assert.That(target.Location.Name, Is.EqualTo("Target Site")); // Name shouldn't be merged as it's not null
            Assert.That(target.Location.LocationId, Is.EqualTo("SiteId1")); // LocationId should be merged
            Assert.That(target.Certificates.FishingAuthorization.Identifier, Is.EqualTo("SourceCert")); // Identifier should be merged
        }

        [Test]
        public void CommonEvent_Merge_WithNullTargetProducts_ShouldCopySourceProducts()
        {
            // Arrange
            var source = new CommonEvent
            {
                Products = new List<CommonProduct>
                    {
                        new CommonProduct { ProductId = "Product1" },
                        new CommonProduct { ProductId = "Product2" }
                    }
            };

            var target = new CommonEvent();

            // Act
            target.Merge(source);

            // Assert
            Assert.That(target.Products, Is.Not.Null);
            Assert.That(target.Products, Has.Count.EqualTo(2));
            Assert.That(target.Products.Select(p => p.ProductId), Is.EquivalentTo(new[] { "Product1", "Product2" }));
        }

        [Test]
        public void CommonEvent_Merge_WithOverlappingProducts_ShouldMergeProducts()
        {
            // Arrange
            var source = new CommonEvent
            {
                Products = new List<CommonProduct>
                    {
                        new CommonProduct { ProductId = "Product1", Name = "Source Name" },
                        new CommonProduct { ProductId = "Product3", Name = "Product 3" }
                    }
            };

            var target = new CommonEvent
            {
                Products = new List<CommonProduct>
                    {
                        new CommonProduct { ProductId = "Product1", Quantity = 10 },
                        new CommonProduct { ProductId = "Product2", Name = "Product 2" }
                    }
            };

            // Act
            target.Merge(source);

            // Assert
            Assert.That(target.Products, Has.Count.EqualTo(3));

            var product1 = target.Products.FirstOrDefault(p => p.ProductId == "Product1");
            Assert.That(product1, Is.Not.Null);
            Assert.That(product1.Name, Is.EqualTo("Source Name"));
            Assert.That(product1.Quantity, Is.EqualTo(10));

            Assert.That(target.Products.Any(p => p.ProductId == "Product2"), Is.True);
            Assert.That(target.Products.Any(p => p.ProductId == "Product3"), Is.True);
        }
    }
}
