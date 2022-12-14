using ExampleProject;
using NUnit.Framework;
using Moq;

namespace ExampleProject.Test
{
    [TestFixture]
    public class ExampleClass2Tests
    {
        private ExampleClass2 _exampleClass2UnderTest;
        private Mock<IKitten> num;
        private Mock<IPuppy> bebra;
        [SetUp]
        public void SetUp()
        {
            num = new Mock<IKitten>();
            bebra = new Mock<IPuppy>();
            int chislo = default;
            _exampleClass2UnderTest = new ExampleClass2(num.Object, bebra.Object, chislo);
        }

        [Test]
        public void Method1Test()
        {
            int num1 = default;
            int num2 = default;
            int actual = _exampleClass2UnderTest.Method1(num1, num2);
            int expected = default;
            Assert.That(actual, Is.EqualTo(expected));
            Assert.Fail("autogenerated");
        }

        [Test]
        public void Method2Test()
        {
            Assert.Fail("autogenerated");
        }
    }
}