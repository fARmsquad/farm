using NUnit.Framework;
using System.Collections.Generic;
using FarmSimVR.Core.Economy;

namespace FarmSimVR.Tests.EditMode
{
    [TestFixture]
    public class WalletServiceTests
    {
        private WalletService _wallet;

        [SetUp]
        public void SetUp()
        {
            _wallet = new WalletService();
        }

        [Test]
        public void NewWallet_BalanceIsZero()
        {
            Assert.AreEqual(0, _wallet.Balance);
        }

        [Test]
        public void AddCoins_IncreasesBalance()
        {
            _wallet.AddCoins(10);
            Assert.AreEqual(10, _wallet.Balance);
        }

        [Test]
        public void AddCoins_MultipleTimes_AccumulatesBalance()
        {
            _wallet.AddCoins(10);
            _wallet.AddCoins(5);
            Assert.AreEqual(15, _wallet.Balance);
        }

        [Test]
        public void AddCoins_ZeroAmount_ThrowsArgumentOutOfRange()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => _wallet.AddCoins(0));
        }

        [Test]
        public void AddCoins_NegativeAmount_ThrowsArgumentOutOfRange()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => _wallet.AddCoins(-5));
        }

        [Test]
        public void SpendCoins_SufficientBalance_ReturnsTrueAndDeductsBalance()
        {
            _wallet.AddCoins(20);
            bool result = _wallet.SpendCoins(15);
            Assert.IsTrue(result);
            Assert.AreEqual(5, _wallet.Balance);
        }

        [Test]
        public void SpendCoins_ExactBalance_ReturnsTrueAndZerosBalance()
        {
            _wallet.AddCoins(10);
            bool result = _wallet.SpendCoins(10);
            Assert.IsTrue(result);
            Assert.AreEqual(0, _wallet.Balance);
        }

        [Test]
        public void SpendCoins_InsufficientBalance_ReturnsFalse()
        {
            _wallet.AddCoins(5);
            bool result = _wallet.SpendCoins(10);
            Assert.IsFalse(result);
        }

        [Test]
        public void SpendCoins_InsufficientBalance_BalanceUnchanged()
        {
            _wallet.AddCoins(5);
            _wallet.SpendCoins(10);
            Assert.AreEqual(5, _wallet.Balance);
        }

        [Test]
        public void SpendCoins_ZeroAmount_ThrowsArgumentOutOfRange()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => _wallet.SpendCoins(0));
        }

        [Test]
        public void SpendCoins_NegativeAmount_ThrowsArgumentOutOfRange()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => _wallet.SpendCoins(-1));
        }

        [Test]
        public void OnBalanceChanged_FiredAfterAddCoins()
        {
            var fired = new List<int>();
            _wallet.OnBalanceChanged += balance => fired.Add(balance);

            _wallet.AddCoins(7);

            Assert.AreEqual(1, fired.Count);
            Assert.AreEqual(7, fired[0]);
        }

        [Test]
        public void OnBalanceChanged_FiredAfterSuccessfulSpend()
        {
            _wallet.AddCoins(20);
            var fired = new List<int>();
            _wallet.OnBalanceChanged += balance => fired.Add(balance);

            _wallet.SpendCoins(6);

            Assert.AreEqual(1, fired.Count);
            Assert.AreEqual(14, fired[0]);
        }

        [Test]
        public void OnBalanceChanged_NotFiredAfterFailedSpend()
        {
            _wallet.AddCoins(5);
            var fired = new List<int>();
            _wallet.OnBalanceChanged += balance => fired.Add(balance);

            _wallet.SpendCoins(10);

            Assert.AreEqual(0, fired.Count);
        }
    }
}
