using Virinco.WATS.Interface;
using Xunit;
using Xunit.Abstractions;
using WATS.Testing;
using Virinco.WATS.Converter.Teradyne;

namespace Virinco.WATS.Converter.Teradyne.Tests
{
    // TextConverterBase: ImportReport() returns null (submit is internal).
    // Only ConvertOnly mode applies.
    public class ConverterTests : TextConverterTestBase
    {
        public ConverterTests(ITestOutputHelper output) : base(output) { }
        protected override IReportConverter_v2 CreateConverter() => new TerradyneSpectrumICTConverter();

        [Fact, Trait("TestMode", "ConvertOnly")]
        public void ConvertOnly_AllFiles() => RunAllFiles();
    }
}
