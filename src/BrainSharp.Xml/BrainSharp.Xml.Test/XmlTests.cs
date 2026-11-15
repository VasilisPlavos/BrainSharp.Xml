using System.Xml.Linq;

namespace BrainSharp.Xml.Test
{
    public class XmlTests
    {
        [SetUp]
        public void Setup(){}

        [Test]
        public void ExplainDifference_StringContents_Failed()
        {
            // Arrange
            const string xml1 = "broken xml content";
            List<string>? listOfDifferences = null;

            // Act
            try
            {
                listOfDifferences = Xml.ExplainDifference(xml1, xml1)!;
            }
            catch
            {
                //
            }

            // Assert
            Assert.That(listOfDifferences, Is.Null);
        }

        [Test]
        public void ExplainDifference_StringContents_ZeroDifferences()
        {
            // Arrange
            var xml1 = "<root><child1>text1</child1><child2 attr1='value1'>text2</child2></root>";

            // Act
            var listOfDifferences = Xml.ExplainDifference(xml1, xml1);

            // Assert
            Assert.That(listOfDifferences.Count, Is.EqualTo(0));
        }

        [Test]
        public void ExplainDifference_XDocuments_157Differences()
        {
            // Arrange
            var filePath1 = $@"{AppContext.BaseDirectory}Files\products1.xml";
            var filePath2 = $@"{AppContext.BaseDirectory}\Files\products2.xml";
            var xdoc1 = Xml.ParseFromFile(filePath1);
            var xdoc2 = Xml.ParseFromFile(filePath2);

            // Act
            var listOfDifferences = Xml.ExplainDifference(xdoc1, xdoc2);

            // Assert
            Assert.That(listOfDifferences.Count, Is.EqualTo(157));
        }

        [Test]
        public async Task ExplainDifferenceFromFileAsync_TwoFiles_157Differences()
        {
            // Arrange
            var filePath1 = $@"{AppContext.BaseDirectory}\Files\products1.xml";
            var filePath2 = $@"{AppContext.BaseDirectory}\Files\products2.xml";

            // Act
            var listOfDifferences = await Xml.ExplainDifferenceFromFilesAsync(filePath1, filePath2);

            // Assert
            Assert.That(listOfDifferences.Count, Is.EqualTo(157));
        }

        [Test]
        [TestCase("products1.xml", "products2.xml", 157)]
        [TestCase("products3a.xml", "products3b.xml", 1)]
        public void ExplainDifferenceFromFile_TwoFiles_XDifferences(string fileName1, string fileName2, int expectedNumberOfDifferences)
        {
            // Arrange
            var filePath1 = $@"{AppContext.BaseDirectory}\Files\{fileName1}";
            var filePath2 = $@"{AppContext.BaseDirectory}\Files\{fileName2}";

            // Act
            var listOfDifferences = Xml.ExplainDifferenceFromFiles(filePath1, filePath2);

            // Assert
            Assert.That(listOfDifferences.Count, Is.EqualTo(expectedNumberOfDifferences));
        }


        //// TODO: Not working as expected
        //// ParseFromFileAsync has to be fixed
        /// At the moment it is overrided by ParseFromFile
        //[Test]
        //public async Task ExplainDifferenceFromFile_1File_0Differences()
        //{
        //    // Arrange
        //    var filePath1 = $@"{AppContext.BaseDirectory}\Files\products1.xml";
        //    var doc1 = Xml.ParseFromFile(filePath1);
        //    var doc2 = await Xml.ParseFromFileAsyncReal(filePath1);

        //    // Act
        //    var listOfDifferences = Xml.ExplainDifference(doc1, doc2);

        //    // Assert
        //    Assert.That(listOfDifferences.Count, Is.EqualTo(0));
        //}

        [Test]
        public void IsEqual_XDocuments_Succeed()
        {
            // Arrange
            var xdoc = Xml.ParseFromFile($@"{AppContext.BaseDirectory}\Files\products1.xml");

            // Act
            var isEqual = Xml.IsEqual(xdoc, xdoc);

            // Assert
            Assert.True(isEqual);
        }

        [Test]
        public void Parse_StringContent_Fail()
        {
            // Arrange
            var xml = "broken xml content";
            XDocument? xDocument = null;

            // Act
            try
            {
                xDocument = Xml.Parse(xml);
            }
            catch
            {
                // ignored
            }

            // Assert
            Assert.Null(xDocument);
        }

        [Test]
        public void Parse_StringContent_Succeed()
        {
            // Arrange
            var xml1 = "<root><child1>text1</child1><child2 attr1='value1'>text2</child2></root>";

            // Act
            var xDocument = Xml.Parse(xml1);

            // Assert
            Assert.NotNull(xDocument.Root);
        }

        [Test]
        public void ParseFromFile_File_Succeed()
        {
            // Arrange
            var filePath = $@"{AppContext.BaseDirectory}\Files\products1.xml";

            // Act
            var xDocument = Xml.ParseFromFile(filePath);

            // Assert
            Assert.NotNull(xDocument.Root);
        }

        [Test]
        public async Task ParseFromFileAsync_File_Failed()
        {
            // Arrange
            var filePath = $@"{AppContext.BaseDirectory}\Files\wrongfilename.xml";
            XDocument? xDocument = null;

            // Act
            try
            {
                xDocument = await Xml.ParseFromFileAsync(filePath);
            }
            catch
            {
                // ignored
            }

            // Assert
            Assert.Null(xDocument);
        }

        [Test]
        public void ExplainDifference_FirstHasExtraSibling_ReturnsOne()
        {
            var differences = Xml.ExplainDifference("<r><a id=\"1\"/><a id=\"2\"/></r>", "<r><a id=\"2\"/></r>");
            Assert.That(differences.Count, Is.EqualTo(1));
        }

        [Test]
        public void ExplainDifference_SecondHasExtraSiblingOfSameName_ReturnsOne()
        {
            var differences = Xml.ExplainDifference("<r><a id=\"2\"/></r>", "<r><a id=\"1\"/><a id=\"2\"/></r>");
            Assert.That(differences.Count, Is.EqualTo(1));
            Assert.That(differences[0], Does.StartWith("Missing child node on first xml"));
        }

        [Test]
        public void ExplainDifference_ReorderedSiblings_ReturnsZero()
        {
            var differences = Xml.ExplainDifference("<r><a id=\"1\"/><b/></r>", "<r><b/><a id=\"1\"/></r>");
            Assert.That(differences.Count, Is.EqualTo(0));
        }

        [Test]
        public void ExplainDifference_DuplicateSiblings_ReportsOnlySurplus()
        {
            var differences = Xml.ExplainDifference("<r><a/><a/></r>", "<r><a/></r>");
            Assert.That(differences.Count, Is.EqualTo(1));
        }

        [Test]
        public void ExplainDifference_SameNameDifferentNamespace_PairedAndReportedAsTagsDiffer()
        {
            // Paired by local name (no "Missing child node"), but the namespace difference is still reported
            var differences = Xml.ExplainDifference("<r><a xmlns=\"urn:x\"/></r>", "<r><a xmlns=\"urn:y\"/></r>");
            Assert.That(differences.Count, Is.EqualTo(1));
            Assert.That(differences[0], Does.StartWith("Tags differ"));
        }

        [Test]
        public async Task ParseFromFileAsync_File_Succeed()
        {
            // Arrange
            var filePath = $@"{AppContext.BaseDirectory}\Files\products1.xml";

            // Act
            var xDocument = await Xml.ParseFromFileAsync(filePath);

            // Assert
            Assert.NotNull(xDocument.Root);
        }
    }
}