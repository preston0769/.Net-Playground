using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FibonacciProject.Tests
{
    [TestClass]
    public class MyTestClass
    {

        [TestMethod]
        public void Fib_Given0_Return0()
        {
            int n = 0;
            int result = Fibonacci.Fib(n);
            Assert.AreEqual(n, result);
        }

        [TestMethod]
        public void Fib_Given1_Return1()
        {
            int n = 1;
            int result = Fibonacci.Fib(n);
            Assert.AreEqual(1, result);
        }
        
        [TestMethod]
        public void Fib_Given2_Return1()
        {
            int n = 2;
            int result = Fibonacci.Fib(n);
            Assert.AreEqual(1, result);
        }

        [TestMethod]
        public void Fib_Given3_Return2()
        {
            int n = 3;
            int result = Fibonacci.Fib(n);
            Assert.AreEqual(2, result);
        }

    }
}
