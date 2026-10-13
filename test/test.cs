using NUnit.Framework; // Hoặc dùng Microsoft.VisualStudio.TestTools.UnitTesting;

namespace QuanLyKhachSan.test
{
    
    public class test
    {
       
        public void TestHashPassword()
        {
            string password = "123456";
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            System.Diagnostics.Debug.WriteLine($"Password: {password}");
            System.Diagnostics.Debug.WriteLine($"Password Hash: {passwordHash}");
        }
    }
}