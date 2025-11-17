using NetArchTest.Rules;
using NetSdrClientApp;
using NUnit.Framework;

namespace NetSdrClientAppTests
{
    public class ArchitectureTests
    {
        // Правило 1: Головний клас програми не повинен залежати від EchoServer (серверної частини)
        [Test]
        public void App_Should_Not_Depend_On_EchoServer()
        {
            var result = Types.InAssembly(typeof(NetSdrClient).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp")
                .ShouldNot()
                .HaveDependencyOn("EchoServer")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True, "UI шар (NetSdrClientApp) не повинен напряму залежати від EchoServer!");
        }

        // Правило 2: Папка Messages не повинна знати про Networking
      
        [Test]
        public void Messages_Should_Not_Depend_On_Networking()
        {
            // Знаходимо всі класи в папці Messages
            var result = Types.InAssembly(typeof(NetSdrClient).Assembly)
                .That()
                .ResideInNamespace("NetSdrClientApp.Messages")
                .ShouldNot()
                .HaveDependencyOn("NetSdrClientApp.Networking")
                .GetResult();

            Assert.That(result.IsSuccessful, Is.True, "Шар Messages не повинен залежати від Networking!");
        }
    }
}