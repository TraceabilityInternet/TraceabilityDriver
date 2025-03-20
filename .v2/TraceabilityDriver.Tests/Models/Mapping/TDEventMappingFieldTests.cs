using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityDriver.Models.Mapping;

namespace TraceabilityDriver.Tests.Models.Mapping
{
    [TestFixture]
    public class TDEventMappingFieldTests
    {
        [Test]
        public void GetMappingValue_StaticValue_ReturnsCorrectValue()
        {
            // Arrange
            var propertyInfo = typeof(DummyClass).GetProperty(nameof(DummyClass.DummyProperty));
            Assert.That(propertyInfo, Is.Not.Null);
            var field = new TDEventMappingField("path", "!123", propertyInfo);
            var dataRow = GetDataRow();

            // Act
            var result = field.GetMappingValue(dataRow);

            // Assert
            Assert.That(result.Type, Is.EqualTo(TDEventMappingValueType.Static));
            Assert.That(result.Values[0], Is.EqualTo("123"));
        }

        [Test]
        public void GetMappingValue_VariableValue_ReturnsCorrectValue()
        {
            // Arrange
            var propertyInfo = typeof(DummyClass).GetProperty(nameof(DummyClass.DummyProperty));
            Assert.That(propertyInfo, Is.Not.Null);
            var field = new TDEventMappingField("path", "$Column1", propertyInfo);
            var dataRow = GetDataRow();

            // Act
            var result = field.GetMappingValue(dataRow);

            // Assert
            Assert.That(result.Type, Is.EqualTo(TDEventMappingValueType.Variable));
            Assert.That(result.Values[0], Is.EqualTo("Value1"));
        }

        [Test]
        public void GetMappingValue_FunctionCall_ReturnsCorrectValue()
        {
            // Arrange
            var propertyInfo = typeof(DummyClass).GetProperty(nameof(DummyClass.DummyProperty));
            Assert.That(propertyInfo, Is.Not.Null);
            var field = new TDEventMappingField("path", "Function($Column1, 22.2)", propertyInfo);
            var dataRow = GetDataRow();

            // Act
            var result = field.GetMappingValue(dataRow);

            // Assert
            Assert.That(result.Type, Is.EqualTo(TDEventMappingValueType.Function));
            Assert.That(result.FunctionName, Is.EqualTo("Function"));
            Assert.That(result.Values[0], Is.EqualTo("Value1"));
            Assert.That(result.Values[1], Is.EqualTo("22.2"));
        }

        private DataRow GetDataRow()
        {
            var table = new DataTable();
            table.Columns.Add("Column1", typeof(string));
            var row = table.NewRow();
            row["Column1"] = "Value1";
            table.Rows.Add(row);
            return row;
        }

        private class DummyClass
        {
            public string DummyProperty { get; set; } = string.Empty;
        }
    }
}
